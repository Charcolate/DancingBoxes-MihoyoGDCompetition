using UnityEngine;

public class NormalAudio : MonoBehaviour
{
    [Header("Audio Configuration")]
    public AudioClip sceneAudioClip;
    public AudioSource audioSource;

    [Header("Playback Options")]
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0f, 1f)] public float spatialBlend = 0f; // 0 = 2D, 1 = 3D
    public bool playOnSceneEnter = true;
    public bool loopAudio = false;

    [Header("Timing Options")]
    public bool useInitialDelay = false;
    public float initialDelay = 0f;
    public bool fadeIn = false;
    public float fadeInDuration = 2f;

    private void Start()
    {
        InitializeAudioSource();

        if (playOnSceneEnter)
        {
            if (useInitialDelay)
            {
                Invoke("PlaySceneAudio", initialDelay);
            }
            else
            {
                PlaySceneAudio();
            }
        }
    }

    private void InitializeAudioSource()
    {
        // Get or add AudioSource component
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        // Configure AudioSource
        audioSource.clip = sceneAudioClip;
        audioSource.volume = volume;
        audioSource.spatialBlend = spatialBlend;
        audioSource.loop = loopAudio;
        audioSource.playOnAwake = false; // We'll control playback manually
    }

    public void PlaySceneAudio()
    {
        if (audioSource != null && sceneAudioClip != null)
        {
            if (fadeIn)
            {
                StartCoroutine(FadeInAudio());
            }
            else
            {
                audioSource.Play();
            }
        }
        else
        {
            Debug.LogWarning("AudioSource or AudioClip is missing!");
        }
    }

    private System.Collections.IEnumerator FadeInAudio()
    {
        audioSource.volume = 0f;
        audioSource.Play();

        float timer = 0f;
        while (timer < fadeInDuration)
        {
            timer += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(0f, volume, timer / fadeInDuration);
            yield return null;
        }

        audioSource.volume = volume;
    }

    public void StopSceneAudio()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }

    // Optional: Restart audio
    public void RestartSceneAudio()
    {
        StopSceneAudio();
        PlaySceneAudio();
    }
}