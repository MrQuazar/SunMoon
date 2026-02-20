using UnityEngine;

public class SettingHandler : MonoBehaviour
{
    public void SetMusicVolume(bool val)
    {
        AudioManager.Instance.MuteMusic(val);
    }

    public void SetSFXVolume(bool val)
    {
        AudioManager.Instance.MuteSFX(val);
    }
}