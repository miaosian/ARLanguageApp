using System;              // for Convert, Action, BitConverter…
using System.Text;         // for Encoding.UTF8
using UnityEngine;         // for AudioSource, AudioClip
using UnityEngine.UI;      // for Image, Button
using UnityEngine.Networking;  // for UnityWebRequest
using TMPro;               // for TMP_Text
using System.Collections;  // for IEnumerator
using System.Collections.Generic;
using System.Linq;
using Firebase.Auth;           // ← for FirebaseAuth
using Firebase.Database;       // ← for DatabaseReference
using Firebase.Extensions; 

[Serializable]
public class ChatRequest {
    public string model;
    public ChatMessage[] messages;
    public int max_tokens;
    public float temperature;
}

[Serializable]
public class ChatResponse
{
    public Choice[] choices;
    [Serializable]
    public class Choice {
        public ChatMessage message;
    }
}

[Serializable]
class TTSRequest {
  public Input input;
  public Voice  voice;
  public AudioConfig audioConfig;
  [Serializable] public class Input { public string text; }
  [Serializable] public class Voice { public string languageCode="en-US"; public string name="en-US-Wavenet-D"; }
  [Serializable] 
	public class AudioConfig {
  // ask for raw WAV PCM so WavUtility can decode it
  public string audioEncoding   = "LINEAR16";
  public int    sampleRateHertz = 16000;
}
}
[Serializable]
class TTSResponse { public string audioContent; 
}

public class ConversationManager : MonoBehaviour
{

// ———————— recording state ————————
    private bool isRecording = false;
    private AudioClip recordedClip;
    private FirebaseAuth auth;
    private DatabaseReference dbRoot;
    private ScenarioData       currentScenario;
    private const int RequiredTurnCount = 20;
    private const float RequiredAvgConfidence = 0.80f;

    private List<float>        speechConfidences = new List<float>();

    [Header("UI Panels")]
    public CanvasGroup conversationCanvasGroup; // your ConversationPanel
    public CanvasGroup contextSelectionCanvasGroup; 
    public Button      backButton; 
    public RawImage    backgroundImage;         // scenario background
    public Transform   chatContent;             // ScrollView→Viewport→Content
    public GameObject  userBubblePrefab;       // small panel + TMP_Text
    public GameObject  botBubblePrefab;        // small panel + TMP_Text
    //[Header("Scroll View")]
    //public ScrollRect scrollRect;    // ← drag your Scroll View here in the Inspector


    [Header("Nav Buttons")]
    public Button endButton;   // left: end conversation
    public Button micButton;   // middle: record
    public Button doneButton;  // right: finish

    [Header("Fade Settings")]
    public float fadeDuration = 0.5f;
    
    [Header("Recording Hint UI")]
    public TMP_Text recordingHintText;

    [Header("Panels & Context")]
    public CanvasGroup   contextSelectionPanel;  // drag your Context‐selection CanvasGroup here

    [Header("End Confirmation Dialog")]
    public CanvasGroup   confirmEndDialog;      
    public Button        confirmEndYesButton;
    public Button        confirmEndNoButton;

    [Header("Session Summary UI")]
    public CanvasGroup   summaryPanel;           // your “Session Summary” CanvasGroup
    public Transform     summaryContent;         // ScrollView→Viewport→Content
    public GameObject    summaryRowPrefab;       // prefab with a TMP_Text for a single line
    public TMP_Text      totalTurnsText;
    public TMP_Text      totalWordsText;
    public TMP_Text      avgConfidenceText;
    public TMP_Text      badgeText;
    public Button        restartButton;          // “Restart Scenario”
    public Button        backToContextsButton;   // “Back to Contexts”
    public Texture       aiSpeakerTexture;          // icon to use for assistant rows
    public Texture       userSpeakerTexture;        // icon to use for user rows

    [Header("AI & TTS Settings")]
    [Tooltip("Your OpenAI Chat API key")]
    public string openAIKey;

    [Tooltip("Your Google Cloud TTS API key")]
    public string ttsApiKey;
    [Tooltip("Your Google Cloud Speech-to-Text API key")]
    public string sttApiKey;

    // AudioSource for playing TTS replies
    public AudioSource ttsAudioSource;

    // Maximum words per assistant reply
    public int maxWordsPerReply = 15;

    [Header("Speech-to-Text Settings")]
    [Tooltip("Sample rate for microphone recording (Hz)")]
    public int recordFrequency = 16000;   // must match your STT config
    [Tooltip("Max seconds per user utterance")]
    public float maxRecordTime   = 4f;

