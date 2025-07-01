using UnityEngine;

[System.Serializable]
public class auth {
  public string api_key;
}


public static class SecretsLoader {
  /// <summary>
  /// Loads Assets/Resources/auth.json, parses it, and returns a Secrets instance.
  /// </summary>
  public static auth Load() {
    // 1) Load the TextAsset named "Secrets" from Resources
    TextAsset ta = Resources.Load<TextAsset>("auth");
    if (ta == null) {
      Debug.LogError("auth.json not found in Resources!");
      return null;
    }
    // 2) Parse its JSON text into our Secrets class
    return JsonUtility.FromJson<auth>(ta.text);
  }
}