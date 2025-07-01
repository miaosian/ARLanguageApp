using UnityEngine;

/// <summary>
/// Attach this to any prefab instance you want to float up/down and rotate slowly.
/// </summary>
public class FloatingRotator : MonoBehaviour
{
    [Tooltip("How high (in world units) the object floats above its start position.")]
    public float floatAmplitude = 0.05f;

    [Tooltip("How fast (in cycles per second) the object floats up/down.")]
    public float floatFrequency = 1f;

    [Tooltip("Degrees per second to rotate around the Y axis.")]
    public float rotationSpeed = 30f;

    private Vector3 startPos;

    void Start()
    {
        // Remember the spawn position so we can oscillate around it
        startPos = transform.position;
    }

    void Update()
    {
        // 1) Floating: move up/down in a sine wave
        float newY = startPos.y + Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
        transform.position = new Vector3(startPos.x, newY, startPos.z);

        // 2) Rotation: rotate around the Y axis at rotationSpeed degrees/sec
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
    }
}
