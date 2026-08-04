using UnityEngine;

public class GuardianTeleportManager_Baseline : GuardianTeleportManager
{
    [Header("References")]
    public Transform xrOrigin;
    public Transform head;
    public Transform simulatedGuardian;

    [Header("Teleport Detection")]
    public float teleportDetectionDistance = 0.75f;

    private Vector3 previousHeadPosXZ;
    private bool initialized;

    private void Start()
    {
        if (xrOrigin == null || head == null || simulatedGuardian == null)
        {
            Debug.LogError("GuardianTeleportManager_Baseline: Assign xrOrigin, head, and simulatedGuardian.");
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
        if (ExperimentLogger.Instance != null)
            ExperimentLogger.Instance.LogTeleport("N/A");

        Vector3 oldGuardianCenter = Flat(simulatedGuardian.position);

        // User's old relative position inside the previous guardian
        Vector3 oldOffset = preTeleportHeadPos - oldGuardianCenter;

        // Baseline logic:
        // user stays exactly where the teleport placed them
        Vector3 finalUserPos = postTeleportHeadPos;

        // guardian shifts so the user keeps the same relative position inside it
        Vector3 finalGuardianCenter = finalUserPos - oldOffset;

        SetGuardianCenter(finalGuardianCenter);

        Debug.Log("Baseline teleport: user stayed at teleport point, guardian shifted to preserve relative position.");
    }

    public override Vector3 PredictFinalUserPosition(Vector3 selectedTeleportPos, Vector3 currentForward)
    {
        return Flat(selectedTeleportPos);
    }

    public override Vector3 PredictFinalGuardianCenter(Vector3 selectedTeleportPos, Vector3 currentForward)
    {
        Vector3 oldGuardianCenter = Flat(simulatedGuardian.position);
        Vector3 oldUserPos = Flat(head.position);
        Vector3 oldOffset = oldUserPos - oldGuardianCenter;

        Vector3 finalUserPos = Flat(selectedTeleportPos);
        return finalUserPos - oldOffset;
    }

    public override void RemoveTarget(Transform target)
    {
        // Baseline has no redirection target logic.
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