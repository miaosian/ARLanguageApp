using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Firebase.Database;      // ← for DatabaseReference, DataSnapshot, SetValueAsync, etc.
using Firebase.Extensions;    // ← for ContinueWithOnMainThread
using TMPro;
using System.Text;            // for Encoding.UTF8
using UnityEngine.Networking; // for UnityWebRequest
using UnityEngine.SceneManagement;
using Firebase.Auth;          // ← for FirebaseAuth

[System.Serializable]
public struct PrefabEntry
{
    public string key;       // e.g. "backpack"
    public GameObject prefab;// drag-in your BackpackPrefab here
}

[System.Serializable]
class RecognitionConfig {
    public string encoding = "LINEAR16";
    public int    sampleRateHertz;
    public string languageCode = "en-US";
}
[System.Serializable]
class RecognitionAudio {
    public string content;
}
[System.Serializable]
class SpeechRequest {
    public RecognitionConfig config;
    public RecognitionAudio  audio;
}
[System.Serializable]
class SpeechResponse {
    public Result[] results;
    [System.Serializable]
    public class Result {
        public Alternative[] alternatives;
        [System.Serializable]
        public class Alternative {
            public string transcript;
            public float  confidence;
        }
    }
}


public class ChallengeManager : MonoBehaviour
{
    private FirebaseAuth      auth;
    private DatabaseReference dbRoot;
    private string            uid;

    struct RoundResult { public string word; public bool correct; }
    List<RoundResult> sessionResults;

    const string GCLOUD_SPEECH_URL =
      "https://speech.googleapis.com/v1/speech:recognize?key=AIzaSyAfNPvWmrYgXBg9iXSsO27Jk_ysck2FRzA";

    // ─── CONFIG ────────────────────────────────────────
    [Header("Firebase Paths")]
    public string objectsPath    = "ar_object";
    public string templatePath   = "challenges/template";
    public string hitsPath       = "hits";        // boolean “mastered”
    public string hitsCountPath  = "hitsCount";   // integer count per object

    [Header("Per-vocab Hits To Master")]
    public int hitsToMaster = 10;

    [Header("UI Panels (CanvasGroups)")]
    public CanvasGroup splashPanel;     // “Get Ready!” + instructions
    public CanvasGroup countdownPanel;  // “3…2…1…”
    public CanvasGroup gamePanel;       // AR camera + object + input + timer + attempts
    public CanvasGroup nextPanel;       // “Next!” between rounds
    public CanvasGroup summaryPanel;    // Final summary
    public CanvasGroup sessionSummaryPanel; // End-of-session summary

    [Header("UI Elements")]
    public TMP_Text instructionsText;
    public TMP_Text countdownText;
    public TMP_InputField answerInput;
    public Slider timerBar;                 // drives each frame
    public TMP_Text attemptsText;
    public TMP_Text summaryText;
    public TMP_Text feedbackText;

    [Header("Spawn & Timing")]
    public Transform  spawnPoint;       
    public float      fadeDuration = 0.5f;
    public float      splashDuration = 2f;
    public int        countdownStart = 3;
    public float      timeLimitSeconds = 30f;  

    [Header("Buttons & Feedback")]
    public Button       submitButton;
    public Button       speakButton;

    [Header("Speech-to-Text (Cloud)")]
    public int    recordFrequency = 16000;      // 16 kHz for Cloud STT
    public float  maxRecordTime   = 4f;         // seconds per utterance

    AudioClip      recordedClip;
    bool isRecording = false;
    bool isPronunciationMode = false;

    [Header("Session Summary UI")]
    public Transform     resultsContent;       // drag in ScrollView→Viewport→Content
    public GameObject    resultRowPrefab;      // drag in ResultRow prefab
    public Button        newSessionButton;
    public Button        exitButton;
    public Sprite        correctSprite;
    public Sprite        incorrectSprite;

    [Header("Session Settings")]
    public int roundsPerSession = 10;

    int currentRoundIndex = 0;              
    bool waitingForTapToContinue = false;

    // ─── STATE ────────────────────────────────────────
    enum State { Splash, Countdown, InGame, Feedback, Next, Summary }
    State currentState;

