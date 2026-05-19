using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class GuardianTeleportPreview : MonoBehaviour
{
    [Header("References")]
    public GuardianTeleportManager guardianTeleportManager;
    public XRRayInteractor teleportInteractor;
    public Transform simulatedGuardian;
    public Transform head;

    [Header("Input")]
    public InputActionReference teleportModeInput;

    [Header("Preview Objects")]
    public Transform guardianPreview;
    public Transform landingDot;

    private void OnEnable()
    {
        if (teleportModeInput != null)
            teleportModeInput.action.Enable();
    }

    private void OnDisable()
    {
        HidePreview();
    }

    private void Update()
    {
        if (teleportModeInput == null || !teleportModeInput.action.IsPressed())
        {
            HidePreview();
            return;
        }

        if (guardianTeleportManager == null ||
            teleportInteractor == null ||
            simulatedGuardian == null ||
            head == null)
        {
            HidePreview();
            return;
        }

        if (teleportInteractor.TryGetCurrent3DRaycastHit(out RaycastHit hit))
        {
            Vector3 teleportPoint = hit.point;
            teleportPoint.y = simulatedGuardian.position.y;

            Vector3 finalUserPos =
                guardianTeleportManager.PredictFinalUserPosition(
                    teleportPoint,
                    Flat(head.forward)
                );

            // Same current logic as GuardianTeleportManager:
            // guardian is centered on selected teleport point,
            // user lands at the redirected/preserved final position.
            Vector3 finalGuardianCenter = teleportPoint;

            ShowPreview(finalGuardianCenter, finalUserPos);
        }
        else
        {
            HidePreview();
        }
    }

    private void ShowPreview(Vector3 guardianCenter, Vector3 landingPosition)
    {
        if (guardianPreview != null)
        {
            guardianPreview.gameObject.SetActive(true);

            guardianPreview.position = new Vector3(
                guardianCenter.x,
                simulatedGuardian.position.y,
                guardianCenter.z
            );

            guardianPreview.rotation = simulatedGuardian.rotation;
            guardianPreview.localScale = simulatedGuardian.localScale;
        }

        if (landingDot != null)
        {
            landingDot.gameObject.SetActive(true);

            landingDot.position = new Vector3(
                landingPosition.x,
                landingDot.position.y,
                landingPosition.z
            );
        }
    }

    private void HidePreview()
    {
        if (guardianPreview != null)
            guardianPreview.gameObject.SetActive(false);

        if (landingDot != null)
            landingDot.gameObject.SetActive(false);
    }

    private Vector3 Flat(Vector3 v)
    {
        return new Vector3(v.x, 0f, v.z);
    }
}