using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using UnityEngine.SceneManagement;

// ─── Reuse the existing PrefabEntry from ChallengeManager (key, prefab) ───
public class ProgressHomeManager : MonoBehaviour
{
    // ─── PUBLIC INSPECTOR FIELDS ─────────────────────────────────────────

    [Header("Greeting")]
    public TMP_Text welcomeText;

    [Header("Scroll Section")]
    public ScrollRect   scrollRect;
    public RectTransform content;
    public RectTransform challengesPanel;
    public RectTransform contextPanel;

    [Header("Prefabs")]
    public GameObject categoryRowPrefab;   // CategoryProgressRow.prefab
    public GameObject contextRowPrefab;    // ContextProgressRow.prefab

    [Header("Challenges Panel Container")]
    public Transform categoryProgressContainer;

    [Header("Context Panel Container")]
    public Transform contextProgressContainer;

    [Header("Mastered Words Section")]
    public TMP_Text   masteredHeaderText;
    public Button     showAllButton;
    public CanvasGroup masteredPopup;
    public Transform  masteredContent;        // popup → ScrollRect → Viewport → Content
    public GameObject masteredWordRowPrefab;  // prefab with “WordText” ± “InspectButton”
    public Transform  masteredPreviewContainer;
    public Button     popupCloseButton;       // assign to the “X” inside the popup

    [Header("Preview Display (UI)")]
    public RawImage previewDisplay;

    [Header("Firebase Paths")]
    public string hitsPath       = "hits";
    public string progressPath   = "progress";
    public string arObjectPath   = "ar_object";
    public string contextsPath   = "contextTypes";
    public string lastChallengePath = "lastChallenge";

    [Header("Object Prefabs (drag-in)")]
    public List<PrefabEntry> objectPrefabs;   // from ChallengeManager: (key, prefab)
    private Dictionary<string, GameObject> prefabMap;

    [System.Serializable]
    public struct PrefabIconEntry
    {
        public string  key;         // e.g. “electronics”
        public Sprite  iconSprite;  // drag your Sprite here
    }

    [Header("Category Icons (drag-in)")]
    public List<PrefabIconEntry> categoryIcons;
    private Dictionary<string, Sprite> _iconMap;

    // If no recent category → show this fallback:
    [Header("Default Resources")]
    public Sprite defaultCategoryIcon;

    // ─── PRIVATE STATE ───────────────────────────────────────────────────
    private FirebaseAuth      auth;
    private DatabaseReference dbRoot;
    private string            uid;
    private string            lastCategoryKey;     // will now come from Firebase “lastChallenge/{uid}/category”
    private string            lastContextName;
    private string            lastScenarioId;
    private GameObject        currentPreviewInstance;

