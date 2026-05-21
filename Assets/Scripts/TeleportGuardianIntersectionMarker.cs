using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class TeleportGuardianIntersectionMarker : MonoBehaviour
{
    [Header("References")]
    public XRRayInteractor teleportInteractor;
    public Transform simulatedGuardian;
    public Transform marker;

    [Header("Guardian Size")]
    public float guardianWidth = 3f;
    public float guardianDepth = 3f;

    [Header("Detection")]
    public float boundaryThreshold = 0.15f;
    public float markerHeightOffset = 0.03f;
    public float markerBackOffset = 0.25f;

    private Vector3[] linePoints = new Vector3[128];

    private void Update()
    {
        if (teleportInteractor == null || simulatedGuardian == null || marker == null)
        {
            HideMarker();
            return;
        }

        if (TryFindBoundaryPoint(out Vector3 boundaryPoint))
        {
            Vector3 controllerPos = teleportInteractor.transform.position;
            Vector3 directionToController = controllerPos - boundaryPoint;
            directionToController.y = 0f;

            if (directionToController.sqrMagnitude > 0.0001f)
                directionToController.Normalize();
            else
                directionToController = -teleportInteractor.transform.forward;

            marker.gameObject.SetActive(true);

            marker.position =
                boundaryPoint +
                directionToController * markerBackOffset +
                Vector3.up * markerHeightOffset;

            // Rotate patch according to the guardian wall the controller/ray is facing
            marker.rotation = GetWallRotationFromLookDirection();
        }
        else
        {
            HideMarker();
        }
    }

    private bool TryFindBoundaryPoint(out Vector3 boundaryPoint)
    {
        boundaryPoint = Vector3.zero;

        if (!teleportInteractor.GetLinePoints(ref linePoints, out int count))
            return false;

        if (count < 2)
            return false;

        float bestDistance = float.MaxValue;
        Vector3 bestPoint = Vector3.zero;

        for (int i = 0; i < count; i++)
        {
            Vector3 p = linePoints[i];
            float distance = DistanceToGuardianBoundaryXZ(p);

            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestPoint = p;
            }
        }

        if (bestDistance <= boundaryThreshold)
        {
            boundaryPoint = ProjectToFacingGuardianWall(bestPoint);
            boundaryPoint.y = bestPoint.y;
            return true;
        }

        return false;
    }

    private Vector3 ProjectToFacingGuardianWall(Vector3 worldPoint)
    {
        Vector3 local = simulatedGuardian.InverseTransformPoint(worldPoint);

        float halfW = guardianWidth * 0.5f;
        float halfD = guardianDepth * 0.5f;

        Vector3 localForward = simulatedGuardian.InverseTransformDirection(
            teleportInteractor.transform.forward
        );

        localForward.y = 0f;

        if (localForward.sqrMagnitude > 0.0001f)
            localForward.Normalize();
        else
            localForward = Vector3.forward;

        if (Mathf.Abs(localForward.x) > Mathf.Abs(localForward.z))
        {
            // Facing left/right wall
            local.x = Mathf.Sign(localForward.x) * halfW;
            local.z = Mathf.Clamp(local.z, -halfD, halfD);
        }
        else
        {
            // Facing front/back wall
            local.z = Mathf.Sign(localForward.z) * halfD;
            local.x = Mathf.Clamp(local.x, -halfW, halfW);
        }

        return simulatedGuardian.TransformPoint(local);
    }

    private float DistanceToGuardianBoundaryXZ(Vector3 worldPoint)
    {
        Vector3 local = simulatedGuardian.InverseTransformPoint(worldPoint);

        float halfW = guardianWidth * 0.5f;
        float halfD = guardianDepth * 0.5f;

        Vector3 localForward = simulatedGuardian.InverseTransformDirection(
            teleportInteractor.transform.forward
        );

        localForward.y = 0f;

        if (localForward.sqrMagnitude > 0.0001f)
            localForward.Normalize();
        else
            localForward = Vector3.forward;

        if (Mathf.Abs(localForward.x) > Mathf.Abs(localForward.z))
        {
            // Distance to the wall the ray is facing on X axis
            float targetX = Mathf.Sign(localForward.x) * halfW;
            return Mathf.Abs(local.x - targetX);
        }
        else
        {
            // Distance to the wall the ray is facing on Z axis
            float targetZ = Mathf.Sign(localForward.z) * halfD;
            return Mathf.Abs(local.z - targetZ);
        }
    }

    private Quaternion GetWallRotationFromLookDirection()
    {
        Vector3 localForward = simulatedGuardian.InverseTransformDirection(
            teleportInteractor.transform.forward
        );

        localForward.y = 0f;

        if (localForward.sqrMagnitude > 0.0001f)
            localForward.Normalize();
        else
            localForward = Vector3.forward;

        Vector3 localNormal;

        if (Mathf.Abs(localForward.x) > Mathf.Abs(localForward.z))
        {
            localNormal = new Vector3(Mathf.Sign(localForward.x), 0f, 0f);
        }
        else
        {
            localNormal = new Vector3(0f, 0f, Mathf.Sign(localForward.z));
        }

        Vector3 worldNormal = simulatedGuardian.TransformDirection(localNormal);

        return Quaternion.LookRotation(worldNormal, Vector3.up);
    }

    private void HideMarker()
    {
        if (marker != null)
            marker.gameObject.SetActive(false);
    }
}