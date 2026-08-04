using TMPro;
using UnityEngine;

public class BusDepositInteractable : MonoBehaviour
{
    [Header("Collection")]
    public int totalTargets = 5;
    public int collectedTargets = 0;
    [Header("UI")]
    public TextMeshPro counterText;


    private void OnTriggerEnter(Collider other)
    {
        GrabbableTarget target = other.GetComponentInParent<GrabbableTarget>();

        if (target == null)
            return;

        collectedTargets++;
        UpdateCounter();


        Debug.Log($"Target collected {collectedTargets}/{totalTargets}: {target.name}");

        if (ExperimentLogger.Instance != null)
            ExperimentLogger.Instance.LogObjectDelivered(target.name);

        target.gameObject.SetActive(false);

        if (collectedTargets >= totalTargets)
        {
            Debug.Log("All targets collected!");
        }
    }
    private void UpdateCounter()
    {
        if (counterText != null)
            counterText.text = $"{collectedTargets}/{totalTargets}";
    }
}