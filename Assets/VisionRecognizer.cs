using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;
using System.Text;

public class VisionRecognizer: MonoBehaviour
{
    public TextMeshProUGUI ResultText;
    public string apiKey = "AIzaSyBdwFTwACqnuviXfUICTHyPid3u5XmuKyo"; 
    private bool isScanningAllowed = true; // 🔍 Controls whether detection is active



    void Start()
    {
       
        InvokeRepeating("AnalyzeImage", 2f, 5f);
    }

    void AnalyzeImage()
    {
        if (isScanningAllowed)
            StartCoroutine(SendImageToGoogle());
    }

public void ResumeScanning()
{
    Debug.Log("✅ Resuming object detection.");
    isScanningAllowed = true;
}



IEnumerator SendImageToGoogle()
{
    Debug.Log("Starting Vision API call");

    yield return new WaitForEndOfFrame(); // wait for full rendered frame

    //Texture2D snap = ScreenCapture.CaptureScreenshotAsTexture(); // take screenshot from AR Camera
    yield return new WaitForEndOfFrame();
    Texture2D snap = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
    snap.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
    snap.Apply();


    byte[] imageBytes = snap.EncodeToJPG();
    string base64 = System.Convert.ToBase64String(imageBytes);
    Destroy(snap); // clean up memory

    string jsonRequest = "{\"requests\": [{\"image\": {\"content\":\"" + base64 + "\"},\"features\": [{\"type\": \"OBJECT_LOCALIZATION\", \"maxResults\": 1}]}]}";

    byte[] postData = Encoding.UTF8.GetBytes(jsonRequest);

    UnityWebRequest request = new UnityWebRequest("https://vision.googleapis.com/v1/images:annotate?key=" + apiKey, "POST");
    request.uploadHandler = new UploadHandlerRaw(postData);
    request.downloadHandler = new DownloadHandlerBuffer();
    request.SetRequestHeader("Content-Type", "application/json");

    yield return request.SendWebRequest();

    if (request.result == UnityWebRequest.Result.Success)
    {
        if (request.downloadHandler == null)
        {
            Debug.LogError("Download handler is null!");
            yield break;
        }

        string jsonResult = request.downloadHandler.text;

        Debug.Log("Google Vision response: " + jsonResult);

        //string label = ParseLabel(jsonResult);
        //label = label.ToLower();
string label = ParseLabel(jsonResult).ToLower();
string firebaseKey = label.Replace(" ", "_");

        if (ResultText != null)
        {
            ResultText.text = $"Detected: {label}";

            isScanningAllowed = false; // ❌ Pause further detection

            // Spawn 3D object from Firebase
            FindFirstObjectByType<ARObjectFetcher>().TrySpawnFromFirebase(firebaseKey);
            // ✅ Assign label to tap spawner script instead of spawning immediately
            FindFirstObjectByType<ObjectExplanationFetcher>().ShowObjectInfo(firebaseKey);
        }
        else
        {
            Debug.LogError("ResultText is null!");
        }
    }
    else
    {
        Debug.LogError("Request failed: " + request.error);
    }
}


    string ParseLabel(string json)
    {
        string key = "\"name\": \"";
        int start = json.IndexOf(key) + key.Length;
        int end = json.IndexOf("\"", start);
        if (start > key.Length && end > start)
        {
            string found = json.Substring(start, end - start);
            Debug.Log("✅ Detected label from Vision API: " + found);
            return found;
        }
        Debug.LogWarning("❌ Failed to parse label from Vision JSON");
        return "Unknown";
    }
}