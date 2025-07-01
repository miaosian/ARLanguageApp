using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ARSpawner : MonoBehaviour
{
    public ARRaycastManager raycastManager;
    public GameObject objectToSpawn;
    public GameObject spawnedObject; // store latest spawned object
    private List<ARRaycastHit> hits = new List<ARRaycastHit>();

    void Update()
    {
        if (objectToSpawn == null)
            return;

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (raycastManager.Raycast(touch.position, hits, TrackableType.PlaneWithinPolygon))
            {
                Pose hitPose = hits[0].pose;
                Instantiate(objectToSpawn, hitPose.position, hitPose.rotation);
                spawnedObject = Instantiate(objectToSpawn, hitPose.position, hitPose.rotation);

                
                // After spawning, clear objectToSpawn so it doesn't spawn again unless new detection
                objectToSpawn = null;
            }
        }
    }
}
