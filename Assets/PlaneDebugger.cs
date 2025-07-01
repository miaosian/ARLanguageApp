using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class PlaneDebugger : MonoBehaviour
{
    private ARPlaneManager planeManager;

    void Start()
    {
        planeManager = FindFirstObjectByType<ARPlaneManager>();
    }

    void Update()
    {
        if (planeManager != null)
        {
            foreach (var plane in planeManager.trackables)
            {
                Debug.Log($"Detected plane at: {plane.transform.position}");
            }
        }
    }
}
