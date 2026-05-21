using UnityEngine;
using UnityEngine.InputSystem;
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
    public Transform landingPreview; // mannequin/player preview

    [Header("Mannequin Rotation")]
    public bool mannequinFacesBackwards = false;

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

            ShowPreview(teleportPoint, finalUserPos);
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

        if (landingPreview != null)
        {
            landingPreview.gameObject.SetActive(true);

            landingPreview.position = new Vector3(
                landingPosition.x,
                landingPreview.position.y,
                landingPosition.z
            );

            Vector3 forward = head.forward;
            forward.y = 0f;

            if (forward.sqrMagnitude > 0.0001f)
            {
                landingPreview.rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);

                if (mannequinFacesBackwards)
                    landingPreview.Rotate(0f, 180f, 0f);
            }
        }
    }

    private void HidePreview()
    {
        if (guardianPreview != null)
            guardianPreview.gameObject.SetActive(false);

        if (landingPreview != null)
            landingPreview.gameObject.SetActive(false);
    }

    private Vector3 Flat(Vector3 v)
    {
        return new Vector3(v.x, 0f, v.z);
    }
}