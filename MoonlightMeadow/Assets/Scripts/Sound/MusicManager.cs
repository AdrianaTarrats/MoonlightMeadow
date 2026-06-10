using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
/// <summary>
/// Singleton that plays and cross-fades background music tracks.
/// <see cref="PlayTrack"/> switches to a new clip with a fade transition;
/// <see cref="PlayDefault"/> returns to the scene's default track (or fades out if none).
/// </summary>
public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [SerializeField] private float fadeDuration = 1f;
    [SerializeField] private AudioClip defaultClip;
    [SerializeField] [Range(0f, 1f)] private float defaultVolume = 1f;

    private AudioSource audioSource;
    private Coroutine fadeCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        audioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        if (defaultClip != null)
            PlayTrack(defaultClip, defaultVolume);
    }

    public void PlayTrack(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;
        if (audioSource.clip == clip && audioSource.isPlaying) return;

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(CrossFade(clip, volume));
    }

    public void PlayDefault()
    {
        if (defaultClip != null)
            PlayTrack(defaultClip, defaultVolume);
        else
        {
            if (fadeCoroutine != null)
                StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(FadeOut());
        }
    }

    private IEnumerator FadeOut()
    {
        float startVol = audioSource.volume;
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVol, 0f, t / fadeDuration);
            yield return null;
        }
        audioSource.Stop();
        audioSource.volume = startVol;
        fadeCoroutine = null;
    }

    private IEnumerator CrossFade(AudioClip newClip, float targetVolume)
    {
        if (audioSource.isPlaying)
        {
            float startVol = audioSource.volume;
            float t = 0f;
            while (t < fadeDuration)
            {
                t += Time.deltaTime;
                audioSource.volume = Mathf.Lerp(startVol, 0f, t / fadeDuration);
                yield return null;
            }
            audioSource.Stop();
        }

        audioSource.volume = 0f;
        audioSource.clip = newClip;
        audioSource.loop = true;
        audioSource.Play();

        float t2 = 0f;
        while (t2 < fadeDuration)
        {
            t2 += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(0f, targetVolume, t2 / fadeDuration);
            yield return null;
        }
        audioSource.volume = targetVolume;
        fadeCoroutine = null;
    }
}
