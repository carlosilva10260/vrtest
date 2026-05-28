using System.Collections.Generic;
using UnityEngine;

public class GuardianTeleportManager_WithRedirection : GuardianTeleportManager
{
    [Header("References")]
    public Transform xrOrigin;
    public Transform head;
    public Transform simulatedGuardian;
    public BoxCollider guardianCollider;
    public List<Transform> targets = new List<Transform>();

    [Header("Teleport Detection")]
    public float teleportDetectionDistance = 0.75f;

    [Header("Target Selection")]
    public float targetSearchRadius = 8f;

    [Header("Safety")]
    public float insideMargin = 0.5f;
    public float preferredTargetApproachDistance = 2.5f;

    private Vector3 previousHeadPosXZ;
    private bool initialized;

    private void Start()
    {
        if (xrOrigin == null || head == null || simulatedGuardian == null || guardianCollider == null)
        {
            Debug.LogError("Assign xrOrigin, head, simulatedGuardian and guardianCollider.");
            enabled = false;
            return;
        }

        previousHeadPosXZ = Flat(head.position);
        initialized = true;
    }

    private void LateUpdate()
    {
        if (!initialized)
            return;

        Vector3 currentHeadPosXZ = Flat(head.position);
        float moveDistance = Vector3.Distance(currentHeadPosXZ, previousHeadPosXZ);

        if (moveDistance >= teleportDetectionDistance)
        {
            HandleTeleport(previousHeadPosXZ, currentHeadPosXZ);
        }

        previousHeadPosXZ = Flat(head.position);
    }

    private void HandleTeleport(Vector3 preTeleportHeadPos, Vector3 postTeleportHeadPos)
    {
        Vector3 oldGuardianCenter = Flat(simulatedGuardian.position);
        Vector3 oldOffset = preTeleportHeadPos - oldGuardianCenter;

        Vector3 finalUserPos = CalculateFinalUserPosition(postTeleportHeadPos, oldOffset);
        Vector3 finalGuardianCenter = finalUserPos - oldOffset;

        SetGuardianCenter(finalGuardianCenter);
        MoveHeadXZTo(finalUserPos);
    }

    public override Vector3 PredictFinalUserPosition(Vector3 selectedTeleportPos, Vector3 currentForward)
    {
        Vector3 oldGuardianCenter = Flat(simulatedGuardian.position);
        Vector3 oldUserPos = Flat(head.position);
        Vector3 oldOffset = oldUserPos - oldGuardianCenter;

        return CalculateFinalUserPosition(Flat(selectedTeleportPos), oldOffset);
    }

    public override Vector3 PredictFinalGuardianCenter(Vector3 selectedTeleportPos, Vector3 currentForward)
    {
        Vector3 oldGuardianCenter = Flat(simulatedGuardian.position);
        Vector3 oldUserPos = Flat(head.position);
        Vector3 oldOffset = oldUserPos - oldGuardianCenter;

        Vector3 finalUserPos = PredictFinalUserPosition(selectedTeleportPos, currentForward);
        return finalUserPos - oldOffset;
    }

    public override void RemoveTarget(Transform target)
    {
        targets.Remove(target);
    }

    private Vector3 CalculateFinalUserPosition(Vector3 baseUserPos, Vector3 oldOffset)
    {
        Vector3 baseGuardianCenter = baseUserPos - oldOffset;

        Transform target = FindRelevantTargetNearTeleportPoint(baseUserPos);

        if (target == null)
        {
            Debug.Log("No relevant target found. No redirection.");
            return baseUserPos;
        }

        Vector3 targetPos = Flat(target.position);

        if (IsInsideGuardianAtCenter(targetPos, baseGuardianCenter, insideMargin))
        {
            Debug.Log($"Target {target.name} is already inside guardian. No redirection.");
            return baseUserPos;
        }

        Debug.Log($"Target {target.name} outside guardian. Redirecting PAST target.");

        Vector3 teleportToTargetDir = targetPos - baseUserPos;

        if (teleportToTargetDir.sqrMagnitude < 0.0001f)
        {
            Debug.LogWarning("Teleport point too close to target. No redirection.");
            return baseUserPos;
        }

        teleportToTargetDir.Normalize();

        // IMPORTANT:
        // Previous version used targetPos - dir * distance.
        // This placed the user BETWEEN teleport point and target.
        // This version uses targetPos + dir * distance.
        // This places the user PAST the target, so the target is behind/in front depending on turn.
        Vector3 redirectedUserPos =
            targetPos + teleportToTargetDir * preferredTargetApproachDistance;

        Debug.Log($"Base user pos = {baseUserPos}");
        Debug.Log($"Target pos = {targetPos}");
        Debug.Log($"Teleport -> target dir = {teleportToTargetDir}");
        Debug.Log($"Redirected user pos = {redirectedUserPos}");
        Debug.Log($"Redirection distance = {Vector3.Distance(baseUserPos, redirectedUserPos):F2}");

        return redirectedUserPos;
    }

    private Transform FindRelevantTargetNearTeleportPoint(Vector3 teleportPoint)
    {
        Transform best = null;
        float bestDistance = float.MaxValue;

        foreach (Transform t in targets)
        {
            if (t == null)
                continue;

            Vector3 targetPos = Flat(t.position);
            float distance = Vector3.Distance(teleportPoint, targetPos);

            if (distance < 0.01f || distance > targetSearchRadius)
                continue;

            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = t;
            }
        }

        if (best != null)
            Debug.Log($"Relevant target found: {best.name}");

        return best;
    }

    private bool IsInsideGuardianAtCenter(Vector3 worldPoint, Vector3 guardianCenter, float margin)
    {
        Vector3 local = WorldToGuardianLocalAtCenter(worldPoint, guardianCenter);

        Vector3 halfSize = guardianCollider.size * 0.5f;

        float halfX = halfSize.x - margin;
        float halfZ = halfSize.z - margin;

        return Mathf.Abs(local.x) <= halfX &&
               Mathf.Abs(local.z) <= halfZ;
    }

    private Vector3 WorldToGuardianLocalAtCenter(Vector3 worldPoint, Vector3 guardianCenter)
    {
        Quaternion guardianRotation = simulatedGuardian.rotation;

        Vector3 offset = worldPoint - guardianCenter;
        Vector3 local = Quaternion.Inverse(guardianRotation) * offset;

        local -= guardianCollider.center;
        local.y = 0f;

        return local;
    }

    private void MoveHeadXZTo(Vector3 desiredHeadXZ)
    {
        Vector3 currentHeadXZ = Flat(head.position);
        Vector3 deltaXZ = desiredHeadXZ - currentHeadXZ;

        xrOrigin.position += new Vector3(deltaXZ.x, 0f, deltaXZ.z);
    }

    private void SetGuardianCenter(Vector3 flatCenter)
    {
        Vector3 current = simulatedGuardian.position;
        simulatedGuardian.position = new Vector3(flatCenter.x, current.y, flatCenter.z);
    }

    private Vector3 Flat(Vector3 v)
    {
        return new Vector3(v.x, 0f, v.z);
    }
}