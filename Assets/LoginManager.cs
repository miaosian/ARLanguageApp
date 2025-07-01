using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Auth;
using Firebase.Extensions;
using UnityEngine.SceneManagement;

public class LoginManager : MonoBehaviour {
  public TMP_InputField emailInput;
  public TMP_InputField passwordInput;
  public Button loginButton;
  public TMP_Text feedbackText;

  private FirebaseAuth auth;

  void Start() {
    auth = FirebaseAuth.DefaultInstance;
    loginButton.onClick.AddListener(Login);
  }

  public void Login() {
    string email = emailInput.text.Trim();
    string pass  = passwordInput.text;
    if (string.IsNullOrEmpty(email) || pass.Length < 6) {
      feedbackText.text = "Enter valid email & password (≥6 chars).";
      return;
    }

    feedbackText.text = "Signing in…";
    auth.SignInWithEmailAndPasswordAsync(email, pass)
      .ContinueWithOnMainThread(task => {
      if (task.Exception != null)
                {
                    // Grab the inner exception message for more detail
                    var msg = task.Exception.Flatten().InnerExceptions[0].Message;
                    feedbackText.text = "Login failed: " + msg;
                    return;
                }

        // At this point auth.CurrentUser has been set for us
        var user = auth.CurrentUser;
        if (user == null) {
          feedbackText.text = "Login succeeded, but no user found!";
          return;
        }

        feedbackText.text = "Welcome back, " +
                             (string.IsNullOrEmpty(user.DisplayName)
                                ? "user"
                                : user.DisplayName)
                             + "!";


        SceneManager.LoadScene("ProgressHome");
      });
  }
}
