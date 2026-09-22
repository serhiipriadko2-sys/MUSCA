using System.Text;
using UnityEngine;

namespace MUSCA.Gate3D
{
    public sealed class GateHud : MonoBehaviour
    {
        [SerializeField] private GateRuntime runtime;
        [SerializeField] private bool compactMode;

        private GateSnapshot _snapshot;
        private bool _showLog;
        private GUIStyle _title;
        private GUIStyle _body;
        private GUIStyle _small;
        private GUIStyle _strong;
        private GUIStyle _center;
        private GUIStyle _fact;
        private GUIStyle _hyp;
        private GUIStyle _interp;
        private GUIStyle _resultSuccess;
        private GUIStyle _resultFailure;
        private GUIStyle _resultBody;
        private readonly StringBuilder _builder = new StringBuilder(1024);

        private static readonly Color Panel = new Color(0.02f, 0.09f, 0.13f, 0.92f);
        private static readonly Color Line = new Color(0.20f, 0.72f, 0.90f, 0.75f);
        private static readonly Color Cyan = new Color(0.28f, 0.86f, 1f);
        private static readonly Color Muted = new Color(0.58f, 0.76f, 0.82f);
        private static readonly Color Green = new Color(0.28f, 0.92f, 0.68f);
        private static readonly Color Amber = new Color(1f, 0.67f, 0.22f);
        private static readonly Color Red = new Color(1f, 0.35f, 0.38f);

        public void Configure(GateRuntime value)
        {
            runtime = value;
        }

        public void SetCompactMode(bool value)
        {
            compactMode = value;
        }

        private void OnEnable()
        {
            if (runtime != null)
            {
                runtime.StateChanged += Refresh;
                Refresh();
            }
        }

        private void Start()
        {
            Refresh();
        }

        private void OnDisable()
        {
            if (runtime != null)
            {
                runtime.StateChanged -= Refresh;
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                _showLog = !_showLog;
            }
        }

        private void OnGUI()
        {
            EnsureStyles();
            if (runtime == null || _snapshot == null)
            {
                return;
            }

            float scale = Mathf.Clamp(Screen.height / 900f, 0.82f, 1.25f);
            Matrix4x4 previous = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float width = Screen.width / scale;
            float height = Screen.height / scale;

            if (compactMode)
            {
                DrawCompactObjective(new Rect(18f, 18f, 310f, 92f));
                DrawCompactTelemetry(new Rect(width - 218f, 18f, 200f, 92f));
            }
            else
            {
                DrawObjectives(new Rect(22f, 22f, 370f, 265f));
                DrawTelemetry(new Rect(width - 300f, 22f, 278f, 175f));
                DrawHowItWorks(new Rect(22f, height - 199f, 370f, 177f));
            }
            DrawReticle(width, height);
            DrawPrompt(width, height);
            DrawWorldMessage(width, height);

            if (_showLog)
            {
                DrawLog(new Rect(width - 470f, 220f, 448f, height - 250f));
            }

            if (_snapshot.Outcome != GateOutcome.Pending)
            {
                DrawResult(width, height);
            }

            GUI.matrix = previous;
        }

        private void DrawCompactObjective(Rect rect)
        {
            DrawPanel(rect);
            int active = FirstIncompleteObjective();
            string text = active switch
            {
                0 => "Дойти до источника сигнала",
                1 => "Осмотреть AMBER и COBALT",
                2 => "Решить: сканировать или рискнуть",
                3 => "Выбрать нейтральный реагент",
                4 => "Открыть шлюз",
                _ => "Экспедиция завершена"
            };
            GUI.Label(new Rect(rect.x + 16f, rect.y + 12f, rect.width - 32f, 20f), "ЦЕЛЬ", _small);
            GUI.Label(new Rect(rect.x + 16f, rect.y + 39f, rect.width - 32f, 38f), text, _strong);
        }

        private void DrawCompactTelemetry(Rect rect)
        {
            DrawPanel(rect);
            float x = rect.x + 14f;
            GUI.Label(new Rect(x, rect.y + 12f, rect.width - 28f, 20f), $"СИГНАЛ {runtime.SignalStrength}/10", _small);
            DrawBar(new Rect(x, rect.y + 37f, rect.width - 28f, 8f), runtime.SignalStrength / 10f, Cyan);
            GUI.Label(new Rect(x, rect.y + 56f, 90f, 20f), $"ЗАПАС {_snapshot.Cells}/2", _small);
            for (int i = 0; i < 2; i++) DrawCell(new Rect(rect.x + 121f + i * 27f, rect.y + 59f, 20f, 10f), i < _snapshot.Cells);
        }

