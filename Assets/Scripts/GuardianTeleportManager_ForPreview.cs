using UnityEngine;

public class GuardianTeleportManager_ForPreview : GuardianTeleportManager
{
    [Header("References")]
    public Transform xrOrigin;
    public Transform head;
    public Transform simulatedGuardian;
    public BoxCollider guardianCollider;

    [Header("Guardian Size")]
    public float guardianWidth = 3f;
    public float guardianDepth = 3f;

    [Header("Teleport Detection")]
    public float teleportDetectionDistance = 0.75f;

    [Header("Guardian Safety")]
    public float insideMargin = 0.5f;

    [Header("Landing Position Validation")]
    public bool validateLandingPosition = true;

    [Tooltip("Collider defining the valid navigation area and floor.")]
    public Collider navigationArea;

    [Tooltip("Approximate horizontal radius occupied by the participant.")]
    public float playerRadius = 0.25f;

    [Tooltip("Approximate participant height.")]
    public float playerHeight = 1.7f;

    [Tooltip("Small offset above the floor for the collision capsule.")]
    public float capsuleFloorOffset = 0.02f;

    [Tooltip(
        "Layers containing obstacles that should invalidate the mannequin " +
        "landing position."
    )]
    public LayerMask obstacleLayers = ~0;

    [Tooltip(
        "Enable this if trigger colliders should also block the teleport."
    )]
    public bool includeTriggerColliders = true;

    [Header("Preview Objects To Ignore")]
    public Transform previewGuardianRoot;
    public Transform landingPreviewRoot;

    [Header("Debug")]
    public bool debugValidation = false;

    private Vector3 previousHeadPosXZ;
    private Quaternion guardianRotation;
    private bool initialized;

    private void Start()
    {
        if (xrOrigin == null ||
            head == null ||
            simulatedGuardian == null ||
            guardianCollider == null)
        {
            Debug.LogError(
                "GuardianTeleportManager_ForPreview: assign xrOrigin, head, " +
                "simulatedGuardian and guardianCollider."
            );

            enabled = false;
            return;
        }

        guardianRotation = simulatedGuardian.rotation;
        previousHeadPosXZ = Flat(head.position);
        initialized = true;
    }

    private void LateUpdate()
    {
        if (!initialized)
            return;

        Vector3 currentHeadPosXZ = Flat(head.position);

        float moveDistance = Vector3.Distance(
            currentHeadPosXZ,
            previousHeadPosXZ
        );

        if (moveDistance >= teleportDetectionDistance)
        {
            HandleTeleport(
                previousHeadPosXZ,
                currentHeadPosXZ
            );
        }

        previousHeadPosXZ = Flat(head.position);
    }

    private void HandleTeleport(
        Vector3 preTeleportHeadPos,
        Vector3 selectedTeleportPos)
    {
        Vector3 oldGuardianCenter =
            Flat(simulatedGuardian.position);

        Vector3 oldOffset =
            preTeleportHeadPos - oldGuardianCenter;

        Vector3 newGuardianCenter =
            Flat(selectedTeleportPos);

        Vector3 finalUserPosition =
            ClampPointInsideGuardian(
                newGuardianCenter + oldOffset,
                newGuardianCenter
            );

        /*
         * This is only a final fallback.
         *
         * GuardianTeleportPreview should prevent an invalid teleport
         * request from being generated in the first place.
         */
        if (validateLandingPosition &&
            !IsValidLandingPosition(finalUserPosition))
        {
            Debug.LogWarning(
                "An invalid preview teleport reached the manager. " +
                "The request should normally have been blocked before this."
            );

            return;
        }

        if (ExperimentLogger.Instance != null)
            ExperimentLogger.Instance.LogTeleport("N/A");

        SetGuardianCenter(newGuardianCenter);
        MoveHeadXZTo(finalUserPosition);

        Debug.Log(
            "Preview teleport completed. Guardian moved to the selected " +
            "position and the participant offset was preserved."
        );
    }

    public override Vector3 PredictFinalUserPosition(
        Vector3 selectedTeleportPos,
        Vector3 currentForward)
    {
        Vector3 oldGuardianCenter =
            Flat(simulatedGuardian.position);

        Vector3 oldUserPosition =
            Flat(head.position);

        Vector3 oldOffset =
            oldUserPosition - oldGuardianCenter;

        Vector3 newGuardianCenter =
            Flat(selectedTeleportPos);

        return ClampPointInsideGuardian(
            newGuardianCenter + oldOffset,
            newGuardianCenter
        );
    }

    public override Vector3 PredictFinalGuardianCenter(
        Vector3 selectedTeleportPos,
        Vector3 currentForward)
    {
        return Flat(selectedTeleportPos);
    }

    public bool IsPredictedTeleportValid(
        Vector3 selectedTeleportPos,
        Vector3 currentForward)
    {
        Vector3 predictedUserPosition =
            PredictFinalUserPosition(
                selectedTeleportPos,
                currentForward
            );

        return IsValidLandingPosition(predictedUserPosition);
    }

