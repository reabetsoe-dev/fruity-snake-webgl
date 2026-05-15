using UnityEngine;

public class CameraController : MonoBehaviour
{
    public GameObject snakeHead;
    private Vector3 offset;

    // Use this for initialization
    void Start()
    {
        offset = transform.position - snakeHead.transform.position;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        // The camera follow the snake while moving
        transform.position = snakeHead.transform.position + offset;
    }
}
