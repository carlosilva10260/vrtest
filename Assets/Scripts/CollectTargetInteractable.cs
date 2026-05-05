using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRSimpleInteractable))]
public class CollectTargetInteractable : MonoBehaviour
{
    public BusCollectionManager collectionManager;
    public Transform playerHead;
    public float maxCollectDistance = 2.0f;

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
            Debug.LogWarning($"No playerHead assigned on {gameObject.name}");
            return;
        }

        float distance = Vector3.Distance(playerHead.position, transform.position);

        if (distance > maxCollectDistance)
        {
            Debug.Log($"Too far to collect {gameObject.name}. Distance: {distance:F2}");
            return;
        }

        Debug.Log($"Target selected: {gameObject.name}");

        if (collectionManager != null)
        {
            collectionManager.TryCollectTarget(gameObject);
        }
    }
}