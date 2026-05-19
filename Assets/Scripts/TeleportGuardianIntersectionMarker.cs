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
            boundaryPoint = ProjectToGuardianBoundaryXZ(bestPoint);
            boundaryPoint.y = bestPoint.y;
            return true;
        }

        return false;
    }

    private float DistanceToGuardianBoundaryXZ(Vector3 worldPoint)
    {
        Vector3 local = simulatedGuardian.InverseTransformPoint(worldPoint);

        float halfW = guardianWidth * 0.5f;
        float halfD = guardianDepth * 0.5f;

        float dx = Mathf.Abs(Mathf.Abs(local.x) - halfW);
        float dz = Mathf.Abs(Mathf.Abs(local.z) - halfD);

        bool insideX = Mathf.Abs(local.x) <= halfW;
        bool insideZ = Mathf.Abs(local.z) <= halfD;

        if (insideX && insideZ)
            return Mathf.Min(halfW - Mathf.Abs(local.x), halfD - Mathf.Abs(local.z));

        if (insideX)
            return dz;

        if (insideZ)
            return dx;

        return Mathf.Sqrt(dx * dx + dz * dz);
    }

    private Vector3 ProjectToGuardianBoundaryXZ(Vector3 worldPoint)
    {
        Vector3 local = simulatedGuardian.InverseTransformPoint(worldPoint);

        float halfW = guardianWidth * 0.5f;
        float halfD = guardianDepth * 0.5f;

        float distToXEdge = Mathf.Abs(Mathf.Abs(local.x) - halfW);
        float distToZEdge = Mathf.Abs(Mathf.Abs(local.z) - halfD);

        if (distToXEdge < distToZEdge)
            local.x = Mathf.Sign(local.x) * halfW;
        else
            local.z = Mathf.Sign(local.z) * halfD;

        return simulatedGuardian.TransformPoint(local);
    }

    private void HideMarker()
    {
        if (marker != null)
            marker.gameObject.SetActive(false);
    }
}