    // conversation state
    List<ChatMessage> history = new List<ChatMessage>();

    void Awake()
    {
        // hide at startup
        conversationCanvasGroup.alpha = 0;
        conversationCanvasGroup.blocksRaycasts = false;

        // hook nav buttons
        endButton.onClick.AddListener(OnEndConversation);
        doneButton.onClick.AddListener(OnDoneConversation);
        micButton.onClick.AddListener(OnMicPressed);
        backButton.onClick.AddListener(OnBackToContext);

        // hide the new panels/dialogs
        confirmEndDialog.alpha          = 0;
        confirmEndDialog.blocksRaycasts = false;
        summaryPanel.alpha              = 0;
        summaryPanel.blocksRaycasts     = false;

        // wire up the buttons
        confirmEndYesButton.onClick.AddListener(ConfirmEndYes);
        confirmEndNoButton .onClick.AddListener(ConfirmEndNo);
        restartButton       .onClick.AddListener(OnRestartScenario);
        backToContextsButton.onClick.AddListener(OnBackToContexts);

    // ───► Initialize Firebase Auth & DatabaseReference ◀───
    auth   = FirebaseAuth.DefaultInstance;
    dbRoot = FirebaseDatabase.DefaultInstance.RootReference;
 

    }

    IEnumerator Start()
    {
        // 1) ask for microphone access
        if (!Application.HasUserAuthorization(UserAuthorization.Microphone))
        {
            yield return Application.RequestUserAuthorization(UserAuthorization.Microphone);
            if (!Application.HasUserAuthorization(UserAuthorization.Microphone))
            {
                Debug.LogError("Microphone permission denied; STT will not work.");
                yield break;
            }
        }

        // 2) now it’s safe to hook your mic button
        micButton.onClick.AddListener(OnMicPressed);

    }


    /// <summary>
    /// Called by ScenarioDetailManager after Start Conversation.
    /// </summary>
    public void StartConversation(ScenarioData scenario)
    {

        currentScenario     = scenario;
        speechConfidences.Clear();

        // 1) fade in the panel, load BG as before…
        StartCoroutine(Fade(conversationCanvasGroup, 0, 1));
        StartCoroutine(LoadBg(scenario.backgroundUrl));

        // 2) clear history and seed with system + word‐limit instruction
        history.Clear();
        history.Add(new ChatMessage {
            role    = "system",
            content = scenario.personaPrompt
                  + "\n\nPlease start the conversation by greeting the user, "
                  + "and keep it under " + maxWordsPerReply + " words."
            });

        // 3) immediately request the AI’s first turn
        StartCoroutine(RequestAI(""));  // empty user message means “you start”
    }

    IEnumerator RequestAI(string userMessage)
    {
        if (botBubblePrefab == null)
        {
            Debug.LogError("ConversationManager.botBubblePrefab is not assigned!");
            yield break;
        }
        if (chatContent == null)
        {
            Debug.LogError("ConversationManager.chatContent is not assigned!");
            yield break;
        }
        if (ttsAudioSource == null)
        {
            Debug.LogWarning("No ttsAudioSource assigned; skipping TTS playback.");
        }

        Debug.Log($"[RequestAI] called with userMessage → “{userMessage}”");

        // 1️⃣ add user message if present
        if (!string.IsNullOrWhiteSpace(userMessage))
            history.Add(new ChatMessage { role = "user", content = userMessage });
        else
            Debug.LogWarning("[RequestAI] userMessage was empty or whitespace");

        // 2️⃣ build a serializable request
        var req = new ChatRequest {
          model       = "gpt-4o-mini", 
          messages    = history.ToArray(),
          max_tokens  = 20,
          temperature = 0.7f
        };

        string json = JsonUtility.ToJson(req);
        Debug.Log($"[RequestAI] JSON → {json}");

        // 3️⃣ send
        using var uw = new UnityWebRequest(
            "https://api.openai.com/v1/chat/completions", "POST");
        uw.uploadHandler   = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        uw.downloadHandler = new DownloadHandlerBuffer();
        uw.SetRequestHeader("Content-Type", "application/json");
        uw.SetRequestHeader("Authorization", "Bearer " + openAIKey);
        yield return uw.SendWebRequest();

        // 4️⃣ if error, log full body
        if (uw.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"Chat API error: {uw.error}\n{uw.downloadHandler.text}");
            yield break;
        }

