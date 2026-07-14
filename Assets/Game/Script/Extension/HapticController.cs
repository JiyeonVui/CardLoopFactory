using UnityEngine;

public static class HapticController {
    public static bool IsEnabled { get; set; } = true;

    public static void Light() {
        if (!IsEnabled) return;
#if UNITY_ANDROID && !UNITY_EDITOR
        VibrateAndroid(200);
#elif UNITY_IOS && !UNITY_EDITOR
        Handheld.Vibrate();
#endif
    }

    public static void Touch() {
        if (!IsEnabled) return;
#if UNITY_ANDROID && !UNITY_EDITOR
        VibrateAndroid(50);
#elif UNITY_IOS && !UNITY_EDITOR
        Handheld.Vibrate();
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private static void VibrateAndroid(long milliseconds) {
        using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer")) {
            AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

            AndroidJavaObject vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");

            if (vibrator == null) return;

            using (AndroidJavaClass version = new AndroidJavaClass("android.os.Build$VERSION")) {
                int sdkInt = version.GetStatic<int>("SDK_INT");

                if (sdkInt >= 26) {
                    using (AndroidJavaClass vibrationEffect = new AndroidJavaClass("android.os.VibrationEffect")) {
                        AndroidJavaObject effect = vibrationEffect.CallStatic<AndroidJavaObject>(
                            "createOneShot",
                            milliseconds,
                            vibrationEffect.GetStatic<int>("DEFAULT_AMPLITUDE")
                        );

                        vibrator.Call("vibrate", effect);
                    }
                } else {
                    vibrator.Call("vibrate", milliseconds);
                }
            }
        }
    }
#endif
}
