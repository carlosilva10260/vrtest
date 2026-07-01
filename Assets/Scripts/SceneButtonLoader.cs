using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneButtonLoader : MonoBehaviour
{
    public void LoadTrainingScene()
    {
        SceneManager.LoadScene("Training Scene");
    }

    public void LoadMenuScene()
    {
        SceneManager.LoadScene("Main Menu");
    }

    public void LoadRedirectScene()
    {
        SceneManager.LoadScene("AutoRedirectScene");
    }
    public void LoadPreviewScene()
    {
        SceneManager.LoadScene("TeleportPreviewScene");
    }

    public void LoadBaselineScene()
    {
        SceneManager.LoadScene("BaselineScene");
    }

    public void LoadTrainingRedirectScene()
    {
        SceneManager.LoadScene("TrainingAutoRedirectScene");
    }
    public void LoadTrainingPreviewScene()
    {
        SceneManager.LoadScene("TrainingTeleportPreviewScene");
    }

    public void LoadTrainingBaselineScene()
    {
        SceneManager.LoadScene("TrainingBaselineScene");
    }

    public void QuitApplication()
    {
        Application.Quit();


    }
}