using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    public AudioClip clickSound;
    public AudioClip winSound;
    public AudioClip loseSound;
    public AudioClip drawSound;
    public AudioClip backgroundMusic;
    public Slider masterSlider, musicSlider, sfxSlider;
    public AudioMixer audioMixer;

    public AudioSource masterAudioSource;
    public AudioSource musicAudioSource;
    public AudioSource sfxAudioSource;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        PlayBackgroundMusic();
    }

    public void ChangeMasterVol()
    {
        float dB = Mathf.Lerp(-80f, 0f, masterSlider.value);
        if (audioMixer != null && masterSlider != null)
            audioMixer.SetFloat("MasterVol", dB);
        else
            Debug.LogWarning("AudioMixer or Master Slider is not assigned.");
    }

    public void ChangeMusicVol()
    {
        float dB = Mathf.Lerp(-80f, 0f, musicSlider.value);
        if (audioMixer != null && musicSlider != null)
            audioMixer.SetFloat("MusicVol", dB);
        else
            Debug.LogWarning("AudioMixer or Music Slider is not assigned.");
    }

    public void ChangeSFXVol()
    {
        float dB = Mathf.Lerp(-80f, 0f, sfxSlider.value);
        if (audioMixer != null && sfxSlider != null)
            audioMixer.SetFloat("SFXVol", dB);
        else
            Debug.LogWarning("AudioMixer or SFX Slider is not assigned.");
    }

    public void PlayBackgroundMusic()
    {
        if (musicAudioSource != null && backgroundMusic != null)
        {
            musicAudioSource.clip = backgroundMusic;
            musicAudioSource.loop = true;
            musicAudioSource.Play();
        }
        else
            Debug.LogWarning("Music AudioSource or Background Music clip is not assigned.");
    }

    public void PlayClickSound()
    {
        if (sfxAudioSource != null && clickSound != null)
            sfxAudioSource.PlayOneShot(clickSound);
        else
            Debug.LogWarning("SFX AudioSource or Click Sound clip is not assigned.");
    }

    public void PlayWinSound()
    {
        if (sfxAudioSource != null && winSound != null)
            sfxAudioSource.PlayOneShot(winSound);
        else
            Debug.LogWarning("SFX AudioSource or Win Sound clip is not assigned.");
    }
}
