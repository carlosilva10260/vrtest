using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

public class GuardianSafetyManager : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public Transform target;
    public GameObject forbiddenVisual;
    public Transform simulatedGuardianObject; // assign the GameObject that represents the guardian

    [Header("Settings")]
    public float predictionDistance = 2f;
    public float safetyMargin = 0.5f;

    [Header("Simulated Guardian (Manual Coordinates)")]
    public Vector3[] simulatedBoundary; // assign 4 corners in inspector

    private List<Vector3> boundaryPoints = new List<Vector3>();
    private XRInputSubsystem xrInput;
    private Vector3 oldPlayerPosition;

    [Header("Teleport Detection")]
    public float teleportThreshold = 0.5f; // distance to detect teleport

    void Start()
    {
        oldPlayerPosition = player.position;

        if (simulatedBoundary != null && simulatedBoundary.Length >= 3)
            boundaryPoints = new List<Vector3>(simulatedBoundary);

        var subsystems = new List<XRInputSubsystem>();
        SubsystemManager.GetSubsystems(subsystems);
        if (subsystems.Count > 0) xrInput = subsystems[0];

        // Position the guardian at the start
        UpdateSimulatedGuardianPosition();
    }

    void Update()
    {
        // Detect teleport by large movement
        Vector3 offset = player.position - oldPlayerPosition;
        if (offset.magnitude > teleportThreshold && simulatedBoundary != null)
        {
            for (int i = 0; i < simulatedBoundary.Length; i++)
                simulatedBoundary[i] += offset;
        }

        oldPlayerPosition = player.position;

        // Update boundaryPoints
        if (simulatedBoundary != null && simulatedBoundary.Length >= 3)
        {
            boundaryPoints = new List<Vector3>(simulatedBoundary);
            UpdateSimulatedGuardianPosition(); // move the visible guardian
        }
        else if (xrInput != null)
        {
            xrInput.TryGetBoundaryPoints(boundaryPoints);
        }

        if (boundaryPoints.Count < 3) return;

        // Update forbidden zone
        bool safe = CheckFuturePath();
        forbiddenVisual.SetActive(!safe);
    }

    // Move the simulated guardian GameObject to the center of the corners
    void UpdateSimulatedGuardianPosition()
    {
        if (simulatedGuardianObject == null || simulatedBoundary == null || simulatedBoundary.Length < 3)
            return;

        Vector3 center = Vector3.zero;
        foreach (Vector3 corner in simulatedBoundary)
            center += corner;

        center /= simulatedBoundary.Length;
        simulatedGuardianObject.position = center;
    }

    bool CheckFuturePath()
    {
        Vector3 direction = (target.position - player.position).normalized;

        for (float d = 0.3f; d <= predictionDistance; d += 0.3f)
        {
            Vector3 future = player.position + direction * d;
            float dist = DistanceToBoundary(future);

            if (dist < safetyMargin)
                return false;
        }

        return true;
    }

    float DistanceToBoundary(Vector3 point)
    {
        float minDist = float.MaxValue;

        for (int i = 0; i < boundaryPoints.Count; i++)
        {
            Vector3 a = boundaryPoints[i];
            Vector3 b = boundaryPoints[(i + 1) % boundaryPoints.Count];
            float dist = DistancePointToSegment(point, a, b);

            if (dist < minDist)
                minDist = dist;
        }

        return minDist;
    }

    float DistancePointToSegment(Vector3 p, Vector3 a, Vector3 b)
    {
        Vector3 ap = p - a;
        Vector3 ab = b - a;

        float t = Mathf.Clamp01(Vector3.Dot(ap, ab) / Vector3.Dot(ab, ab));
        Vector3 closest = a + ab * t;

        return Vector3.Distance(p, closest);
    }
}