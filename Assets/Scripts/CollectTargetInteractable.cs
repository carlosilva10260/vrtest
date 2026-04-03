using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRSimpleInteractable))]
public class CollectTargetInteractable : MonoBehaviour
{
    public BusCollectionManager collectionManager;

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
        if (collectionManager != null)
        {
            Debug.Log($"Target selected: {gameObject.name}");
            collectionManager.TryCollectTarget(gameObject);
        }
    }
}