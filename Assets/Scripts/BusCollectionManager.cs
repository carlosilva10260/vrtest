using TMPro;
using UnityEngine;

public class BusCollectionManager : MonoBehaviour
{
    [Header("UI")]
    public TextMeshPro counterText;

    [Header("Targets")]
    public int totalTargets = 5;

    public int deliveredTargets = 0;

    private void Start()
    {
        UpdateCounter();
    }

    public void RegisterDeliveredTarget(GameObject target)
    {
        deliveredTargets++;
        UpdateCounter();

        Debug.Log($"Delivered {target.name}. {deliveredTargets}/{totalTargets}");

        if (deliveredTargets >= totalTargets)
        {
            Debug.Log("All targets delivered!");
        }
    }

    private void UpdateCounter()
    {
        if (counterText != null)
            counterText.text = $"{deliveredTargets}/{totalTargets}";
    }
}