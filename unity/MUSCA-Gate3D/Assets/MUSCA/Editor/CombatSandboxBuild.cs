using System;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace MUSCA.Gate3D.Editor
{
    public static class CombatSandboxBuild
    {
        private const string ScenePath = "Assets/MUSCA/Scenes/CombatSandbox_v01.unity";

        [Serializable]
        private sealed class Receipt
        {
            public string status;
            public string unityVersion;
            public string scene;
            public string output;
            public ulong bytes;
            public long exeBytes;
            public string exeSha256;
            public int errors;
            public int warnings;
        }

        public static void BuildWindows()
        {
            if (AssetDatabase.LoadMainAssetAtPath(ScenePath) == null)
                throw new InvalidOperationException($"Missing combat scene: {ScenePath}");

            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? ".";
            string output = Path.Combine(projectRoot, "Builds", "CombatSandbox-v01", "MUSCA-CombatSandbox-v01.exe");
            Directory.CreateDirectory(Path.GetDirectoryName(output) ?? projectRoot);

            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = output,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            bool passed = summary.result == BuildResult.Succeeded && summary.totalErrors == 0;
            var receipt = new Receipt
            {
                status = passed ? "PASS" : "FAIL",
                unityVersion = Application.unityVersion,
                scene = ScenePath,
                output = output,
                bytes = summary.totalSize,
                exeBytes = File.Exists(output) ? new FileInfo(output).Length : 0L,
                exeSha256 = File.Exists(output) ? Sha256(output) : string.Empty,
                errors = (int)summary.totalErrors,
                warnings = (int)summary.totalWarnings
            };

            string receiptPath = Path.Combine(projectRoot, "Docs", "AI", "CombatSandboxBuild.json");
            Directory.CreateDirectory(Path.GetDirectoryName(receiptPath) ?? projectRoot);
            File.WriteAllText(receiptPath, JsonUtility.ToJson(receipt, true));
            Debug.Log($"MUSCA_COMBAT_BUILD status={receipt.status} errors={receipt.errors} warnings={receipt.warnings} bytes={receipt.bytes} path={output}");

            if (!passed)
                throw new InvalidOperationException($"Combat sandbox Windows build failed: {summary.result}");
        }

        private static string Sha256(string path)
        {
            using SHA256 sha = SHA256.Create();
            using FileStream stream = File.OpenRead(path);
            return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty).ToLowerInvariant();
        }
    }
}
