using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace MUSCA.Gate3D
{
    public sealed class RuntimeQaCapture : MonoBehaviour
    {
        [SerializeField] private FirstPersonController player;
        [SerializeField] private GateRuntime runtime;

        [Serializable]
        private sealed class QaReceipt
        {
            public string view;
            public string outcome;
            public int cells;
            public int signal;
            public float x;
            public float y;
            public float z;
            public string screenshot;
        }

        public void Configure(FirstPersonController controller, GateRuntime gateRuntime)
        {
            player = controller;
            runtime = gateRuntime;
        }

        private IEnumerator Start()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            string view = ArgValue("--musca-qa=");
            if (string.IsNullOrEmpty(view) || player == null || runtime == null)
            {
                yield break;
            }

            player.InputEnabled = false;
            FirstPersonController.UnlockCursor();
            switch (view)
            {
                case "spawn":
                    player.Teleport(new Vector3(0f, 0f, 10f), 180f);
                    break;
                case "decision":
                    player.Teleport(new Vector3(0f, 0f, -1.8f), 180f);
                    break;
                case "gate":
                    player.Teleport(new Vector3(0f, 0f, -5.5f), 180f);
                    break;
                case "amber":
                    player.Teleport(new Vector3(-2.1f, 0f, -5.8f), 235f);
                    break;
                case "opened":
                    player.Teleport(new Vector3(0f, 0f, -5.5f), 180f);
                    runtime.PrepareQaOpenedState();
                    break;
                case "openedworld":
                    player.Teleport(new Vector3(0f, 0f, -5.5f), 180f);
                    runtime.PrepareQaOpenedState();
                    GateHud hud = FindFirstObjectByType<GateHud>();
                    if (hud != null) hud.enabled = false;
                    break;
                default:
                    Debug.LogError($"Unknown MUSCA QA view: {view}");
                    Application.Quit(3);
                    yield break;
            }

            yield return new WaitForSeconds(1.0f);
            string output = ArgValue("--musca-qa-output=");
            if (string.IsNullOrEmpty(output))
            {
                output = Path.Combine(Application.persistentDataPath, $"musca-qa-{view}.png");
            }

            string fullOutput = Path.GetFullPath(output);
            Directory.CreateDirectory(Path.GetDirectoryName(fullOutput) ?? Application.persistentDataPath);
            ScreenCapture.CaptureScreenshot(fullOutput, 1);
            yield return new WaitForSeconds(0.75f);

            GateSnapshot snapshot = runtime.Snapshot();
            Vector3 position = player.transform.position;
            var receipt = new QaReceipt
            {
                view = view,
                outcome = snapshot.Outcome.ToString(),
                cells = snapshot.Cells,
                signal = runtime.SignalStrength,
                x = position.x,
                y = position.y,
                z = position.z,
                screenshot = fullOutput
            };
            string jsonPath = Path.ChangeExtension(fullOutput, ".json");
            File.WriteAllText(jsonPath, JsonUtility.ToJson(receipt, true));
            Debug.Log($"MUSCA_QA_CAPTURE view={view} screenshot={fullOutput} receipt={jsonPath}");
            yield return null;
            Application.Quit(0);
#else
            yield break;
#endif
        }

        private static string ArgValue(string prefix)
        {
            foreach (string arg in Environment.GetCommandLineArgs())
            {
                if (arg.StartsWith(prefix, StringComparison.Ordinal))
                {
                    return arg.Substring(prefix.Length).Trim('"');
                }
            }
            return string.Empty;
        }
    }
}
