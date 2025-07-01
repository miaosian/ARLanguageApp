using UnityEngine;

public class TouchRotate : MonoBehaviour
{
    // 📦 Floating
    public float floatAmplitude = 0.01f;   // How high the object floats
    public float floatFrequency = 1f;      // How fast it floats
    private Vector3 startPos;

    // 🔄 Touch Rotation
    private bool isDragging = false;
    private Vector2 previousTouchPos;
    private float rotationSpeed = 0.2f;

    void Start()
    {
        startPos = transform.position;
        Debug.Log("🌀 TouchRotate with floating running on: " + gameObject.name);
    }

    void Update()
    {
        HandleFloating();
        HandleTouchRotation();
    }

    void HandleFloating()
    {
        float newY = startPos.y + Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
        transform.position = new Vector3(startPos.x, newY, startPos.z);
    }

    void HandleTouchRotation()
    {
        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);
            Ray ray = Camera.main.ScreenPointToRay(touch.position);
            RaycastHit hit;

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    if (Physics.Raycast(ray, out hit))
                    {
                        if (hit.transform == transform)
                        {
                            isDragging = true;
                            previousTouchPos = touch.position;
                        }
                    }
                    break;

                case TouchPhase.Moved:
                    if (isDragging)
                    {
                        Vector2 delta = touch.position - previousTouchPos;
                        float rotateY = -delta.x * rotationSpeed; // horizontal swipe
                        float rotateX = delta.y * rotationSpeed;  // vertical swipe
                        transform.Rotate(rotateX, rotateY, 0, Space.World);
                        previousTouchPos = touch.position;
                    }
                    break;

                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    isDragging = false;
                    break;
            }
        }
    }
}
