using UnityEngine;

/// <summary>
/// Song packs are read from a user folder on shared storage. Android 11+ with targetSdk 30+
/// blocks that path unless the user grants All files access.
/// </summary>
public static class AndroidStorageAccess
{
    public static void RequestIfNeeded()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
            {
                int sdk = version.GetStatic<int>("SDK_INT");
                if (sdk <= 32 &&
                    !UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.ExternalStorageRead))
                {
                    UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.ExternalStorageRead);
                }

                if (sdk >= 30)
                {
                    using (var environment = new AndroidJavaClass("android.os.Environment"))
                    {
                        if (!environment.CallStatic<bool>("isExternalStorageManager"))
                        {
                            OpenAllFilesAccessSettings();
                        }
                    }
                }
            }
        }
        catch (System.Exception exception)
        {
            Debug.LogWarning("Android storage permission request failed: " + exception.Message);
        }
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private static void OpenAllFilesAccessSettings()
    {
        using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
        using (var uriClass = new AndroidJavaClass("android.net.Uri"))
        using (var intent = new AndroidJavaObject(
            "android.content.Intent",
            "android.settings.ACTION_MANAGE_APP_ALL_FILES_ACCESS_PERMISSION"))
        {
            string packageName = activity.Call<string>("getPackageName");
            var uri = uriClass.CallStatic<AndroidJavaObject>("parse", "package:" + packageName);
            intent.Call<AndroidJavaObject>("setData", uri);
            activity.Call("startActivity", intent);
        }
    }
#endif
}
