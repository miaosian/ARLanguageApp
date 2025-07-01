using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Auth;
using Firebase.Extensions;
using UnityEngine.SceneManagement;

public class ChangePasswordManager : MonoBehaviour
{
    [Header("UI References (Change Password Scene)")]
    public TMP_InputField newPasswordInput;
    public TMP_InputField confirmPasswordInput;
    public Button        saveButton;
    public Button        backButton;
    public TMP_Text      feedbackText;

    private FirebaseAuth auth;

    void Start()
    {
        auth = FirebaseAuth.DefaultInstance;

        backButton.onClick.AddListener(() => SceneManager.LoadScene("Settings"));
        saveButton.onClick .AddListener(OnSave);
    }

    void OnSave()
    {
        var p1 = newPasswordInput.text;
        var p2 = confirmPasswordInput.text;

        if (p1.Length < 6)
        {
            feedbackText.text = "Password must be at least 6 characters.";
            return;
        }
        if (p1 != p2)
        {
            feedbackText.text = "Passwords do not match.";
            return;
        }

        feedbackText.text = "Updating password…";
        var user = auth.CurrentUser;
        if (user == null)
        {
            feedbackText.text = "Not signed in!";
            return;
        }

        user.UpdatePasswordAsync(p1)
            .ContinueWithOnMainThread(task => {
                if (task.Exception != null)
                {
                    feedbackText.text = "Error: " + task.Exception.Flatten().Message;
                }
                else
                {
                    feedbackText.text = "Password updated!";
                    // after a short delay, return to Settings
                    Invoke(nameof(ReturnToSettings), 1f);
                }
            });
    }

    void ReturnToSettings()
    {
        SceneManager.LoadScene("Setting");
    }
}