        private void DrawObjectives(Rect rect)
        {
            DrawPanel(rect);
            GUI.Label(new Rect(rect.x + 18f, rect.y + 15f, rect.width - 36f, 24f), "MUSCA // GATE · UNITY FUNCTION PROTOTYPE", _small);
            GUI.Label(new Rect(rect.x + 18f, rect.y + 42f, rect.width - 36f, 30f), "ЦЕЛЬ ЭКСПЕДИЦИИ", _title);

            string[] objectives = {
                "Дойти до источника сигнала",
                "Осмотреть станции AMBER и COBALT",
                "Решить: просканировать состав или рискнуть",
                "Выбрать нейтральный реагент",
                "Открыть шлюз"
            };
            float y = rect.y + 78f;
            int active = FirstIncompleteObjective();
            for (int i = 0; i < objectives.Length; i++)
            {
                bool done = _snapshot.ObjectiveComplete(i);
                Color old = GUI.color;
                GUI.color = done ? Green : (i == active ? Cyan : new Color(0.35f, 0.55f, 0.64f));
                GUI.DrawTexture(new Rect(rect.x + 20f, y + 3f, 14f, 14f), Texture2D.whiteTexture);
                GUI.color = old;
                GUI.Label(new Rect(rect.x + 46f, y - 2f, rect.width - 64f, 35f), objectives[i], done || i == active ? _strong : _body);
                y += 34f;
            }
        }

        private void DrawTelemetry(Rect rect)
        {
            DrawPanel(rect);
            float x = rect.x + 18f;
            GUI.Label(new Rect(x, rect.y + 15f, rect.width - 36f, 24f), $"Сигнал     {runtime.SignalStrength}/10", _strong);
            DrawBar(new Rect(x, rect.y + 47f, rect.width - 36f, 10f), runtime.SignalStrength / 10f, Cyan);
            GUI.Label(new Rect(x, rect.y + 72f, rect.width - 36f, 24f), $"Запас      {_snapshot.Cells}/2", _strong);
            for (int i = 0; i < 2; i++)
            {
                DrawCell(new Rect(x + i * 31f, rect.y + 104f, 24f, 13f), i < _snapshot.Cells);
            }
            GUI.Label(new Rect(x, rect.y + 133f, rect.width - 36f, 24f), "Режим      light_chemical", _small);
        }

        private void DrawHowItWorks(Rect rect)
        {
            DrawPanel(rect);
            GUI.Label(new Rect(rect.x + 18f, rect.y + 15f, rect.width - 36f, 22f), "КАК ЭТО РАБОТАЕТ", _small);
            GUI.Label(new Rect(rect.x + 18f, rect.y + 44f, rect.width - 36f, 20f), "Ты выбираешь намерение и решение.", _body);
            GUI.Label(new Rect(rect.x + 18f, rect.y + 68f, rect.width - 36f, 20f), "ISKRA отделяет факт от гипотезы.", _body);
            GUI.Label(new Rect(rect.x + 18f, rect.y + 92f, rect.width - 36f, 20f), "MUSCA замечает среду и сопровождает.", _body);
            GUI.Label(new Rect(rect.x + 18f, rect.y + 118f, rect.width - 36f, 20f), "WASD — движение · мышь — обзор · Q/E — действие", _small);
            GUI.Label(new Rect(rect.x + 18f, rect.y + 143f, rect.width - 36f, 20f), "TAB — журнал · F5 — заново · ESC — курсор", _small);
        }

        private void DrawPrompt(float width, float height)
        {
            string prompt = runtime.InteractionPrompt;
            if (string.IsNullOrEmpty(prompt))
            {
                return;
            }

            Rect rect = compactMode
                ? new Rect(width / 2f - 220f, height - 116f, 440f, 42f)
                : new Rect(width / 2f - 285f, height - 150f, 570f, 48f);
            Color previous = GUI.color;
            GUI.color = new Color(0.06f, 0.11f, 0.14f, 0.95f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = Amber;
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 2f), Texture2D.whiteTexture);
            GUI.color = previous;
            GUI.Label(rect, prompt, _center);
        }