    // ──────────────────────────────────────────────────────────────────────
    void Awake()
    {
        Debug.Log("[ProgressHome] Awake(): Initializing Firebase/auth…");
        auth   = FirebaseAuth.DefaultInstance;
        dbRoot = FirebaseDatabase.DefaultInstance.RootReference;
        previewDisplay.gameObject.SetActive(false);

        if (auth.CurrentUser == null)
        {
            Debug.LogWarning("[ProgressHome] No authenticated user; redirecting to Login.");
            SceneManager.LoadScene("Login");
            return;
        }

        uid = auth.CurrentUser.UserId;
        Debug.Log($"[ProgressHome] CurrentUser: {auth.CurrentUser.Email} (UID: {uid})");

        // Wire up “View All” in mastered words
        showAllButton.onClick.AddListener(ShowMasteredPopup);
        Debug.Log("[ProgressHome] showAllButton listener attached.");

        // Wire up the popup’s Close button
        if (popupCloseButton != null)
        {
            popupCloseButton.onClick.AddListener(HideMasteredPopup);
            Debug.Log("[ProgressHome] popupCloseButton listener attached.");
        }
        else
        {
            Debug.LogWarning("[ProgressHome] popupCloseButton is null! Assign it in the Inspector.");
        }

        masteredPopup.alpha = 0;
        masteredPopup.blocksRaycasts = false;
        masteredPopup.interactable = false;

        Debug.Log("[ProgressHome] masteredPopup hidden on Awake.");

        // ─── Build icon lookup from category key → Sprite ───────────────────
        _iconMap = new Dictionary<string, Sprite>();
        foreach (var entry in categoryIcons)
        {
            if (!string.IsNullOrEmpty(entry.key) && entry.iconSprite != null)
            {
                string lowerKey = entry.key.Trim().ToLower();
                if (!_iconMap.ContainsKey(lowerKey))
                {
                    _iconMap.Add(lowerKey, entry.iconSprite);
                    Debug.Log($"[ProgressHome] Icon map: added key='{lowerKey}' → sprite='{entry.iconSprite.name}'");
                }
                else
                {
                    Debug.LogWarning($"[ProgressHome] Duplicate icon key: {lowerKey}");
                }
            }
            else
            {
                Debug.LogWarning($"[ProgressHome] Invalid categoryIcons entry: key='{entry.key}', sprite='{entry.iconSprite}'");
            }
        }

        // ─── Build prefab lookup from objectKey → GameObject ───────────────────
        prefabMap = new Dictionary<string, GameObject>();
        foreach (var entry in objectPrefabs)
        {
            if (!string.IsNullOrEmpty(entry.key) && entry.prefab != null)
            {
                string lookupKey = entry.key.Trim().ToLower();
                if (!prefabMap.ContainsKey(lookupKey))
                {
                    prefabMap.Add(lookupKey, entry.prefab);
                    Debug.Log($"[ProgressHome] Prefab map: added key='{lookupKey}' → prefab='{entry.prefab.name}'");
                }
                else
                {
                    Debug.LogWarning($"[ProgressHome] Duplicate PrefabEntry key: {lookupKey}");
                }
            }
            else
            {
                Debug.LogWarning($"[ProgressHome] Invalid objectPrefabs entry: key='{entry.key}', prefab='{entry.prefab}'");
            }
        }
    }

    void Start()
    {
        Debug.Log("[ProgressHome] Start(): Populating UI…");

        // A) Display greeting
        var displayName = auth.CurrentUser.DisplayName;
        if (string.IsNullOrEmpty(displayName))
            displayName = auth.CurrentUser.Email.Split('@')[0];
        welcomeText.text = $"Welcome Back, {displayName}!";
        Debug.Log($"[ProgressHome] Greeting set to: {welcomeText.text}");

        // B) Instead of reading PlayerPrefs, fetch “lastChallenge/{uid}/category” from Firebase:
        LoadLastChallengeFromFirebase();

        // Context panel will be populated once LoadLastContextProgress completes
        LoadLastContextProgress();

        // Mastered words preview can load immediately
        LoadMasteredWordsPreview();
    }

    // ──────────────────────────────────────────────────────────────────────
    #region ─── A) Load Last Challenge from Firebase ─────────────────────────
    void LoadLastChallengeFromFirebase()
    {
        Debug.Log($"[ProgressHome] Fetching /{lastChallengePath}/{uid}/category …");
        dbRoot.Child(lastChallengePath)
              .Child(uid)
              .GetValueAsync()
              .ContinueWithOnMainThread(task =>
        {
            if (task.Exception != null)
            {
                Debug.LogError($"[ProgressHome] Failed to load /{lastChallengePath}/{uid}: {task.Exception}");
                ShowNoCategoryChosen();
                return;
            }

            var snap = task.Result;
            if (!snap.Exists || snap.Value == null)
            {
                Debug.Log("[ProgressHome] No lastChallenge record found; showing placeholder.");
                ShowNoCategoryChosen();
                return;
            }

            // The “category” node under /lastChallenge/{uid}
            string categoryKey = snap.Child("category").Value?.ToString() ?? "";
            if (string.IsNullOrEmpty(categoryKey))
            {
                Debug.Log("[ProgressHome] lastChallenge exists but 'category' is empty; showing placeholder.");
                ShowNoCategoryChosen();
                return;
            }

            lastCategoryKey = categoryKey.Trim().ToLower();
            Debug.Log($"[ProgressHome] Found lastChallenge category='{lastCategoryKey}'. Loading its progress.");

            // Now that we have a valid category, populate the Challenges panel
            LoadCategoryProgress(lastCategoryKey);
        });
    }
    #endregion