    // tracking
    string currentKey;                  
    GameObject currentInstance;
    Dictionary<string,int> hits = new Dictionary<string,int>();
    List<string> pool = new List<string>();

    // firebase refs
    DatabaseReference objRef, tmplRef;

    [Header("Prefab Mapping (key → prefab)")]
    public PrefabEntry[] prefabEntries;
    private Dictionary<string, GameObject> prefabMap;

    // ─── NEW FIELD: timerCoroutine reference ─────────────────────────────
    private Coroutine timerCoroutine;


    void Awake()
    {
        // build the dictionary for quick lookups
        prefabMap = prefabEntries.ToDictionary(e => e.key, e => e.prefab);

        // ensure all panels start hidden
        splashPanel.alpha           = 0;
        countdownPanel.alpha        = 0;
        gamePanel.alpha             = 0;
        nextPanel.alpha             = 0;
        summaryPanel.alpha          = 0;
        sessionSummaryPanel.alpha   = 0;

        // make sure UI panels don’t block touches when hidden
        splashPanel.blocksRaycasts         = false;
        countdownPanel.blocksRaycasts      = false;
        gamePanel.blocksRaycasts           = false;
        nextPanel.blocksRaycasts           = false;
        summaryPanel.blocksRaycasts        = false;
        sessionSummaryPanel.blocksRaycasts = false;

        submitButton.gameObject.SetActive(false);
        speakButton.gameObject.SetActive(false);
        feedbackText.gameObject.SetActive(false);

        sessionResults = new List<RoundResult>();

        auth = FirebaseAuth.DefaultInstance;
        dbRoot = FirebaseDatabase.DefaultInstance.RootReference;
        if (auth.CurrentUser != null)
            uid = auth.CurrentUser.UserId;
    }


    void Start()
    {
        // 1) Firebase refs
        objRef  = FirebaseDatabase.DefaultInstance.GetReference(objectsPath);
        tmplRef = FirebaseDatabase.DefaultInstance.GetReference(templatePath);

        // ───► 1.a) Load fully‐mastered booleans for this user
        if (!string.IsNullOrEmpty(uid))
        {
            // 1.a.i) Load boolean “hits” (fully mastered)
            dbRoot.Child(hitsPath)
                  .Child(uid)
                  .GetValueAsync()
                  .ContinueWithOnMainThread(task =>
                  {
                      if (task.Exception != null)
                      {
                          Debug.LogError("[ChallengeManager] Failed to load /hits/" + uid + ": " + task.Exception);
                      }
                      else
                      {
                          DataSnapshot hitsSnap = task.Result;
                          if (hitsSnap.Exists)
                          {
                              foreach (var child in hitsSnap.Children)
                              {
                                  string objectKey = child.Key;
                                  hits[objectKey] = hitsToMaster;
                              }
                          }
                      }
                      // 1.a.ii) Then load partial hit counts (if any)
                      LoadPartialHitCounts();
                  });
        }
        else
        {
            // No user signed in: just proceed (hits remains empty)
            StartCoroutine(EnterSplash());
        }

        // 2) Hook the input submission
        submitButton.onClick.AddListener(OnSubmitButton);
        speakButton.onClick.AddListener(OnSpeakButton);

        newSessionButton.onClick.AddListener(() => {
            sessionResults.Clear();
            ResetSession();
            StartCoroutine(Fade(sessionSummaryPanel, 1, 0));
            StartCoroutine(EnterSplash());
        });
        exitButton.onClick.AddListener(() => {
            SceneManager.LoadScene("ProgressHome");
        });
    }