        // 5️⃣ parse
        var resp    = JsonUtility.FromJson<ChatResponse>(uw.downloadHandler.text);
        string aiText = resp.choices[0].message.content.Trim();
        Debug.Log($"[RequestAI] reply → “{aiText}”");
        // bail early if OpenAI gave us nothing
        if (string.IsNullOrEmpty(aiText))
        {
            Debug.LogWarning("OpenAI returned an empty message, skipping bubble & TTS.");
            yield break;
        }

        history.Add(new ChatMessage { role = "assistant", content = aiText });

        // 6️⃣ show & speak
        SpawnBotMessage(aiText);
        // only start TTS if we have an AudioSource
        if (ttsAudioSource != null)
        StartCoroutine(SynthesizeSpeech(aiText));
        // scroll to bottom
        Canvas.ForceUpdateCanvases();
        var sr = GetComponentInChildren<ScrollRect>();  // or cache a reference
        sr.verticalNormalizedPosition = 0f;
    }



    IEnumerator LoadBg(string url)
    {
        using (var uw = UnityWebRequestTexture.GetTexture(url))
        {
            yield return uw.SendWebRequest();
            if (uw.result == UnityWebRequest.Result.Success)
            {
                backgroundImage.texture = DownloadHandlerTexture.GetContent(uw);
            }
            else
            {
                Debug.LogError($"[Conversation] BG load failed: {uw.error}");
            }
        }
    }

    public void OnMicPressed()
    {
        if (isRecording) return;
        Debug.Log("[Mic] Attempting to start recording… devices: " + string.Join(",", Microphone.devices));


        // 1️⃣ Start recording…
        isRecording = true;
        recordedClip = Microphone.Start(
            null, false,
            Mathf.CeilToInt(maxRecordTime),
            recordFrequency
        );

        if (recordedClip == null)
            Debug.LogError("[Mic] Microphone.Start returned null!");
        else
            Debug.Log($"[Mic] Recording started, clip length={recordedClip.samples} samples");

        // 2️⃣ Show hint
        recordingHintText.text = "Recording… speak now";
        recordingHintText.gameObject.SetActive(true);

        // 3️⃣ Stop after timeout
        Invoke(nameof(StopRecording), maxRecordTime);
    }

    void StopRecording()
    {
        // 1️⃣ Stop mic
        if (!isRecording) return;
        Microphone.End(null);
        isRecording = false;

        // 2️⃣ Update hint
        recordingHintText.text = "Processing…";
        Debug.Log("[Mic] Stopped recording; clip.samples=" + (recordedClip?.samples ?? 0));

        // 3️⃣ Kick off STT
        StartCoroutine(SendToSpeechAPI(recordedClip));
    }


    IEnumerator SendToSpeechAPI(AudioClip clip)
    {
        Debug.Log("[STT] ▶ Entering SendToSpeechAPI");

        // 1) Guard: did we actually record anything?
        if (clip == null || clip.samples == 0)
        {
            Debug.LogError("[STT] No audio captured—aborting STT call.");
            recordingHintText.gameObject.SetActive(false);
            yield break;
        }

        // 2) Encode to WAV and log length
        var wav = WaveEncoder.FromAudioClip(clip);
        Debug.Log($"[STT] WAV bytes length = {wav.Length}");

        string b64 = Convert.ToBase64String(wav);

        // 3) Build the request
        var req = new SpeechRequest {
            config = new RecognitionConfig {
            encoding        = "LINEAR16",
            sampleRateHertz = clip.frequency,
            languageCode    = "en-US"
          },
        audio = new RecognitionAudio {
        content = Convert.ToBase64String(wav)
        }
        };
        string reqJson = JsonUtility.ToJson(req);
        Debug.Log($"[STT] Request JSON → {reqJson.Substring(0, Math.Min(reqJson.Length,200))}…");

        // 4) Fire the web request
        using var uw = new UnityWebRequest(
          "https://speech.googleapis.com/v1/speech:recognize?key=" + sttApiKey,
          "POST");
        uw.uploadHandler   = new UploadHandlerRaw(Encoding.UTF8.GetBytes(reqJson));
        uw.downloadHandler = new DownloadHandlerBuffer();
        uw.SetRequestHeader("Content-Type","application/json");
        yield return uw.SendWebRequest();

        // 5) Check network errors
        if (uw.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[STT] Network error: {uw.error}\nBody: {uw.downloadHandler.text}");
            SpawnBotMessage("⚠️ STT Error");
            recordingHintText.gameObject.SetActive(false);
            yield break;
        }

        // 6) Log raw response
        string body = uw.downloadHandler.text;
        Debug.Log($"[STT] Raw response → {body}");

         // 7) Parse
        var resp = JsonUtility.FromJson<SpeechResponse>(body);
        if (resp.results == null || resp.results.Length == 0)
        {
            Debug.LogWarning("[STT] No results array in response");
            SpawnBotMessage("❌ I didn’t hear anything");
            recordingHintText.gameObject.SetActive(false);
            yield break;
        }

        // 8) Extract transcript
        var alts = resp.results[0].alternatives;
        if (alts == null || alts.Length == 0 || string.IsNullOrWhiteSpace(alts[0].transcript))
        {
            Debug.LogWarning("[STT] No transcript found in alternatives");
            SpawnBotMessage("❌ Sorry, couldn’t transcribe");
            recordingHintText.gameObject.SetActive(false);
            yield break;
        }
        var firstAlt = alts[0];
        speechConfidences.Add(firstAlt.confidence);
        string transcript = firstAlt.transcript.Trim();
        Debug.Log($"[STT] Transcript → “{transcript}”");

        // 9) Show user bubble
        SpawnUserBubble(transcript);
        recordingHintText.gameObject.SetActive(false);

        // 10) Send that to AI
        StartCoroutine(RequestAI(transcript));
    }

    /// <summary>
    /// Synthesizes speech from text and plays it.
    /// </summary>
