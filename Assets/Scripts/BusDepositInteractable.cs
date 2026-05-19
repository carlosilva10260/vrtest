using UnityEngine;

public class BusDepositInteractable : MonoBehaviour
{
    [Header("Collection")]
    public int totalTargets = 5;
    public int collectedTargets = 0;

    private void OnTriggerEnter(Collider other)
    {
        GrabbableTarget target = other.GetComponentInParent<GrabbableTarget>();

        if (target == null)
            return;

        collectedTargets++;

        Debug.Log($"Target collected {collectedTargets}/{totalTargets}: {target.name}");

        target.gameObject.SetActive(false);

        if (collectedTargets >= totalTargets)
        {
            Debug.Log("All targets collected!");
        }
    }
}