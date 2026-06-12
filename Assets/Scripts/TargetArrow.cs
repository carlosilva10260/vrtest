using UnityEngine;

public class TargetArrow : MonoBehaviour
{
    [Header("Floating Animation")]
    public float floatAmplitude = 0.1f;
    public float floatSpeed = 5f;

    [Header("Axis")]
    public Vector3 movementAxis = Vector3.up;

    private Vector3 startLocalPosition;
    private Renderer[] renderers;

    private void Awake()
    {
        startLocalPosition = transform.localPosition;
        renderers = GetComponentsInChildren<Renderer>();
    }

    private void Update()
    {
        Vector3 axis = movementAxis;

        if (axis.sqrMagnitude < 0.0001f)
            axis = Vector3.up;

        axis.Normalize();

        float offset = Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;

        transform.localPosition = startLocalPosition + axis * offset;
    }

    public void SetVisible(bool visible)
    {
        foreach (Renderer r in renderers)
            r.enabled = visible;
    }
}