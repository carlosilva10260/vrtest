using UnityEngine;

public class GuardianBoundaryCollider : MonoBehaviour
{
    public Transform xrOrigin;
    public Transform head;

    public float teleportDetectionDistance = 0.75f;
    public float teleportIgnoreTime = 0.3f;

    private BoxCollider guardianCollider;
    private Vector3 previousHeadPos;
    private float ignoreClampUntil;

    void Awake()
    {
        guardianCollider = GetComponent<BoxCollider>();
    }

    void Start()
    {
        previousHeadPos = Flat(head.position);
    }

    void LateUpdate()
    {
        Vector3 currentHeadPos = Flat(head.position);

        // Detect teleport / sudden jump
        if (Vector3.Distance(currentHeadPos, previousHeadPos) > teleportDetectionDistance)
        {
            ignoreClampUntil = Time.time + teleportIgnoreTime;
            previousHeadPos = currentHeadPos;
            return;
        }

        previousHeadPos = currentHeadPos;

        // Do not clamp immediately after teleport
        if (Time.time < ignoreClampUntil)
            return;

        if (guardianCollider == null || xrOrigin == null || head == null)
            return;

        Vector3 headPos = head.position;

        Vector3 local = transform.InverseTransformPoint(headPos);
        Vector3 halfSize = guardianCollider.size * 0.5f;

        Vector3 clampedLocal = local;

        clampedLocal.x = Mathf.Clamp(local.x, -halfSize.x, halfSize.x);
        clampedLocal.z = Mathf.Clamp(local.z, -halfSize.z, halfSize.z);

        if (clampedLocal != local)
        {
            Vector3 clampedWorld = transform.TransformPoint(clampedLocal);

            Vector3 correction = clampedWorld - headPos;
            correction.y = 0f;

            xrOrigin.position += correction;
        }
    }

    private Vector3 Flat(Vector3 v)
    {
        return new Vector3(v.x, 0f, v.z);
    }
}