IEnumerator SynthesizeSpeech(string text)
{
    var req = new TTSRequest {
      input = new TTSRequest.Input { text = text },
      voice = new TTSRequest.Voice(),
      audioConfig = new TTSRequest.AudioConfig()
    };
    string json = JsonUtility.ToJson(req);

    using var uw = new UnityWebRequest(
      "https://texttospeech.googleapis.com/v1/text:synthesize?key=" + ttsApiKey,
      "POST");
    uw.uploadHandler   = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
    uw.downloadHandler = new DownloadHandlerBuffer();
    uw.SetRequestHeader("Content-Type","application/json");
    yield return uw.SendWebRequest();

    if (uw.result != UnityWebRequest.Result.Success)
    {
        Debug.LogError("TTS error: " + uw.error);
        yield break;
    }

    var resp = JsonUtility.FromJson<TTSResponse>(uw.downloadHandler.text);
    if (string.IsNullOrEmpty(resp.audioContent))
    {
        Debug.LogWarning("TTS returned empty audioContent—skipping playback.");
        yield break;
    }
 byte[] wavBytes = Convert.FromBase64String(resp.audioContent);
    AudioClip clip;
    try {
      clip = WavUtility.ToAudioClip(wavBytes, "tts");
    }
    catch(Exception e) {
      Debug.LogWarning("Failed to parse WAV: " + e);
      yield break;
    }

    ttsAudioSource.PlayOneShot(clip);
}


    void StartRecordingAndTranscribe()
    {
        // **STUB**: in your real code, start Microphone,
        // encode WAV, send to STT, then call OnUserResponse(transcript)
        // Here we'll simulate:
        Invoke(nameof(SimulateUserResponse), 2f);
    }

    void SimulateUserResponse()
    {
        OnUserResponse("This is a test user response.");
    }

    void OnUserResponse(string text)
    {
        // add to history & UI
        history.Add(new ChatMessage { role = "user", content = text });
        var userBubble = Instantiate(userBubblePrefab, chatContent);
        userBubble.GetComponentInChildren<TMP_Text>().text = text;

        // then send to AI
        SendToAI(text);
    }

    async void SendToAI(string userText)
    {
        // **STUB**: integrate your AI call here.
        // For now, echo back:
        await System.Threading.Tasks.Task.Delay(1000);
        var reply = $"You said: {userText}";
        history.Add(new ChatMessage { role = "assistant", content = reply });

        var botBubble = Instantiate(botBubblePrefab, chatContent);
        botBubble.GetComponentInChildren<TMP_Text>().text = reply;
    }

    /// <summary>
    /// User tapped the “End” (left) button in‐panel.
    /// </summary>
    void OnEndConversation()
    {
        // show confirmation modal
        confirmEndDialog.alpha          = 1;
        confirmEndDialog.blocksRaycasts = true;
    }

    void ConfirmEndYes()
    {
        // hide the modal…
        confirmEndDialog.alpha          = 0;
        confirmEndDialog.blocksRaycasts = false;
        // 1) Save current progress snapshot → /progress/{uid}
        SaveLatestProgressToFirebase();
        ClearConversation();

        // …then fade out conversation and return to contexts
        StartCoroutine(Fade(conversationCanvasGroup, 1, 0, () => {
          contextSelectionPanel.alpha          = 1;
          contextSelectionPanel.blocksRaycasts = true;
        }));
    }

    void ConfirmEndNo()
    {
        // simply hide the modal
        confirmEndDialog.alpha          = 0;
        confirmEndDialog.blocksRaycasts = false;
    }

    /// <summary>
    /// User tapped the “Done” (right) button in‐panel.
    /// </summary>
    void OnDoneConversation()
    {
        // fade out the live convo…
        StartCoroutine(Fade(conversationCanvasGroup, 1, 0));
        // …and show the summary overlay
        ShowSessionSummary();
    }

    void ShowSessionSummary()
    {
        // 1) clear out any old lines
        foreach (Transform t in summaryContent)
            Destroy(t.gameObject);

        // 2) one row per chat‐turn (skip the system prompt if you like)
        // skip the system-role prompt; start at the first assistant (AI) bubble
            foreach (var msg in history.Skip(1))
            {
                var row = Instantiate(summaryRowPrefab, summaryContent);

                // find & set the text
                var txt = row.transform.Find("MessageText")
                       .GetComponent<TMP_Text>();
                txt.text = msg.content;

                // — set the speaker icon
                var raw = row.transform.Find("SpeakerIcon")
                       .GetComponent<RawImage>();
                raw.texture = (msg.role=="assistant")
                     ? aiSpeakerTexture
                     : userSpeakerTexture;
               }


        // 3) quick stats
        int turns      = history.Count - 1; // minus the system line
        int wordsSpoken = history
            .Where(m => m.role == "user")
            .Sum(m => m.content.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length);
        float avgConf = speechConfidences.Count > 0
            ? speechConfidences.Average()
            : 0f;

        totalTurnsText    .text = $"Turns: {turns}";
        totalWordsText    .text = $"Your words: {wordsSpoken}";
        avgConfidenceText .text = $"Accuracy: {avgConf:P0}";

        // — badge logic —
            if (turns >= RequiredTurnCount && avgConf >= RequiredAvgConfidence)
            {
                // gold badge
                badgeText.text = "Scenario completed!";
            }
            else
            {
                // progress badge
                badgeText.text =
                  $"{turns}/{RequiredTurnCount} turns, avg {avgConf:P0}";
            }

        // 5) fade it up
        summaryPanel.alpha          = 1;
        summaryPanel.blocksRaycasts = true;
    }

    void OnRestartScenario()
    {
        // hide summary, then restart same scenario
        ClearConversation();
        summaryPanel.alpha          = 0;
        summaryPanel.blocksRaycasts = false;
        StartConversation(currentScenario);
    }

    void OnBackToContexts()
    {
        // 1) Save current progress snapshot → /progress/{uid}
        SaveLatestProgressToFirebase();

        // 2) Hide summary, then go back to context‐picker
        summaryPanel.alpha              = 0;
        summaryPanel.blocksRaycasts     = false;
        contextSelectionPanel.alpha     = 1;
        contextSelectionPanel.blocksRaycasts = true;
        ClearConversation();

    }

    IEnumerator Fade(CanvasGroup cg, float from, float to, Action onComplete = null)
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
        onComplete?.Invoke();
    }

