using System;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Generarea pachetelor Android, apelabilă din linia de comandă.
///
///   BuildApk  - pachet unic pentru instalare prin adb, testare pe dispozitiv fizic
///   BuildAab  - pachet pentru incarcarea in Google Play
///
/// Ambele ies cu un cod diferit de zero dacă build-ul nu a reușit.
/// </summary>
public static class BuildAndroid
{
    /// <summary>Păstrat pentru apelurile existente: un APK este ce vrei pe telefon.</summary>
    public static void Build() => BuildApk();

    public static void BuildApk()
    {
        Run(appBundle: false, defaultPath: "Build Android/SpaceshipDivine.apk", label: "BuildApk");
    }

    public static void BuildAab()
    {
        Run(appBundle: true, defaultPath: "Build Android/SpaceshipDivine.aab", label: "BuildAab");
    }

    private static void Run(bool appBundle, string defaultPath, string label)
    {
        BuildCommon.ApplyVersionFromEnvironment();

        string[] scenes = BuildCommon.EnabledScenes();
        Debug.Log($"[{label}] {scenes.Length} scene incluse:");
        foreach (string s in scenes) Debug.Log($"[{label}]   {s}");

        // Parolele magaziei de chei se preiau din mediu, ca să nu fie scrise în depozit.
        string storePass = Environment.GetEnvironmentVariable("ANDROID_KEYSTORE_PASS");
        string aliasPass = Environment.GetEnvironmentVariable("ANDROID_KEYALIAS_PASS") ?? storePass;

        if (!string.IsNullOrEmpty(storePass))
        {
            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystorePass = storePass;
            PlayerSettings.Android.keyaliasPass = aliasPass;
            Debug.Log($"[{label}] semnare cu magazia proprie: " +
                      $"{PlayerSettings.Android.keystoreName} / {PlayerSettings.Android.keyaliasName}");
        }
        else
        {
            PlayerSettings.Android.useCustomKeystore = false;
            // Un APK semnat cu cheia de depanare se instalează pe telefon, dar NU poate fi
            // încărcat în Google Play.
            Debug.LogWarning($"[{label}] fără parolă: se folosește cheia implicită de depanare, " +
                             "pachetul nu este acceptat de magazin.");
        }

        EditorUserBuildSettings.buildAppBundle = appBundle;
        EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.Generic;
        EditorUserBuildSettings.androidCreateSymbols = AndroidCreateSymbols.Disabled;
        EditorUserBuildSettings.development = false;

        string outputPath = Environment.GetEnvironmentVariable("ANDROID_BUILD_PATH");
        if (string.IsNullOrEmpty(outputPath)) outputPath = defaultPath;

        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outputPath,
            target = BuildTarget.Android,
            targetGroup = BuildTargetGroup.Android,
            options = BuildOptions.None,
        };

        BuildCommon.Finish(label, BuildPipeline.BuildPlayer(options));
    }
}
