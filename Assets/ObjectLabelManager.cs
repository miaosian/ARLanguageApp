using UnityEngine;
using UnityEngine.UI;

public class ObjectLabelManager : MonoBehaviour
{
    public static ObjectLabelManager Instance;
    public GameObject labelPrefab;  // UI prefab with Text component

    void Awake()
    {
        Instance = this;
    }

    public void DisplayDetectedObject(string objectName)
    {
        Vector3 screenPos = new Vector3(Screen.width / 2, Screen.height / 2, 0);
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(screenPos);

        GameObject labelInstance = Instantiate(labelPrefab, worldPos, Quaternion.identity);
        labelInstance.GetComponentInChildren<Text>().text = objectName;
        labelInstance.transform.LookAt(Camera.main.transform);
    }
}
