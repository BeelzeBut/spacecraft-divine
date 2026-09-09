using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Bucăți comune celor două puncte de intrare headless.
/// </summary>
public static class BuildCommon
{
    public static string[] EnabledScenes()
    {
        return EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
    }

    /// <summary>
    /// Versiunea și numărul compilării vin din mediu, ca o linie de lansare să nu fie nevoită
    /// să modifice proiectul. applicationIdentifier NU se atinge nici aici, nici altundeva:
    /// listarea din magazin există deja pe com.KodaGames.SpaceshipDivine.
    /// </summary>
    public static void ApplyVersionFromEnvironment()
    {
        string version = Environment.GetEnvironmentVariable("SD_VERSION");
        if (!string.IsNullOrEmpty(version))
            PlayerSettings.bundleVersion = version;

        string code = Environment.GetEnvironmentVariable("SD_BUILD_NUMBER");
        if (!string.IsNullOrEmpty(code) && int.TryParse(code, out int parsed))
        {
            PlayerSettings.Android.bundleVersionCode = parsed;
            PlayerSettings.iOS.buildNumber = code;
        }

        Debug.Log($"[Build] versiune {PlayerSettings.bundleVersion} " +
                  $"cod {PlayerSettings.Android.bundleVersionCode} " +
                  $"identificator {PlayerSettings.applicationIdentifier}");
    }

    /// <summary>
    /// Încheie procesul cu un cod de ieșire în care apelantul poate avea încredere.
    /// Exit(1) este urmat de return: fără el, execuția ar cădea în Exit(0) de dedesubt și o
    /// compilare eșuată ar raporta succes - aceeași clasă de defect ca un harnașament de teste
    /// care raportează fals succes.
    /// </summary>
    public static void Finish(string label, BuildReport report)
    {
        BuildSummary summary = report.summary;
        Debug.Log($"[{label}] rezultat: {summary.result} | erori: {summary.totalErrors} | " +
                  $"durata: {summary.totalTime} | ieșire: {summary.outputPath}");

        if (summary.result != BuildResult.Succeeded)
        {
            foreach (BuildStep step in report.steps)
                foreach (BuildStepMessage msg in step.messages)
                    if (msg.type == LogType.Error || msg.type == LogType.Exception)
                        Debug.LogError($"[{label}] {step.name}: {msg.content}");

            EditorApplication.Exit(1);
            return;
        }

        Debug.Log($"[{label}] OK: {summary.outputPath} ({summary.totalSize} octeți)");
        EditorApplication.Exit(0);
    }
}
