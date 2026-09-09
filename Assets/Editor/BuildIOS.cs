using System;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Generarea proiectului Xcode pentru iOS, apelabilă din linia de comandă.
/// Unity produce un proiect Xcode, nu un .ipa: arhivarea și semnarea rămân în seama Xcode
/// sau a liniilor fastlane.
/// </summary>
public static class BuildIOS
{
    public static void Build() => BuildXcodeProject();

    public static void BuildXcodeProject()
    {
        const string label = "BuildIOS";
        BuildCommon.ApplyVersionFromEnvironment();

        string[] scenes = BuildCommon.EnabledScenes();
        Debug.Log($"[{label}] {scenes.Length} scene incluse:");
        foreach (string s in scenes) Debug.Log($"[{label}]   {s}");

        // Calea implicită este în depozit, nu una absolută din directorul personal:
        // scriptul trebuie să funcționeze pe orice clonă, nu doar pe această mașină.
        string outputPath = Environment.GetEnvironmentVariable("IOS_BUILD_PATH");
        if (string.IsNullOrEmpty(outputPath)) outputPath = "Build IOS";

        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outputPath,
            target = BuildTarget.iOS,
            targetGroup = BuildTargetGroup.iOS,
            options = BuildOptions.None,
        };

        BuildCommon.Finish(label, BuildPipeline.BuildPlayer(options));
    }
}
