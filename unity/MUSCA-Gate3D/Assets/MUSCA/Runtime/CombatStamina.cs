using System;

namespace MUSCA.Gate3D
{
    public sealed class CombatStamina
    {
        public float MaxStamina { get; }
        public float RegenPerSecond { get; }
        public float RegenDelaySeconds { get; }
        public float CurrentStamina { get; private set; }
        public float RegenDelayRemaining { get; private set; }

        public CombatStamina(float maxStamina, float regenPerSecond, float regenDelaySeconds)
        {
            if (maxStamina <= 0f) throw new ArgumentOutOfRangeException(nameof(maxStamina));
            if (regenPerSecond < 0f) throw new ArgumentOutOfRangeException(nameof(regenPerSecond));
            if (regenDelaySeconds < 0f) throw new ArgumentOutOfRangeException(nameof(regenDelaySeconds));

            MaxStamina = maxStamina;
            RegenPerSecond = regenPerSecond;
            RegenDelaySeconds = regenDelaySeconds;
            CurrentStamina = maxStamina;
        }

        public bool CanSpend(float amount)
        {
            return amount > 0f && CurrentStamina + 0.0001f >= amount;
        }

        public bool TrySpend(float amount)
        {
            if (!CanSpend(amount)) return false;

            CurrentStamina = Math.Max(0f, CurrentStamina - amount);
            RegenDelayRemaining = RegenDelaySeconds;
            return true;
        }

        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0f) return;

            float availableForRegen = deltaTime;
            if (RegenDelayRemaining > 0f)
            {
                float delayStep = Math.Min(RegenDelayRemaining, availableForRegen);
                RegenDelayRemaining = Math.Max(0f, RegenDelayRemaining - delayStep);
                availableForRegen -= delayStep;
            }

            if (availableForRegen > 0f)
            {
                CurrentStamina = Math.Min(
                    MaxStamina, CurrentStamina + RegenPerSecond * availableForRegen);
            }
        }

        public void Reset()
        {
            CurrentStamina = MaxStamina;
            RegenDelayRemaining = 0f;
        }
    }
}
