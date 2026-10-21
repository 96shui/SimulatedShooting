using System;
using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace VRShooting.Tests.Editor
{
    /// <summary>Opt-in request file bridge for the already-open Editor. Never changes scene assets.</summary>
    [InitializeOnLoad]
    public static class P3ContractTestRunner
    {
        const string RequestPath = "Logs/p3-task001-request.json";
        const string ActiveKey = "P3Task001.ActiveResultPath";
        const string RefreshKey = "P3Task001.RefreshedRequest";
        static readonly TestRunnerApi Api;
        static double nextPoll;
        [Serializable] public sealed class Request { public string mode; public string filter; public string name; public bool resetInterruptedRun; }
        static P3ContractTestRunner()
        {
            Api = ScriptableObject.CreateInstance<TestRunnerApi>();
            Api.RegisterCallbacks(new Callbacks());
            EditorApplication.update += Poll;
        }
        static void Poll()
        {
            if (EditorApplication.timeSinceStartup < nextPoll) return;
            nextPoll = EditorApplication.timeSinceStartup + 1;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode
                || !File.Exists(RequestPath)) return;
            try
            {
                var requestText = File.ReadAllText(RequestPath);
                var request = JsonUtility.FromJson<Request>(requestText);
                if (request == null || (request.mode != "EditMode" && request.mode != "PlayMode")) throw new ArgumentException("mode must be EditMode or PlayMode");
                if (string.IsNullOrWhiteSpace(request.name) || request.name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || request.name.Contains("..")) throw new ArgumentException("name must be a file name");
                var previousRun = SessionState.GetString(ActiveKey, "");
                if (previousRun.Length != 0)
                {
                    if (!request.resetInterruptedRun) return;
                    // Explicit recovery after a PlayMode run was stopped before RunFinished.
                    File.WriteAllText(previousRun + ".status", "Interrupted before completion; result not counted");
                    SessionState.EraseString(ActiveKey);
                }
                // An unfocused Editor may still have the previous test assembly loaded.
                // Refresh first, then let compilation/domain reload finish before executing.
                if (SessionState.GetString(RefreshKey, "") != requestText)
                {
                    SessionState.SetString(RefreshKey, requestText);
                    AssetDatabase.Refresh();
                    return;
                }
                if (EditorUtility.scriptCompilationFailed)
                {
                    SessionState.EraseString(RefreshKey);
                    return;
                }
                Directory.CreateDirectory("Logs/p3-task001");
                var path = Path.GetFullPath("Logs/p3-task001/" + request.name + ".xml");
                File.Delete(RequestPath);
                SessionState.SetString(ActiveKey, path);
                File.WriteAllText(path + ".status", "running");
                var filter = new Filter { testMode = request.mode == "EditMode" ? TestMode.EditMode : TestMode.PlayMode };
                if (!string.IsNullOrWhiteSpace(request.filter)) filter.groupNames = new[] { request.filter };
                Api.Execute(new ExecutionSettings(filter));
            }
            catch (Exception error)
            {
                SessionState.EraseString(ActiveKey);
                File.WriteAllText("Logs/p3-task001-runner-error.txt", error.ToString());
                Debug.LogException(error);
            }
        }
        sealed class Callbacks : ICallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun) { }
            public void TestStarted(ITestAdaptor test) { }
            public void TestFinished(ITestResultAdaptor result) { }
            public void RunFinished(ITestResultAdaptor result)
            {
                var path = SessionState.GetString(ActiveKey, "");
                if (path.Length == 0) return;
                try
                {
                    File.WriteAllText(path, result.ToXml().OuterXml);
                    File.WriteAllText(path + ".status", $"{result.ResultState}: passed={result.PassCount} failed={result.FailCount} skipped={result.SkipCount}");
                }
                finally { SessionState.EraseString(ActiveKey); }
            }
        }
    }
}
