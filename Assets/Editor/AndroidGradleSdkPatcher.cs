#if UNITY_EDITOR
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor.Android;
using UnityEngine;

/// <summary>
/// Keeps generated Gradle projects on API 36 even if the Editor dropdown clamps the value.
/// Unity 2019.4 still invokes its bundled Gradle/AGP; if that build fails, run
/// tools/upgrade_exported_android_gradle.py on the exported project.
/// </summary>
public class AndroidGradleSdkPatcher : IPostGenerateGradleAndroidProject
{
    private const int TargetSdk = 36;

    public int callbackOrder
    {
        get { return 0; }
    }

    public void OnPostGenerateGradleAndroidProject(string path)
    {
        var root = Directory.GetParent(path);
        if (root == null)
        {
            return;
        }

        var gradleRoot = root.FullName;
        foreach (var file in Directory.GetFiles(gradleRoot, "*.gradle", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(file);
            var updated = Regex.Replace(text, @"compileSdkVersion\s+\d+", "compileSdkVersion " + TargetSdk);
            updated = Regex.Replace(updated, @"targetSdkVersion\s+\d+", "targetSdkVersion " + TargetSdk);
            if (updated != text)
            {
                File.WriteAllText(file, updated);
            }
        }

        var propertiesPath = Path.Combine(gradleRoot, "gradle.properties");
        var properties = File.Exists(propertiesPath) ? File.ReadAllText(propertiesPath) : string.Empty;
        if (properties.IndexOf("android.suppressUnsupportedCompileSdk") < 0)
        {
            if (properties.Length > 0 && !properties.EndsWith("\n"))
            {
                properties += "\n";
            }

            properties += "android.suppressUnsupportedCompileSdk=" + TargetSdk + "\n";
            File.WriteAllText(propertiesPath, properties);
        }

        Debug.Log("Android Gradle project pinned to target/compile SDK " + TargetSdk + " at " + gradleRoot);
    }
}
#endif
