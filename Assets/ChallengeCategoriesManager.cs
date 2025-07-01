using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using UnityEngine.SceneManagement;

/// <summary>
/// Populates a scrollable list of vocabulary categories.
/// Each category shows:
///  - Icon (from Assets/Resources/Sprites/{categoryName}.png),
///  - Category name,
///  - “X / Y words” text,
///  - A progress bar showing (X/Y) as a percentage,
///  - Tapping the row enters the Challenges scene but filtered to that category.
/// </summary>


public class ChallengeCategoriesManager : MonoBehaviour
{
[Serializable]
public struct CategoryIcon
{
    public string  categoryName; // must match the key exactly (lowercase, etc.)
    public Sprite  iconSprite;   // drag the Sprite asset here
}
    [Header("UI References")]
    public CanvasGroup        canvasGroup;          // The root CanvasGroup so we can fade in/out if desired
    public Transform          contentParent;        // ScrollView→Viewport→Content
    public GameObject         categoryRowPrefab;    // Prefab with (Icon, CategoryNameText, ProgressText, ProgressBarFill, Button)
    public Button             backButton;           // “Back” button to return to ProgressHome or previous menu

    [Header("Challenge Settings")]
    public int                hitsToMaster = 10;    // how many hits needed to “master” a word
    public string             challengesSceneName = "cha_enter"; // scene to load when a category is tapped

    [Header("Inspector-Assigned Icons")]
    [Tooltip("For each category, enter its exact categoryName (lowercase) and drag its Sprite here.")]
    public CategoryIcon[]     categoryIcons;


    // Internal working data
    private DatabaseReference dbRoot;
    private string            uid;
    private FirebaseAuth      auth;

    // Used to cache category → list of object keys in that category
    private Dictionary<string, List<string>> categoryToObjects;
    private Dictionary<string, Sprite>      _spriteMap;


    void Start()
    {
    // 1) Build the sprite‐lookup dictionary:
    _spriteMap = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
    foreach (var entry in categoryIcons)
    {
        if (!string.IsNullOrEmpty(entry.categoryName) && entry.iconSprite != null)
            _spriteMap[ entry.categoryName.Trim() ] = entry.iconSprite;
        else
            Debug.LogWarning($"[ChallengeCategories] Icon entry for '{entry.categoryName}' is missing or invalid.");
    }
        // 1) Hide the UI until we load
        canvasGroup.alpha = 0;
        canvasGroup.blocksRaycasts = false;

        // 2) Init Firebase references
        auth = FirebaseAuth.DefaultInstance;
        if (auth.CurrentUser == null)
        {
            // If no one is signed in, send them back to Login
            Debug.LogWarning("[ChallengeCategories] No user signed in. Redirecting to Main.");
            SceneManager.LoadScene("Main");
            return;
        }
        uid = auth.CurrentUser.UserId;
        dbRoot = FirebaseDatabase.DefaultInstance.RootReference;

        // 3) Wire up the Back button
        backButton.onClick.AddListener(OnBackPressed);

        // 4) Start loading categories from Firebase
        LoadAllARObjects();
    }

    /// <summary>
    /// Step 1: Load /ar_object to discover each objectKey and its category.
    /// We'll group by category.
    /// </summary>
    void LoadAllARObjects()
    {
        dbRoot.Child("ar_object").GetValueAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.Exception != null)
                {
                    Debug.LogError("[ChallengeCategories] Failed to load /ar_object: " + task.Exception);
                    return;
                }

                DataSnapshot snap = task.Result;
        Debug.Log($"[ChallengeCategories] /ar_object snapshot exists? {snap.Exists}, children count = {snap.ChildrenCount}");
                if (!snap.Exists)
                {
                    Debug.LogWarning("[ChallengeCategories] No /ar_object data found!");
                    return;
                }

                // 1) Build a dictionary: category → list of objectKeys
                categoryToObjects = new Dictionary<string, List<string>>();

