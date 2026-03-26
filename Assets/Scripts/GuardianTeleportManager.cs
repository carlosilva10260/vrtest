using System.Collections.Generic;
using UnityEngine;

public class GuardianTeleportManager : MonoBehaviour
{
    [Header("References")]
    public Transform xrOrigin;                  // XR Origin root
    public Transform head;                      // Main Camera / HMD
    public Transform simulatedGuardian;         // Visual guardian center object
    public List<Transform> targets = new List<Transform>();

    [Header("Guardian Size (meters)")]
    public float guardianWidth = 15f;
    public float guardianDepth = 15f;

    [Header("Teleport Detection")]
    public float teleportDetectionDistance = 0.75f;

    [Header("Target Selection")]
    public float targetSearchRadius = 4f;
    public float targetMaxAngle = 70f;

    [Header("Safety")]
    public float insideMargin = 0.5f;
    public float preferredTargetApproachDistance = 1.25f;
    public int pathSamples = 8;

    [Header("Debug")]
    public bool drawDebug = true;

    private Vector3 previousHeadPosXZ;
    private Quaternion guardianRotation;
    private bool initialized;

    private enum ResolutionMode
    {
        Preserve_NoTarget,
        Preserve_TargetSafe,
        Adjust_ForTarget
    }

    private ResolutionMode lastMode = ResolutionMode.Preserve_NoTarget;
    private Transform lastChosenTarget;
    private Vector3 lastGuardianCenter;
    private Vector3 lastFinalUserPos;

    private void Start()
    {
        if (xrOrigin == null || head == null || simulatedGuardian == null)
        {
            Debug.LogError("GuardianTeleportManager: Assign xrOrigin, head, and simulatedGuardian.");
            enabled = false;
            return;
        }

        guardianRotation = simulatedGuardian.rotation;
        previousHeadPosXZ = Flat(head.position);
        lastGuardianCenter = Flat(simulatedGuardian.position);
        lastFinalUserPos = previousHeadPosXZ;
        initialized = true;
    }

    private void LateUpdate()
    {
        if (!initialized)
            return;

        Vector3 currentHeadPosXZ = Flat(head.position);
        float moveDistance = Vector3.Distance(currentHeadPosXZ, previousHeadPosXZ);

        // A large sudden displacement is treated as a teleport.
        if (moveDistance >= teleportDetectionDistance)
        {
            HandleTeleport(previousHeadPosXZ, currentHeadPosXZ, Flat(head.forward));
        }

        previousHeadPosXZ = Flat(head.position);
    }

    private void HandleTeleport(Vector3 preTeleportHeadPos, Vector3 selectedTeleportPos, Vector3 currentForward)
    {
        lastChosenTarget = null;

        // Old relative offset of the user inside the OLD guardian.
        Vector3 oldGuardianCenter = Flat(simulatedGuardian.position);
        Vector3 oldOffset = preTeleportHeadPos - oldGuardianCenter;

        // CASE FOUNDATION:
        // First build the NEW guardian centered at the selected teleport point (purple).
        Vector3 newGuardianCenter = selectedTeleportPos;

        // Default preserved user position inside the NEW guardian.
        Vector3 preservedUserPos = ClampPointInsideGuardian(newGuardianCenter + oldOffset, newGuardianCenter);

        // Find targets that are inside the guardian built around the selected teleport point.
        Transform relevantTarget = FindRelevantTargetInsideGuardian(newGuardianCenter, selectedTeleportPos, currentForward);
        lastChosenTarget = relevantTarget;

        // CASE 1 + CASE 2:
        // No target in the newly built guardian -> preserve old offset.
        if (relevantTarget == null)
        {
            ApplyResult(newGuardianCenter, preservedUserPos, ResolutionMode.Preserve_NoTarget);
            return;
        }

        Vector3 targetPos = Flat(relevantTarget.position);

        // CASE 3:
        // A target exists, but the preserved user position is still safe for reaching it.
        if (SupportsTargetSafely(newGuardianCenter, preservedUserPos, targetPos))
        {
            ApplyResult(newGuardianCenter, preservedUserPos, ResolutionMode.Preserve_TargetSafe);
            return;
        }

        // CASE 4:
        // A target exists, and preserved placement is not good enough.
        // Reposition the user inside the guardian to support movement toward the target.
        Vector3 adjustedUserPos = ComputeTargetSupportPosition(newGuardianCenter, targetPos);

        // Fallback safety check.
        if (!SupportsTargetSafely(newGuardianCenter, adjustedUserPos, targetPos))
        {
            adjustedUserPos = preservedUserPos;
            ApplyResult(newGuardianCenter, adjustedUserPos, ResolutionMode.Preserve_TargetSafe);
            return;
        }

        ApplyResult(newGuardianCenter, adjustedUserPos, ResolutionMode.Adjust_ForTarget);
    }

    private void ApplyResult(Vector3 guardianCenter, Vector3 finalUserPos, ResolutionMode mode)
    {
        SetGuardianCenter(guardianCenter);
        MoveHeadXZTo(finalUserPos);

        lastGuardianCenter = guardianCenter;
        lastFinalUserPos = finalUserPos;
        lastMode = mode;
    }