    // ──────────────────────────────────────────────────────────────────────
    #region ─── B) Load Last Context Progress ────────────────────────────────
    void LoadLastContextProgress()
    {
        Debug.Log($"[ProgressHome] Fetching /{progressPath}/{uid} for last context…");
        dbRoot.Child(progressPath)
              .Child(uid)
              .GetValueAsync()
              .ContinueWithOnMainThread(task =>
        {
            if (task.Exception != null)
            {
                Debug.LogError($"[ProgressHome] Failed to load /{progressPath}/{uid}: {task.Exception}");
                return;
            }
            var snap = task.Result;
            if (!snap.Exists)
            {
                Debug.Log("[ProgressHome] No context progress found; showing placeholder.");
                ShowNoContextChosen();
                return;
            }

            lastContextName = snap.Child("context").Value?.ToString() ?? "";
            lastScenarioId  = snap.Child("scenarioId").Value?.ToString() ?? "";
            int turns       = Convert.ToInt32(snap.Child("turnCount").Value ?? 0);
            float avgConf   = 0;
            float.TryParse(snap.Child("avgConfidence").Value?.ToString(), out avgConf);

            Debug.Log($"[ProgressHome] Found context='{lastContextName}', scenarioId='{lastScenarioId}', turns={turns}, avgConf={avgConf}");
            PopulateContextPanel(lastContextName, lastScenarioId, turns, avgConf);
        });
    }

