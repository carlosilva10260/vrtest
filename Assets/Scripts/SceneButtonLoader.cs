using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneButtonLoader : MonoBehaviour
{
    public void LoadTrainingScene()
    {
        SceneManager.LoadScene("Training Scene");
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

    public void QuitApplication()
    {
        Application.Quit();


    }
}