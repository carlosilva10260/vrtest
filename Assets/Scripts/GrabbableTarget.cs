using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRGrabInteractable))]
public class GrabbableTarget : MonoBehaviour
{
    [Header("References")]
    public BusCollectionManager collectionManager;
    public GuardianTeleportManager guardianTeleportManager;

    [Header("Arrow")]
    public TargetArrow targetArrow;

    [Header("Grab Distance")]
    public float maxGrabDistance = 0.2f;

    [Header("Teleport Ray Blocking Fix")]
    public bool disableCollidersWhileGrabbed = true;
    public float reenableColliderDelay = 0.15f;

    private XRGrabInteractable grabInteractable;
    private Collider[] targetColliders;

    private bool removedFromRedirectTargets = false;
    private bool delivered = false;

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        targetColliders = GetComponentsInChildren<Collider>();
    }

    private void OnEnable()
    {
        grabInteractable.selectEntered.AddListener(OnGrabbed);
        grabInteractable.selectExited.AddListener(OnReleased);
    }

    private void OnDisable()
    {
        grabInteractable.selectEntered.RemoveListener(OnGrabbed);
        grabInteractable.selectExited.RemoveListener(OnReleased);
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        if (delivered)
            return;

        Vector3 handPos = args.interactorObject.transform.position;

        Vector3 targetPos = transform.position;

        Collider targetCollider = GetComponentInChildren<Collider>();
        if (targetCollider != null)
            targetPos = targetCollider.bounds.center;

        float distance = Vector3.Distance(handPos, targetPos);

        if (distance > maxGrabDistance)
        {
            Debug.Log($"Too far to grab {gameObject.name}. Hand distance: {distance:F2}");

            grabInteractable.interactionManager.SelectExit(
                args.interactorObject,
                grabInteractable
            );

            return;
        }

        Debug.Log($"Grabbed target: {gameObject.name}");

        if (targetArrow != null)
            targetArrow.SetVisible(false);

        if (disableCollidersWhileGrabbed)
            SetTargetColliders(false);

        if (!removedFromRedirectTargets && guardianTeleportManager != null)
        {
            guardianTeleportManager.RemoveTarget(transform);
            removedFromRedirectTargets = true;

            Debug.Log($"{gameObject.name} removed from redirection target list.");
        }
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        if (!delivered && targetArrow != null)
            targetArrow.SetVisible(true);

        if (disableCollidersWhileGrabbed)
            Invoke(nameof(ReenableColliders), reenableColliderDelay);
    }

    private void ReenableColliders()
    {
        if (!delivered)
            SetTargetColliders(true);
    }

    private void SetTargetColliders(bool enabled)
    {
        foreach (Collider col in targetColliders)
        {
            if (col != null)
                col.enabled = enabled;
        }
    }

    public void Deliver()
    {
        if (delivered)
            return;

        delivered = true;

        if (collectionManager != null)
            collectionManager.RegisterDeliveredTarget(gameObject);

        gameObject.SetActive(false);
    }
}