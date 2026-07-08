using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;

public class FirebaseRemoteConfigConstants
{
    public static string BASE_URL
    {
        get => PlayerPrefs.GetString("BASE_URL", "");
        set => PlayerPrefs.SetString("BASE_URL", value);
    }
}
