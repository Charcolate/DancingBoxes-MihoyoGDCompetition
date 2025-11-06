using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class TutorialManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject tutorialPanel;
    public Image leftSideArt;
    public Button nextButton;
    public Button previousButton;
    public Button startGameButton;
    public Text pageIndicator;

    [Header("Tutorial Art")]
    public Sprite[] tutorialArt; // 4 different art images for left side

    [Header("Practice Areas")]
    public GameObject[] practiceAreas; // 4 different practice areas for right side

    [Header("Scene Settings")]
    public string gameSceneName = "game";

    [Header("Tutorial Objects")]
    public GameObject ghostPrefab;
    public GameObject wandererPrefab;
    public GameObject projectilePrefab;

    [Header("Cylinder Controller")]
    public ColliderController_NewInput cylinderController;

    private int currentPage = 0;
    private GameObject currentGhost;
    private GameObject currentWanderer;

    void Start()
    {
        nextButton.onClick.AddListener(NextPage);
        previousButton.onClick.AddListener(PreviousPage);
        startGameButton.onClick.AddListener(StartGame);

        startGameButton.gameObject.SetActive(false);

        // Ensure cylinder controller is disabled at start
        if (cylinderController != null)
            cylinderController.enabled = false;

        ShowPage(0);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            NextPage();
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            PreviousPage();
        }
    }

    void NextPage()
    {
        if (currentPage < tutorialArt.Length - 1)
        {
            currentPage++;
            ShowPage(currentPage);
        }
        else
        {
            EndTutorial();
        }
    }

    void PreviousPage()
    {
        if (currentPage > 0)
        {
            currentPage--;
            ShowPage(currentPage);
        }
    }

    void ShowPage(int pageIndex)
    {
        // Clean up previous page objects
        CleanupPageObjects();

        // Update left side art
        if (leftSideArt != null && tutorialArt.Length > pageIndex && tutorialArt[pageIndex] != null)
        {
            leftSideArt.sprite = tutorialArt[pageIndex];
        }

        // Update page indicator
        pageIndicator.text = $"{pageIndex + 1}/{tutorialArt.Length}";

        // Update practice area and setup page-specific objects
        UpdatePracticeArea(pageIndex);
        SetupPageObjects(pageIndex);

        // Update button states
        previousButton.interactable = (pageIndex > 0);

        if (pageIndex == tutorialArt.Length - 1)
        {
            nextButton.gameObject.SetActive(false);
            startGameButton.gameObject.SetActive(true);
        }
        else
        {
            nextButton.gameObject.SetActive(true);
            startGameButton.gameObject.SetActive(false);
        }
    }

    void UpdatePracticeArea(int pageIndex)
    {
        // Hide all practice areas
        foreach (var area in practiceAreas)
        {
            if (area != null)
                area.SetActive(false);
        }

        // Show current practice area
        if (practiceAreas.Length > pageIndex && practiceAreas[pageIndex] != null)
        {
            practiceAreas[pageIndex].SetActive(true);
        }

        // Enable/disable cylinder controller based on page
        if (cylinderController != null)
        {
            // Enable cylinder controller for pages that need cylinder movement (pages 2, 3, 4)
            bool enableController = (pageIndex == 1 || pageIndex == 2 || pageIndex == 3);
            cylinderController.enabled = enableController;

            if (enableController)
            {
                Debug.Log($"🎮 Cylinder controller enabled for page {pageIndex + 1}");
            }
        }
    }

    void SetupPageObjects(int pageIndex)
    {
        switch (pageIndex)
        {
            case 0: // Page 1: Introduction
                SetupPage1();
                break;
            case 1: // Page 2: Cylinder Movement
                SetupPage2();
                break;
            case 2: // Page 3: Path Creation
                SetupPage3();
                break;
            case 3: // Page 4: Projectile Blocking
                SetupPage4();
                break;
        }
    }

    void SetupPage1()
    {
        // Page 1: Simple character display
        Vector3 ghostPos = new Vector3(-3f, 1f, 0f);
        Vector3 wandererPos = new Vector3(3f, 1f, 0f);

        currentGhost = Instantiate(ghostPrefab, ghostPos, Quaternion.identity);
        currentWanderer = Instantiate(wandererPrefab, wandererPos, Quaternion.identity);

        // Make them face forward
        currentGhost.transform.rotation = Quaternion.Euler(0, 90, 0);
        currentWanderer.transform.rotation = Quaternion.Euler(0, -90, 0);

        Debug.Log("👻 Page 1: Character display setup");
    }

    void SetupPage2()
    {
        // Page 2: Cylinder movement practice
        ResetCylinderPositions();

        // Create simple target for practice
        CreateTargetMarker(new Vector3(5f, 0.1f, 0f), "Target", Color.yellow);

        Debug.Log("🔵 Page 2: Cylinder movement practice setup");
    }

    void SetupPage3()
    {
        // Page 3: Path creation practice
        ResetCylinderPositions();

        // Create start and end markers
        CreateWaypointMarker(new Vector3(-6f, 0.1f, 0f), "Start", Color.green);
        CreateWaypointMarker(new Vector3(6f, 0.1f, 0f), "End", Color.red);

        // Create obstacle
        CreateObstacle(new Vector3(0f, 0.5f, 0f), "Obstacle", Color.black);

        Debug.Log("🛣️ Page 3: Path creation setup");
    }

    void SetupPage4()
    {
        // Page 4: Projectile blocking practice
        ResetCylinderPositions();

        // Spawn ghost and wanderer
        Vector3 ghostPos = new Vector3(-8f, 1f, 0f);
        currentGhost = Instantiate(ghostPrefab, ghostPos, Quaternion.identity);

        Vector3 wandererPos = new Vector3(8f, 1f, 0f);
        currentWanderer = Instantiate(wandererPrefab, wandererPos, Quaternion.identity);

        // Make them face each other
        currentGhost.transform.LookAt(currentWanderer.transform.position);
        currentWanderer.transform.LookAt(currentGhost.transform.position);

        // Add projectile shooter
        FlatPlaneGhostShooter ghostShooter = currentGhost.AddComponent<FlatPlaneGhostShooter>();
        ghostShooter.projectilePrefab = projectilePrefab;
        ghostShooter.target = currentWanderer.transform;
        ghostShooter.fireInterval = 2f;
        ghostShooter.projectileSpeed = 6f;

        Debug.Log("🛡️ Page 4: Projectile blocking setup");
    }

    void ResetCylinderPositions()
    {
        if (cylinderController == null || cylinderController.cylinders == null) return;

        Vector3[] defaultPositions = {
            new Vector3(-2f, 0f, -2f),
            new Vector3(0f, 0f, -2f),
            new Vector3(2f, 0f, -2f),
            new Vector3(-1f, 0f, 2f),
            new Vector3(1f, 0f, 2f)
        };

        for (int i = 0; i < cylinderController.cylinders.Length && i < defaultPositions.Length; i++)
        {
            if (cylinderController.cylinders[i] != null)
            {
                cylinderController.cylinders[i].transform.position = defaultPositions[i];
                Vector3 pos = cylinderController.cylinders[i].transform.position;
                pos.y = 0f;
                cylinderController.cylinders[i].transform.position = pos;
            }
        }

        Debug.Log("🔄 Cylinder positions reset");
    }

    void CreateWaypointMarker(Vector3 position, string label, Color color)
    {
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
        marker.name = label + "Marker";
        marker.transform.localScale = new Vector3(1f, 0.1f, 1f);
        marker.transform.position = position;
        marker.tag = "PracticeObject";

        Renderer rend = marker.GetComponent<Renderer>();
        rend.material.color = color;
    }

    void CreateTargetMarker(Vector3 position, string label, Color color)
    {
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        marker.name = label + "Target";
        marker.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
        marker.transform.position = position;
        marker.tag = "PracticeObject";

        Renderer rend = marker.GetComponent<Renderer>();
        rend.material.color = color;
    }

    void CreateObstacle(Vector3 position, string label, Color color)
    {
        GameObject obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obstacle.name = label;
        obstacle.transform.localScale = new Vector3(2f, 1f, 2f);
        obstacle.transform.position = position;
        obstacle.tag = "PracticeObject";

        Renderer rend = obstacle.GetComponent<Renderer>();
        rend.material.color = color;
    }

    void CleanupPageObjects()
    {
        if (currentGhost != null) Destroy(currentGhost);
        if (currentWanderer != null) Destroy(currentWanderer);

        GameObject[] practiceObjects = GameObject.FindGameObjectsWithTag("PracticeObject");
        foreach (var obj in practiceObjects)
        {
            Destroy(obj);
        }
    }

    void StartGame()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    void EndTutorial()
    {
        CleanupPageObjects();
        StartGame();
    }
}