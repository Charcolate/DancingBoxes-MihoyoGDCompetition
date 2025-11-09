using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SphericalCameraFollow : MonoBehaviour
{
    [System.Serializable]
    public class PhaseCamera
    {
        public string phaseName;
        public Transform cameraTransform;
        [HideInInspector] public float transitionDuration = 2.0f; // Always 2 seconds
        [HideInInspector] public bool waitForPhaseCompletion = true; // Always wait
    }

    [Header("References")]
    public GoalManager goalManager;
    public Camera mainCamera;

    [Header("Phase Cameras")]
    public List<PhaseCamera> phaseCameras = new List<PhaseCamera>();

    [Header("Settings")]
    public bool autoCycle = true;
    private const float TRANSITION_DURATION = 2.0f; // Always 2 seconds

    private int currentCameraIndex = -1;
    private int targetCameraIndex = -1;
    private bool isTransitioning = false;
    private Vector3 transitionVelocity = Vector3.zero;

    void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (goalManager == null)
            goalManager = FindObjectOfType<GoalManager>();

        // Start with first camera if phases exist
        if (phaseCameras.Count > 0 && goalManager != null)
        {
            int startPhase = goalManager.GetCurrentSmallPhaseIndex();
            if (startPhase < phaseCameras.Count)
            {
                currentCameraIndex = startPhase;
                targetCameraIndex = startPhase;
                // Snap immediately to start camera (no transition)
                SnapToCamera(phaseCameras[startPhase]);
            }
        }
    }

    void Update()
    {
        if (!autoCycle || goalManager == null) return;

        int currentPhase = goalManager.GetCurrentSmallPhaseIndex();

        // Only switch cameras when phase changes AND we're not already transitioning
        if (currentPhase < phaseCameras.Count && currentPhase != targetCameraIndex && !isTransitioning)
        {
            targetCameraIndex = currentPhase;
            StartCoroutine(TransitionToCamera(phaseCameras[currentPhase]));
        }
    }

    private void SnapToCamera(PhaseCamera phaseCamera)
    {
        if (phaseCamera.cameraTransform == null) return;

        mainCamera.transform.position = phaseCamera.cameraTransform.position;
        mainCamera.transform.rotation = phaseCamera.cameraTransform.rotation;

        Debug.Log($"📸 Snapped to camera: {phaseCamera.phaseName}");
    }

    private System.Collections.IEnumerator TransitionToCamera(PhaseCamera phaseCamera)
    {
        if (phaseCamera.cameraTransform == null || isTransitioning) yield break;

        isTransitioning = true;

        Transform targetTransform = phaseCamera.cameraTransform;
        float elapsedTime = 0f;

        Vector3 startPosition = mainCamera.transform.position;
        Quaternion startRotation = mainCamera.transform.rotation;

        Debug.Log($"🎬 Starting 2s transition to: {phaseCamera.phaseName}");

        while (elapsedTime < TRANSITION_DURATION)
        {
            float t = elapsedTime / TRANSITION_DURATION;

            // Smooth position transition
            mainCamera.transform.position = Vector3.Lerp(startPosition, targetTransform.position, t);

            // Smooth rotation transition
            mainCamera.transform.rotation = Quaternion.Lerp(startRotation, targetTransform.rotation, t);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure exact final position and rotation
        mainCamera.transform.position = targetTransform.position;
        mainCamera.transform.rotation = targetTransform.rotation;

        currentCameraIndex = targetCameraIndex;
        isTransitioning = false;

        Debug.Log($"✅ Completed transition to: {phaseCamera.phaseName}");
    }

    // Called by GoalManager when a phase completes
    public void OnPhaseCompleted(int phaseIndex)
    {
        if (!autoCycle) return;

        int nextPhase = phaseIndex + 1;

        // If we have a camera for the next phase, switch to it
        if (nextPhase < phaseCameras.Count && !isTransitioning)
        {
            targetCameraIndex = nextPhase;
            StartCoroutine(TransitionToCamera(phaseCameras[nextPhase]));
        }
        else if (nextPhase >= phaseCameras.Count)
        {
            Debug.Log("🎬 All phase cameras completed!");
        }
    }

    // Manual control methods
    public void SwitchToCamera(string cameraName)
    {
        for (int i = 0; i < phaseCameras.Count; i++)
        {
            if (phaseCameras[i].phaseName == cameraName && !isTransitioning)
            {
                targetCameraIndex = i;
                StartCoroutine(TransitionToCamera(phaseCameras[i]));
                return;
            }
        }
        Debug.LogWarning($"PhaseCameraManager: Camera with name '{cameraName}' not found or already transitioning!");
    }

    public void SetAutoCycle(bool enable)
    {
        autoCycle = enable;
    }

    public string GetCurrentCameraName()
    {
        if (currentCameraIndex >= 0 && currentCameraIndex < phaseCameras.Count)
            return phaseCameras[currentCameraIndex].phaseName;
        return "No Camera";
    }

    public int GetCurrentCameraIndex()
    {
        return currentCameraIndex;
    }

    public bool IsTransitioning()
    {
        return isTransitioning;
    }

    // Editor helper
    void OnValidate()
    {
        // Ensure phase names are unique and set fixed values
        HashSet<string> usedNames = new HashSet<string>();
        for (int i = 0; i < phaseCameras.Count; i++)
        {
            if (string.IsNullOrEmpty(phaseCameras[i].phaseName))
            {
                phaseCameras[i].phaseName = $"Phase {i}";
            }

            if (usedNames.Contains(phaseCameras[i].phaseName))
            {
                phaseCameras[i].phaseName = $"{phaseCameras[i].phaseName} {i}";
            }
            usedNames.Add(phaseCameras[i].phaseName);

            // Force fixed values
            phaseCameras[i].transitionDuration = TRANSITION_DURATION;
            phaseCameras[i].waitForPhaseCompletion = true;
        }
    }

    // Gizmos to visualize camera positions in scene
    void OnDrawGizmosSelected()
    {
        if (phaseCameras == null) return;

        Gizmos.color = Color.cyan;
        foreach (var phaseCamera in phaseCameras)
        {
            if (phaseCamera.cameraTransform != null)
            {
                Gizmos.DrawWireSphere(phaseCamera.cameraTransform.position, 0.5f);
                Gizmos.DrawLine(phaseCamera.cameraTransform.position, phaseCamera.cameraTransform.position + phaseCamera.cameraTransform.forward * 2f);
            }
        }
    }
}