    public bool IsValidLandingPosition(Vector3 candidatePosition)
    {
        if (!validateLandingPosition)
            return true;

        candidatePosition = Flat(candidatePosition);

        /*
         * First verify that the participant's complete horizontal radius
         * remains inside the navigation area.
         */
        if (navigationArea != null &&
            !IsInsideNavigationAreaXZ(candidatePosition))
        {
            if (debugValidation)
            {
                Debug.Log(
                    $"Invalid landing: {candidatePosition} is outside the " +
                    "navigation area."
                );
            }

            return false;
        }

        float floorY = GetNavigationFloorY();

        float safeRadius =
            Mathf.Max(0.01f, playerRadius);

        float safeHeight =
            Mathf.Max(playerHeight, safeRadius * 2f);

        Vector3 bottom = new Vector3(
            candidatePosition.x,
            floorY + safeRadius + capsuleFloorOffset,
            candidatePosition.z
        );

        Vector3 top = new Vector3(
            candidatePosition.x,
            floorY + safeHeight - safeRadius,
            candidatePosition.z
        );

        QueryTriggerInteraction triggerMode =
            includeTriggerColliders
                ? QueryTriggerInteraction.Collide
                : QueryTriggerInteraction.Ignore;

        Collider[] hits = Physics.OverlapCapsule(
            bottom,
            top,
            safeRadius,
            obstacleLayers,
            triggerMode
        );

        foreach (Collider hit in hits)
        {
            if (hit == null)
                continue;

            if (ShouldIgnoreColliderForLandingValidation(hit))
                continue;

            if (debugValidation)
            {
                Debug.Log(
                    "Invalid landing: predicted participant capsule " +
                    $"overlaps {GetFullPath(hit.transform)}."
                );
            }

            return false;
        }

        return true;
    }

    public override void RemoveTarget(Transform target)
    {
        // Preview mode does not maintain an AutoRedirect target list.
    }

    private bool IsInsideNavigationAreaXZ(Vector3 candidatePosition)
    {
        if (navigationArea == null)
            return true;

        Bounds bounds = navigationArea.bounds;

        float safeRadius =
            Mathf.Max(0f, playerRadius);

        float minX =
            bounds.min.x + safeRadius;

        float maxX =
            bounds.max.x - safeRadius;

        float minZ =
            bounds.min.z + safeRadius;

        float maxZ =
            bounds.max.z - safeRadius;

        /*
         * Handles navigation areas that are smaller than the configured
         * participant diameter.
         */
        if (minX > maxX || minZ > maxZ)
            return false;

        return candidatePosition.x >= minX &&
               candidatePosition.x <= maxX &&
               candidatePosition.z >= minZ &&
               candidatePosition.z <= maxZ;
    }

    private float GetNavigationFloorY()
    {
        if (navigationArea != null)
            return navigationArea.bounds.max.y;

        return simulatedGuardian.position.y;
    }

    private bool ShouldIgnoreColliderForLandingValidation(
        Collider colliderToCheck)
    {
        if (colliderToCheck == null)
            return true;

        if (colliderToCheck == navigationArea)
            return true;

        if (colliderToCheck == guardianCollider)
            return true;

        Transform colliderTransform =
            colliderToCheck.transform;

        if (IsTransformPartOf(
                colliderTransform,
                xrOrigin))
        {
            return true;
        }

        if (IsTransformPartOf(
                colliderTransform,
                simulatedGuardian))
        {
            return true;
        }

        if (IsTransformPartOf(
                colliderTransform,
                previewGuardianRoot))
        {
            return true;
        }

        if (IsTransformPartOf(
                colliderTransform,
                landingPreviewRoot))
        {
            return true;
        }

        return false;
    }

    private bool IsTransformPartOf(
        Transform candidate,
        Transform root)
    {
        if (candidate == null || root == null)
            return false;

        return candidate == root ||
               candidate.IsChildOf(root);
    }

    private Vector3 ClampPointInsideGuardian(
        Vector3 point,
        Vector3 guardianCenter)
    {
        Vector3 localPosition =
            WorldToGuardianLocal(
                point,
                guardianCenter
            );

        float halfWidth =
            guardianWidth * 0.5f - insideMargin;

        float halfDepth =
            guardianDepth * 0.5f - insideMargin;

        halfWidth =
            Mathf.Max(0f, halfWidth);

        halfDepth =
            Mathf.Max(0f, halfDepth);

        localPosition.x = Mathf.Clamp(
            localPosition.x,
            -halfWidth,
            halfWidth
        );

        localPosition.z = Mathf.Clamp(
            localPosition.z,
            -halfDepth,
            halfDepth
        );

        localPosition.y = 0f;

        Vector3 worldPosition =
            guardianCenter +
            guardianRotation * localPosition;

        worldPosition.y = 0f;

        return worldPosition;
    }

    private Vector3 WorldToGuardianLocal(
        Vector3 worldPoint,
        Vector3 guardianCenter)
    {
        Vector3 offset =
            worldPoint - guardianCenter;

        Vector3 localPosition =
            Quaternion.Inverse(guardianRotation) *
            offset;

        localPosition.y = 0f;

        return localPosition;
    }

    private void MoveHeadXZTo(Vector3 desiredHeadPosition)
    {
        Vector3 currentHeadPosition =
            Flat(head.position);

        Vector3 movement =
            desiredHeadPosition - currentHeadPosition;

        xrOrigin.position += new Vector3(
            movement.x,
            0f,
            movement.z
        );
    }

    private void SetGuardianCenter(Vector3 flatCenter)
    {
        Vector3 currentPosition =
            simulatedGuardian.position;

        simulatedGuardian.position = new Vector3(
            flatCenter.x,
            currentPosition.y,
            flatCenter.z
        );
    }

    private string GetFullPath(Transform target)
    {
        if (target == null)
            return "Unknown collider";

        string path = target.name;

        while (target.parent != null)
        {
            target = target.parent;
            path = target.name + "/" + path;
        }

        return path;
    }

    private Vector3 Flat(Vector3 value)
    {
        return new Vector3(
            value.x,
            0f,
            value.z
        );
    }
}