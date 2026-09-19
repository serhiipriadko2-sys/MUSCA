using UnityEngine;

namespace MUSCA.Gate3D
{
    public sealed class CombatSandboxHud : MonoBehaviour
    {
        [SerializeField] private CombatDamageReceiver target;
        [SerializeField] private PlayerMeleeCombat combat;

        private GUIStyle _title;
        private GUIStyle _body;

        public void Configure(CombatDamageReceiver receiver, PlayerMeleeCombat playerCombat)
        {
            target = receiver;
            combat = playerCombat;
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
        }

        private void OnGUI()
        {
            EnsureStyles();
            Rect panel = new Rect(24f, 24f, 360f, 126f);
            GUI.Box(panel, GUIContent.none);
            GUI.Label(new Rect(40f, 36f, 320f, 28f), "MUSCA // COMBAT SANDBOX v0.1", _title);
            GUI.Label(new Rect(40f, 68f, 320f, 22f), "WASD + mouse — movement / view", _body);
            GUI.Label(new Rect(40f, 90f, 320f, 22f), "LMB — melee strike · ESC — cursor", _body);

            string targetText = target == null
                ? "Sentinel: unavailable"
                : target.IsAlive
                    ? $"Sentinel: {target.CurrentHealth:0}/{target.MaxHealth:0}"
                    : "Sentinel: rebooting…";
            GUI.Label(new Rect(40f, 112f, 200f, 22f), targetText, _body);

            if (combat != null && combat.CooldownRemaining > 0f)
            {
                GUI.Label(new Rect(240f, 112f, 120f, 22f), $"CD {combat.CooldownRemaining:0.00}s", _body);
            }
        }
    }
}
