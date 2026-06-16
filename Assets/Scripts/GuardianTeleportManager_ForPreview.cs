using UnityEngine;

public class GuardianTeleportManager_ForPreview : GuardianTeleportManager
{
    [Header("References")]
    public Transform xrOrigin;
    public Transform head;
    public Transform simulatedGuardian;

    [Header("Guardian Size")]
    public float guardianWidth = 15f;
    public float guardianDepth = 15f;

    [Header("Teleport Detection")]
    public float teleportDetectionDistance = 0.75f;

    [Header("Safety")]
    public float insideMargin = 0.5f;

    private Vector3 previousHeadPosXZ;
    private Quaternion guardianRotation;
    private bool initialized;

    private void Start()
    {
        if (xrOrigin == null || head == null || simulatedGuardian == null)
        {
            Debug.LogError("Assign xrOrigin, head, and simulatedGuardian.");
            enabled = false;
            return;
        }

        guardianRotation = simulatedGuardian.rotation;
        previousHeadPosXZ = Flat(head.position);
        initialized = true;
    }

    private void LateUpdate()
    {
        if (!initialized) return;

        Vector3 currentHeadPosXZ = Flat(head.position);
        float moveDistance = Vector3.Distance(currentHeadPosXZ, previousHeadPosXZ);

        if (moveDistance >= teleportDetectionDistance)
        {
            HandleTeleport(previousHeadPosXZ, currentHeadPosXZ);
        }

        previousHeadPosXZ = Flat(head.position);
    }

    private void HandleTeleport(Vector3 preTeleportHeadPos, Vector3 selectedTeleportPos)
    {
        Vector3 oldGuardianCenter = Flat(simulatedGuardian.position);
        Vector3 oldOffset = preTeleportHeadPos - oldGuardianCenter;

        // Guardian is built at the selected teleport point
        Vector3 newGuardianCenter = Flat(selectedTeleportPos);

        // User is placed inside that new guardian preserving their previous offset
        Vector3 finalUserPos =
            ClampPointInsideGuardian(newGuardianCenter + oldOffset, newGuardianCenter);

        SetGuardianCenter(newGuardianCenter);
        MoveHeadXZTo(finalUserPos);

        Debug.Log("Preview scene teleport: guardian centered on teleport point, user offset preserved.");
    }

    public override Vector3 PredictFinalUserPosition(Vector3 selectedTeleportPos, Vector3 currentForward)
    {
        Vector3 oldGuardianCenter = Flat(simulatedGuardian.position);

        // For preview, use the real current head position so physical movement
        // changes where the mannequin appears inside the fixed preview guardian.
        Vector3 oldUserPos = Flat(head.position);
        Vector3 oldOffset = oldUserPos - oldGuardianCenter;

        Vector3 newGuardianCenter = Flat(selectedTeleportPos);

        return ClampPointInsideGuardian(
            newGuardianCenter + oldOffset,
            newGuardianCenter
        );
    }

    public override Vector3 PredictFinalGuardianCenter(Vector3 selectedTeleportPos, Vector3 currentForward)
    {
        // IMPORTANT:
        // The preview guardian must stay exactly where the ray is pointing.
        // It should not move when the user moves their head/body.
        return Flat(selectedTeleportPos);
    }

    public override void RemoveTarget(Transform target)
    {
        // Preview scene has no redirection target logic.
    }

    private Vector3 ClampPointInsideGuardian(Vector3 point, Vector3 guardianCenter)
    {
        Vector3 local = WorldToGuardianLocal(point, guardianCenter);

        float halfW = guardianWidth * 0.5f - insideMargin;
        float halfD = guardianDepth * 0.5f - insideMargin;

        local.x = Mathf.Clamp(local.x, -halfW, halfW);
        local.z = Mathf.Clamp(local.z, -halfD, halfD);
        local.y = 0f;

        Vector3 world = guardianCenter + guardianRotation * local;
        world.y = 0f;
        return world;
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
        simulatedGuardian.position =
            new Vector3(flatCenter.x, current.y, flatCenter.z);
    }

    private Vector3 Flat(Vector3 v)
    {
        return new Vector3(v.x, 0f, v.z);
    }
}