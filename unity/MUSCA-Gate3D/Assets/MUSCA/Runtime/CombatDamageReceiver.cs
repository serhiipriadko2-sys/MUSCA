using UnityEngine;

namespace MUSCA.Gate3D
{
    public sealed class CombatDamageReceiver : MonoBehaviour
    {
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float respawnDelaySeconds = 1.75f;

        private CombatHealth _health;
        private Renderer[] _renderers;
        private Collider[] _colliders;
        private Vector3 _spawnPosition;
        private Quaternion _spawnRotation;
        private float _respawnAt = -1f;

        public float CurrentHealth => _health?.CurrentHealth ?? 0f;
        public float MaxHealth => _health?.MaxHealth ?? maxHealth;
        public bool IsAlive => _health != null && _health.IsAlive;

        private void Awake()
        {
            _spawnPosition = transform.position;
            _spawnRotation = transform.rotation;
            _renderers = GetComponentsInChildren<Renderer>(true);
            _colliders = GetComponentsInChildren<Collider>(true);
            RebuildState();
        }

        public void Configure(float health, float respawnDelay)
        {
            maxHealth = Mathf.Max(1f, health);
            respawnDelaySeconds = Mathf.Max(0.1f, respawnDelay);
            if (Application.isPlaying)
            {
                RebuildState();
            }
        }

        public void ApplyDamage(float amount)
        {
            if (_health == null || !_health.IsAlive)
            {
                return;
            }

            bool died = _health.ApplyDamage(amount);
            if (!died)
            {
                return;
            }

            SetVisibleAndSolid(false);
            _respawnAt = Time.time + respawnDelaySeconds;
        }

        private void Update()
        {
            if (_respawnAt < 0f || Time.time < _respawnAt)
            {
                return;
            }

            transform.SetPositionAndRotation(_spawnPosition, _spawnRotation);
            RebuildState();
            SetVisibleAndSolid(true);
            _respawnAt = -1f;
        }

        private void RebuildState()
        {
            _health = new CombatHealth(Mathf.Max(1f, maxHealth));
        }

        private void SetVisibleAndSolid(bool value)
        {
            if (_renderers != null)
            {
                foreach (Renderer item in _renderers) item.enabled = value;
            }

            if (_colliders != null)
            {
                foreach (Collider item in _colliders) item.enabled = value;
            }
        }
    }
}