    /// <summary>
    /// After loading /hits (fully mastered), also load /hitsCount to restore partial counts.
    /// </summary>
    void LoadPartialHitCounts()
    {
        dbRoot.Child(hitsCountPath)
              .Child(uid)
              .GetValueAsync()
              .ContinueWithOnMainThread(task =>
        {
            if (task.Exception != null)
            {
                Debug.LogError("[ChallengeManager] Failed to load /hitsCount/" + uid + ": " + task.Exception);
            }
            else
            {
                DataSnapshot countSnap = task.Result;
                if (countSnap.Exists)
                {
                    foreach (var child in countSnap.Children)
                    {
                        string objectKey = child.Key;
                        int storedCount = 0;
                        int.TryParse(child.Value?.ToString(), out storedCount);
                        // Only overwrite if not fully mastered already
                        if (!hits.ContainsKey(objectKey) || hits[objectKey] < hitsToMaster)
                        {
                            hits[objectKey] = Mathf.Clamp(storedCount, 0, hitsToMaster);
                        }
                    }
                }
            }
            // Only now start the gameplay flow:
            StartCoroutine(EnterSplash());
        });
    }


    void SpawnPrefab(string objectKey)
    {
        if (!prefabMap.TryGetValue(objectKey, out var prefab))
        {
            Debug.LogError($"No prefab for '{objectKey}'");
            return;
        }

        // Always spawn half a meter in front of the AR camera:
        var cam = Camera.main.transform;
        Vector3 pos = cam.position + cam.forward * 0.5f;
        Quaternion rot = cam.rotation;

        // Clean up previous
        if (currentInstance != null) Destroy(currentInstance);

        currentInstance = Instantiate(prefab, pos, rot);
        currentInstance.AddComponent<TouchRotate>();
    }


    #region ─── SCENE FLOW COROUTINES ────────────────────
    IEnumerator EnterSplash()
    {
        currentState = State.Splash;
        yield return Fade(splashPanel, 0, 1);
        instructionsText.text = "Get ready!\nType the name of the object you see.";
        yield return new WaitForSeconds(splashDuration);
        yield return Fade(splashPanel, 1, 0);

        StartCoroutine(DoCountdown());
    }

    IEnumerator DoCountdown()
    {
        currentState = State.Countdown;
        yield return Fade(countdownPanel, 0, 1);
        for (int i = countdownStart; i > 0; i--)
        {
            countdownText.text = i.ToString();
            yield return new WaitForSeconds(1f);
        }
        yield return Fade(countdownPanel, 1, 0);

        // build our pool from Firebase, but only for the chosen category
        LoadObjectPool();
    }

    void LoadObjectPool()
    {
        //-------------------------------------------------------------
        // ◀ CHANGE #1: Filter pool by the “ChosenCategoryObjects” JSON.
        //-------------------------------------------------------------
        string jsonList = PlayerPrefs.GetString("ChosenCategoryObjects", "");
        List<string> chosenKeys = new List<string>();

        if (!string.IsNullOrEmpty(jsonList))
        {
            var wrapper = JsonUtility.FromJson<SerializableStringList>(jsonList);
            chosenKeys = wrapper != null && wrapper.items != null
                         ? wrapper.items
                         : new List<string>();
        }

        // If nothing passed in, fallback to ALL /ar_object keys.
        if (chosenKeys.Count == 0)
        {
            objRef.GetValueAsync().ContinueWithOnMainThread(task =>
            {
                if (task.Exception != null)
                {
                    Debug.LogError("[ChallengeManager] Failed to load /ar_object: " + task.Exception);
                    ShowSummary();
                    return;
                }
                pool.Clear();
                foreach (var child in task.Result.Children)
                {
                    string key = child.Key;
                    if (!prefabMap.ContainsKey(key)) continue;
                    if (!hits.ContainsKey(key)) hits[key] = 0;
                    if (hits[key] < hitsToMaster)
                        pool.Add(key);
                }
                if (pool.Count == 0) ShowSummary();
                else              StartCoroutine(StartRound());
            });
        }
        else
        {
            pool.Clear();
            foreach (string key in chosenKeys)
            {
                if (!prefabMap.ContainsKey(key)) continue;
                if (!hits.ContainsKey(key)) hits[key] = 0;
                if (hits[key] < hitsToMaster)
                    pool.Add(key);
            }
            if (pool.Count == 0) ShowSummary();
            else              StartCoroutine(StartRound());
        }
    }


