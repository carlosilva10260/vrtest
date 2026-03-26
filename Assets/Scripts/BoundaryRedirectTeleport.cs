using UnityEngine;
using Unity.XR.CoreUtils;

public class BoundaryRedirectTeleport : MonoBehaviour
{
    [Header("References")]
    public XROrigin xrOrigin;
    public Transform simulatedGuardian;
    public BoxCollider guardianCollider;
    public Transform target;                 // PullySmooth
    public Transform redirectHeadPoint;      // Where the HMD should land after redirect
    public GameObject cylinderObject;        // Your teleport cylinder

    [Header("Settings")]
    public float boundaryWarningDistance = 0.75f;
    public bool hideCylinderWhenSafe = true;

    private Transform head;
    private Vector3 cachedHeadLocalInGuardian;
    private Vector3 cachedNearestSideLocal;
    private bool isNearBoundary;

    private void Awake()
    {
        if (xrOrigin != null)
            head = xrOrigin.Camera.transform;

        if (cylinderObject != null)
            cylinderObject.SetActive(false);
    }

    private void Update()
    {
        if (xrOrigin == null || head == null || simulatedGuardian == null || guardianCollider == null)
            return;

        UpdateBoundaryState();
    }

    private void UpdateBoundaryState()
    {
        Vector3 headLocal = simulatedGuardian.InverseTransformPoint(head.position);

        Vector3 half = guardianCollider.size * 0.5f;
        Vector3 localCenter = guardianCollider.center;

        float leftDist = Mathf.Abs((headLocal.x - localCenter.x) - (-half.x));
        float rightDist = Mathf.Abs((headLocal.x - localCenter.x) - (half.x));
        float backDist = Mathf.Abs((headLocal.z - localCenter.z) - (-half.z));
        float frontDist = Mathf.Abs((headLocal.z - localCenter.z) - (half.z));

        float minDist = leftDist;
        Vector3 nearestSideLocal = Vector3.left;

        if (rightDist < minDist)
        {
            minDist = rightDist;
            nearestSideLocal = Vector3.right;
        }

        if (backDist < minDist)
        {
            minDist = backDist;
            nearestSideLocal = Vector3.back;
        }

        if (frontDist < minDist)
        {
            minDist = frontDist;
            nearestSideLocal = Vector3.forward;
        }

        isNearBoundary = minDist <= boundaryWarningDistance;

        if (isNearBoundary)
        {
            cachedHeadLocalInGuardian = headLocal;
            cachedNearestSideLocal = nearestSideLocal;

            if (cylinderObject != null && !cylinderObject.activeSelf)
                cylinderObject.SetActive(true);
        }
        else if (hideCylinderWhenSafe)
        {
            if (cylinderObject != null && cylinderObject.activeSelf)
                cylinderObject.SetActive(false);
        }
    }

    public void RedirectTeleport()
    {
        if (!isNearBoundary || xrOrigin == null || head == null || target == null || redirectHeadPoint == null)
            return;

        // 1) Rotate the XR Origin so the user faces the target after teleport.
        Vector3 desiredForward = target.position - redirectHeadPoint.position;
        desiredForward.y = 0f;

        if (desiredForward.sqrMagnitude < 0.0001f)
            desiredForward = target.forward;

        desiredForward.Normalize();

        Vector3 currentForward = head.forward;
        currentForward.y = 0f;

        if (currentForward.sqrMagnitude < 0.0001f)
            currentForward = xrOrigin.transform.forward;

        currentForward.Normalize();

        float yawDelta = Vector3.SignedAngle(currentForward, desiredForward, Vector3.up);

        // Rotate around the current head position so the user's viewpoint stays stable.
        xrOrigin.transform.RotateAround(head.position, Vector3.up, yawDelta);

        // 2) Move the XR Origin so the HMD lands at the redirect point.
        Vector3 moveOffset = redirectHeadPoint.position - head.position;
        xrOrigin.transform.position += moveOffset;

        // 3) Move and rotate the guardian so:
        //    - the same local offset inside the guardian is preserved
        //    - the side the user was near is now behind them
        AlignGuardianAfterTeleport(desiredForward);

        // 4) Hide the cylinder after teleport
        if (cylinderObject != null)
            cylinderObject.SetActive(false);

        isNearBoundary = false;
    }

    private void AlignGuardianAfterTeleport(Vector3 userForward)
    {
        // We want the previously nearest side to end up behind the user.
        Vector3 desiredSideWorld = -userForward;
        desiredSideWorld.y = 0f;
        desiredSideWorld.Normalize();

        float localSideAngle = Mathf.Atan2(cachedNearestSideLocal.x, cachedNearestSideLocal.z) * Mathf.Rad2Deg;
        float desiredWorldAngle = Mathf.Atan2(desiredSideWorld.x, desiredSideWorld.z) * Mathf.Rad2Deg;

        float guardianYaw = desiredWorldAngle - localSideAngle;
        simulatedGuardian.rotation = Quaternion.Euler(0f, guardianYaw, 0f);

        // Preserve the user's local position inside the guardian
        Vector3 worldOffset = simulatedGuardian.rotation * cachedHeadLocalInGuardian;
        simulatedGuardian.position = head.position - worldOffset;
    }

    public bool IsNearBoundary()
    {
        return isNearBoundary;
    }
}