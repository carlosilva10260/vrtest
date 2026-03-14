using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

public class TeleportSafetyValidator : MonoBehaviour
{
    public GuardianSafetyManager safetyManager;

    TeleportationArea area;

    void Start()
    {
        area = GetComponent<TeleportationArea>();
    }

    void Update()
    {
        area.enabled = safetyManager.enabled;
    }
}
