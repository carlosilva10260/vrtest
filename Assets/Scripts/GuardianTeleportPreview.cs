using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

public class GuardianTeleportPreview :
    MonoBehaviour,
    IXRHoverFilter,
    IXRSelectFilter
{
    [Header("References")]
    public GuardianTeleportManager_ForPreview guardianTeleportManager;
    public XRRayInteractor teleportInteractor;
    public Transform simulatedGuardian;
    public Transform head;

    [Header("Input")]
    public InputActionReference teleportModeInput;

    [Header("Preview Objects")]
    public Transform previewGuardian;
    public Transform landingPreview;

    [Header("Mannequin Rotation")]
    public bool mannequinFacesBackwards = false;

    [Header("Preview Settings")]
    public float rayMoveThreshold = 0.05f;

    [Header("Debug")]
    public bool debugValidation = false;

    private bool hasCachedGuardianCenter;
    private Vector3 cachedGuardianCenter;

    private Renderer[] previewGuardianRenderers;

    private bool currentTeleportIsValid;

    private bool hoverFilterRegistered;
    private bool selectFilterRegistered;

    /*
     * Teleport-trigger suppression state.
     */
    private BaseTeleportationInteractable currentTeleportInteractable;

    private BaseTeleportationInteractable.TeleportTrigger
        originalTeleportTrigger;

    private bool teleportTriggerSuppressed;
    private bool waitingForInvalidRelease;

    public bool canProcess =>
        isActiveAndEnabled;

    private void Awake()
    {
        if (previewGuardian != null)
        {
            previewGuardianRenderers =
                previewGuardian.GetComponentsInChildren<Renderer>(true);
        }

        currentTeleportIsValid = false;
        HidePreview();
    }

    private void OnEnable()
    {
        if (teleportModeInput != null)
            teleportModeInput.action.Enable();

        RegisterFilters();
    }

    private void OnDisable()
    {
        RestoreTeleportTrigger();
        UnregisterFilters();

        currentTeleportInteractable = null;
        waitingForInvalidRelease = false;
        currentTeleportIsValid = false;

        HidePreview();
    }

    private void Update()
    {
        bool teleportPressed =
            teleportModeInput != null &&
            teleportModeInput.action.IsPressed();

        /*
         * The user released teleport while the destination was invalid.
         *
         * Keep the trigger suppressed until the XR Interaction Manager has
         * completed the selection exit. Restoring it during that same
         * release could generate the unwanted teleport request.
         */
        if (!teleportPressed &&
            waitingForInvalidRelease)
        {
            currentTeleportIsValid = false;
            HidePreview();

            if (teleportInteractor == null ||
                !teleportInteractor.hasSelection)
            {
                RestoreTeleportTrigger();
                waitingForInvalidRelease = false;
                currentTeleportInteractable = null;
            }

            return;
        }

        if (!teleportPressed)
        {
            RestoreTeleportTrigger();

            waitingForInvalidRelease = false;
            currentTeleportInteractable = null;
            currentTeleportIsValid = false;

            HidePreview();
            return;
        }

        if (!HasRequiredReferences())
        {
            currentTeleportIsValid = false;
            SuppressCurrentTeleportTrigger();
            waitingForInvalidRelease = true;

            HidePreview();
            return;
        }

        bool valid =
            TryEvaluateCurrentDestination(
                out Vector3 guardianCenter,
                out Vector3 predictedUserPosition
            );

        currentTeleportIsValid = valid;

        if (!valid)
        {
            /*
             * Do not cancel the current selection.
             *
             * Cancelling a selected TeleportationArea can invoke
             * OnSelectExited, which may generate the teleport request.
             */
            SuppressCurrentTeleportTrigger();
            waitingForInvalidRelease = true;

            HidePreviewObjectsOnly();
            return;
        }

        /*
         * The user moved back to a valid position before releasing.
         * Normal teleport behaviour can resume.
         */
        RestoreTeleportTrigger();
        waitingForInvalidRelease = false;

        ShowGuardianPreview(guardianCenter);
        ShowLandingPreview(predictedUserPosition);
    }

    /*
     * HOVER FILTER
     *
     * Returning false makes the destination invalid for hover. This should
     * make the XR line visual use its normal invalid colour rather than
     * treating the destination as a valid-but-unselectable target.
     */
    public bool Process(
        IXRHoverInteractor interactor,
        IXRHoverInteractable interactable)
    {
        if (!IsTeleportInteractable(interactable))
            return true;

        if (!IsConfiguredInteractor(interactor))
            return true;

        RememberTeleportInteractable(interactable);

        bool valid =
            EvaluateForInteractionFilter();

        currentTeleportIsValid = valid;

        if (!valid)
        {
            SuppressCurrentTeleportTrigger();
            waitingForInvalidRelease = true;

            if (debugValidation)
            {
                Debug.Log(
                    "Teleport hover is invalid. The teleport trigger has " +
                    "been suppressed."
                );
            }
        }

        return valid;
    }

    /*
     * SELECT FILTER
     *
     * Before initial selection, an invalid destination is rejected.
     *
     * If the teleport area is already selected, the method returns true
     * so Unity does not force an early Select Exited event. Instead, the
     * teleport trigger remains suppressed until the invalid release has
     * completed.
     */
    public bool Process(
        IXRSelectInteractor interactor,
        IXRSelectInteractable interactable)
    {
        if (!IsTeleportInteractable(interactable))
            return true;

        if (!IsConfiguredInteractor(interactor))
            return true;

        RememberTeleportInteractable(interactable);

        bool valid =
            EvaluateForInteractionFilter();

        currentTeleportIsValid = valid;

        if (!valid)
        {
            SuppressCurrentTeleportTrigger();
            waitingForInvalidRelease = true;

            if (debugValidation)
            {
                Debug.Log(
                    "Teleport selection is invalid. No teleport request " +
                    "should be generated."
                );
            }
        }

        /*
         * Never invalidate an already-selected target through this filter.
         * Doing so could produce Select Exited while the user is still
         * holding the teleport control.
         */
        if (teleportInteractor != null &&
            teleportInteractor.hasSelection)
        {
            return true;
        }

        return valid;
    }

    public bool IsTeleportCurrentlyAllowed()
    {
        return currentTeleportIsValid;
    }

    private bool EvaluateForInteractionFilter()
    {
        if (teleportModeInput == null ||
            !teleportModeInput.action.IsPressed())
        {
            return false;
        }

        return TryEvaluateCurrentDestination(
            out _,
            out _
        );
    }

    private bool TryEvaluateCurrentDestination(
        out Vector3 guardianCenter,
        out Vector3 predictedUserPosition)
    {
        guardianCenter = Vector3.zero;
        predictedUserPosition = Vector3.zero;

        if (!HasRequiredReferences())
            return false;

        if (!teleportInteractor.TryGetCurrent3DRaycastHit(
                out RaycastHit hit))
        {
            return false;
        }

        if (!IsValidTeleportHit(hit))
            return false;

        RememberTeleportInteractableFromHit(hit);

        Vector3 rayGuardianCenter =
            hit.point;

        rayGuardianCenter.y =
            simulatedGuardian.position.y;

        UpdateCachedGuardianCenter(
            rayGuardianCenter
        );

        guardianCenter =
            cachedGuardianCenter;

        predictedUserPosition =
            guardianTeleportManager.PredictFinalUserPosition(
                guardianCenter,
                Flat(head.forward)
            );

        bool landingIsValid =
            guardianTeleportManager.IsValidLandingPosition(
                predictedUserPosition
            );

        if (!landingIsValid &&
            debugValidation)
        {
            Debug.Log(
                $"Invalid mannequin position: {predictedUserPosition}"
            );
        }

        return landingIsValid;
    }

    private bool HasRequiredReferences()
    {
        return guardianTeleportManager != null &&
               teleportInteractor != null &&
               simulatedGuardian != null &&
               head != null &&
               previewGuardian != null;
    }

    private bool IsConfiguredInteractor(
        IXRHoverInteractor interactor)
    {
        if (teleportInteractor == null)
            return false;

        return ReferenceEquals(
            interactor,
            teleportInteractor
        );
    }

    private bool IsConfiguredInteractor(
        IXRSelectInteractor interactor)
    {
        if (teleportInteractor == null)
            return false;

        return ReferenceEquals(
            interactor,
            teleportInteractor
        );
    }

    private void RegisterFilters()
    {
        if (teleportInteractor == null)
        {
            Debug.LogError(
                "GuardianTeleportPreview: Teleport Interactor is not assigned."
            );

            return;
        }

        if (!hoverFilterRegistered)
        {
            teleportInteractor.hoverFilters.Add(this);
            hoverFilterRegistered = true;
        }

        if (!selectFilterRegistered)
        {
            teleportInteractor.selectFilters.Add(this);
            selectFilterRegistered = true;
        }

        /*
         * Do not set keepSelectedTargetValid to false here.
         *
         * Keeping the selected target prevents Unity from forcing an
         * early Select Exited event when the ray moves into an invalid
         * mannequin position.
         */
    }

    private void UnregisterFilters()
    {
        if (teleportInteractor == null)
            return;

        if (hoverFilterRegistered)
        {
            teleportInteractor.hoverFilters.Remove(this);
            hoverFilterRegistered = false;
        }

        if (selectFilterRegistered)
        {
            teleportInteractor.selectFilters.Remove(this);
            selectFilterRegistered = false;
        }
    }

    private void RememberTeleportInteractable(
        IXRHoverInteractable interactable)
    {
        Component component =
            interactable as Component;

        if (component == null)
            return;

        BaseTeleportationInteractable teleportInteractable =
            component.GetComponent<BaseTeleportationInteractable>();

        if (teleportInteractable == null)
        {
            teleportInteractable =
                component.GetComponentInParent<
                    BaseTeleportationInteractable
                >();
        }

        SetCurrentTeleportInteractable(
            teleportInteractable
        );
    }

    private void RememberTeleportInteractable(
        IXRSelectInteractable interactable)
    {
        Component component =
            interactable as Component;

        if (component == null)
            return;

        BaseTeleportationInteractable teleportInteractable =
            component.GetComponent<BaseTeleportationInteractable>();

        if (teleportInteractable == null)
        {
            teleportInteractable =
                component.GetComponentInParent<
                    BaseTeleportationInteractable
                >();
        }

        SetCurrentTeleportInteractable(
            teleportInteractable
        );
    }

    private void RememberTeleportInteractableFromHit(
        RaycastHit hit)
    {
        if (hit.collider == null)
            return;

        BaseTeleportationInteractable teleportInteractable =
            hit.collider.GetComponentInParent<
                BaseTeleportationInteractable
            >();

        SetCurrentTeleportInteractable(
            teleportInteractable
        );
    }

    private void SetCurrentTeleportInteractable(
        BaseTeleportationInteractable interactable)
    {
        if (interactable == null)
            return;

        if (currentTeleportInteractable == interactable)
            return;

        /*
         * Restore the previous area before switching to another one.
         */
        RestoreTeleportTrigger();

        currentTeleportInteractable =
            interactable;

        originalTeleportTrigger =
            currentTeleportInteractable.teleportTrigger;
    }

    private void SuppressCurrentTeleportTrigger()
    {
        if (currentTeleportInteractable == null)
            return;

        if (teleportTriggerSuppressed)
            return;

        originalTeleportTrigger =
            currentTeleportInteractable.teleportTrigger;

        /*
         * The normal trigger is expected to be OnSelectExited.
         *
         * While invalid, use OnActivated so ending selection cannot queue
         * a teleport request.
         */
        currentTeleportInteractable.teleportTrigger =
            BaseTeleportationInteractable.TeleportTrigger.OnActivated;

        teleportTriggerSuppressed = true;

        if (debugValidation)
        {
            Debug.Log(
                $"Suppressed teleport trigger on " +
                $"{currentTeleportInteractable.name}."
            );
        }
    }

    private void RestoreTeleportTrigger()
    {
        if (currentTeleportInteractable == null)
        {
            teleportTriggerSuppressed = false;
            return;
        }

        if (!teleportTriggerSuppressed)
            return;

        currentTeleportInteractable.teleportTrigger =
            originalTeleportTrigger;

        teleportTriggerSuppressed = false;

        if (debugValidation)
        {
            Debug.Log(
                $"Restored teleport trigger on " +
                $"{currentTeleportInteractable.name}."
            );
        }
    }

    private bool IsTeleportInteractable(
        IXRHoverInteractable interactable)
    {
        if (interactable == null)
            return false;

        Component component =
            interactable as Component;

        if (component == null)
            return false;

        return component.GetComponent<
                   BaseTeleportationInteractable
               >() != null ||
               component.GetComponentInParent<
                   BaseTeleportationInteractable
               >() != null;
    }

    private bool IsTeleportInteractable(
        IXRSelectInteractable interactable)
    {
        if (interactable == null)
            return false;

        Component component =
            interactable as Component;

        if (component == null)
            return false;

        return component.GetComponent<
                   BaseTeleportationInteractable
               >() != null ||
               component.GetComponentInParent<
                   BaseTeleportationInteractable
               >() != null;
    }

    private bool IsValidTeleportHit(RaycastHit hit)
    {
        if (hit.collider == null)
            return false;

        return hit.collider.GetComponentInParent<
            BaseTeleportationInteractable
        >() != null;
    }

    private void UpdateCachedGuardianCenter(
        Vector3 rayGuardianCenter)
    {
        if (!hasCachedGuardianCenter)
        {
            cachedGuardianCenter =
                rayGuardianCenter;

            hasCachedGuardianCenter = true;
            return;
        }

        float rayMovementDistance =
            Vector3.Distance(
                Flat(cachedGuardianCenter),
                Flat(rayGuardianCenter)
            );

        if (rayMovementDistance >
            rayMoveThreshold)
        {
            cachedGuardianCenter =
                rayGuardianCenter;
        }
    }

    private void ShowGuardianPreview(
        Vector3 guardianCenter)
    {
        previewGuardian.gameObject.SetActive(true);
        SetPreviewGuardianVisible(true);

        previewGuardian.position = new Vector3(
            guardianCenter.x,
            simulatedGuardian.position.y,
            guardianCenter.z
        );

        previewGuardian.rotation =
            simulatedGuardian.rotation;
    }

    private void ShowLandingPreview(
        Vector3 landingPosition)
    {
        if (landingPreview == null)
            return;

        landingPreview.gameObject.SetActive(true);

        landingPreview.position = new Vector3(
            landingPosition.x,
            landingPreview.position.y,
            landingPosition.z
        );

        Vector3 forward =
            Flat(head.forward);

        if (forward.sqrMagnitude <= 0.0001f)
            return;

        landingPreview.rotation =
            Quaternion.LookRotation(
                forward.normalized,
                Vector3.up
            );

        if (mannequinFacesBackwards)
        {
            landingPreview.Rotate(
                0f,
                180f,
                0f
            );
        }
    }

    private void HidePreview()
    {
        hasCachedGuardianCenter = false;
        HidePreviewObjectsOnly();
    }

    private void HidePreviewObjectsOnly()
    {
        if (previewGuardian != null)
        {
            SetPreviewGuardianVisible(false);
            previewGuardian.gameObject.SetActive(false);
        }

        if (landingPreview != null)
            landingPreview.gameObject.SetActive(false);
    }

    private void SetPreviewGuardianVisible(
        bool visible)
    {
        if (previewGuardianRenderers == null)
            return;

        foreach (
            Renderer rendererComponent
            in previewGuardianRenderers)
        {
            if (rendererComponent != null)
                rendererComponent.enabled = visible;
        }
    }

    private Vector3 Flat(Vector3 value)
    {
        return new Vector3(
            value.x,
            0f,
            value.z
        );
    }
}