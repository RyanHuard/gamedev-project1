using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace HollowTournament.EditorTools
{
    /// <summary>
    /// Headless Play Mode smoke test: opens the built scene, runs it for a few
    /// seconds, and dumps any error/exception console output to a file so a
    /// batchmode run can catch runtime null-refs that a compile pass can't see.
    /// Not a substitute for actually playing it in-editor, just a fast first filter.
    /// </summary>
    public static class SmokeTest
    {
        private const string ScenePath = "Assets/Scenes/Arena.unity";
        private const float RunSeconds = 4f;

        private static List<string> _errors;
        private static double _stopAtTime;
        private static string _outputPath;

        public static void Run()
        {
            _outputPath = Path.Combine(Directory.GetCurrentDirectory(), "smoke_test_result.txt");
            _errors = new List<string>();

            // Entering Play Mode normally triggers a C# domain reload, which wipes
            // this class's static fields and unsubscribes OnUpdate before it ever
            // fires again -- the run just hangs forever. Disable it for this pass.
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload | EnterPlayModeOptions.DisableSceneReload;

            EditorSceneManager.OpenScene(ScenePath);
            Application.logMessageReceived += OnLog;

            _stopAtTime = EditorApplication.timeSinceStartup + RunSeconds;
            EditorApplication.update += OnUpdate;
            EditorApplication.isPlaying = true;
        }

        private static void OnLog(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                _errors.Add($"[{type}] {condition}\n{stackTrace}");
        }

        private static void OnUpdate()
        {
            if (!EditorApplication.isPlaying) return;
            if (EditorApplication.timeSinceStartup < _stopAtTime) return;

            EditorApplication.update -= OnUpdate;
            Application.logMessageReceived -= OnLog;
            EditorApplication.isPlaying = false;

            string report = _errors.Count == 0
                ? "SMOKE TEST PASSED: no errors/exceptions during play.\n"
                : $"SMOKE TEST FAILED: {_errors.Count} error(s).\n\n" + string.Join("\n---\n", _errors);

            File.WriteAllText(_outputPath, report);
            Debug.Log(report);
            EditorApplication.Exit(_errors.Count == 0 ? 0 : 1);
        }
    }
}
