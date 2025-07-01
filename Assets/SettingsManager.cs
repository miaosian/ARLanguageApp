using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using UnityEngine.SceneManagement;

public class SettingsManager : MonoBehaviour {
  [Header("UI Fields")]
  public TMP_Text nameText;
  public TMP_Text emailText;
  public TMP_Text languageText;
  public TMP_Text goalText;

  public Button signOutButton;
  public Button backButton;
  public Button changePasswordButton;

  private FirebaseAuth auth;
  private DatabaseReference dbRoot;

  void Start() {
    auth   = FirebaseAuth.DefaultInstance;
    dbRoot = FirebaseDatabase.DefaultInstance.RootReference;

    // If there's no current user, send them back to Login:
    if (auth.CurrentUser == null) {
      SceneManager.LoadScene("Login");
      Debug.LogWarning("No user signed in—redirecting to Login.");
      return;
    }

    // Wire up sign-out:
    signOutButton.onClick.AddListener(() => {
      auth.SignOut();
      // then go back to Login scene
      SceneManager.LoadScene("Login");
    });

    LoadUserProfile();

    backButton.onClick.AddListener(() => SceneManager.LoadScene("ProgressHome"));
        changePasswordButton.onClick.AddListener(() => SceneManager.LoadScene("password"));

    LoadUserSettings();
  }

  void LoadUserProfile() {
    string uid = auth.CurrentUser.UserId;
    dbRoot.Child("users")
          .Child(uid)
          .GetValueAsync()
          .ContinueWithOnMainThread(task => {
      if (task.Exception != null) {
        Debug.LogError("Failed to load profile: " + task.Exception);
        return;
      }
      var snap = task.Result;
      if (!snap.Exists) {
        Debug.LogError("User data not found in database!");
        return;
      }

      // Extract each field; if you used the exact keys in RegisterManager:
      nameText.text      = snap.Child("Name").Value?.ToString()             ?? "";
      emailText.text     = snap.Child("Email").Value?.ToString()            ?? "";
      languageText.text  = snap.Child("PreferredLanguage").Value?.ToString()?? "";
      goalText.text      = snap.Child("LearningGoal").Value?.ToString()     ?? "";
    });
  }

    void LoadUserSettings()
    {
        var user = auth.CurrentUser;
        if (user == null)
        {
            Debug.LogWarning("No signed-in user, redirecting to login.");
            SceneManager.LoadScene("Login");
            return;
        }

        string uid = user.UserId;
        dbRoot.Child("users").Child(uid)
              .GetValueAsync()
              .ContinueWithOnMainThread(task => {
            if (task.Exception != null)
            {
                Debug.LogError(task.Exception);
                return;
            }
            var snap = task.Result;
            if (!snap.Exists) return;

            // pull out the fields
            var fullLang = snap.Child("PreferredLanguage").Value?.ToString() ?? "";
            var goal     = snap.Child("LearningGoal"     ).Value?.ToString() ?? "";

            languageText.text = ToShortLang(fullLang);
            goalText.text     = goal;
        });
    }

    string ToShortLang(string full)
    {
        switch (full.Trim().ToLower())
        {
            case "english":      return "EN";
            case "bahasa melayu":return "BM";
            case "chinese":      return "CN";
            // add more mappings as needed
            default:
                if (full.Length >= 2) return full.Substring(0,2).ToUpper();
                return full.ToUpper();
        }
    }
}
