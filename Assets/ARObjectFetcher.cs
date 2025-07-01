using UnityEngine;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using System.Linq;

public class ARObjectFetcher : MonoBehaviour
{
    public GameObject[] prefabs;
    private DatabaseReference dbRef;
    public GameObject currentSpawnedPrefab;

    void Start()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task => {
            if (task.Result == DependencyStatus.Available)
            {
                dbRef = FirebaseDatabase.DefaultInstance.RootReference;
                Debug.Log("Firebase initialized successfully!");
            }
            else
            {
                Debug.LogError("Firebase dependencies not resolved: " + task.Result.ToString());
            }
        });
    }

    public void TrySpawnFromFirebase(string label)
    {
        if (dbRef == null)
        {
            Debug.LogError("Database reference is null! Firebase not ready?");
            return;
        }

        dbRef.Child("ar_object").Child(label.ToLower()).GetValueAsync().ContinueWithOnMainThread(task => {
            if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;
                if (snapshot.Exists)
                {
                    string prefabName = snapshot.Child("prefabName").Value.ToString();
                    Debug.Log("Prefab found in Firebase: " + prefabName);

                    GameObject prefab = prefabs.FirstOrDefault(p => p.name == prefabName);
                    if (prefab != null)
                    {
                        // Destroy previous if any
                        if (currentSpawnedPrefab != null)
                            Destroy(currentSpawnedPrefab);

                        Vector3 spawnPos = Camera.main.transform.position + Camera.main.transform.forward * 0.5f;
                        currentSpawnedPrefab = Instantiate(prefab, spawnPos, Quaternion.identity);
                        currentSpawnedPrefab.AddComponent<TouchRotate>(); // add rotation script
                    Debug.Log("✅ TouchRotate attached to: " + currentSpawnedPrefab.name);

}
                    else
                    {
                        Debug.LogWarning("Prefab not found in Prefabs List in Unity for: " + prefabName);
                    }
                }
                else
                {
                    Debug.LogWarning("No object matched in Firebase for label: " + label);
                }
            }
            else
            {
                Debug.LogError("Firebase query failed: " + task.Exception);
            }
        });
    }

    public void ClearSpawnedObject()
    {
        if (currentSpawnedPrefab != null)
        {
            Destroy(currentSpawnedPrefab);
            currentSpawnedPrefab = null;
        }
    }
}