void SpawnBotMessage(string text)
{
  Debug.Log($"[SpawnBot ] BEFORE: {chatContent.childCount} bubbles");

    if (botBubblePrefab == null) {
        Debug.LogError("SpawnBotMessage: botBubblePrefab is null!");
        return;
    }
    if (chatContent == null) {
        Debug.LogError("SpawnBotMessage: chatContent (parent) is null!");
        return;
    }
    //var botBubble = Instantiate(botBubblePrefab);
    //botBubble.transform.SetParent(chatContent, /*worldPositionStays=*/false);
    var botBubble = Instantiate(botBubblePrefab, chatContent);
    botBubble.transform.localScale = Vector3.one;
var img = botBubble.GetComponent<Image>();
Debug.Log($"[SpawnBot] prefab root has Image? {img != null}");
if (img != null)
    Debug.Log($"[SpawnBot] sprite = {img.sprite?.name}, enabled = {img.enabled}, alpha = {img.color.a}");

    var tmp = botBubble.GetComponentInChildren<TMP_Text>();
    if (tmp == null)
    {
        Debug.LogError("SpawnBotMessage: your botBubblePrefab has no child with a TMP_Text component!");
    }
    else
    {
        tmp.text = text;
    }

    // 5) Optional: force the ScrollRect to scroll to show it
    var sr = chatContent.GetComponentInParent<ScrollRect>();
    if (sr != null)
    {
        Canvas.ForceUpdateCanvases();
        sr.verticalNormalizedPosition = 0f;  // bottom
    }
  Debug.Log($"[SpawnBot ] AFTER: {chatContent.childCount} bubbles");

    //ScrollToBottom();

}


