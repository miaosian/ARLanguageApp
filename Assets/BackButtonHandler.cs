using UnityEngine;
using UnityEngine.SceneManagement;

public class BackButtonHandler : MonoBehaviour
{
    public string targetSceneName = "ProgressHome";

    public void GoBackToHome()
    {
        SceneManager.LoadScene(targetSceneName);
    }
}
