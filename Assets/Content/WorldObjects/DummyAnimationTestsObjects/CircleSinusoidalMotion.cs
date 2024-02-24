using UnityEngine;

public class CircleSinusoidalMotion : MonoBehaviour
{
    public float radius = 5f;        // Radius of the circular motion
    public float speed = 1f;         // Speed of the motion
    public float minHeight = 1f;     // Minimum height of the sinusoidal motion
    public float maxHeight = 3f;     // Maximum height of the sinusoidal motion
    public float waveFrequency = 4f; // Frequency of the sinusoidal motion

    private float angle = 0f;

    void Update()
    {
        // Increment angle based on speed
        angle += speed * Time.deltaTime;

        // Calculate position using sine and cosine functions
        float x = Mathf.Cos(angle) * radius;
        float y = Mathf.Lerp(minHeight, maxHeight, Mathf.Sin(angle * waveFrequency) * 0.5f + 0.5f); // Dynamically adjust height
        float z = Mathf.Sin(angle) * radius;

        // Update the position of the GameObject
        transform.position = new Vector3(x, y, z);
    }
}
