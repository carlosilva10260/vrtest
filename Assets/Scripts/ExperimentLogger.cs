using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ExperimentLogger : MonoBehaviour
{
    public static ExperimentLogger Instance;

    [Header("Logging")]
    public bool enableLogging = true;
    public bool disableInTrainingScenes = true;

    [Header("References")]
    public Transform playerHead;

    private string sceneName;
    private string filePath;

    private float sceneStartTime;
    private float totalWalkingDistance;
    private Vector3 lastHeadPosXZ;
    private bool hasLastHeadPos;

    private int deliveredCount;
    private int guardianCollisions;
    private int teleportCount;
    private int redirectedTeleportCount;

    private bool sceneCompleted;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        sceneName = SceneManager.GetActiveScene().name;

        if (disableInTrainingScenes && sceneName.ToLower().Contains("training"))
        {
            enableLogging = false;
            return;
        }

        sceneStartTime = Time.time;
        deliveredCount = 0;
        guardianCollisions = 0;
        teleportCount = 0;
        redirectedTeleportCount = 0;
        totalWalkingDistance = 0f;
        sceneCompleted = false;

        if (playerHead != null)
        {
            lastHeadPosXZ = Flat(playerHead.position);
            hasLastHeadPos = true;
        }

        string folder = Path.Combine(Application.persistentDataPath, "ExperimentLogs");

        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        filePath = Path.Combine(folder, $"ExperimentLog_{sceneName}_{timestamp}.csv");

        File.WriteAllText(filePath,
            "scene,event,timeSinceSceneStart,objectName,deliveredCount,completionTime,totalWalkingDistance,guardianCollisions,teleportCount,wasRedirected,redirectedTeleportCount\n"
        );

        Debug.Log($"Experiment logging to: {filePath}");
    }

    private void Update()
    {
        if (!enableLogging || playerHead == null)
            return;

        Vector3 current = Flat(playerHead.position);

        if (!hasLastHeadPos)
        {
            lastHeadPosXZ = current;
            hasLastHeadPos = true;
            return;
        }

        float distance = Vector3.Distance(lastHeadPosXZ, current);

        // Ignore big jumps caused by teleport
        if (distance < 0.75f)
            totalWalkingDistance += distance;

        lastHeadPosXZ = current;
    }

    public void LogObjectDelivered(string objectName)
    {
        if (!enableLogging || sceneCompleted)
            return;

        deliveredCount++;

        float elapsed = Time.time - sceneStartTime;
        float completionTime = deliveredCount >= 5 ? elapsed : -1f;

        WriteRow(
            "object_delivered",
            elapsed,
            objectName,
            deliveredCount,
            completionTime,
            "N/A"
        );

        if (deliveredCount >= 5)
        {
            sceneCompleted = true;

            WriteRow(
                "scene_completed",
                elapsed,
                "",
                deliveredCount,
                completionTime,
                "N/A"
            );
        }
    }

    public void LogGuardianCollision()
    {
        if (!enableLogging || sceneCompleted)
            return;

        guardianCollisions++;

        WriteRow(
            "guardian_collision",
            Time.time - sceneStartTime,
            "",
            deliveredCount,
            -1f,
            "N/A"
        );
    }

    public void LogTeleport(string wasRedirected)
    {
        if (!enableLogging || sceneCompleted)
            return;

        teleportCount++;

        if (wasRedirected.Equals("Redirected"))
            redirectedTeleportCount++;

        WriteRow(
            "teleport",
            Time.time - sceneStartTime,
            "",
            deliveredCount,
            -1f,
            wasRedirected
        );
        if (playerHead != null)
        {
            lastHeadPosXZ = Flat(playerHead.position);
            hasLastHeadPos = true;
        }
    }

    private void WriteRow(
    string eventName,
    float elapsed,
    string objectName,
    int delivered,
    float completionTime,
    string redirectStatus)
    {
        string row =
            $"{sceneName}," +
            $"{eventName}," +
            $"{elapsed:F3}," +
            $"{objectName}," +
            $"{delivered}," +
            $"{completionTime:F3}," +
            $"{totalWalkingDistance:F3}," +
            $"{guardianCollisions}," +
            $"{teleportCount}," +
            $"{redirectStatus}," +
            $"{redirectedTeleportCount}\n";

        File.AppendAllText(filePath, row);
    }

    private Vector3 Flat(Vector3 v)
    {
        return new Vector3(v.x, 0f, v.z);
    }
}