using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SnakeController : MonoBehaviour
{
    public List<GameObject> snakeParts = new List<GameObject>();

    private int partsCount = 3;

    public float speed;
    public float minDistance;

    private Rigidbody rb;

    public GameObject playground;
    private GameObject currentPart;
    private GameObject prevPart;
    public GameObject game_over_panel;

    public GameObject snakePartPrefab;
    public GameObject fruitPrefab;

    private int score;
    public Text scoreText;

    // Use this for initialization
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        Move();
    }

    private void Move()
    {
        // If the user swipes the mobile screen using one finger
        if (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Moved)
        {
            // Get movement of the finger since last frame
            Vector2 deltaPosition = Input.GetTouch(0).deltaPosition;
            Vector3 movement = new Vector3(deltaPosition.x, 0, deltaPosition.y);

            if (deltaPosition.x != 0 || deltaPosition.y != 0)
            {
                // Move the snake's head to new position
                rb.AddForce(movement * speed);
            }
        }

        else
        {
            // Move snake at game start and whenever the player releases his finger from screen
            rb.AddForce(Vector3.forward * speed);
        }

        /*
         * Move other parts of the snake one by one
         */
        for (int index = 1; index < snakeParts.Count; index++)
        {
            currentPart = snakeParts[index];
            prevPart = snakeParts[index - 1];

            Vector3 newPos = prevPart.transform.position;
            float distance = Vector3.Distance(prevPart.transform.position,
                currentPart.transform.position);

            // Keep distance between snake parts
            float time = Time.deltaTime * distance / minDistance * speed;
            if (time > 0.5f)
                time = 0.5f;

            // Move current part to new position
            currentPart.transform.position = Vector3.Slerp(currentPart.transform.position,
                newPos, time);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Fruit")
        {
            AddSnakePart();

            // Make fruit disappear
            DestroyObject(other.transform.gameObject);

            // Play "eat_fruit" sound
            GetComponent<AudioSource>().Play();

            // Spawn a new fruit on playground at random position
            GameObject instance = Instantiate(fruitPrefab) as GameObject;
            instance.transform.position = RandomPointInBox();
            instance.transform.rotation = Quaternion.identity;

            // Increase the player's score
            score++;
            // Update score in UI
            scoreText.text = "" + score;

            if (score >= 10)
            {
                NextLevel();
            }
        }

        // The game is over when the snake hits the border, an obstacle or itself
        else if (other.tag == "Obstacle" || other.tag == "Snake")
        {
            StartCoroutine(GameOver());
        }
    }

    private void AddSnakePart()
    {
        // Spawn a new part of snake
        GameObject newPart = Instantiate(snakePartPrefab) as GameObject;
        newPart.transform.SetParent(transform.parent);

        // Get last snakePart position
        Vector3 oldPos = snakeParts[snakeParts.Count - 1].transform.localPosition;

        // Attach new part at the end of snake body
        Vector3 newPos = new Vector3(oldPos.x + 1, oldPos.y, oldPos.z);
        newPart.transform.localPosition = newPos;
        newPart.transform.localRotation = Quaternion.identity;

        partsCount++;

        // Rename newPart to be its position
        newPart.name = "" + partsCount;

        // Add newPart to list of parts
        snakeParts.Add(newPart);
    }

    // Get random position on playground
    private Vector3 RandomPointInBox()
    {
        Vector3 center = playground.GetComponent<Collider>().bounds.center;
        Vector3 size = playground.GetComponent<Collider>().bounds.size;

        return center + new Vector3(
           (Random.value - 0.5f) * size.x,
           (Random.value - 0.5f) * size.y,
           (Random.value - 0.5f) * size.z
        );
    }

    private IEnumerator GameOver()
    {
        // Stop snake movement
        rb.isKinematic = true;
        rb.velocity = Vector3.zero;

        // Show Gameover for 2 seconds
        game_over_panel.SetActive(true);
        yield return new WaitForSeconds(2);

        // Return to main menu
        SceneManager.LoadScene("Main Menu");
    }

    private void NextLevel()
    {
        // Stop snake movement
        rb.isKinematic = true;
        rb.velocity = Vector3.zero;

        // Reset score
        score = 0;

        // Reset snake to its default state
        partsCount = 3;

        // Increase level number
        LoadingLevel.level++;
        SceneManager.LoadScene("LoadingScene");
    }
}
