using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRGrabInteractable))]
public class GrabbableTarget : MonoBehaviour
{
    [Header("References")]
    public BusCollectionManager collectionManager;
    public GuardianTeleportManager guardianTeleportManager;
    public Transform playerHead;

    [Header("Grab Distance")]
    public float maxGrabDistance = 2.0f;

    private XRGrabInteractable grabInteractable;
    private bool removedFromRedirectTargets = false;
    private bool delivered = false;

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
    }

    private void OnEnable()
    {
        grabInteractable.selectEntered.AddListener(OnGrabbed);
    }

    private void OnDisable()
    {
        grabInteractable.selectEntered.RemoveListener(OnGrabbed);
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        if (delivered)
            return;

        if (playerHead == null)
        {
            Debug.LogWarning($"No playerHead assigned on {gameObject.name}");
            return;
        }

        float distance = Vector3.Distance(
            new Vector3(playerHead.position.x, 0f, playerHead.position.z),
            new Vector3(transform.position.x, 0f, transform.position.z)
        );

        if (distance > maxGrabDistance)
        {
            Debug.Log($"Too far to grab {gameObject.name}. Distance: {distance:F2}");

            // Force release if grabbed from too far
            grabInteractable.interactionManager.SelectExit(
                args.interactorObject,
                grabInteractable
            );

            return;
        }

        Debug.Log($"Grabbed target: {gameObject.name}");

        if (!removedFromRedirectTargets && guardianTeleportManager != null)
        {
            guardianTeleportManager.RemoveTarget(transform);
            removedFromRedirectTargets = true;
            Debug.Log($"{gameObject.name} removed from redirection target list.");
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