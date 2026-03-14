using UnityEngine;

[ExecuteAlways]
public class SimulatedGuardianVisualizer : MonoBehaviour
{
    public Vector3 size = new Vector3(4, 0, 4);  // X,Z = floor size
    private LineRenderer line;

    void Awake()
    {
        line = GetComponent<LineRenderer>();
        if (line == null) line = gameObject.AddComponent<LineRenderer>();

        line.positionCount = 5;
        line.loop = true;
        line.widthMultiplier = 0.05f;
        line.useWorldSpace = false;
    }

    void Update()
    {
        Vector3 half = size / 2f;

        line.SetPosition(0, new Vector3(-half.x, 0, -half.z));
        line.SetPosition(1, new Vector3(-half.x, 0, half.z));
        line.SetPosition(2, new Vector3(half.x, 0, half.z));
        line.SetPosition(3, new Vector3(half.x, 0, -half.z));
        line.SetPosition(4, new Vector3(-half.x, 0, -half.z));
    }
}
