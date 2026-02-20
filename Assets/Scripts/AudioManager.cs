using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Settings")]
    [Range(0f, 1f)] public float musicVolume = 1f;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    private bool musicMuted = false;
    private bool sfxMuted = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        ApplyVolumes();
    }

    // =========================
    // MUSIC
    // =========================

    public void PlayMusic(AudioClip clip, bool loop = true)
    {
        if (clip == null) return;

        musicSource.clip = clip;
        musicSource.loop = loop;
        musicSource.Play();
    }

    public void StopMusic()
    {
        musicSource.Stop();
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = volume;
        ApplyVolumes();
    }

    public void MuteMusic(bool mute)
    {
        musicMuted = mute;
        ApplyVolumes();
    }

    // =========================
    // SFX
    // =========================

    public void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;

        sfxSource.PlayOneShot(clip, sfxVolume);
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = volume;
        ApplyVolumes();
    }

    public void MuteSFX(bool mute)
    {
        sfxMuted = mute;
        ApplyVolumes();
    }

    // =========================
    // APPLY SETTINGS
    // =========================

    private void ApplyVolumes()
    {
        musicSource.volume = musicMuted ? 0f : musicVolume;
        sfxSource.volume = sfxMuted ? 0f : sfxVolume;
    }

    //FOR UI (RAFAEL)
    public void OnMusicToggleChanged(bool isOn)
    {
        AudioManager.Instance.MuteMusic(!isOn);
    }

    public void OnSFXToggleChanged(bool isOn)
    {
        AudioManager.Instance.MuteSFX(!isOn);
    }
}