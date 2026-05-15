using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SnakeController : MonoBehaviour
{
    public List<GameObject> snakeParts = new List<GameObject>();

    private int partsCount = 3;

    public float moveInterval = 0.25f;
    public float minDistance;
    public int scoreToNextLevel = 5;
    public int scoreToWin = 10;

    private Rigidbody rb;
    private Vector3 currentDirection = Vector3.forward;
    private Vector3 queuedDirection = Vector3.forward;
    private float moveTimer;
    private bool gameOverStarted;
    private float ignoreSelfCollisionUntil;

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
        queuedDirection = currentDirection;
        UpdateScoreText();
    }

    // Update is called once per frame
    void Update()
    {
        if (gameOverStarted)
        {
            return;
        }

        HandleKeyboardInput();
        HandleTouchInput();

        float interval = GetMoveInterval();
        moveTimer += Time.deltaTime;
        if (moveTimer >= interval)
        {
            moveTimer -= interval;
            MoveHeadOneStep();
        }

        MoveSnakeBody();
    }

    private void MoveHeadOneStep()
    {
        currentDirection = queuedDirection;

        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.MovePosition(rb.position + currentDirection * GetStepDistance());
        }
        else
        {
            transform.position += currentDirection * GetStepDistance();
        }
    }

    private void MoveSnakeBody()
    {
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
            float time = Time.deltaTime * distance / GetStepDistance() / GetMoveInterval();
            if (time > 0.5f)
                time = 0.5f;

            // Move current part to new position
            currentPart.transform.position = Vector3.Slerp(currentPart.transform.position,
                newPos, time);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == null || gameOverStarted)
        {
            return;
        }

        if (other.CompareTag("Fruit"))
        {
            EatFruit(other);
            return;
        }

        if (IsDeadlyCollision(other))
        {
            StartGameOver("Hit " + other.gameObject.name + " with tag " + other.gameObject.tag);
        }
    }

    private void EatFruit(Collider fruitCollider)
    {
        Debug.Log("Fruit eaten: " + fruitCollider.gameObject.name);

        AddSnakePart();
        ignoreSelfCollisionUntil = Time.time + (GetMoveInterval() * 2f);

        // Make fruit disappear
        Destroy(fruitCollider.transform.gameObject);

        // Play "eat_fruit" sound if present
        AudioSource audioSource = GetComponent<AudioSource>();
        if (audioSource != null)
        {
            audioSource.Play();
        }

        // Spawn a new fruit on playground at random position
        if (fruitPrefab != null)
        {
            GameObject instance = Instantiate(fruitPrefab) as GameObject;
            instance.transform.position = RandomPointInBox();
            instance.transform.rotation = Quaternion.identity;
        }

        // Increase the player's score
        score++;
        UpdateScoreText();
        Debug.Log("Score updated: " + score);

        CheckLevelProgress();
    }

    private void AddSnakePart()
    {
        if (snakePartPrefab == null || snakeParts.Count == 0)
        {
            Debug.Log("Snake growth skipped because the part prefab or snake part list is missing.");
            return;
        }

        // Spawn a new part of snake
        GameObject newPart = Instantiate(snakePartPrefab) as GameObject;
        newPart.transform.SetParent(transform.parent);

        int lastIndex = snakeParts.Count - 1;
        Vector3 oldPos = snakeParts[lastIndex].transform.localPosition;
        Vector3 growDirection = -currentDirection;

        if (snakeParts.Count > 1)
        {
            Vector3 beforeLastPos = snakeParts[lastIndex - 1].transform.localPosition;
            Vector3 tailDirection = oldPos - beforeLastPos;

            if (tailDirection.sqrMagnitude > 0.01f)
            {
                growDirection = tailDirection.normalized;
            }
        }

        // Attach new part at the end of snake body
        Vector3 newPos = oldPos + (growDirection * GetStepDistance());
        newPart.transform.localPosition = newPos;
        newPart.transform.localRotation = Quaternion.identity;

        partsCount++;

        // Rename newPart to be its position
        newPart.name = "" + partsCount;

        // Add newPart to list of parts
        snakeParts.Add(newPart);
    }

    private void UpdateScoreText()
    {
        if (scoreText != null)
        {
            scoreText.text = "" + score;
        }
    }

    private void CheckLevelProgress()
    {
        string currentScene = SceneManager.GetActiveScene().name;

        if (currentScene == "Level_1" && score >= scoreToNextLevel)
        {
            Debug.Log("Level_1 completed at score " + score + ". Loading Level_2.");
            LoadNextLevel("Level_2");
        }
        else if (currentScene == "Level_2" && score >= scoreToWin)
        {
            Debug.Log("Level_2 completed at score " + score + ". Returning to Main Menu.");
            SceneManager.LoadScene("Main Menu");
        }
    }

    private void LoadNextLevel(string sceneName)
    {
        LoadingLevel.SetNextScene(sceneName);
        SceneManager.LoadScene("LoadingScene");
    }

    private void HandleKeyboardInput()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
        {
            QueueDirection(Vector3.forward);
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
        {
            QueueDirection(Vector3.back);
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
        {
            QueueDirection(Vector3.left);
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
        {
            QueueDirection(Vector3.right);
        }
    }

    private void HandleTouchInput()
    {
        // Keep the existing mobile swipe/drag control path.
        if (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Moved)
        {
            Vector2 deltaPosition = Input.GetTouch(0).deltaPosition;

            if (deltaPosition.sqrMagnitude > 0.01f)
            {
                if (Mathf.Abs(deltaPosition.x) > Mathf.Abs(deltaPosition.y))
                {
                    QueueDirection(deltaPosition.x > 0 ? Vector3.right : Vector3.left);
                }
                else
                {
                    QueueDirection(deltaPosition.y > 0 ? Vector3.forward : Vector3.back);
                }
            }
        }
    }

    private void QueueDirection(Vector3 newDirection)
    {
        if (IsOppositeDirection(currentDirection, newDirection) ||
            IsOppositeDirection(queuedDirection, newDirection))
        {
            return;
        }

        queuedDirection = newDirection;
    }

    private bool IsOppositeDirection(Vector3 firstDirection, Vector3 secondDirection)
    {
        return Vector3.Dot(firstDirection, secondDirection) < -0.9f;
    }

    private float GetMoveInterval()
    {
        if (moveInterval <= 0.01f)
        {
            return 0.01f;
        }

        return moveInterval;
    }

    private float GetStepDistance()
    {
        if (minDistance <= 0.01f)
        {
            return 1f;
        }

        return minDistance;
    }

    private bool IsDeadlyCollision(Collider other)
    {
        if (other.CompareTag("Obstacle"))
        {
            return true;
        }

        if (other.CompareTag("Snake"))
        {
            if (Time.time < ignoreSelfCollisionUntil)
            {
                Debug.Log("Ignored temporary self-collision after fruit growth: " + other.gameObject.name);
                return false;
            }

            if (IsIgnoredSnakePart(other.gameObject))
            {
                Debug.Log("Ignored adjacent snake body contact: " + other.gameObject.name);
                return false;
            }

            return true;
        }

        return false;
    }

    private bool IsIgnoredSnakePart(GameObject snakePart)
    {
        for (int index = 0; index < snakeParts.Count; index++)
        {
            if (snakeParts[index] == snakePart ||
                snakePart.transform.IsChildOf(snakeParts[index].transform))
            {
                return index <= 2;
            }
        }

        return false;
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

    private void StartGameOver(string reason)
    {
        if (gameOverStarted)
        {
            return;
        }

        StartCoroutine(GameOver(reason));
    }

    private IEnumerator GameOver(string reason)
    {
        gameOverStarted = true;
        Debug.Log("Game Over: " + reason);

        // Stop snake movement
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.velocity = Vector3.zero;
        }

        // Show Gameover for 2 seconds
        if (game_over_panel != null)
        {
            game_over_panel.SetActive(true);
        }

        yield return new WaitForSeconds(2);

        // Return to main menu
        SceneManager.LoadScene("Main Menu");
    }
}
