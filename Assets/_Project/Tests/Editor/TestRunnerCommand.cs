using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace SpaceSurvivors.EditorTools
{
    /// <summary>
    /// Runs the EditMode suite and writes the outcome to a file.
    ///
    /// <para>The Test Runner window already does this, but only for somebody sitting in front
    /// of the editor. A suite nobody can run without clicking is a suite that stops being run —
    /// and this project's whole weakness has been that its only automatic check was "does it
    /// compile". This makes the result readable from outside the editor: by a future session,
    /// by a script, and by CI if it ever gets one.</para>
    ///
    /// <para>The API is asynchronous and there is no way to wait for it, so the caller starts a
    /// run and then watches <see cref="ResultPath"/> for a fresh file. The file is deleted first
    /// so a stale result from a previous run can never be mistaken for this one — which would be
    /// the worst possible failure for a thing whose only job is telling the truth about tests.</para>
    /// </summary>
    public static class TestRunnerCommand
    {
        /// <summary>Under Temp/ deliberately: a result is about one run, not part of the project.</summary>
        public const string ResultPath = "Temp/editmode-tests.txt";

        [MenuItem("SpaceSurvivors/Run EditMode Tests")]
        public static void Run()
        {
            if (File.Exists(ResultPath))
            {
                File.Delete(ResultPath);
            }

            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.RegisterCallbacks(new ResultWriter());
            api.Execute(new ExecutionSettings(new Filter { testMode = TestMode.EditMode }));
        }

        private sealed class ResultWriter : ICallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun) { }

            public void TestStarted(ITestAdaptor test) { }

            public void TestFinished(ITestResultAdaptor result) { }

            public void RunFinished(ITestResultAdaptor result)
            {
                var report = new StringBuilder();
                report.Append("passed=").Append(result.PassCount)
                      .Append(" failed=").Append(result.FailCount)
                      .Append(" skipped=").Append(result.SkipCount)
                      .Append(" inconclusive=").Append(result.InconclusiveCount)
                      .AppendLine();

                AppendFailures(result, report);

                Directory.CreateDirectory(Path.GetDirectoryName(ResultPath) ?? ".");
                File.WriteAllText(ResultPath, report.ToString());

                Debug.Log($"[Tests] passed={result.PassCount} failed={result.FailCount} " +
                          $"-> {ResultPath}");
            }

            /// <summary>
            /// Only the failures, and each with its message. A list of names is enough to know
            /// something broke and never enough to know what.
            /// </summary>
            private static void AppendFailures(ITestResultAdaptor node, StringBuilder report)
            {
                if (node.Test.IsSuite)
                {
                    foreach (var child in node.Children)
                    {
                        AppendFailures(child, report);
                    }
                    return;
                }

                if (node.TestStatus == TestStatus.Failed)
                {
                    report.Append("FAILED  ").AppendLine(node.Test.FullName);
                    if (!string.IsNullOrWhiteSpace(node.Message))
                    {
                        report.AppendLine(node.Message.Trim());
                    }
                    report.AppendLine();
                }
            }
        }
    }
}
