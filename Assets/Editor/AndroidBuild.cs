#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Headless Android APK build for CI (game-ci) or local batchmode.
/// </summary>
public static class AndroidBuild
{
    private const string DefaultApkPath = "build/android/NijigenMusicMaster.apk";

    private static readonly string[] BuildScenes =
    {
        "Assets/Scenes/gameplay.unity",
        "Assets/Scenes/songselect.unity"
    };

    public static void BuildApk()
    {
        EnsureAndroidPlayerSettings();
        SwitchToAndroid();

        var outputPath = ResolveOutputPath();
        var outputDirectory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        var options = new BuildPlayerOptions
        {
            scenes = BuildScenes,
            locationPathName = outputPath,
            target = BuildTarget.Android,
            options = BuildOptions.CompressWithLz4
        };

        var report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
        {
            Debug.LogError("Android build failed: " + report.summary.result);
            EditorApplication.Exit(101);
        }

        Debug.Log("Android APK written to: " + Path.GetFullPath(outputPath));
        EditorApplication.Exit(0);
    }

    private static void EnsureAndroidPlayerSettings()
    {
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.stelledge.nijigenmusicmaster");
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
        PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)36;
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;
        EditorUserBuildSettings.buildAppBundle = false;
        EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;
    }

    private static void SwitchToAndroid()
    {
        if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android)
        {
            return;
        }

        if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
        {
            Debug.LogError("Failed to switch active build target to Android.");
            EditorApplication.Exit(121);
        }
    }

    private static string ResolveOutputPath()
    {
        var customPath = GetCommandLineArg("-customBuildPath");
        if (string.IsNullOrEmpty(customPath))
        {
            return DefaultApkPath;
        }

        var buildName = GetCommandLineArg("-customBuildName");
        if (!string.IsNullOrEmpty(buildName))
        {
            if (!customPath.EndsWith("/") && !customPath.EndsWith("\\"))
            {
                customPath += Path.DirectorySeparatorChar;
            }

            customPath += buildName;
        }

        if (!customPath.EndsWith(".apk", StringComparison.OrdinalIgnoreCase) &&
            !customPath.EndsWith(".aab", StringComparison.OrdinalIgnoreCase))
        {
            customPath += ".apk";
        }

        return customPath;
    }

    private static string GetCommandLineArg(string flag)
    {
        var args = Environment.GetCommandLineArgs();
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == flag)
            {
                return args[i + 1];
            }
        }

        return null;
    }
}
#endif
