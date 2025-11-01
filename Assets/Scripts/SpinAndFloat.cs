using UnityEngine;

public class SpinAndFloat : MonoBehaviour
{
    [Header("Rotation Settings")]
    public float rotationSpeed = 20f; // degrees per second

    [Header("Float Settings")]
    public float floatAmplitude = 0.5f; // how high/low it moves
    public float floatFrequency = 1f;   // how fast it oscillates

    private Vector3 startPos;

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        // Rotate around Y axis
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);

        // Oscillate up and down
        float newY = startPos.y + Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
        transform.position = new Vector3(startPos.x, newY, startPos.z);
    }
}
