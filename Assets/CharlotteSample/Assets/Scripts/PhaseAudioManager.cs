using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhaseAudioManager : MonoBehaviour
{
    [System.Serializable]
    public class PhaseAudio
    {
        [Range(1, 5)]
        public int bigPhaseNumber = 1;
        public AudioClip backgroundMusic;
        [Range(0f, 1f)]
        public float volume = 0.7f;
        public bool loop = true;
    }

    [Header("References")]
    public GoalManager goalManager;

    [Header("Phase Audio")]
    public List<PhaseAudio> phaseAudios = new List<PhaseAudio>();

    [Header("Settings")]
    public float fadeDuration = 2f;

    private AudioSource audioSource;
    private int currentBigPhase = -1;
    private bool isTransitioning = false;

    void Awake()
    {
        // Always ensure we have an AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            Debug.Log("🔊 Created AudioSource component");
        }

        audioSource.loop = true;
        audioSource.spatialBlend = 0f; // 2D sound
    }

    void Start()
    {
        if (goalManager == null)
        {
            goalManager = FindObjectOfType<GoalManager>();
            if (goalManager == null)
            {
                Debug.LogError("❌ PhaseAudioManager: No GoalManager found in scene!");
                return;
            }
        }

        // Validate phase audios on start
        ValidatePhaseAudios();

        // Start with current phase audio
        int startingPhase = GetCurrentBigPhase();
        if (startingPhase >= 1 && startingPhase <= 5)
        {
            PlayPhaseAudio(startingPhase);
        }
        else
        {
            Debug.Log($"⚠️ PhaseAudioManager: Starting phase is {startingPhase}, waiting for valid phase...");
        }
    }

    void Update()
    {
        if (goalManager == null) return;

        int newBigPhase = GetCurrentBigPhase();

        if (newBigPhase != currentBigPhase && newBigPhase >= 1 && newBigPhase <= 5)
        {
            PlayPhaseAudio(newBigPhase);
        }
    }

    void ValidatePhaseAudios()
    {
        // Ensure we have exactly 5 phases with numbers 1-5
        if (phaseAudios.Count != 5)
        {
            Debug.LogWarning($"⚠️ PhaseAudioManager: Expected 5 phases, found {phaseAudios.Count}. Fixing...");

            // Resize to 5 if needed
            while (phaseAudios.Count < 5)
            {
                phaseAudios.Add(new PhaseAudio());
            }
            while (phaseAudios.Count > 5)
            {
                phaseAudios.RemoveAt(phaseAudios.Count - 1);
            }
        }

        // Set phase numbers to 1-5
        for (int i = 0; i < phaseAudios.Count; i++)
        {
            if (phaseAudios[i].bigPhaseNumber < 1 || phaseAudios[i].bigPhaseNumber > 5)
            {
                phaseAudios[i].bigPhaseNumber = i + 1;
                Debug.Log($"🔧 Fixed phase {i} number to {phaseAudios[i].bigPhaseNumber}");
            }
        }

        Debug.Log("✅ Phase audio configuration validated");
    }

    public int GetCurrentBigPhase()
    {
        if (goalManager == null) return -1;
        int smallPhaseIndex = goalManager.GetCurrentSmallPhaseIndex();
        return Mathf.FloorToInt(smallPhaseIndex / 5) + 1;
    }

    public void PlayPhaseAudio(int bigPhaseNumber)
    {
        if (isTransitioning)
        {
            return;
        }

        PhaseAudio phaseAudio = GetPhaseAudio(bigPhaseNumber);
        if (phaseAudio == null)
        {
            Debug.LogWarning($"❌ PhaseAudioManager: No audio configuration found for big phase {bigPhaseNumber}");
            return;
        }

        if (phaseAudio.backgroundMusic == null)
        {
            Debug.LogWarning($"❌ PhaseAudioManager: No audio clip assigned for big phase {bigPhaseNumber}");
            return;
        }

        StartCoroutine(TransitionToAudio(phaseAudio));
    }

    private PhaseAudio GetPhaseAudio(int bigPhaseNumber)
    {
        foreach (var phaseAudio in phaseAudios)
        {
            if (phaseAudio.bigPhaseNumber == bigPhaseNumber)
            {
                return phaseAudio;
            }
        }
        return null;
    }

    private IEnumerator TransitionToAudio(PhaseAudio phaseAudio)
    {
        isTransitioning = true;

        Debug.Log($"🔊 Switching to Big Phase {phaseAudio.bigPhaseNumber} audio: {phaseAudio.backgroundMusic.name}");

        // Fade out current audio if playing
        if (audioSource.isPlaying)
        {
            yield return StartCoroutine(FadeAudio(0f, fadeDuration));
            audioSource.Stop();
        }

        // Play new audio
        audioSource.clip = phaseAudio.backgroundMusic;
        audioSource.volume = 0f;
        audioSource.loop = phaseAudio.loop;
        audioSource.Play();

        // Fade in new audio
        yield return StartCoroutine(FadeAudio(phaseAudio.volume, fadeDuration));

        currentBigPhase = phaseAudio.bigPhaseNumber;
        isTransitioning = false;

        Debug.Log($"✅ Now playing audio for Big Phase {phaseAudio.bigPhaseNumber}");
    }

    private IEnumerator FadeAudio(float targetVolume, float duration)
    {
        float startVolume = audioSource.volume;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, targetVolume, elapsedTime / duration);
            yield return null;
        }

        audioSource.volume = targetVolume;
    }

    // Public control methods
    public void StopAudio()
    {
        StopAllCoroutines();
        if (audioSource != null)
        {
            audioSource.Stop();
        }
        isTransitioning = false;
    }

    public void SetVolume(float volume)
    {
        if (audioSource != null)
        {
            audioSource.volume = volume;
        }
    }

    // Gizmos for debugging
    void OnDrawGizmosSelected()
    {
        if (goalManager != null && audioSource != null)
        {
            int currentBigPhase = GetCurrentBigPhase();

            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, 0.8f);

#if UNITY_EDITOR
            string status = audioSource.isPlaying ? "Playing" : "Stopped";
            UnityEditor.Handles.Label(transform.position + Vector3.up * 1.2f,
                $"Audio: Big Phase {currentBigPhase}\n{status}");
#endif
        }
    }

    // Editor validation
    void OnValidate()
    {
        // Auto-fix phase numbers in editor
        for (int i = 0; i < phaseAudios.Count; i++)
        {
            if (phaseAudios[i].bigPhaseNumber < 1 || phaseAudios[i].bigPhaseNumber > 5)
            {
                phaseAudios[i].bigPhaseNumber = Mathf.Clamp(i + 1, 1, 5);
            }
        }

        // Warn if not exactly 5 phases
        if (phaseAudios.Count != 5)
        {
            Debug.LogWarning("PhaseAudioManager: Please configure exactly 5 big phases!");
        }
    }
}