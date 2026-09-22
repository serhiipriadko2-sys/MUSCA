using UnityEngine;

namespace MUSCA.Gate3D
{
    public sealed class CombatSandboxHud : MonoBehaviour
    {
        [SerializeField] private CombatDamageReceiver target;
        [SerializeField] private PlayerMeleeCombat combat;
        [SerializeField] private PlayerCombatVitals vitals;
        [SerializeField] private PlayerDodgeController dodge;
        [SerializeField] private PlayerLockOn lockOn;
        [SerializeField] private SentinelCombatBrain brain;
        [SerializeField] private KaelPredictionProbe prediction;

        private GUIStyle _title;
        private GUIStyle _body;
        private GUIStyle _warn;

        public void Configure(
            CombatDamageReceiver receiver,
            PlayerMeleeCombat playerCombat,
            PlayerCombatVitals playerVitals,
            PlayerDodgeController playerDodge,
            PlayerLockOn playerLockOn,
            SentinelCombatBrain sentinelBrain,
            KaelPredictionProbe kaelPrediction)
        {
            target = receiver;
            combat = playerCombat;
            vitals = playerVitals;
            dodge = playerDodge;
            lockOn = playerLockOn;
            brain = sentinelBrain;
            prediction = kaelPrediction;
        }

        private void EnsureStyles()
        {
            if (_title != null) return;
            _title = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
            _body = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                normal = { textColor = new Color(0.82f, 0.92f, 0.96f) }
            };
            _warn = new GUIStyle(_body)
            {
                normal = { textColor = new Color(1f, 0.72f, 0.28f) }
            };
        }

        private void OnGUI()
        {
            EnsureStyles();
            Rect panel = new Rect(24f, 24f, 460f, 252f);
            GUI.Box(panel, GUIContent.none);
            GUI.Label(new Rect(40f, 36f, 410f, 28f), "MUSCA // FIRST THRESHOLD v0.6.1", _title);
            GUI.Label(new Rect(40f, 68f, 410f, 22f), "WASD + mouse — movement / view", _body);
            GUI.Label(new Rect(40f, 90f, 410f, 22f), "SPACE — jump · SHIFT — dodge · LMB — strike", _body);
            GUI.Label(new Rect(40f, 112f, 410f, 22f), "Q / MMB — lock target · ESC — cursor", _body);
            GUI.Label(new Rect(40f, 134f, 410f, 22f), "K — toggle Kael history-only prediction probe", _body);

            string playerText = vitals == null
                ? "Player: unavailable"
                : $"Player HP {vitals.CurrentHealth:0}/{vitals.MaxHealth:0}  ST {vitals.CurrentStamina:0}/{vitals.MaxStamina:0}";
            GUI.Label(new Rect(40f, 162f, 410f, 22f), playerText, _body);

            string targetText = target == null
                ? "KAEL PROXY: unavailable"
                : target.IsAlive
                    ? $"KAEL PROXY {target.CurrentHealth:0}/{target.MaxHealth:0}"
                    : "KAEL PROXY: rebooting…";
            GUI.Label(new Rect(40f, 184f, 210f, 22f), targetText, _body);

            string lockText = lockOn != null && lockOn.IsLocked
                ? $"LOCK: {lockOn.TargetName}"
                : "LOCK: —";
            GUI.Label(new Rect(250f, 184f, 190f, 22f), lockText, _body);

            string stateText = brain == null ? "AI: —" : $"AI: {brain.State}";
            GUI.Label(new Rect(40f, 206f, 180f, 22f), stateText,
                brain != null && brain.State == SentinelCombatState.Telegraph ? _warn : _body);

            if (combat != null && combat.CooldownRemaining > 0f)
            {
                GUI.Label(new Rect(220f, 206f, 100f, 22f), $"ATK {combat.CooldownRemaining:0.00}s", _body);
            }

            if (dodge != null && !dodge.CanDodge)
            {
                GUI.Label(new Rect(320f, 206f, 120f, 22f), "DODGE WAIT", _warn);
            }

            string predictionText = "PRED: OFF";
            GUIStyle predictionStyle = _body;
            if (prediction != null && prediction.PrototypeActive)
            {
                KaelPredictionSnapshot snapshot = prediction.Snapshot;
                string mode = snapshot.Locked
                    ? prediction.PredictionFresh ? "LOCK" : "STALE"
                    : "learning";
                predictionText = snapshot.SampleCount == 0
                    ? "PRED: learning — no dodge history"
                    : $"PRED: {snapshot.Direction}  {snapshot.SampleCount} samples  {snapshot.Confidence * 100f:0}%  {mode}";
                if (snapshot.Locked && prediction.PredictionFresh)
                {
                    predictionStyle = _warn;
                }

                if (brain != null && brain.PredictionAppliedThisTelegraph)
                {
                    predictionText += "  // FORECAST ZONE";
                }
                if (brain != null && brain.PredictionBreakPulse)
                {
                    predictionText += $"  // FUTURE BROKEN #{brain.BrokenPredictionCount}";
                    predictionStyle = _warn;
                }
            }
            GUI.Label(new Rect(40f, 230f, 410f, 22f), predictionText, predictionStyle);
        }
    }
}
