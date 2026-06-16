using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

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
    public Transform previewGuardian;   // Parent with 4 edge children
    public Transform landingPreview;    // Mannequin

    [Header("Mannequin Rotation")]
    public bool mannequinFacesBackwards = false;

    [Header("Preview Settings")]
    public float rayMoveThreshold = 0.05f;

    private bool hasCachedGuardianCenter = false;
    private Vector3 cachedGuardianCenter;

    private Renderer[] previewGuardianRenderers;

    private void Awake()
    {
        if (previewGuardian != null)
            previewGuardianRenderers = previewGuardian.GetComponentsInChildren<Renderer>(true);

        HidePreview();
    }

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
            head == null ||
            previewGuardian == null)
        {
            HidePreview();
            return;
        }

        if (!teleportInteractor.TryGetCurrent3DRaycastHit(out RaycastHit hit))
        {
            HidePreview();
            return;
        }

        if (!IsValidTeleportHit(hit))
        {
            HidePreview();
            return;
        }

        Vector3 rayGuardianCenter = hit.point;
        rayGuardianCenter.y = simulatedGuardian.position.y;

        if (!hasCachedGuardianCenter)
        {
            cachedGuardianCenter = rayGuardianCenter;
            hasCachedGuardianCenter = true;
        }
        else
        {
            float rayMoveDistance = Vector3.Distance(
                Flat(cachedGuardianCenter),
                Flat(rayGuardianCenter)
            );

            if (rayMoveDistance > rayMoveThreshold)
                cachedGuardianCenter = rayGuardianCenter;
        }

        Vector3 finalUserPos =
            guardianTeleportManager.PredictFinalUserPosition(
                cachedGuardianCenter,
                Flat(head.forward)
            );

        ShowGuardianPreview(cachedGuardianCenter);
        ShowLandingPreview(finalUserPos);
    }

    private void ShowGuardianPreview(Vector3 guardianCenter)
    {
        previewGuardian.gameObject.SetActive(true);
        SetPreviewGuardianVisible(true);

        previewGuardian.position = new Vector3(
            guardianCenter.x,
            simulatedGuardian.position.y,
            guardianCenter.z
        );

        previewGuardian.rotation = simulatedGuardian.rotation;
    }

    private void ShowLandingPreview(Vector3 landingPosition)
    {
        if (landingPreview == null)
            return;

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
            landingPreview.rotation =
                Quaternion.LookRotation(forward.normalized, Vector3.up);

            if (mannequinFacesBackwards)
                landingPreview.Rotate(0f, 180f, 0f);
        }
    }

    private bool IsValidTeleportHit(RaycastHit hit)
    {
        if (hit.collider == null)
            return false;

        if (hit.collider.GetComponentInParent<TeleportationArea>() != null)
            return true;

        if (hit.collider.GetComponentInParent<TeleportationAnchor>() != null)
            return true;

        return false;
    }

    private void HidePreview()
    {
        hasCachedGuardianCenter = false;

        if (previewGuardian != null)
        {
            SetPreviewGuardianVisible(false);
            previewGuardian.gameObject.SetActive(false);
        }

        if (landingPreview != null)
            landingPreview.gameObject.SetActive(false);
    }

    private void SetPreviewGuardianVisible(bool visible)
    {
        if (previewGuardianRenderers == null)
            return;

        foreach (Renderer r in previewGuardianRenderers)
        {
            if (r != null)
                r.enabled = visible;
        }
    }

    private Vector3 Flat(Vector3 v)
    {
        return new Vector3(v.x, 0f, v.z);
    }
}