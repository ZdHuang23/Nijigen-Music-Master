using System.IO;
using UnityEngine;

/// <summary>
/// Android 10+ uses app-scoped storage under persistentDataPath. Legacy shared-storage
/// folders are only used on older API levels.
/// </summary>
public static class AndroidStorageAccess
{
    public static string PersistentMusicDataPath
    {
        get { return Application.persistentDataPath + "/MusicData"; }
    }

    public static void PrepareForMusicLoading()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        EnsurePersistentMusicDataDirectory();

        var sdk = GetSdkInt();
        if (sdk >= 30)
        {
            return;
        }

        if (sdk >= 23 &&
            sdk <= 32 &&
            !UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.ExternalStorageRead))
        {
            UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.ExternalStorageRead);
        }
#endif
    }

    public static void EnsurePersistentMusicDataDirectory()
    {
        var path = PersistentMusicDataPath;
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
            Debug.Log("Created MusicData directory: " + path);
        }
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    public static int GetSdkInt()
    {
        using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
        {
            return version.GetStatic<int>("SDK_INT");
        }
    }
#else
    public static int GetSdkInt()
    {
        return 0;
    }
#endif
}