    void ShowNoContextChosen()
    {
        Debug.Log("[ProgressHome] ShowNoContextChosen(): clearing and showing placeholder row.");
        foreach (Transform t in contextProgressContainer)
            Destroy(t.gameObject);

        var rowGO = Instantiate(contextRowPrefab, contextProgressContainer);
        rowGO.transform.localScale = Vector3.one;

        var nameTxt = rowGO.transform.Find("ContextNameText").GetComponent<TMP_Text>();
        nameTxt.text = "Context Simulation";
        var turnTxt = rowGO.transform.Find("TurnAvgText").GetComponent<TMP_Text>();
        turnTxt.text = "Tap here to pick a new context";

        var button = rowGO.GetComponent<Button>();
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() =>
        {
            Debug.Log("[ProgressHome] User tapped 'No recent conversation' → loading scene 'contextual'.");
            SceneManager.LoadScene("contextual");
        });
    }

    void PopulateContextPanel(string contextName, string scenarioId, int turns, float avgConf)
    {
        Debug.Log($"[ProgressHome] PopulateContextPanel(): contextName='{contextName}', scenarioId='{scenarioId}', turns={turns}, avgConf={avgConf}");
        foreach (Transform t in contextProgressContainer)
            Destroy(t.gameObject);

        var rowGO = Instantiate(contextRowPrefab, contextProgressContainer);
        rowGO.transform.localScale = Vector3.one;

        var nameTxt = rowGO.transform.Find("ContextNameText").GetComponent<TMP_Text>();
        nameTxt.text = CultureInfoUtility.ToTitleCase(contextName);

        var turnTxt = rowGO.transform.Find("TurnAvgText").GetComponent<TMP_Text>();
        turnTxt.text = $"Turns: {turns}, Avg: {avgConf:P0}";

        var button = rowGO.GetComponent<Button>();
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() =>
        {
            Debug.Log($"[ProgressHome] User tapped context row → saving ResumeContext='{contextName}', ResumeScenarioId='{scenarioId}', loading 'contextual'.");
            PlayerPrefs.SetString("ResumeContext", contextName);
            PlayerPrefs.SetString("ResumeScenarioId", scenarioId);
            PlayerPrefs.Save();

            SceneManager.LoadScene("contextual");
        });
    }
    #endregion

    // ──────────────────────────────────────────────────────────────────────
    #region ─── C) Load Last Chosen Category Progress ───────────────────────
    void LoadCategoryProgress(string categoryKey)
    {
        Debug.Log($"[ProgressHome] LoadCategoryProgress('{categoryKey}') → fetching all /{arObjectPath} …");
        dbRoot.Child(arObjectPath)
              .GetValueAsync()
              .ContinueWithOnMainThread(task =>
        {
            if (task.Exception != null)
            {
                Debug.LogError($"[ProgressHome] Failed to load /{arObjectPath}: {task.Exception}");
                ShowNoCategoryChosen();
                return;
            }

            var snap = task.Result;
            if (!snap.Exists)
            {
                Debug.Log("[ProgressHome] /ar_object is empty; showing no-category placeholder.");
                ShowNoCategoryChosen();
                return;
            }

            // Build a list of all objectKeys under this category
            var objectKeysInCategory = new List<string>();
            foreach (var child in snap.Children)
            {
                var catNode = child.Child("category");
                if (catNode.Exists && catNode.Value.ToString() == categoryKey)
                    objectKeysInCategory.Add(child.Key);
            }
            Debug.Log($"[ProgressHome] Found {objectKeysInCategory.Count} objects under category '{categoryKey}'.");

            // Now fetch /hits/{uid} to see which are mastered
            dbRoot.Child(hitsPath)
                  .Child(uid)
                  .GetValueAsync()
                  .ContinueWithOnMainThread(hitsTask =>
            {
                int masteredCount = 0;
                int totalCount    = objectKeysInCategory.Count;

                if (hitsTask.Exception == null)
                {
                    var hitsSnap = hitsTask.Result;
                    if (hitsSnap.Exists)
                    {
                        foreach (var objKey in objectKeysInCategory)
                        {
                            if (hitsSnap.HasChild(objKey))
                                masteredCount++;
                        }
                    }
                    Debug.Log($"[ProgressHome] {masteredCount}/{totalCount} objects were mastered in category '{categoryKey}'.");
                }
                else
                {
                    Debug.LogError($"[ProgressHome] Failed to load /{hitsPath}/{uid}: {hitsTask.Exception}");
                }

                // Populate a single CategoryProgressRow
                PopulateCategoryPanel(categoryKey, masteredCount, totalCount);
            });
        });
    }

    void ShowNoCategoryChosen()
    {
        Debug.Log("[ProgressHome] ShowNoCategoryChosen(): clearing and showing placeholder row.");
        foreach (Transform t in categoryProgressContainer)
            Destroy(t.gameObject);

        var rowGO = Instantiate(categoryRowPrefab, categoryProgressContainer);
        rowGO.transform.localScale = Vector3.one;

        var nameTxt = rowGO.transform.Find("CategoryNameText").GetComponent<TMP_Text>();
        nameTxt.text = "Challenges";
        var progTxt = rowGO.transform.Find("ProgressText").GetComponent<TMP_Text>();
        progTxt.text = "Tap here to start a challenge";

        var barFill = rowGO.transform.Find("ProgressBarBG/ProgressBarFill").GetComponent<Image>();
        barFill.fillAmount = 0f;

        // Set default icon
        var iconTr = rowGO.transform.Find("Icon");
        if (iconTr != null)
        {
            var iconRaw = iconTr.GetComponent<RawImage>();
            if (iconRaw != null && defaultCategoryIcon != null)
            {
                iconRaw.texture = defaultCategoryIcon.texture;
                iconRaw.color   = Color.white;
                Debug.Log("[ProgressHome] ShowNoCategoryChosen: set defaultCategoryIcon");
            }
        }
        else
        {
            Debug.LogWarning("[ProgressHome] ShowNoCategoryChosen: 'Icon' child not found to set default icon.");
        }

        var button = rowGO.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                Debug.Log("[ProgressHome] User tapped 'No recent challenge' → loading scene 'Challenge'.");
                SceneManager.LoadScene("Challenge");
            });
        }
    }

    void PopulateCategoryPanel(string categoryKey, int masteredCount, int totalCount)
    {
        Debug.Log($"[ProgressHome] PopulateCategoryPanel(): categoryKey='{categoryKey}', mastered={masteredCount}, total={totalCount}");

        foreach (Transform t in categoryProgressContainer)
            Destroy(t.gameObject);

        var rowGO = Instantiate(categoryRowPrefab, categoryProgressContainer);
        rowGO.transform.localScale = Vector3.one;

        // 1) Category name
        var nameTxt = rowGO.transform.Find("CategoryNameText").GetComponent<TMP_Text>();
        nameTxt.text = CultureInfoUtility.ToTitleCase(categoryKey);

        // 2) Progress text
        var progTxt = rowGO.transform.Find("ProgressText").GetComponent<TMP_Text>();
        progTxt.text = $"{masteredCount}/{totalCount} words";

        // 3) Progress bar fill
        var barFill = rowGO.transform.Find("ProgressBarBG/ProgressBarFill").GetComponent<Image>();
        float pct = (totalCount == 0) ? 0f : (float)masteredCount / totalCount;
        barFill.fillAmount = pct;

        // 4) Category icon
        var iconTr = rowGO.transform.Find("Icon");
        if (iconTr != null)
        {
            var iconRaw = iconTr.GetComponent<RawImage>();
            string lookup = categoryKey.Trim().ToLower();
            if (_iconMap.TryGetValue(lookup, out Sprite sprite))
            {
                iconRaw.texture = sprite.texture;
                iconRaw.color   = Color.white;
                Debug.Log($"[ProgressHome] Set icon for '{categoryKey}' from sprite '{sprite.name}'.");
            }
            else
            {
                Debug.LogWarning($"[ProgressHome] No icon found in _iconMap for key='{lookup}'. Hiding icon.");
                iconRaw.color   = new Color(0,0,0,0);
            }
        }
        else
        {
            Debug.LogWarning($"[ProgressHome] Couldn’t find child 'Icon' in {rowGO.name}. Is it named exactly 'Icon' with a RawImage?");
        }

        // 5) Wire up the button for category redirect
        var button = rowGO.GetComponent<Button>();
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() =>
        {
            Debug.Log($"[ProgressHome] User tapped category '{categoryKey}' → saving LastChosenCategory and loading 'Cha_enter'.");
            PlayerPrefs.SetString("LastChosenCategory", categoryKey); // you can still keep this if you want to remember it locally
            PlayerPrefs.Save();
            SceneManager.LoadScene("Cha_enter");
        });
    }
    #endregion

    // ──────────────────────────────────────────────────────────────────────
    #region ─── C) Mastered Words Preview & Popup ──────────────────────────
    void LoadMasteredWordsPreview()
    {
        Debug.Log($"[ProgressHome] LoadMasteredWordsPreview(): fetching /{hitsPath}/{uid}");
        dbRoot.Child(hitsPath)
              .Child(uid)
              .GetValueAsync()
              .ContinueWithOnMainThread(task =>
        {
            if (task.Exception != null)
            {
                Debug.LogError($"[ProgressHome] Failed to load hits for preview: {task.Exception}");
                return;
            }

            var hitsSnap = task.Result;
            if (!hitsSnap.Exists)
            {
                masteredHeaderText.text = "No mastered word.";
                Debug.Log("[ProgressHome] No hits found; showing empty message.");
                return;
            }

            var masteredKeys = new List<string>();
            foreach (var child in hitsSnap.Children)
                masteredKeys.Add(child.Key);

            if (masteredKeys.Count == 0)
            {
                masteredHeaderText.text = "No mastered word.";
                Debug.Log("[ProgressHome] hitsSnap exists but zero children; showing empty message.");
                return;
            }

            masteredHeaderText.text = $"Words Mastered ({masteredKeys.Count})";
            Debug.Log($"[ProgressHome] Found {masteredKeys.Count} mastered words. Populating up to 3 previews.");

            // Clear old previews
            foreach (Transform old in masteredPreviewContainer)
                Destroy(old.gameObject);

            // Show up to 3 preview rows
            int previewCount = Mathf.Min(3, masteredKeys.Count);
            for (int i = 0; i < previewCount; i++)
            {
                string objectKey = masteredKeys[i];
                Debug.Log($"[ProgressHome] Preview row: objectKey='{objectKey}'");
                var previewGO = Instantiate(masteredWordRowPrefab, masteredPreviewContainer);
                previewGO.transform.localScale = Vector3.one;

                var wordTxt = previewGO.transform.Find("WordText").GetComponent<TMP_Text>();
                wordTxt.text = CultureInfoUtility.ToTitleCase(objectKey);

                var rootBtn = previewGO.GetComponent<Button>();
                if (rootBtn != null)
                {
                    rootBtn.onClick.RemoveAllListeners();
                    rootBtn.onClick.AddListener(() =>
                    {
                        Debug.Log($"[ProgressHome] User tapped preview row '{objectKey}' → opening full popup.");
                        ShowMasteredPopup();
                    });
                }
                else
                {
                    var inspectBtn = previewGO.transform.Find("InspectButton")?.GetComponent<Button>();
                    if (inspectBtn != null)
                    {
                        inspectBtn.onClick.RemoveAllListeners();
                        inspectBtn.onClick.AddListener(() =>
                        {
                            Debug.Log($"[ProgressHome] User tapped preview row’s InspectButton for '{objectKey}' → opening full popup.");
                            ShowMasteredPopup();
                        });
                    }
                    else
                    {
                        Debug.LogWarning($"[ProgressHome] previewGO has no Button/InspectButton for '{objectKey}'.");
                    }
                }
            }
        });
    }

    void ShowMasteredPopup()
    {
        Debug.Log("[ProgressHome] ShowMasteredPopup(): clearing existing popup rows and fetching hits again.");

        foreach (Transform t in masteredContent)
            Destroy(t.gameObject);

        dbRoot.Child(hitsPath)
              .Child(uid)
              .GetValueAsync()
              .ContinueWithOnMainThread(task =>
        {
            if (task.Exception != null)
            {
                Debug.LogError($"[ProgressHome] Failed to load hits for popup: {task.Exception}");
                return;
            }

            var hitsSnap = task.Result;
            if (!hitsSnap.Exists)
            {
                Debug.Log("[ProgressHome] No mastered words found; returning without showing popup.");
                return;
            }

            int rowIndex = 0;
            foreach (var child in hitsSnap.Children)
            {
                string objectKey = child.Key;
                Debug.Log($"[ProgressHome] Popup row {rowIndex++}: loading objectKey='{objectKey}' description…");

                dbRoot.Child(arObjectPath)
                      .Child(objectKey)
                      .GetValueAsync()
                      .ContinueWithOnMainThread(objTask =>
                {
                    if (objTask.Exception != null)
                    {
                        Debug.LogError($"[ProgressHome] Failed to fetch /{arObjectPath}/{objectKey}: {objTask.Exception}");
                        return;
                    }

                    var objSnap = objTask.Result;
                    string description = objSnap.Child("description").Value?.ToString() ?? "";
                    Debug.Log($"[ProgressHome] Got description='{description}' for '{objectKey}'. Instantiating popup row.");

                    var rowGO = Instantiate(masteredWordRowPrefab, masteredContent);
                    rowGO.transform.localScale = Vector3.one;

                    var wordTxt = rowGO.transform.Find("WordText")?.GetComponent<TMP_Text>();
                    if (wordTxt == null)
                    {
                        Debug.LogError($"[ProgressHome] Popup row prefab missing 'WordText' child! Prefab name = '{masteredWordRowPrefab.name}'");
                    }
                    else
                    {
                        wordTxt.text = CultureInfoUtility.ToTitleCase(objectKey);
                    }

                    var inspectBtn = rowGO.transform.Find("InspectButton")?.GetComponent<Button>();
                    if (inspectBtn != null)
                    {
                        Debug.Log($"[ProgressHome] Wiring InspectButton on popup row '{objectKey}'.");
                        inspectBtn.onClick.RemoveAllListeners();
                        inspectBtn.onClick.AddListener(() =>
                        {
                            Debug.Log($"[ProgressHome] Popup InspectButton tapped → ShowObjectPreview('{objectKey}').");
                            ShowObjectPreview(objectKey, description);
                        });
                    }
                    else
                    {
                        Debug.LogWarning($"[ProgressHome] No 'InspectButton' found on popup row for '{objectKey}'.");

                        var rootBtn = rowGO.GetComponent<Button>();
                        if (rootBtn != null)
                        {
                            Debug.Log($"[ProgressHome] Wiring root Button on popup row '{objectKey}'.");
                            rootBtn.onClick.RemoveAllListeners();
                            rootBtn.onClick.AddListener(() =>
                            {
                                Debug.Log($"[ProgressHome] Popup row tapped → ShowObjectPreview('{objectKey}').");
                                ShowObjectPreview(objectKey, description);
                            });
                        }
                        else
                        {
                            Debug.LogWarning($"[ProgressHome] Popup row prefab has no 'InspectButton' or root Button! objectKey='{objectKey}'");
                        }
                    }
                });
            }

            Debug.Log("[ProgressHome] Fading in masteredPopup (alpha=1, blocksRaycasts=true, interactable=true).");
            masteredPopup.alpha = 1;
            masteredPopup.blocksRaycasts = true;
            masteredPopup.interactable = true;
        });
    }

    void HideMasteredPopup()
    {
        Debug.Log("[ProgressHome] HideMasteredPopup(): hiding popup, destroying preview model.");
        masteredPopup.alpha = 0;
        masteredPopup.blocksRaycasts = false;
        masteredPopup.interactable = false;
        DestroyCurrentPreview();
        previewDisplay.gameObject.SetActive(false);
    }

    void ShowObjectPreview(string objectKey, string description)
    {
        Debug.Log($"[ProgressHome] ShowObjectPreview('{objectKey}'): fetching prefabName…");

        dbRoot.Child(arObjectPath)
              .Child(objectKey)
              .Child("prefabName")
              .GetValueAsync()
              .ContinueWithOnMainThread(task =>
        {
            if (task.Exception != null)
            {
                Debug.LogError($"[ProgressHome] Failed to fetch /{arObjectPath}/{objectKey}/prefabName: {task.Exception}");
                return;
            }

            string prefabName = task.Result.Value?.ToString() ?? "";
            if (string.IsNullOrEmpty(prefabName))
            {
                Debug.LogWarning($"[ProgressHome] No prefabName found under /{arObjectPath}/{objectKey}");
                return;
            }
            Debug.Log($"[ProgressHome] Got prefabName='{prefabName}'. Looking up in prefabMap…");

            string lookupKey = objectKey.Trim().ToLower().Replace(" ", "_");
            if (!prefabMap.TryGetValue(lookupKey, out GameObject prefab))
            {
                Debug.LogWarning($"[ProgressHome] prefabMap does not contain a prefab for key '{lookupKey}'");
                return;
            }
            Debug.Log($"[ProgressHome] Found prefab '{prefab.name}'. Preparing to instantiate under PreviewRoot…");

            previewDisplay.gameObject.SetActive(true);

            DestroyCurrentPreview();

            var previewRoot = GameObject.Find("PreviewRoot");
            if (previewRoot == null)
            {
                Debug.LogError("[ProgressHome] Cannot find GameObject named 'PreviewRoot' in the scene!");
                return;
            }

            currentPreviewInstance = Instantiate(prefab, previewRoot.transform);
            currentPreviewInstance.transform.localPosition = new Vector3(0, 0, 2f);
            currentPreviewInstance.transform.localRotation = Quaternion.identity;
            currentPreviewInstance.transform.localScale = Vector3.one * 2f;
            SetLayerRecursively(currentPreviewInstance, LayerMask.NameToLayer("PreviewOnly"));
            currentPreviewInstance.AddComponent<FloatingRotator>();

            Debug.Log($"[ProgressHome] Instantiated preview '{prefab.name}' under PreviewRoot with localPos={currentPreviewInstance.transform.localPosition}.");
        });
    }

    void DestroyCurrentPreview()
    {
        if (currentPreviewInstance != null)
        {
            Debug.Log($"[ProgressHome] Destroying existing preview '{currentPreviewInstance.name}'.");
            Destroy(currentPreviewInstance);
        }
    }

    static void SetLayerRecursively(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform child in go.transform)
            SetLayerRecursively(child.gameObject, layer);
    }
    #endregion

    // ──────────────────────────────────────────────────────────────────────
    #region ─── D) Helper for Title‐Casing (first letter capital) ──────────
    static class CultureInfoUtility
    {
        public static string ToTitleCase(string lower)
        {
            if (string.IsNullOrEmpty(lower)) return lower;
            return char.ToUpper(lower[0]) + lower.Substring(1).ToLower();
        }
    }
    #endregion
}
