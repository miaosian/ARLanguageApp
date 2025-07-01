using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneNavigator : MonoBehaviour
{
    public void LoadLoginScene()
    {
        SceneManager.LoadScene("Login"); 
    }

    public void LoadRegisterScene()
    {
        SceneManager.LoadScene("Register"); 
    }

    public void LoadMainScene()
    {
        SceneManager.LoadScene("ProgressHome"); 
    }

    public void LoadScanScene()
    {
        SceneManager.LoadScene("Scan"); 
    }

    public void LoadScanSampleScene()
    {
        SceneManager.LoadScene("scan2"); 
    }

    public void LoadBackMain()
    {
        SceneManager.LoadScene("ProgressHome"); 
    }

    public void LoadChallengesScene()
    {
        SceneManager.LoadScene("Challenge"); 
    }

    public void LoadChallengesQuestionScene()
    {
        SceneManager.LoadScene("Cha_enter"); 
    }

    public void LoadContextualScene()
    {
        SceneManager.LoadScene("contextual"); 
    }

    public void LoadContextSimulationScene()
    {
        SceneManager.LoadScene("Contextual2"); 
    }

    public void LoadSettingScene()
    {
        SceneManager.LoadScene("Setting"); 
    }

    public void LoadPasswordScene()
    {
        SceneManager.LoadScene("password"); 
    }
}
