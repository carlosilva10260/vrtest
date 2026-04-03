using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class BusCollectionManager : MonoBehaviour
{
    [Header("References")]
    public GuardianTeleportManager guardianTeleportManager;
    public TextMeshPro counterText;

    [Header("Targets")]
    public int totalTargets = 5;

    [Header("State")]
    public int deliveredTargets = 0;
    public bool carryingTarget = false;

    private HashSet<GameObject> collectedOrDelivered = new HashSet<GameObject>();

    private void Start()
    {
        UpdateCounter();
    }

    public bool TryCollectTarget(GameObject target)
    {
        if (carryingTarget)
        {
            Debug.Log("Already carrying a target.");
            return false;
        }

        if (collectedOrDelivered.Contains(target))
            return false;

        carryingTarget = true;
        collectedOrDelivered.Add(target);

        // Remove from teleport manager target list so it stops influencing redirect logic.
        if (guardianTeleportManager != null)
        {
            Transform targetTransform = target.transform;
            guardianTeleportManager.targets.Remove(targetTransform);
        }

        target.SetActive(false);
        Debug.Log("Target collected.");
        return true;
    }

    public bool TryDepositAtBus()
    {
        if (!carryingTarget)
        {
            Debug.Log("No target being carried.");
            return false;
        }

        carryingTarget = false;
        deliveredTargets++;
        UpdateCounter();

        Debug.Log($"Target delivered. {deliveredTargets}/{totalTargets}");

        if (deliveredTargets >= totalTargets)
        {
            Debug.Log("All targets delivered!");
        }

        return true;
    }

    private void UpdateCounter()
    {
        if (counterText != null)
        {
            counterText.text = $"{deliveredTargets}/{totalTargets}";
        }
    }
}