        private void DrawWorldMessage(float width, float height)
        {
            Rect rect = compactMode
                ? new Rect(width / 2f - 255f, height - 62f, 510f, 32f)
                : new Rect(width / 2f - 330f, height - 73f, 660f, 38f);
            Color previous = GUI.color;
            GUI.color = new Color(0.01f, 0.05f, 0.08f, 0.86f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = Cyan;
            GUI.DrawTexture(new Rect(rect.x, rect.y, 3f, rect.height), Texture2D.whiteTexture);
            GUI.color = previous;
            GUI.Label(new Rect(rect.x + 14f, rect.y + 9f, rect.width - 24f, 22f), runtime.WorldMessage, _small);
        }

        private void DrawReticle(float width, float height)
        {
            Color previous = GUI.color;
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(width / 2f - 8f, height / 2f - 1f, 16f, 2f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(width / 2f - 1f, height / 2f - 8f, 2f, 16f), Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private void DrawLog(Rect rect)
        {
            DrawPanel(rect);
            GUI.Label(new Rect(rect.x + 18f, rect.y + 16f, rect.width - 36f, 26f), "ЖУРНАЛ НАБЛЮДЕНИЙ", _title);
            float y = rect.y + 54f;
            foreach (EvidenceEntry entry in _snapshot.Evidence)
            {
                GUIStyle style = entry.Label switch
                {
                    EvidenceLabel.Fact => _fact,
                    EvidenceLabel.Hypothesis => _hyp,
                    EvidenceLabel.Interpretation => _interp,
                    _ => _small
                };
                _builder.Clear();
                _builder.Append('[').Append(LabelCode(entry.Label)).Append("] ").Append(entry.Text);
                float height = style.CalcHeight(new GUIContent(_builder.ToString()), rect.width - 36f);
                GUI.Label(new Rect(rect.x + 18f, y, rect.width - 36f, height), _builder.ToString(), style);
                y += height + 10f;
                if (y > rect.yMax - 30f)
                {
                    break;
                }
            }
        }

        private void DrawResult(float width, float height)
        {
            Rect shade = new Rect(0f, 0f, width, height);
            Color previous = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.72f);
            GUI.DrawTexture(shade, Texture2D.whiteTexture);
            GUI.color = previous;

            bool success = _snapshot.Outcome == GateOutcome.Opened;
            Rect rect = new Rect(width / 2f - 310f, height / 2f - 150f, 620f, 300f);
            DrawPanel(rect);
            GUIStyle resultStyle = success ? _resultSuccess : _resultFailure;
            GUI.Label(new Rect(rect.x + 20f, rect.y + 50f, rect.width - 40f, 52f), success ? "ШЛЮЗ ОТКРЫТ" : "ШЛЮЗ ЗАБЛОКИРОВАН", resultStyle);
            string text = success
                ? $"Решение подтверждено миром. Остаток ресурса: {_snapshot.Cells}/2."
                : "Реактивный реагент заблокировал проход. Гипотеза не подтвердилась.";
            GUI.Label(new Rect(rect.x + 35f, rect.y + 120f, rect.width - 70f, 70f), text, _resultBody);
            GUI.Label(new Rect(rect.x + 35f, rect.y + 210f, rect.width - 70f, 42f), "F5 — пройти снова", _center);
        }

        private void Refresh()
        {
            _snapshot = runtime != null ? runtime.Snapshot() : null;
        }

        private int FirstIncompleteObjective()
        {
            for (int i = 0; i < 5; i++)
            {
                if (!_snapshot.ObjectiveComplete(i))
                {
                    return i;
                }
            }
            return -1;
        }

        private void EnsureStyles()
        {
            if (_title != null)
            {
                return;
            }
            _title = MakeStyle(20, Color.white, FontStyle.Bold);
            _body = MakeStyle(14, Color.white, FontStyle.Normal);
            _small = MakeStyle(12, Muted, FontStyle.Normal);
            _strong = MakeStyle(14, Color.white, FontStyle.Bold);
            _center = MakeStyle(16, Color.white, FontStyle.Bold);
            _center.alignment = TextAnchor.MiddleCenter;
            _fact = MakeStyle(13, Cyan, FontStyle.Normal);
            _hyp = MakeStyle(13, Amber, FontStyle.Normal);
            _interp = MakeStyle(13, Green, FontStyle.Normal);
            _resultSuccess = MakeStyle(34, Green, FontStyle.Bold);
            _resultFailure = MakeStyle(34, Red, FontStyle.Bold);
            _resultBody = MakeStyle(18, Color.white, FontStyle.Normal);
            _resultSuccess.alignment = TextAnchor.MiddleCenter;
            _resultFailure.alignment = TextAnchor.MiddleCenter;
            _resultBody.alignment = TextAnchor.MiddleCenter;
        }

        private static GUIStyle MakeStyle(int size, Color color, FontStyle fontStyle)
        {
            return new GUIStyle(GUI.skin.label)
            {
                fontSize = size,
                normal = { textColor = color },
                fontStyle = fontStyle,
                wordWrap = true
            };
        }

        private static void DrawPanel(Rect rect)
        {
            Color previous = GUI.color;
            GUI.color = Panel;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = Line;
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 1f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.y, 1f, rect.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.xMax - 1f, rect.y, 1f, rect.height), Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private static void DrawBar(Rect rect, float value, Color color)
        {
            Color previous = GUI.color;
            GUI.color = new Color(0.03f, 0.08f, 0.11f, 1f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = color;
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(value), rect.height), Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private static void DrawCell(Rect rect, bool filled)
        {
            Color previous = GUI.color;
            GUI.color = filled ? Cyan : new Color(0.18f, 0.33f, 0.39f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private static string LabelCode(EvidenceLabel label)
        {
            return label switch
            {
                EvidenceLabel.Fact => "FACT",
                EvidenceLabel.Hypothesis => "HYP",
                EvidenceLabel.Interpretation => "INTERP",
                _ => "UNKNOWN"
            };
        }
    }
}
