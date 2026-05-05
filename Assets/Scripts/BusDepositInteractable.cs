using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRSimpleInteractable))]
public class BusDepositInteractable : MonoBehaviour
{
    public BusCollectionManager collectionManager;
    public Transform playerHead;
    public float maxDepositDistance = 2.0f;

    private XRSimpleInteractable interactable;

    private void Awake()
    {
        interactable = GetComponent<XRSimpleInteractable>();
    }

    private void OnEnable()
    {
        interactable.selectEntered.AddListener(OnSelected);
    }

    private void OnDisable()
    {
        interactable.selectEntered.RemoveListener(OnSelected);
    }

    private void OnSelected(SelectEnterEventArgs args)
    {
        if (playerHead == null)
        {
            Debug.LogWarning("No playerHead assigned on bus deposit.");
            return;
        }

        float distance = Vector3.Distance(playerHead.position, transform.position);

        if (distance > maxDepositDistance)
        {
            Debug.Log($"Too far from bus to deposit. Distance: {distance:F2}");
            return;
        }

        Debug.Log("Bus selected for deposit.");

        if (collectionManager != null)
        {
            collectionManager.TryDepositAtBus();
        }
    }
}