                foreach (var child in snap.Children)
                {
                    string objectKey = child.Key; 
                    // Expect each child to have at least a “category” field:
                    var categoryNode = child.Child("category");
                    if (categoryNode == null || categoryNode.Value == null)
                        continue; // skip if no category

                    string categoryName = categoryNode.Value.ToString();

                    // Track this object in the category bucket:
                    if (!categoryToObjects.ContainsKey(categoryName))
                        categoryToObjects[categoryName] = new List<string>();

                    categoryToObjects[categoryName].Add(objectKey);
                }

Debug.Log("[ChallengeCategories]  Found “" + categoryToObjects.Count + "” categories:");
foreach (var kv in categoryToObjects)
{
    Debug.Log($"    • Category = {kv.Key} (contains {kv.Value.Count} objects)");
}

                // 2) Once we have category → object list, fetch the user’s “hits” so we can compute how many are mastered
                LoadUserHitsAndPopulate();
            });
    }

    /// <summary>
    /// Step 2: Load /hits/{uid} to see how many times each objectKey has been solved.
    /// Then compute, for each category, how many words are “mastered” (hits >= hitsToMaster).
    /// Finally, instantiate a row prefab for each category.
    /// </summary>
void LoadUserHitsAndPopulate()
{
    dbRoot
      .Child("hits")
      .Child(uid)
      .GetValueAsync()
      .ContinueWithOnMainThread(task =>
      {
          if (task.Exception != null)
          {
              Debug.LogError($"[ChallengeCategories] Failed to load /hits/{uid}: {task.Exception}");
              return;
          }

          DataSnapshot hitsSnap = task.Result;

          // Now loop through every category in categoryToObjects:
          foreach (var kv in categoryToObjects)
          {
              string categoryName = kv.Key;
              List<string> objectKeys = kv.Value;

              // Count how many words in this category have been mastered:
              int totalCount    = objectKeys.Count;
              int masteredCount = 0;

              if (hitsSnap.Exists)
              {
                  // If /hits/{uid}/{objectKey} exists, that word is considered mastered.
                  foreach (string objKey in objectKeys)
                  {
                      if (hitsSnap.HasChild(objKey))
                          masteredCount++;
                  }
              }

              // Percentage mastered (0.0–1.0)
              float pct = (totalCount == 0) ? 0f : ((float)masteredCount / totalCount);

              // Log each category so we can see it in the console
              Debug.Log($"[ChallengeCategories] {categoryName} → {masteredCount}/{totalCount} mastered ({pct:P0})");

              // Instantiate one row for this category
              CreateCategoryRow(categoryName, objectKeys, masteredCount, totalCount, pct);
          }

          // Finally, fade in the whole UI:
          canvasGroup.alpha = 1;
          canvasGroup.blocksRaycasts = true;
      });
}




    /// <summary>
    /// Instantiates one CategoryRow prefab under contentParent.
    /// Fills in the icon (from Resources/Sprites/{categoryName}.png),
    /// category name, “X/Y words” text, and adjusts the progress bar’s fill.
    /// Also wires up the tap/click to call OnCategoryTapped(categoryName, objectKeys).
    /// </summary>
    void CreateCategoryRow(
        string categoryName,
        List<string> objectKeys,
        int masteredCount,
        int totalCount,
        float pct)
    {
    Debug.Log($"[CreateCategoryRow] category={categoryName}, mastered={masteredCount}/{totalCount} ({pct:P0})");

        // 1) Instantiate the prefab under contentParent
        GameObject rowGO = Instantiate(categoryRowPrefab, contentParent);
        rowGO.transform.localScale = Vector3.one; // ensure correct scaling

// 2) Find & set the CategoryNameText
Transform nameTr = rowGO.transform.Find("CategoryNameText");
if (nameTr == null)
    Debug.LogError($"[CreateCategoryRow] Could not find a child named 'CategoryNameText' on prefab {rowGO.name}");
var nameText = nameTr.GetComponent<TMP_Text>();
string niceName = char.ToUpper(categoryName[0]) + categoryName.Substring(1);
nameText.text = niceName;

// 3) Find & set the ProgressText
Transform progTr = rowGO.transform.Find("ProgressText");
if (progTr == null)
    Debug.LogError($"[CreateCategoryRow] Could not find a child named 'ProgressText' on prefab {rowGO.name}");
var progText = progTr.GetComponent<TMP_Text>();
progText.text = $"{masteredCount}/{totalCount} words";

// 4) Find & set the ProgressBar fill
Transform pbTr = rowGO.transform.Find("ProgressBarBG/ProgressBarFill");
if (pbTr == null)
    Debug.LogError($"[CreateCategoryRow] Could not find 'ProgressBarBG/ProgressBarFill' on prefab {rowGO.name}");
var pbFill = pbTr.GetComponent<Image>();
pbFill.fillAmount = pct;

// 5) Find & set the Icon RawImage
Transform iconTr = rowGO.transform.Find("Icon");
if (iconTr == null)
    Debug.LogError($"[CreateCategoryRow] Could not find a child named 'Icon' on prefab {rowGO.name}");
var iconRaw = rowGO.transform.Find("Icon").GetComponent<RawImage>();

Sprite iconSprite;
if (_spriteMap != null && _spriteMap.TryGetValue(categoryName, out iconSprite))
{
    iconRaw.texture = iconSprite.texture;
    iconRaw.color   = Color.white;
}
else
{
    Debug.LogWarning($"[CreateCategoryRow] No inspector‐assigned sprite for category '{categoryName}'");
    iconRaw.color = new Color(0,0,0,0); // hide the RawImage
}


        // 6) Wire up the Button on the root of rowGO
        var btn = rowGO.GetComponent<Button>();
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => {
    Debug.Log($"Row “{categoryName}” was tapped!");

            OnCategoryTapped(categoryName, objectKeys);
        });
    }

    /// <summary>
    /// Called when the user taps on a category row. 
    /// We’ll pass the chosen category name (and its objectKeys) into PlayerPrefs or a static variable,
    /// then load the normal “Challenges” scene (which should read that info and only quiz from that category).
    /// </summary>
    void OnCategoryTapped(string categoryName, List<string> objectKeys)
    {
        // 1) Store the chosen category name/object list somewhere accessible to the Challenges scene.
        //    For simplicity, we’ll JSON‐serialize the list of objectKeys into PlayerPrefs:
        PlayerPrefs.SetString("ChosenCategory", categoryName);
        PlayerPrefs.SetString(
            "ChosenCategoryObjects",
            JsonUtility.ToJson(new SerializableStringList(objectKeys))
        );
        PlayerPrefs.Save();
        if (auth.CurrentUser != null)
        {
            string uid = auth.CurrentUser.UserId;
            // build a small object with just “category”
            var lastData = new Dictionary<string, object>()
            {
                { "category", categoryName }
            };
            dbRoot
              .Child("lastChallenge")
              .Child(uid)
              .SetValueAsync(lastData)
              .ContinueWithOnMainThread(task =>
              {
                  if (task.Exception != null)
                      Debug.LogError($"[ChallengeCategories] Could not save lastChallenge: {task.Exception}");
                  else
                      Debug.Log($"[ChallengeCategories] Saved lastChallenge/{uid} = {categoryName}");
              });
        }

        // 2) Load the existing “Challenges” scene:
        SceneManager.LoadScene(challengesSceneName);
    }

    /// <summary>
    /// Called when the Back button is pressed. Simply return to the ProgressHome scene.
    /// </summary>
    void OnBackPressed()
    {
        SceneManager.LoadScene("ProgressHome");
    }

    /// <summary>
    /// A small helper class so we can JSON‐serialize a List<string> into PlayerPrefs.
    /// </summary>
    [Serializable]
    public class SerializableStringList
    {
        public List<string> items;
        public SerializableStringList(List<string> list) { items = list; }
    }
}
