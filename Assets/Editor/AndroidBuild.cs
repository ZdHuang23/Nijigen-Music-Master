#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Headless Android APK build for CI or local batchmode.
/// </summary>
public static class AndroidBuild
{
    private const string ApkPath = "build/android/NijigenMusicMaster.apk";

    public static void BuildApk()
    {
        // Scene GUIDs are not committed (.meta is gitignored); use explicit paths for CI batch builds.
        var buildScenes = new[]
        {
            "Assets/Scenes/gameplay.unity",
            "Assets/Scenes/songselect.unity"
        };

        Directory.CreateDirectory(Path.GetDirectoryName(ApkPath));

        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.stelledge.nijigenmusicmaster");
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
        PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)36;
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;
        EditorUserBuildSettings.buildAppBundle = false;

        var options = new BuildPlayerOptions
        {
            scenes = buildScenes,
            locationPathName = ApkPath,
            target = BuildTarget.Android,
            options = BuildOptions.CompressWithLz4
        };

        var report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
        {
            Debug.LogError("Android build failed: " + report.summary.result);
            EditorApplication.Exit(1);
        }

        Debug.Log("Android APK written to: " + Path.GetFullPath(ApkPath));
        EditorApplication.Exit(0);
    }
}
#endif