    private Transform FindRelevantTargetInsideGuardian(Vector3 guardianCenter, Vector3 teleportAnchorPos, Vector3 userForward)
    {
        Transform best = null;
        float bestScore = float.MaxValue;

        foreach (Transform t in targets)
        {
            if (t == null)
                continue;

            Vector3 targetPos = Flat(t.position);

            // Must be inside the newly built guardian.
            if (!IsInsideGuardian(targetPos, guardianCenter, insideMargin))
                continue;

            Vector3 toTarget = targetPos - teleportAnchorPos;
            float distance = toTarget.magnitude;
            if (distance < 0.01f)
                continue;

            if (distance > targetSearchRadius)
                continue;

            float angle = Vector3.Angle(userForward, toTarget.normalized);
            if (angle > targetMaxAngle)
                continue;

            // Prefer close and roughly forward targets.
            float score = distance + angle * 0.02f;
            if (score < bestScore)
            {
                bestScore = score;
                best = t;
            }
        }

        return best;
    }

    private bool SupportsTargetSafely(Vector3 guardianCenter, Vector3 userPos, Vector3 targetPos)
    {
        // Target must be comfortably inside guardian.
        if (!IsInsideGuardian(targetPos, guardianCenter, insideMargin))
            return false;

        // User must also be comfortably inside guardian.
        if (!IsInsideGuardian(userPos, guardianCenter, insideMargin))
            return false;

        // Straight path from user to target must stay inside with margin.
        if (!IsPathInsideGuardian(userPos, targetPos, guardianCenter, insideMargin))
            return false;

        return true;
    }

    private Vector3 ComputeTargetSupportPosition(Vector3 guardianCenter, Vector3 targetPos)
    {
        // Place the user "behind" the target relative to the target direction,
        // so the user can walk forward toward it with space.
        Vector3 dirFromCenterToTarget = targetPos - guardianCenter;
        Vector3 dir;

        if (dirFromCenterToTarget.sqrMagnitude < 0.0001f)
        {
            // If target is too close to center, use a fallback direction.
            dir = Vector3.forward;
        }
        else
        {
            dir = dirFromCenterToTarget.normalized;
        }

        Vector3 candidate = targetPos - dir * preferredTargetApproachDistance;
        candidate = ClampPointInsideGuardian(candidate, guardianCenter);

        return candidate;
    }

    private bool IsInsideGuardian(Vector3 worldPoint, Vector3 guardianCenter, float margin)
    {
        Vector3 local = WorldToGuardianLocal(worldPoint, guardianCenter);

        float halfW = guardianWidth * 0.5f - margin;
        float halfD = guardianDepth * 0.5f - margin;

        return Mathf.Abs(local.x) <= halfW && Mathf.Abs(local.z) <= halfD;
    }

    private bool IsPathInsideGuardian(Vector3 start, Vector3 end, Vector3 guardianCenter, float margin)
    {
        for (int i = 0; i <= pathSamples; i++)
        {
            float t = i / (float)pathSamples;
            Vector3 p = Vector3.Lerp(start, end, t);

            if (!IsInsideGuardian(p, guardianCenter, margin))
                return false;
        }

        return true;
    }

    private Vector3 ClampPointInsideGuardian(Vector3 point, Vector3 guardianCenter)
    {
        Vector3 local = WorldToGuardianLocal(point, guardianCenter);

        float halfW = guardianWidth * 0.5f - insideMargin;
        float halfD = guardianDepth * 0.5f - insideMargin;

        local.x = Mathf.Clamp(local.x, -halfW, halfW);
        local.z = Mathf.Clamp(local.z, -halfD, halfD);
        local.y = 0f;

        Vector3 clampedWorld = guardianCenter + (guardianRotation * local);
        clampedWorld.y = 0f;
        return clampedWorld;
    }

    private Vector3 WorldToGuardianLocal(Vector3 worldPoint, Vector3 guardianCenter)
    {
        Vector3 offset = worldPoint - guardianCenter;
        Vector3 local = Quaternion.Inverse(guardianRotation) * offset;
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

    private void OnDrawGizmos()
    {
        if (!drawDebug || simulatedGuardian == null)
            return;

        Vector3 center = Application.isPlaying ? lastGuardianCenter : Flat(simulatedGuardian.position);

        Color guardianColor = Color.cyan;
        if (Application.isPlaying)
        {
            switch (lastMode)
            {
                case ResolutionMode.Preserve_NoTarget:
                    guardianColor = Color.cyan;
                    break;
                case ResolutionMode.Preserve_TargetSafe:
                    guardianColor = Color.green;
                    break;
                case ResolutionMode.Adjust_ForTarget:
                    guardianColor = Color.yellow;
                    break;
            }
        }

        Gizmos.color = guardianColor;

        Matrix4x4 oldMatrix = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(center, guardianRotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(guardianWidth, 0.01f, guardianDepth));
        Gizmos.matrix = oldMatrix;

        if (Application.isPlaying)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(lastFinalUserPos, 0.15f);

            if (lastChosenTarget != null)
            {
                Vector3 targetPos = Flat(lastChosenTarget.position);
                Gizmos.color = Color.white;
                Gizmos.DrawLine(lastFinalUserPos, targetPos);
                Gizmos.DrawSphere(targetPos, 0.12f);
            }
        }
    }
}