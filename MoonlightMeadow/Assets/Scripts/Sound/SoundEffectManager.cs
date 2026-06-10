using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Singleton that exposes static helpers for playing one-shot sound effects
/// and NPC voice clips. Manages three <see cref="AudioSource"/> channels:
/// normal, random-pitch (footsteps, etc.), and voice.
/// </summary>
public class SoundEffectManager : MonoBehaviour
{
    private static SoundEffectManager instance;
    private static AudioSource audioSource;
    private static AudioSource randomPitchAudioSource;
    private static AudioSource voiceAudioSource;
    private static SoundEffectLibrary soundLibrary;
    [SerializeField] private Slider sfxSlider;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        AudioSource[] sources = GetComponents<AudioSource>();
        audioSource = sources[0];
        randomPitchAudioSource = sources[1];
        voiceAudioSource = sources[2];
        voiceAudioSource.volume = 0.05f;
        soundLibrary = GetComponent<SoundEffectLibrary>();
    }

    public static void Play(string soundName, bool randomPitch = false)
    {
        AudioClip clip = soundLibrary.GetRandomClip(soundName);
        if (clip != null)
        {
            if (randomPitch)
            {
                randomPitchAudioSource.pitch = Random.Range(1f, 1.5f);
                randomPitchAudioSource.PlayOneShot(clip);
            }
            else
            {
                audioSource.PlayOneShot(clip);
            }
        }
    }

    public static void PlayVoice(AudioClip audioClip, float pitch = 1f)
    {
        voiceAudioSource.pitch = pitch;
        voiceAudioSource.PlayOneShot(audioClip);
    }

    void Start()
    {
        sfxSlider.onValueChanged.AddListener(delegate { OnValueChanged(); });
    }

    public static void SetVolume(float volume)
    {
        audioSource.volume = volume;
        randomPitchAudioSource.volume = volume;
        voiceAudioSource.volume = volume;
    }

    public void OnValueChanged()
    {
        SetVolume(sfxSlider.value);
    }
}
