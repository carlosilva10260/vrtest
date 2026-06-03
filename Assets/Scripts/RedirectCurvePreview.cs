using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[RequireComponent(typeof(LineRenderer))]
public class RedirectCurvePreview : MonoBehaviour
{
    [Header("References")]
    public GuardianTeleportManager guardianTeleportManager;
    public XRRayInteractor teleportInteractor;
    public Transform head;

    [Header("Input")]
    public InputActionReference teleportModeInput;

    [Header("Landing Preview")]
    public Transform landingMannequin;
    public bool mannequinFacesBackwards = false;

    [Header("Curve Settings")]
    public int curveResolution = 32;
    public float curveHeight = 0.05f;
    public float curveArcHeight = 1.0f;
    public float curveSideOffset = 0.2f;
    public float minRedirectionDistanceToShow = 0.05f;

    [Header("Debug")]
    public bool debugLogs = false;

    private LineRenderer lineRenderer;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();

        lineRenderer.useWorldSpace = true;
        lineRenderer.positionCount = 0;
        lineRenderer.enabled = false;
        lineRenderer.widthMultiplier = 0.05f;

        HideLandingMannequin();
    }

    private void OnEnable()
    {
        if (teleportModeInput != null)
            teleportModeInput.action.Enable();
    }

    private void OnDisable()
    {
        HideCurve();
        HideLandingMannequin();
    }

    private void Update()
    {
        if (teleportModeInput == null || !teleportModeInput.action.IsPressed())
        {
            HideCurve();
            HideLandingMannequin();
            return;
        }

        if (guardianTeleportManager == null || teleportInteractor == null || head == null)
        {
            HideCurve();
            HideLandingMannequin();
            return;
        }

        if (!teleportInteractor.TryGetCurrent3DRaycastHit(out RaycastHit hit))
        {
            HideCurve();
            HideLandingMannequin();
            return;
        }

        Vector3 teleportPoint = Flat(hit.point);

        Vector3 redirectedPosition =
            guardianTeleportManager.PredictFinalUserPosition(
                teleportPoint,
                Flat(head.forward)
            );

        float distance = Vector3.Distance(teleportPoint, redirectedPosition);

        if (debugLogs)
            Debug.Log($"Redirect curve distance: {distance:F2}");

        if (distance < minRedirectionDistanceToShow)
        {
            HideCurve();
            HideLandingMannequin();
            return;
        }

        DrawCurve(teleportPoint, redirectedPosition);
        ShowLandingMannequin(redirectedPosition);
    }

    private void DrawCurve(Vector3 start, Vector3 end)
    {
        lineRenderer.enabled = true;
        lineRenderer.positionCount = curveResolution;

        Vector3 direction = end - start;

        if (direction.sqrMagnitude < 0.0001f)
        {
            HideCurve();
            HideLandingMannequin();
            return;
        }

        direction.Normalize();

        Vector3 side = Vector3.Cross(Vector3.up, direction).normalized;

        Vector3 p0 = start + Vector3.up * curveHeight;
        Vector3 p3 = end + Vector3.up * curveHeight;

        Vector3 p1 =
            Vector3.Lerp(start, end, 0.33f)
            + side * curveSideOffset
            + Vector3.up * curveArcHeight;

        Vector3 p2 =
            Vector3.Lerp(start, end, 0.66f)
            + side * curveSideOffset
            + Vector3.up * curveArcHeight;

        for (int i = 0; i < curveResolution; i++)
        {
            float t = i / (float)(curveResolution - 1);
            lineRenderer.SetPosition(i, CubicBezier(p0, p1, p2, p3, t));
        }
    }

    private void ShowLandingMannequin(Vector3 landingPosition)
    {
        if (landingMannequin == null)
            return;

        landingMannequin.gameObject.SetActive(true);

        landingMannequin.position = new Vector3(
            landingPosition.x,
            landingMannequin.position.y,
            landingPosition.z
        );

        Vector3 forward = head.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude > 0.0001f)
        {
            landingMannequin.rotation =
                Quaternion.LookRotation(forward.normalized, Vector3.up);

            if (mannequinFacesBackwards)
                landingMannequin.Rotate(0f, 180f, 0f);
        }
    }

    private void HideLandingMannequin()
    {
        if (landingMannequin != null)
            landingMannequin.gameObject.SetActive(false);
    }

    private Vector3 CubicBezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float u = 1f - t;

        return
            u * u * u * p0 +
            3f * u * u * t * p1 +
            3f * u * t * t * p2 +
            t * t * t * p3;
    }

    private void HideCurve()
    {
        if (lineRenderer == null)
            return;

        lineRenderer.enabled = false;
        lineRenderer.positionCount = 0;
    }

    private Vector3 Flat(Vector3 v)
    {
        return new Vector3(v.x, 0f, v.z);
    }
}