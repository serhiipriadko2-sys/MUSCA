using System;

namespace MUSCA.Gate3D
{
    public sealed class CombatHealth
    {
        public float MaxHealth { get; }
        public float CurrentHealth { get; private set; }
        public bool IsAlive => CurrentHealth > 0f;
        public float Normalized => MaxHealth <= 0f ? 0f : CurrentHealth / MaxHealth;

        public CombatHealth(float maxHealth)
        {
            if (maxHealth <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(maxHealth), "Max health must be positive.");
            }

            MaxHealth = maxHealth;
            CurrentHealth = maxHealth;
        }

        public bool ApplyDamage(float amount)
        {
            if (amount <= 0f || !IsAlive)
            {
                return false;
            }

            float previous = CurrentHealth;
            CurrentHealth = Math.Max(0f, CurrentHealth - amount);
            return previous > 0f && CurrentHealth <= 0f;
        }

        public void Reset()
        {
            CurrentHealth = MaxHealth;
        }
    }
}
