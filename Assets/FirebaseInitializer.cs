using UnityEngine;
using UnityEngine.SceneManagement;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;

public class FirebaseInitializer : MonoBehaviour {
  void Start() {
    FirebaseApp.CheckAndFixDependenciesAsync()
      .ContinueWithOnMainThread(task => {
        if (task.Result != DependencyStatus.Available) {
          Debug.LogError("Could not resolve all Firebase dependencies");
          return;
        }

        // Now Firebase is ready—decide which scene to show:
        var user = FirebaseAuth.DefaultInstance.CurrentUser;
        if (user != null) {
          // already logged in → skip straight to your main/home
          SceneManager.LoadScene("ProgressHome");
        }
        else {
          // not signed-in → show the login/register flow
          SceneManager.LoadScene("Main");
        }
      });
  }
}
