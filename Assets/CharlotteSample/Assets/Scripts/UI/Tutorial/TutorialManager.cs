using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SimpleSceneNavigation : MonoBehaviour
{
    [Header("Scene Names")]
    public string nextSceneName;
    public string previousSceneName;
    public string gameSceneName = "game";

    [Header("Buttons - Drag your buttons here")]
    public Button nextButton;
    public Button previousButton;
    public Button startGameButton;

    void Start()
    {
        Debug.Log("🎯 SimpleSceneNavigation Started");

        // Set up button listeners - this should work automatically
        if (nextButton != null)
        {
            nextButton.onClick.AddListener(LoadNextScene);
            Debug.Log("✅ Next button listener added via code");
        }
        else
        {
            Debug.LogError("❌ NextButton is null! Drag the button into the inspector.");
        }

        if (previousButton != null)
        {
            previousButton.onClick.AddListener(LoadPreviousScene);
            Debug.Log("✅ Previous button listener added via code");
        }
        else
        {
            Debug.LogError("❌ PreviousButton is null! Drag the button into the inspector.");
        }

        if (startGameButton != null)
        {
            startGameButton.onClick.AddListener(StartGame);
            Debug.Log("✅ StartGame button listener added via code");
        }
        else
        {
            Debug.LogError("❌ StartGameButton is null! Drag the button into the inspector.");
        }
    }

    void LoadNextScene()
    {
        Debug.Log("🎯 Next Button Clicked!");
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }

    void LoadPreviousScene()
    {
        Debug.Log("🎯 Previous Button Clicked!");
        if (!string.IsNullOrEmpty(previousSceneName))
        {
            SceneManager.LoadScene(previousSceneName);
        }
    }

    void StartGame()
    {
        Debug.Log("🎯 Start Game Button Clicked!");
        if (!string.IsNullOrEmpty(gameSceneName))
        {
            SceneManager.LoadScene(gameSceneName);
        }
    }
}