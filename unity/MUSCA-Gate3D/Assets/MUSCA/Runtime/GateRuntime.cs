using System;
using UnityEngine;

namespace MUSCA.Gate3D
{
    public sealed class GateRuntime : MonoBehaviour
    {
        [SerializeField] private SensorMode sensorMode = SensorMode.LightChemical;
        [SerializeField] private Reagent hiddenNeutral = Reagent.Cobalt;
        [SerializeField] private FirstPersonController player;
        [SerializeField] private Transform gateAnchor;
        [SerializeField] private Transform gateLeftPanel;
        [SerializeField] private Transform gateRightPanel;
        [SerializeField] private Light gateLight;
        [SerializeField] private ReagentStation amberStation;
        [SerializeField] private ReagentStation cobaltStation;
        [SerializeField] private float interactionDistance = 2.7f;
        [SerializeField] private float gateDiscoveryDistance = 6.4f;
        [SerializeField] private float gateOpenOffset = 2.0f;

        private GateDomain _domain;
        private ReagentStation _activeStation;
        private Vector3 _leftClosed;
        private Vector3 _rightClosed;
        private Vector3 _spawnPosition;
        private Quaternion _spawnRotation;
        private string _message = "MUSCA: сигнал обнаружен впереди.";

        public event Action StateChanged;
        public ReagentStation ActiveStation => _activeStation;
        public GateOutcome Outcome => _domain?.Outcome ?? GateOutcome.Pending;
        public string WorldMessage => _message;

        public int SignalStrength
        {
            get
            {
                if (player == null)
                {
                    return 2;
                }

                float progress = Mathf.InverseLerp(12f, -11f, player.transform.position.z);
                return Mathf.RoundToInt(Mathf.Lerp(2f, 10f, progress));
            }
        }

        public string InteractionPrompt
        {
            get
            {
                if (_activeStation == null || _domain == null || _domain.Outcome != GateOutcome.Pending)
                {
                    return string.Empty;
                }

                string scan = _domain.Mode == SensorMode.LightChemical && !_domain.Scanned ? "Q — химическая проба · " : string.Empty;
                return $"{scan}E — выбрать {ReagentCode(_activeStation.Reagent)}";
            }
        }

        public void Configure(
            FirstPersonController playerController,
            Transform gatePoint,
            Transform leftPanel,
            Transform rightPanel,
            Light responseLight,
            ReagentStation amber,
            ReagentStation cobalt)
        {
            player = playerController;
            gateAnchor = gatePoint;
            gateLeftPanel = leftPanel;
            gateRightPanel = rightPanel;
            gateLight = responseLight;
            amberStation = amber;
            cobaltStation = cobalt;
        }

        private void Awake()
        {
            _domain = new GateDomain(sensorMode, hiddenNeutral);
            _spawnPosition = player != null ? player.transform.position : new Vector3(0f, 0f, 12f);
            _spawnRotation = player != null ? player.transform.rotation : Quaternion.identity;
            _leftClosed = gateLeftPanel != null ? gateLeftPanel.localPosition : Vector3.zero;
            _rightClosed = gateRightPanel != null ? gateRightPanel.localPosition : Vector3.zero;
            ApplyGateVisualImmediate();
        }

        private void Update()
        {
            if (_domain == null || player == null)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                ResetRun();
                return;
            }

            UpdateSpatialState();

            if (_domain.Outcome == GateOutcome.Pending && player.InputEnabled && Cursor.lockState == CursorLockMode.Locked)
            {
                if (Input.GetKeyDown(KeyCode.Q))
                {
                    TryScan();
                }

                if (Input.GetKeyDown(KeyCode.E))
                {
                    TryChoose();
                }
            }

            AnimateGate();
        }

        private void UpdateSpatialState()
        {
            if (gateAnchor != null)
            {
                Vector3 playerFlat = player.transform.position;
                playerFlat.y = gateAnchor.position.y;
                if (Vector3.Distance(playerFlat, gateAnchor.position) <= gateDiscoveryDistance && _domain.MarkReachedGate())
                {
                    _message = "MUSCA: источник сигнала локализован у шлюза.";
                    NotifyChanged();
                }
            }

            ReagentStation next = FindActiveStation();
            if (next != _activeStation)
            {
                _activeStation?.SetHighlighted(false);
                _activeStation = next;
                _activeStation?.SetHighlighted(true);
            }

            if (_activeStation != null && _domain.ObserveStation(_activeStation.Reagent))
            {
                _message = $"MUSCA: станция {ReagentCode(_activeStation.Reagent)} доступна для анализа.";
                NotifyChanged();
            }
        }

