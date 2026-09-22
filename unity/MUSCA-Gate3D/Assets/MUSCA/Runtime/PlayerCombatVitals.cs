using UnityEngine;

namespace MUSCA.Gate3D
{
    [RequireComponent(typeof(FirstPersonController))]
    public sealed class PlayerCombatVitals : MonoBehaviour
    {
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float maxStamina = 100f;
        [SerializeField] private float staminaRegenPerSecond = 28f;
        [SerializeField] private float staminaRegenDelaySeconds = 0.65f;
        [SerializeField] private float respawnDelaySeconds = 1.25f;

        private FirstPersonController _movement;
        private CombatHealth _health;
        private CombatStamina _stamina;
        private Vector3 _spawnPosition;
        private float _spawnYaw;
        private float _respawnAt = -1f;

        public float CurrentHealth => _health?.CurrentHealth ?? 0f;
        public float MaxHealth => _health?.MaxHealth ?? maxHealth;
        public float CurrentStamina => _stamina?.CurrentStamina ?? 0f;
        public float MaxStamina => _stamina?.MaxStamina ?? maxStamina;
        public float StaminaNormalized => MaxStamina <= 0f ? 0f : CurrentStamina / MaxStamina;
        public bool IsAlive => _health != null && _health.IsAlive;
        public bool IsRespawning => _respawnAt >= 0f;

        private void Awake()
        {
            _movement = GetComponent<FirstPersonController>();
            _spawnPosition = transform.position;
            _spawnYaw = transform.eulerAngles.y;
            RebuildState();
        }

        public void Configure(
            float health = 100f,
            float stamina = 100f,
            float regenPerSecond = 28f,
            float regenDelay = 0.65f,
            float respawnDelay = 1.25f)
        {
            maxHealth = Mathf.Max(1f, health);
            maxStamina = Mathf.Max(1f, stamina);
            staminaRegenPerSecond = Mathf.Max(0f, regenPerSecond);
            staminaRegenDelaySeconds = Mathf.Max(0f, regenDelay);
            respawnDelaySeconds = Mathf.Max(0.1f, respawnDelay);
            if (Application.isPlaying) RebuildState();
        }

        private void Update()
        {
            _stamina?.Tick(Time.deltaTime);

            if (_respawnAt < 0f || Time.time < _respawnAt)
            {
                return;
            }

            _health.Reset();
            _stamina.Reset();
            _movement.Teleport(_spawnPosition, _spawnYaw);
            _movement.InputEnabled = true;
            _respawnAt = -1f;
        }

        public bool TrySpendStamina(float amount)
        {
            return IsAlive && _stamina != null && _stamina.TrySpend(amount);
        }

        public bool ApplyDamage(float amount)
        {
            if (_health == null || !_health.IsAlive || amount <= 0f)
            {
                return false;
            }

            if (_movement != null && _movement.IsDodgeInvulnerable)
            {
                return false;
            }

            bool died = _health.ApplyDamage(amount);
            if (died)
            {
                _movement.CancelDodge();
                _movement.InputEnabled = false;
                _respawnAt = Time.time + respawnDelaySeconds;
            }

            return true;
        }

        public void ResetForQa(Vector3 position, float yawDegrees)
        {
            if (_health == null || _stamina == null) RebuildState();
            _health.Reset();
            _stamina.Reset();
            _respawnAt = -1f;
            _movement.InputEnabled = true;
            _movement.Teleport(position, yawDegrees);
        }

        private void RebuildState()
        {
            _health = new CombatHealth(Mathf.Max(1f, maxHealth));
            _stamina = new CombatStamina(
                Mathf.Max(1f, maxStamina),
                Mathf.Max(0f, staminaRegenPerSecond),
                Mathf.Max(0f, staminaRegenDelaySeconds));
        }
    }
}
