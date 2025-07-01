using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using TMPro;

public class ARTapToPlace : MonoBehaviour
{
[HideInInspector] public GameObject spawnedObject;

    public GameObject prefabToSpawn;
    public GameObject textLabelObject; // this will be a simple TextMeshPro object
    public ARRaycastManager raycastManager;

    [HideInInspector] public string objectName = ""; // assigned by VisionRecognizer

    private List<ARRaycastHit> hits = new List<ARRaycastHit>();

    void Update()
    {
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began && objectName != "")
        {
Debug.Log("User tapped to spawn: " + objectName);

            Vector2 touchPosition = Input.GetTouch(0).position;

            if (raycastManager.Raycast(touchPosition, hits, TrackableType.Planes))
            {
                Pose hitPose = hits[0].pose;

                // Spawn 3D model
                spawnedObject = Instantiate(prefabToSpawn, hitPose.position, hitPose.rotation);
                spawnedObject.AddComponent<FaceBasedHide>();
                // Spawn text label above model
                GameObject label = Instantiate(textLabelObject, hitPose.position + Vector3.up * 0.15f, Quaternion.identity);
                label.GetComponent<TextMeshPro>().text = objectName.ToUpper();

                // Make label face the user
                label.transform.LookAt(Camera.main.transform);
                label.transform.Rotate(0, 180, 0);

                objectName = ""; // prevent double-spawn
            }
        }
    }

}
