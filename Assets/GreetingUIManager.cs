using UnityEngine;

public class GreetingUIManager : MonoBehaviour
{
    public GameObject greetingPrompt;
    public GameObject objectDetectionManager; // your script or GameObject controlling detection

    private void Start()
    {
        // Disable detection system at start
        objectDetectionManager.SetActive(false);
    }

    public void HideGreeting()
    {
        if (greetingPrompt != null)
        {
            greetingPrompt.SetActive(false);
            objectDetectionManager.SetActive(true);

        }
    }
}
