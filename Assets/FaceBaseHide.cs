using UnityEngine;

public class FaceBasedHide : MonoBehaviour
{
    public float hideAngleThreshold = 90f; // degrees
    private Transform mainCamera;

    void Start()
    {
        mainCamera = Camera.main.transform;
    }

    void Update()
    {
        if (mainCamera == null) return;

        Vector3 toCamera = mainCamera.position - transform.position;
        float angle = Vector3.Angle(transform.forward, toCamera);

        if (angle > hideAngleThreshold)
        {
            gameObject.SetActive(false); // Hide the prefab
        }
        else
        {
            gameObject.SetActive(true);  // Show the prefab
        }
    }
}
