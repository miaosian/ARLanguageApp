using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Database;
using Firebase.Extensions;
using UnityEngine.Networking;

public class ContextSelectionManager : MonoBehaviour
{
    public ScrollRect scrollRect;
    [Header("Firebase Path")]
    public string contextTypesPath = "contextTypes";

    [Header("UI References")]
    public CanvasGroup contextSelectionPanel;   // fills the screen
    public Transform    contextScrollContent;   // ScrollView→Viewport→Content
    public GameObject   contextCardPrefab;      // prefab with a Button + child TMP_Text("NameText")

    [Header("Scene Flow")]
    public ScenarioDetailManager scenarioDetailManager;  // assign in Inspector

    [Header("Fade Settings")]
    public float fadeDuration = 0.5f;

    // runtime
    private List<ContextType> contexts = new List<ContextType>();

    void Awake()
    {
        // Ensure the selection panel is visible at start
        contextSelectionPanel.alpha = 1;
        contextSelectionPanel.blocksRaycasts = true;
        contextSelectionPanel.transform.SetAsLastSibling();

    }

    void Start()
    {
        LoadContextsFromFirebase();

    // Immediately resume if PlayerPrefs are set
    if (PlayerPrefs.HasKey("ResumeContext") && PlayerPrefs.HasKey("ResumeScenarioId"))
    {
        string ctxKey = PlayerPrefs.GetString("ResumeContext");
        string scId   = PlayerPrefs.GetString("ResumeScenarioId");
        PlayerPrefs.DeleteKey("ResumeContext");
        PlayerPrefs.DeleteKey("ResumeScenarioId");

        // Wait until Firebase has populated `contexts` list, then:
        ContextType ct = contexts.FirstOrDefault(c => c.key == ctxKey);
        if (ct != null && ct.scenarios.TryGetValue(scId, out ScenarioData sd))
        {
            Debug.Log($"[ContextSelection] Auto‐resuming scenario '{scId}' in context '{ctxKey}'");
            // fade out selection panel, show detail immediately
            StartCoroutine(Fade(contextSelectionPanel, 1, 0, () => {
                scenarioDetailManager.ShowScenario(sd);
            }));
        }
    }
    }

    void LoadContextsFromFirebase()
{
    var dbRef = FirebaseDatabase.DefaultInstance
                 .GetReference(contextTypesPath);
    dbRef.GetValueAsync()
         .ContinueWithOnMainThread(task => {
            var snap = task.Result;
            contexts.Clear();

            foreach (var child in snap.Children)
            {
                string contextKey = child.Key;

                // 1) Read “name” and “imageUrl” exactly as before:
                    var ct = new ContextType();
                    ct.key      = contextKey;
                    ct.name     = child.Child("name").Value.ToString();
                    ct.imageUrl = child.Child("imageUrl")?.Value?.ToString() ?? "";

                    // 2) Build the dictionary of ScenarioData, but also fill contextName & id:
                    ct.scenarios = new Dictionary<string, ScenarioData>();
                    var scenariosNode = child.Child("scenarios");

                    foreach (var sc in scenariosNode.Children)
                    {
                        string scenarioKey = sc.Key;
                        // Deserialize JSON into a brand‐new ScenarioData:
                        ScenarioData s = JsonUtility.FromJson<ScenarioData>(sc.GetRawJsonValue());

                        // ←── RIGHT HERE: assign contextName & id before storing:
                        s.contextName = contextKey;
                        s.id          = scenarioKey;

                        ct.scenarios.Add(scenarioKey, s);
                    }

                    contexts.Add(ct);
                    Debug.Log($"[Context] loaded {ct.key} / {ct.name} with {ct.scenarios.Count} scenarios");
                }

            Debug.Log($"[Context] total contexts = {contexts.Count}");
            PopulateContextCards();
        });
}

IEnumerator LoadCardImage(string url, Image target)
{
    if (string.IsNullOrEmpty(url))
        yield break;
    using var uw = UnityWebRequestTexture.GetTexture(url);
    yield return uw.SendWebRequest();
    if (uw.result == UnityWebRequest.Result.Success)
    {
        var tex = DownloadHandlerTexture.GetContent(uw);
        var sprite = Sprite.Create(
            tex,
            new Rect(0, 0, tex.width, tex.height),
            new Vector2(0.5f, 0.5f)
        );
        target.sprite = sprite;
    }
    else Debug.LogWarning($"Image load failed: {uw.error}");
}

void PopulateContextCards()
{
    foreach (Transform t in contextScrollContent) Destroy(t.gameObject);

    foreach (var ct in contexts)
    {
        var card = Instantiate(contextCardPrefab, contextScrollContent);
        card.transform.localScale = Vector3.one;

        var ctrl = card.GetComponent<ContextCardController>();
        ctrl.nameText.text    = ct.name;
        StartCoroutine(LoadCardImage(ct.imageUrl, ctrl.contentImage));

        var btn = card.GetComponent<Button>();
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => OnContextCardClicked(ct.key));
    }

    Debug.Log($"[Context] Populated {contexts.Count} cards.");
}


/// <summary>
/// Called when the user taps a ContextCard.
/// </summary>
/// <param name="contextKey">The Firebase key for the selected context.</param>
    void OnContextCardClicked(string contextKey)
{
    Debug.Log($"Tapped context: {contextKey}");
    // fade out...
    StartCoroutine(Fade(contextSelectionPanel, 1, 0, () => {
        // pick random scenario & show detail...
        var ct = contexts.First(c => c.key == contextKey);
        var rndKey = ct.scenarios.Keys
                        .ElementAt(UnityEngine.Random.Range(0, ct.scenarios.Count));
        scenarioDetailManager.ShowScenario(ct.scenarios[rndKey]);
    }));
}



    void OnContextSelected(ContextType ct)
    {
        // Fade out the selection panel, then show the scenario detail
        StartCoroutine(Fade(contextSelectionPanel, 1, 0, () =>
        {
            // pick a random scenario from this context
            var keys = ct.scenarios.Keys.ToList();
            var randKey = keys[UnityEngine.Random.Range(0, keys.Count)];
            var scenario = ct.scenarios[randKey];

            // hand off to the ScenarioDetailManager
            scenarioDetailManager.ShowScenario(scenario);
        }));
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
}
