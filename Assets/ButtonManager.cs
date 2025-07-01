using UnityEngine;

public class ButtonManager : MonoBehaviour
{
    public GameObject translationPanel;

    public void OnTranslateButtonClicked()
    {
        translationPanel.SetActive(true);
    }

    public void OnScanButtonClicked()
    {
        Debug.Log("Scan button clicked. Scanning for objects...");
    }
}
