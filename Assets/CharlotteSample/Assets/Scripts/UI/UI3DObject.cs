// Create a script for the 3D UI element
using UnityEngine;
using UnityEngine.UI;

public class UI3DObject : MonoBehaviour
{
    [SerializeField] private Camera renderCamera;
    [SerializeField] private RawImage targetImage;
    [SerializeField] private GameObject objectToDisplay;

    private RenderTexture renderTexture;

    void Start()
    {
        // Create render texture
        renderTexture = new RenderTexture(256, 256, 16);
        renderTexture.Create();

        // Setup camera
        renderCamera.targetTexture = renderTexture;
        renderCamera.enabled = true;

        // Setup UI image
        targetImage.texture = renderTexture;

        // Position object in front of camera
        if (objectToDisplay != null)
        {
            objectToDisplay.transform.SetParent(renderCamera.transform);
            objectToDisplay.transform.localPosition = new Vector3(0, 0, 2f);
        }
    }

    void Update()
    {
        // Optional: Rotate object continuously
        if (objectToDisplay != null)
        {
            objectToDisplay.transform.Rotate(0, 30 * Time.deltaTime, 0);
        }
    }

    void OnDestroy()
    {
        if (renderTexture != null)
        {
            renderTexture.Release();
        }
    }
}