        private ReagentStation FindActiveStation()
        {
            ReagentStation best = null;
            float bestDistance = interactionDistance;
            EvaluateStation(amberStation, ref best, ref bestDistance);
            EvaluateStation(cobaltStation, ref best, ref bestDistance);
            return best;
        }

        private void EvaluateStation(ReagentStation station, ref ReagentStation best, ref float bestDistance)
        {
            if (station == null)
            {
                return;
            }

            float dx = player.transform.position.x - station.transform.position.x;
            float dz = player.transform.position.z - station.transform.position.z;
            float distance = Mathf.Sqrt(dx * dx + dz * dz);
            if (distance <= bestDistance)
            {
                best = station;
                bestDistance = distance;
            }
        }

        private void TryScan()
        {
            if (_activeStation == null)
            {
                _message = "MUSCA: подойди к станции реагента, чтобы взять химическую пробу.";
                NotifyChanged();
                return;
            }

            if (_domain.Scan())
            {
                _message = "ISKRA: проба дала новый факт. Теперь выбор можно обосновать наблюдением.";
                NotifyChanged();
            }
        }

        private void TryChoose()
        {
            if (_activeStation == null)
            {
                _message = "MUSCA: взаимодействовать можно только рядом со станцией.";
                NotifyChanged();
                return;
            }

            GateOutcome result = _domain.Choose(_activeStation.Reagent);
            player.InputEnabled = false;
            FirstPersonController.UnlockCursor();
            _message = result == GateOutcome.Opened
                ? "MUSCA: шлюз отвечает. Проход открыт."
                : "ISKRA: мир опроверг решение. Шлюз заблокирован.";
            NotifyChanged();
        }

        public void ResetRun()
        {
            _domain.Reset();
            _activeStation?.SetHighlighted(false);
            _activeStation = null;
            _message = "MUSCA: сигнал обнаружен впереди.";
            player.InputEnabled = true;
            player.Teleport(_spawnPosition, _spawnRotation.eulerAngles.y);
            FirstPersonController.LockCursor();
            ApplyGateVisualImmediate();
            NotifyChanged();
        }

        private void AnimateGate()
        {
            if (gateLeftPanel == null || gateRightPanel == null)
            {
                return;
            }

            bool opened = _domain.Outcome == GateOutcome.Opened;
            Vector3 leftTarget = _leftClosed + Vector3.left * (opened ? gateOpenOffset : 0f);
            Vector3 rightTarget = _rightClosed + Vector3.right * (opened ? gateOpenOffset : 0f);
            float t = 1f - Mathf.Exp(-4.5f * Time.deltaTime);
            gateLeftPanel.localPosition = Vector3.Lerp(gateLeftPanel.localPosition, leftTarget, t);
            gateRightPanel.localPosition = Vector3.Lerp(gateRightPanel.localPosition, rightTarget, t);

            if (gateLight != null)
            {
                gateLight.color = _domain.Outcome switch
                {
                    GateOutcome.Opened => new Color(0.2f, 1f, 0.65f),
                    GateOutcome.Sealed => new Color(1f, 0.2f, 0.25f),
                    _ => new Color(0.25f, 0.85f, 1f)
                };
            }
        }

        private void ApplyGateVisualImmediate()
        {
            if (gateLeftPanel != null)
            {
                gateLeftPanel.localPosition = _leftClosed;
            }
            if (gateRightPanel != null)
            {
                gateRightPanel.localPosition = _rightClosed;
            }
            if (gateLight != null)
            {
                gateLight.color = new Color(0.25f, 0.85f, 1f);
            }
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public void PrepareQaOpenedState()
        {
            if (_domain == null)
            {
                return;
            }

            _domain.MarkReachedGate();
            _domain.ObserveStation(Reagent.Amber);
            _domain.ObserveStation(Reagent.Cobalt);
            _domain.Scan();
            _domain.Choose(hiddenNeutral);
            _message = "QA: шлюз открыт подтверждённым нейтральным реагентом.";
            player.InputEnabled = false;
            NotifyChanged();
        }
#endif

        public GateSnapshot Snapshot()
        {
            return _domain?.Snapshot();
        }

        private void NotifyChanged()
        {
            StateChanged?.Invoke();
        }

        private static string ReagentCode(Reagent reagent)
        {
            return reagent == Reagent.Amber ? "AMBER" : "COBALT";
        }
    }
}