    IEnumerator StartRound()
    {
        if (currentRoundIndex >= roundsPerSession)
        {
            ShowResultsPanel();
            yield break;
        }

        currentState = State.InGame;
        waitingForTapToContinue = false;

        //-------------------------------------------------------------
        // ◀ CHANGE #2: Reset timer bar to 30s
        //-------------------------------------------------------------
        timerBar.maxValue = timeLimitSeconds;
        timerBar.value    = timeLimitSeconds;

        // Randomize mode each round:
        isPronunciationMode = (Random.value > 0.5f);
        feedbackText.gameObject.SetActive(false);

        // 2️⃣ Pick a new word
        currentKey = pool[Random.Range(0, pool.Count)];

        // 3️⃣ Spawn it
        SpawnPrefab(currentKey);

        // 4️⃣ Reset UI
        answerInput.gameObject.SetActive(!isPronunciationMode);
        submitButton.gameObject.SetActive(!isPronunciationMode);
        speakButton.gameObject.SetActive(isPronunciationMode);

        // 5️⃣ Fade in the game panel
        yield return Fade(gamePanel, 0, 1);

        // 6️⃣ Start exactly one timer
        if (timerCoroutine != null) StopCoroutine(timerCoroutine);
        timerCoroutine = StartCoroutine(RunTimer());
    }

IEnumerator RunTimer()
{
    float t = timeLimitSeconds;
    while (t > 0 && currentState == State.InGame)
    {
        t -= Time.deltaTime;
        timerBar.value = t;
        yield return null;
    }

    if (currentState != State.InGame)
        yield break;

    // ─── NEW: Treat timeout as a wrong answer ───────────────
    currentState = State.Feedback;
    if (timerCoroutine != null)
        StopCoroutine(timerCoroutine);

    feedbackText.text = "⏰ Time’s up!";
    feedbackText.gameObject.SetActive(true);
    yield return new WaitForSeconds(1f);
    feedbackText.gameObject.SetActive(false);

    // Instead of fading out and showing the summary, just call OnWrong():
    OnWrong();
}


void OnSubmitAnswer(string userInput)
{
    if (currentState != State.InGame) return;

    string normalizedInput = userInput.Trim().ToLower().Replace(" ", "").Replace("_", "");
    string normalizedKey   = currentKey.ToLower().Replace("_", "");

    bool correct = normalizedInput == normalizedKey;

    if (correct) OnCorrect();
    else         OnWrong();
}


    void OnCorrect() => StartCoroutine(Feedback(true));
    void OnWrong()   => StartCoroutine(Feedback(false));

    IEnumerator Feedback(bool correct)
    {
        currentState = State.Feedback;

        if (timerCoroutine != null) StopCoroutine(timerCoroutine);

        // 1️⃣ Adjust hits count (and persist)
        int oldCount = hits.ContainsKey(currentKey) ? hits[currentKey] : 0;
        if (correct)
            hits[currentKey] = Mathf.Min(oldCount + 1, hitsToMaster);
        else if (oldCount > 0)
            hits[currentKey] = Mathf.Max(oldCount - 1, 0);

        // 1.a) Write new hit count to Firebase under /hitsCount/{uid}/{currentKey}
        if (!string.IsNullOrEmpty(uid))
        {
            dbRoot.Child(hitsCountPath)
                  .Child(uid)
                  .Child(currentKey)
                  .SetValueAsync(hits[currentKey])
                  .ContinueWithOnMainThread(countTask =>
            {
                if (countTask.Exception != null)
                    Debug.LogError($"Failed to write hit count for '{currentKey}': {countTask.Exception}");
                else
                    Debug.Log($"Wrote hit count {hits[currentKey]} to /hitsCount/{uid}/{currentKey}");
            });

            // 1.b) If it just reached mastery, write boolean under /hits/{uid}/{currentKey}
            if (hits[currentKey] >= hitsToMaster)
            {
                dbRoot.Child(hitsPath)
                      .Child(uid)
                      .Child(currentKey)
                      .SetValueAsync(true)
                      .ContinueWithOnMainThread(dbTask =>
                {
                    if (dbTask.Exception != null)
                        Debug.LogError($"Failed to write mastery flag for '{currentKey}': {dbTask.Exception}");
                    else
                        Debug.Log($"Mastered '{currentKey}' → wrote true to /hits/{uid}/{currentKey}");
                });
            }
        }

        // 2️⃣ Show feedback message
        feedbackText.text = correct ? "✅ Correct!" : "❌ Wrong!";
        feedbackText.gameObject.SetActive(true);

        // 3️⃣ Update attempts-remaining label
        int remain = hitsToMaster - hits[currentKey];
        attemptsText.text = $"Attempts remaining: {remain}";

        sessionResults.Add(new RoundResult {
            word = currentKey,
            correct = correct
        });

        // 4️⃣ If we’ve done enough rounds, go straight to summary
        if (sessionResults.Count >= roundsPerSession)
        {
            feedbackText.gameObject.SetActive(false);
            yield return Fade(gamePanel, 1, 0);
            ShowResultsPanel();
            yield break;
        }

        // 5️⃣ If word just mastered, remove from future pool
        if (hits[currentKey] >= hitsToMaster)
            pool.Remove(currentKey);

        // 6️⃣ Pause so user can read feedback
        yield return new WaitForSeconds(0.8f);

        feedbackText.gameObject.SetActive(false);
        yield return Fade(gamePanel, 1, 0);

        // 7️⃣ Show “Tap to Continue” if more rounds remain
        waitingForTapToContinue = true;
        feedbackText.text = "→ Tap screen to continue ←";
        feedbackText.gameObject.SetActive(true);

        while (waitingForTapToContinue)
            yield return null;

        feedbackText.gameObject.SetActive(false);
        currentRoundIndex++;
        StartCoroutine(StartRound());
    }

