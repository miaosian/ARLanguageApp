using UnityEngine;

public class ARObjectSpawner : MonoBehaviour
{
    public GameObject bottlePrefab;
    public GameObject applePrefab;
    public Transform spawnPoint;

    private GameObject currentModel;
    public float prefabScale = 50f; 


    public void SpawnObject(string label)
{
    if (currentModel != null)
        Destroy(currentModel);

    label = label.ToLower();
    Debug.Log("Trying to match label: " + label);


    Camera cam = Camera.main;
    if (cam == null)
    {
        Debug.LogError("Main Camera not found!");
        return;
    }

    Vector3 spawnPosition = cam.transform.position + cam.transform.forward * 1.5f; 
    Quaternion spawnRotation = Quaternion.LookRotation(cam.transform.forward); // face forward

    if (label.Contains("bottle") && bottlePrefab != null)
    {
        currentModel = Instantiate(bottlePrefab, spawnPosition, spawnRotation);
        currentModel.transform.localScale *= prefabScale; // 🔥 MAGIC HERE

	Debug.Log("Camera position: " + cam.transform.position);
	Debug.Log("Spawn position: " + spawnPosition);

    }
    else
    {
        Debug.Log("No prefab mapped for label: " + label);
    }
}
}