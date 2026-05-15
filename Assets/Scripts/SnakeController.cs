using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SnakeController : MonoBehaviour
{
    public List<GameObject> snakeParts = new List<GameObject>();

    private int partsCount = 3;

    // Lower = faster, Higher = slower
    public float moveInterval = 0.14f;
    public float minDistance;
    public int growthPerFruit = 3;
    public int level1FruitTarget = 12;
    public int level2FruitTarget = 18;

    private Rigidbody rb;
    private Vector3 currentDirection = Vector3.forward;
    private Vector3 queuedDirection = Vector3.forward;
    private float moveTimer;
    private bool gameOverStarted;
    private bool isPaused;
    private float ignoreSelfCollisionUntil;

    public GameObject playground;
    private GameObject currentPart;
    private GameObject prevPart;
    public GameObject game_over_panel;
    public GameObject pausePanel;

    public GameObject snakePartPrefab;
    public GameObject fruitPrefab;

    private int score;
    private int fruitsEaten;
    public Text scoreText;

    // Use this for initialization
    void Start()
    {
        Time.timeScale = 1f;
        rb = GetComponent<Rigidbody>();
        queuedDirection = currentDirection;
        UpdateScoreText();
        SetupPausePanel();
    }

    // Update is called once per frame
    void Update()
    {
        HandlePauseInput();

        if (gameOverStarted || isPaused)
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
        HandleContact(other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision != null)
        {
            HandleContact(collision.collider);
        }
    }

    private void HandleContact(Collider other)
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

        int growthAmount = GetGrowthPerFruit();
        for (int index = 0; index < growthAmount; index++)
        {
            AddSnakePart();
        }

        ignoreSelfCollisionUntil = Time.time + (GetMoveInterval() * (growthAmount + 3));

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
        fruitsEaten++;
        UpdateScoreText();
        Debug.Log("Score updated: " + score + ", fruits eaten: " + fruitsEaten);

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

        if (currentScene == "Level_1" && fruitsEaten >= level1FruitTarget)
        {
            Debug.Log("Level_1 completed after " + fruitsEaten + " fruits. Loading Level_2.");
            LoadNextLevel("Level_2");
        }
        else if (currentScene == "Level_2" && fruitsEaten >= level2FruitTarget)
        {
            Debug.Log("Level_2 completed after " + fruitsEaten + " fruits. Returning to Main Menu.");
            QuitToMainMenu();
        }
    }

    private void LoadNextLevel(string sceneName)
    {
        Time.timeScale = 1f;
        LoadingLevel.SetNextScene(sceneName);
        SceneManager.LoadScene("LoadingScene");
    }

    private void HandlePauseInput()
    {
        if (Input.GetKeyDown(KeyCode.P) || Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    private void TogglePause()
    {
        if (isPaused)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }

    private void PauseGame()
    {
        if (gameOverStarted)
        {
            return;
        }

        isPaused = true;
        Time.timeScale = 0f;

        if (pausePanel != null)
        {
            pausePanel.SetActive(true);
        }

        Debug.Log("Game Paused");
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;

        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        Debug.Log("Game Resumed");
    }

    public void QuitToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Main Menu");
    }

    private void SetupPausePanel()
    {
        EnsureEventSystem();

        if (pausePanel == null)
        {
            pausePanel = CreatePausePanel();
        }

        pausePanel.SetActive(false);
    }

    private void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<StandaloneInputModule>();
        }
    }

    private GameObject CreatePausePanel()
    {
        GameObject canvasObject = new GameObject("Pause Canvas", typeof(RectTransform));
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject panelObject = new GameObject("Pause Panel", typeof(RectTransform));
        panelObject.transform.SetParent(canvasObject.transform, false);

        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 0f);
        panelRect.anchorMax = new Vector2(1f, 1f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image panelImage = panelObject.AddComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.75f);

        Text titleText = CreatePauseText(panelObject.transform, "Paused", 34, new Vector2(0f, 85f), new Vector2(320f, 70f));
        titleText.fontStyle = FontStyle.Bold;

        Button resumeButton = CreatePauseButton(panelObject.transform, "Resume", new Vector2(0f, 0f));
        resumeButton.onClick.AddListener(ResumeGame);

        Button quitButton = CreatePauseButton(panelObject.transform, "Quit to Main Menu", new Vector2(0f, -70f));
        quitButton.onClick.AddListener(QuitToMainMenu);

        return panelObject;
    }

    private Text CreatePauseText(Transform parent, string label, int fontSize, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        GameObject textObject = new GameObject(label, typeof(RectTransform));
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        Text text = textObject.AddComponent<Text>();
        text.text = label;
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;

        return text;
    }

    private Button CreatePauseButton(Transform parent, string label, Vector2 anchoredPosition)
    {
        GameObject buttonObject = new GameObject(label + " Button", typeof(RectTransform));
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(280f, 48f);

        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.95f);

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;

        Text buttonText = CreatePauseText(buttonObject.transform, label, 20, Vector2.zero, new Vector2(260f, 44f));
        buttonText.color = Color.black;

        return button;
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

    private int GetGrowthPerFruit()
    {
        if (growthPerFruit < 1)
        {
            return 1;
        }

        return growthPerFruit;
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

        Time.timeScale = 1f;
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
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

    private void OnDestroy()
    {
        Time.timeScale = 1f;
    }
}
