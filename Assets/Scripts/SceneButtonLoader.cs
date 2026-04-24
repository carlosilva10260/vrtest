using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneButtonLoader : MonoBehaviour
{
    public void LoadTrainingScene()
    {
        SceneManager.LoadScene("Training Scene");
    }

    public void LoadTaskScene()
    {
        SceneManager.LoadScene("Teste1");
    }

    public void QuitApplication()
    {
        Application.Quit();


    }
}