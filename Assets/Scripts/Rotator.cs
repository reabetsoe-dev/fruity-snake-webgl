using UnityEngine;

public class Rotator : MonoBehaviour
{
    public float Y_angle;

    // Update is called once per frame
    void Update()
    {
        // Rotate fruit 180 degrees around Y-axis
        transform.Rotate(new Vector3(0, Y_angle, 0) * Time.deltaTime);
    }
}
