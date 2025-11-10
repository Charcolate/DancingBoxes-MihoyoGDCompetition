using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseManager : MonoBehaviour
{
    [Header("Scene Names")]
    [SerializeField] private string mainMenuScene = "MM";
    [SerializeField] private string endingScene = "EndingScene"; // Add your ending scene name here

    private GameObject currentPauseMenuUI;
    private bool isPaused = false;
    private string currentSceneName;

    // Singleton pattern to ensure only one instance exists
    private static PauseManager instance;

    void Awake()
    {
        // Handle singleton pattern
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        currentSceneName = SceneManager.GetActiveScene().name;
        Debug.Log($"PauseManager started in scene: {currentSceneName}");

        // Don't search for UI if we're in main menu
        if (currentSceneName != mainMenuScene)
        {
            FindAndSetupPauseUI();
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"Scene loaded: {scene.name}, Main Menu Scene: {mainMenuScene}");
        CheckSceneChange();
    }

    void Update()
    {
        // Check for ESC key press using the new Input System
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            TogglePause();
        }
    }

    private void CheckSceneChange()
    {
        string newSceneName = SceneManager.GetActiveScene().name;

        if (newSceneName != currentSceneName)
        {
            Debug.Log($"Scene changed from {currentSceneName} to {newSceneName}");

            // Destroy old UI reference
            currentPauseMenuUI = null;
            currentSceneName = newSceneName;

            // If we're in main menu, destroy this manager
            if (newSceneName == mainMenuScene)
            {
                Debug.Log("In main menu, destroying PauseManager");
                // Ensure game is not paused
                if (isPaused)
                {
                    ResumeGame();
                }

                // Destroy this manager when returning to main menu
                if (instance == this)
                {
                    SceneManager.sceneLoaded -= OnSceneLoaded;
                    Destroy(gameObject);
                }
            }
            else
            {
                // Search for UI in new scene
                FindAndSetupPauseUI();

                // Ensure game is not paused when changing to new game scene
                if (isPaused)
                {
                    ResumeGame();
                }
            }
        }
    }

    private void FindAndSetupPauseUI()
    {
        // Search for pause UI in the scene
        currentPauseMenuUI = FindPauseMenuInScene();

        if (currentPauseMenuUI != null)
        {
            // Hide the pause menu at start
            currentPauseMenuUI.SetActive(false);

            // Setup button events
            SetupButtonEvents();

            Debug.Log("Pause UI found and setup completed for scene: " + currentSceneName);
        }
        else
        {
            Debug.LogWarning("No pause UI found in scene: " + currentSceneName + ". Please make sure there's a UI object with 'PauseMenu' in its name.");
        }
    }

    private GameObject FindPauseMenuInScene()
    {
        // Search for any UI object that might be the pause menu
        // Look for common names
        string[] possibleNames = { "PauseMenu", "PauseUI", "PausePanel", "PauseCanvas", "Pause" };

        foreach (string name in possibleNames)
        {
            GameObject foundObj = GameObject.Find(name);
            if (foundObj != null)
            {
                Debug.Log($"Found pause UI with name: {name}");
                return foundObj;
            }
        }

        // If no exact match, search for objects containing "pause" (case insensitive)
        Canvas[] allCanvases = FindObjectsOfType<Canvas>(true);
        foreach (Canvas canvas in allCanvases)
        {
            if (canvas.name.ToLower().Contains("pause"))
            {
                Debug.Log($"Found pause UI with canvas name: {canvas.name}");
                return canvas.gameObject;
            }

            // Also check if any of its children have pause-related names
            foreach (Transform child in canvas.transform)
            {
                if (child.name.ToLower().Contains("pause"))
                {
                    Debug.Log($"Found pause UI with child name: {child.name}");
                    return child.gameObject;
                }
            }
        }

        Debug.Log("No pause UI found in scene");
        return null;
    }

    private void SetupButtonEvents()
    {
        if (currentPauseMenuUI == null) return;

        // Find buttons by searching for common names
        var continueBtn = FindButton(new string[] { "ContinueButton", "ResumeButton", "Continue", "Resume" });
        var restartBtn = FindButton(new string[] { "RestartButton", "Restart", "ResetButton" });
        var mainMenuBtn = FindButton(new string[] { "MainMenuButton", "MenuButton", "MainMenu", "ToMenu" });
        var winBtn = FindButton(new string[] { "Win" }); // Specifically looking for "Win" button
        var exitBtn = FindButton(new string[] { "ExitButton", "QuitButton", "Exit", "Quit" });

        // Setup button events
        if (continueBtn != null)
        {
            continueBtn.onClick.RemoveAllListeners();
            continueBtn.onClick.AddListener(ResumeGame);
            Debug.Log("Continue button setup successfully");
        }

        if (restartBtn != null)
        {
            restartBtn.onClick.RemoveAllListeners();
            restartBtn.onClick.AddListener(RestartGame);
            Debug.Log("Restart button setup successfully");
        }

        if (mainMenuBtn != null)
        {
            mainMenuBtn.onClick.RemoveAllListeners();
            mainMenuBtn.onClick.AddListener(ReturnToMainMenu);
            Debug.Log("Main Menu button setup successfully");
        }
        else
        {
            Debug.LogError("Main Menu button NOT found! Check your button names.");
        }

        if (winBtn != null)
        {
            winBtn.onClick.RemoveAllListeners();
            winBtn.onClick.AddListener(GoToEndingScene);
            Debug.Log("Win button found and setup successfully - will load ending scene");
        }
        else
        {
            Debug.LogWarning("Win button not found. If you want this feature, make sure you have a button named 'Win' in your pause menu.");
        }

        if (exitBtn != null)
        {
            exitBtn.onClick.RemoveAllListeners();
            exitBtn.onClick.AddListener(ExitGame);
            Debug.Log("Exit button setup successfully");
        }

        // Log which buttons were found
        Debug.Log($"Buttons found - Continue: {continueBtn != null}, Restart: {restartBtn != null}, MainMenu: {mainMenuBtn != null}, Win: {winBtn != null}, Exit: {exitBtn != null}");
    }

    private Button FindButton(string[] possibleNames)
    {
        if (currentPauseMenuUI == null) return null;

        // Search for buttons in the pause menu hierarchy
        Button[] allButtons = currentPauseMenuUI.GetComponentsInChildren<Button>(true);

        foreach (Button button in allButtons)
        {
            foreach (string name in possibleNames)
            {
                if (button.name.Equals(name, System.StringComparison.OrdinalIgnoreCase))
                {
                    return button;
                }
            }
        }

        return null;
    }

    private void TogglePause()
    {
        // Don't allow pausing in main menu or if no UI found
        if (SceneManager.GetActiveScene().name == mainMenuScene || currentPauseMenuUI == null)
        {
            Debug.Log("Cannot pause - in main menu or no UI found");
            return;
        }

        if (isPaused)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }

    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f; // Freeze game time

        if (currentPauseMenuUI != null)
        {
            currentPauseMenuUI.SetActive(true);
        }

        // Show and unlock cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Debug.Log("Game Paused");
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f; // Resume game time

        if (currentPauseMenuUI != null)
        {
            currentPauseMenuUI.SetActive(false);
        }

        // Hide and lock cursor (adjust based on your game)
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Debug.Log("Game Resumed");
    }

    public void RestartGame()
    {
        Debug.Log("Restarting game...");
        // Resume time scale before loading scene
        Time.timeScale = 1f;
        isPaused = false;

        // Reload current scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void ReturnToMainMenu()
    {
        Debug.Log($"Attempting to return to main menu: {mainMenuScene}");

        // Resume time scale before loading scene
        Time.timeScale = 1f;
        isPaused = false;

        // Try loading by name first, then by build index if that fails
        try
        {
            SceneManager.LoadScene(mainMenuScene);
            Debug.Log($"Loading main menu scene by name: {mainMenuScene}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load scene by name: {mainMenuScene}. Error: {e.Message}");
            // Fallback: try to load scene at index 0 (usually main menu)
            SceneManager.LoadScene(0);
            Debug.Log("Falling back to loading scene at index 0");
        }
    }

    public void GoToEndingScene()
    {
        Debug.Log($"Attempting to go to ending scene: {endingScene}");

        // Resume time scale before loading scene
        Time.timeScale = 1f;
        isPaused = false;

        // Try loading by name first, then by build index if that fails
        try
        {
            SceneManager.LoadScene(endingScene);
            Debug.Log($"Loading ending scene by name: {endingScene}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load scene by name: {endingScene}. Error: {e.Message}");
            // Fallback: try to find ending scene in build settings
            bool sceneFound = false;
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
                string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
                if (sceneName.ToLower().Contains("ending") || sceneName.ToLower().Contains("end"))
                {
                    SceneManager.LoadScene(i);
                    Debug.Log($"Loading ending scene by index: {i} ({sceneName})");
                    sceneFound = true;
                    break;
                }
            }

            if (!sceneFound)
            {
                Debug.LogError("No ending scene found in build settings!");
            }
        }
    }

    public void ExitGame()
    {
        Debug.Log("Exiting game...");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // Public property to check pause state from other scripts
    public bool IsPaused => isPaused;

    // Clean up when destroyed
    void OnDestroy()
    {
        if (instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            // Ensure time scale is reset
            Time.timeScale = 1f;
            Debug.Log("PauseManager destroyed");
        }
    }
}