void SpawnUserBubble(string text)
{
  Debug.Log($"[SpawnUser] BEFORE: {chatContent.childCount} bubbles");

    if (userBubblePrefab == null) {
        Debug.LogError("SpawnUserBubble: userBubblePrefab is null!");
        return;
    }
    if (chatContent == null) {
        Debug.LogError("SpawnUserBubble: chatContent (parent) is null!");
        return;
    }

  //var userBubble = Instantiate(userBubblePrefab);
  //userBubble.transform.SetParent(chatContent, false);
    var userBubble = Instantiate(userBubblePrefab, chatContent);
    userBubble.transform.localScale = Vector3.one;

    var tmp = userBubble.GetComponentInChildren<TMP_Text>();
    if (tmp == null) {
        Debug.LogError("SpawnUserBubble: no TMP_Text in userBubblePrefab!");
    } else {
        tmp.text = text;
    }
  Debug.Log($"[SpawnUser] AFTER: {chatContent.childCount} bubbles");

    //ScrollToBottom();
}

  void OnBackToContext() {
    // fade conversation out, then fade context selection back in
    StartCoroutine(BackToContextRoutine());
  }

  IEnumerator BackToContextRoutine() {
    // 1) hide convo panel
    yield return Fade(conversationCanvasGroup, 1, 0);
    // 2) show context-selection panel
    yield return Fade(contextSelectionCanvasGroup, 0, 1);
  }

void ClearConversation()
{
    // 1) Clear the stored history
    history.Clear();

    // 2) Destroy every child under the chatContent (all bubbles)
    foreach (Transform child in chatContent)
        Destroy(child.gameObject);
}

    /// <summary>
    /// Returns the total number of {user + bot} turns so far 
    /// (excluding the initial system prompt).  Also returns average STT confidence.
    /// </summary>
    private void GetCurrentProgressMetrics(out int turnCount, out float avgConfidence)
    {
        // “turnCount” is simply: history.Count − 1 (skip the system prompt)
        // but if you want only “user” turns (and matching bot turns), you can do:
        turnCount = Mathf.Max(0, history.Count - 1);

        if (speechConfidences.Count == 0)
        {
            avgConfidence = 0f;
        }
        else
        {
            avgConfidence = speechConfidences.Average();
        }
    }

    /// <summary>
    /// Writes one “latest progress” record under /progress/{uid} in Firebase.
    /// We store: contextName, scenarioId, turns count, average confidence.
    /// </summary>
    private void SaveLatestProgressToFirebase()
    {
        if (auth.CurrentUser == null)
        {
            Debug.LogWarning("[ConversationManager] SaveLatestProgress: no user is signed in.");
            return;
        }

        string uid = auth.CurrentUser.UserId;
        // 1) Compute metrics
        GetCurrentProgressMetrics(out int turns, out float avgConf);

        // 2) Build a simple object; you can expand with timestamp if you like
        var progressData = new Dictionary<string, object>()
        {
            { "context",       currentScenario.contextName ?? "" },
            { "scenarioId",    currentScenario.id ?? "" },
            { "turnCount",     turns },
            { "avgConfidence", avgConf }
        };

        // 3) Write it at /progress/{uid}
        dbRoot
          .Child("progress")
          .Child(uid)
          .SetValueAsync(progressData)
          .ContinueWithOnMainThread(task =>
          {
              if (task.Exception != null)
              {
                  Debug.LogError($"[ConversationManager] Failed to save progress: {task.Exception}");
              }
              else
              {
                  Debug.Log($"[ConversationManager] Saved latest progress for uid={uid}: turns={turns}, avgConf={avgConf:P0}");
              }
          });
    }



}