    void ShowSummary()
    {
        currentState = State.Summary;
        summaryText.text = $"All done!\nMastered {hits.Keys.Count(k => hits[k] >= hitsToMaster)}/{hits.Count} words.";
        StartCoroutine(Fade(summaryPanel, 0, 1));
    }

    #endregion

    #region ─── UTILITIES ────────────────────────────────
    IEnumerator Fade(CanvasGroup cg, float from, float to)
    {
        float t = 0f;
        cg.alpha = from;
        cg.blocksRaycasts = to > 0.5f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            cg.alpha = Mathf.Lerp(from, to, t / fadeDuration);
            yield return null;
        }
        cg.alpha = to;
    }

    void UpdateAttemptsText()
    {
        int remain = hitsToMaster - hits[currentKey];
        attemptsText.text = $"Hits to master: {remain}";
    }
    #endregion

    void OnDestroy()
    {
        // Already persisted hits during gameplay; no additional action needed here.
    }

    public void OnSubmitButton()
    {
        OnSubmitAnswer(answerInput.text);
        answerInput.DeactivateInputField();  // dismiss keyboard
    }

    public void OnSpeakButton()
    {
        Debug.Log("🔊 OnSpeakButton() called, starting record…");

        if (isRecording) return;
        isRecording = true;
        feedbackText.text = "🎙️ Recording...";
        feedbackText.gameObject.SetActive(true);

        recordedClip = Microphone.Start(
            null,            // default device 
            false,           // one-shot
            Mathf.CeilToInt(maxRecordTime),
            recordFrequency
        );
        Invoke(nameof(StopRecording), maxRecordTime);
    }

    void StopRecording()
    {
        if (!isRecording) return;
        Microphone.End(null);
        isRecording = false;
        feedbackText.text = "⌛ Processing...";
        StartCoroutine(SendToSpeechAPI(recordedClip));
    }

    IEnumerator SendToSpeechAPI(AudioClip clip)
    {
        // 1) Encode + base64
        var wavBytes = WaveEncoder.FromAudioClip(clip);
        string b64    = System.Convert.ToBase64String(wavBytes);

        // 2) Build request
        var reqObj = new SpeechRequest {
            config = new RecognitionConfig { sampleRateHertz = clip.frequency },
            audio  = new RecognitionAudio  { content = b64 }
        };
        string json = JsonUtility.ToJson(reqObj);

        // 3) POST
        using var uw = new UnityWebRequest(GCLOUD_SPEECH_URL, "POST") {
            uploadHandler   = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json)),
            downloadHandler = new DownloadHandlerBuffer()
        };
        uw.SetRequestHeader("Content-Type", "application/json");
        yield return uw.SendWebRequest();

        // 4) Handle response
        if (uw.result != UnityWebRequest.Result.Success)
        {
            feedbackText.text = "⚠️ STT Error";
            yield break;
        }
        var resp = JsonUtility.FromJson<SpeechResponse>(uw.downloadHandler.text);
        if (resp.results != null && resp.results.Length > 0)
        {
            string transcript = resp.results[0].alternatives[0].transcript;
            // Show the raw transcript
            feedbackText.text = $"You said:\n“{transcript}”";
            feedbackText.gameObject.SetActive(true);

            yield return new WaitForSeconds(1f);

            feedbackText.gameObject.SetActive(false);
            OnSubmitAnswer(transcript);
        }
        else
        {
            //-----------------------------------------------------------
            // ◀ CHANGE #4: “No speech detected” now counts as WRONG
            //-----------------------------------------------------------
            feedbackText.text = "❌ No speech detected";
            feedbackText.gameObject.SetActive(true);
            yield return new WaitForSeconds(1f);
            feedbackText.gameObject.SetActive(false);

            // Treat as wrong and move on
            OnWrong();
        }
    }

    void Update()
    {
        if (waitingForTapToContinue && currentState == State.Feedback)
        {
            #if UNITY_EDITOR
            if (Input.GetMouseButtonDown(0) && Input.mousePosition.x > Screen.width/2)
            {
                waitingForTapToContinue = false;
            }
            #else
            if (Input.touchCount > 0)
            {
                var t = Input.GetTouch(0);
                if (t.phase == TouchPhase.Began && t.position.x > Screen.width/2)
                    waitingForTapToContinue = false;
            }
            #endif
        }
    }

    void ShowResultsPanel()
    {
        // 1) Clear out any old rows
        foreach (Transform child in resultsContent)
            Destroy(child.gameObject);

        // 2) Instantiate one row per RoundResult
        foreach (var r in sessionResults)
        {
            var row = Instantiate(resultRowPrefab, resultsContent);

            // — find & set the word text
            var txtTr = row.transform.Find("WordText");
            if (txtTr == null)
            {
                Debug.LogError("ResultRow prefab is missing a child named 'WordText'!");
                continue;
            }
            var txt = txtTr.GetComponent<TMP_Text>();
            txt.text = r.word;

            // — find & set the ✓/✕ icon
            var iconTr = row.transform.Find("ResultIcon");
            if (iconTr == null)
            {
                Debug.LogError("ResultRow prefab is missing a child named 'ResultIcon'!");
                continue;
            }
            var iconImg = iconTr.GetComponent<Image>();
            if (iconImg == null)
            {
                Debug.LogError("'ResultIcon' has no Image component!");
                continue;
            }

            var spr = r.correct ? correctSprite : incorrectSprite;
            if (spr == null)
            {
                Debug.LogError($"Sprite for {(r.correct ? "correct" : "incorrect")} is null!");
                continue;
            }

            iconImg.enabled = true;
            iconImg.sprite  = spr;
            iconImg.SetNativeSize();
            Debug.Log($"Assigned sprite '{spr.name}' to ResultIcon for word '{r.word}'");
        }

        // 3) Fade in the full-screen session summary panel
        StartCoroutine(Fade(sessionSummaryPanel, 0, 1));
    }

    void ResetSession()
    {
        currentRoundIndex = 0;
        sessionResults.Clear();
        sessionSummaryPanel.alpha = 0;
        sessionSummaryPanel.blocksRaycasts = false;
    }

    void OnNewChallenges()
    {
        sessionResults.Clear();
        StartCoroutine(EnterSplash());
        StartCoroutine(Fade(sessionSummaryPanel, 1, 0));
    }

    void OnExitChallenges()
    {
        SceneManager.LoadScene("MainMenu");
    }


    /// <summary>
    /// Helper class for JSON‐serializing a List<string> into PlayerPrefs.
    /// (Used by the Categories‐to‐Challenges handoff.)
    /// </summary>
    [System.Serializable]
    public class SerializableStringList
    {
        public List<string> items;
        public SerializableStringList(List<string> list) { items = list; }
    }
}
