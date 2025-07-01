using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class RegisterManager : MonoBehaviour {
  [Header("Input Fields")]
  public TMP_InputField nameInput;
  public TMP_InputField emailInput;
  public TMP_InputField passwordInput;
  public TMP_Dropdown languageInput;
  public TMP_InputField learningGoalInput;

  public Button signUpButton;
  public TMP_Text feedbackText;

  FirebaseAuth auth;
  DatabaseReference dbRoot;

  void Start() {
    auth = FirebaseAuth.DefaultInstance;
    dbRoot = FirebaseDatabase.DefaultInstance.RootReference;
    signUpButton.onClick.AddListener(RegisterUser);
  }

  public void RegisterUser() {
Debug.Log("[RegisterUser] called");
    var name  = nameInput.text.Trim();
    var email = emailInput.text.Trim();
    var pass  = passwordInput.text;
    var lang  = languageInput.options[ languageInput.value ].text;
    var goal  = learningGoalInput.text.Trim();

  if (string.IsNullOrEmpty(email) || pass.Length < 6) {
    feedbackText.text = "Email & password (≥6 chars) required.";
    return;
  }
    feedbackText.text = "Registering…";
    Debug.Log("[RegisterUser] calling Firebase...");

    auth.CreateUserWithEmailAndPasswordAsync(email, pass)
      .ContinueWithOnMainThread(task => {
        if (task.Exception != null) {
          Debug.LogError("[Auth] " + task.Exception);
          feedbackText.text = "Error: " +
            task.Exception.Flatten().InnerExceptions[0].Message;
          return;
        }

        // at this point the user is signed in
        var user = auth.CurrentUser;
        if (user == null) {
          Debug.LogError("[Auth] CurrentUser is null after CreateUser!");
          feedbackText.text = "Registration failed.";
          return;
        }
        string uid = user.UserId;
        Debug.Log($"[Auth] new user UID = {uid}");


        // build our user object
        var userData = new Dictionary<string, object>() {
          { "UserID",            uid          },
          { "Name",              name         },
          { "Email",             email        },
          { "PreferredLanguage", lang         },
          { "LearningGoal",      goal         }
        };
Debug.Log($"[Register] About to write user data for UID={uid}");
        // write to /users/{uid}

        dbRoot.Child("users").Child(uid)
              .SetValueAsync(userData)
              .ContinueWithOnMainThread(dbTask => {
               if (dbTask.Exception != null) {
                Debug.LogError("[DB] " + dbTask.Exception);
                feedbackText.text = "DB save error: " +
                  dbTask.Exception.Flatten().InnerExceptions[0].Message;
              } else {
                Debug.Log("[DB] save successful");
                feedbackText.text = "Registered successfully!";
                SceneManager.LoadScene("Login");
              }
              });
      });
  }
}
