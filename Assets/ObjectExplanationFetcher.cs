using UnityEngine;
using TMPro;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using GoodEnough.TextToSpeech;
using UnityEngine.UI;


public class ObjectExplanationFetcher : MonoBehaviour
{
    public GameObject objectInfoPanel;
    public TextMeshProUGUI ObjectNameText;
    public TextMeshProUGUI ObjectDescriptionText;
    public Button CloseButton;
    public GameObject panel;

    private DatabaseReference dbRef;
    private string currentLabel = "";

    void Start()
    {
        var voices = GoodEnough.TextToSpeech.TTS.AllAvailableVoices;
        Debug.Log("✅ TTS initialized with " + voices.Count + " voices.");

        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task => {
            if (task.Result == DependencyStatus.Available)
            {
                dbRef = FirebaseDatabase.DefaultInstance.RootReference;
                Debug.Log("✅ Firebase initialized for object explanation!");
            }
            else
            {
                Debug.LogError("❌ Firebase not available: " + task.Result);
            }
        });

        CloseButton.onClick.AddListener(HidePanel); // ✅ hook close
    }

    public void ShowObjectInfo(string label)
    {
        if (dbRef == null)
        {
            Debug.LogWarning("🚫 Firebase dbRef is null — skipping ShowObjectInfo");
            return;
        }

        currentLabel = label;
        panel.SetActive(true);

        dbRef.Child("ar_object").Child(label.ToLower()).GetValueAsync().ContinueWithOnMainThread(task => {
            if (task.IsFaulted || !task.IsCompleted)
            {
                Debug.LogError("Failed to fetch from Firebase: " + task.Exception);
                return;
            }

            DataSnapshot snapshot = task.Result;

            if (snapshot.Exists)
            {
                string name = snapshot.HasChild("name") ? snapshot.Child("name").Value.ToString() : label;
                string desc = snapshot.HasChild("description") ? snapshot.Child("description").Value.ToString() : "No description.";

                ObjectNameText.text = name;
                ObjectDescriptionText.text = desc;
            }
            else
            {
                ObjectNameText.text = label.ToUpper();
                ObjectDescriptionText.text = "No description found for this object.";
            }
        });
    }

    public void HidePanel()
    {
        panel.SetActive(false);
        FindFirstObjectByType<ARObjectFetcher>()?.ClearSpawnedObject();

        // ✅ Resume scanning
        FindFirstObjectByType<VisionRecognizer>().ResumeScanning();
    }


    public void PlayPronunciation()
    {
        if (!string.IsNullOrEmpty(currentLabel))
        {
            Debug.Log("🔊 Speaking: " + currentLabel);
            try
            {
                GoodEnough.TextToSpeech.TTS.Speak(currentLabel.Trim());
            }
            catch (System.Exception ex)
            {
                Debug.LogError("TTS Failed: " + ex.Message);
            }
        }
    }
}


