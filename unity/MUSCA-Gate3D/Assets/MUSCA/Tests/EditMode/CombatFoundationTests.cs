using NUnit.Framework;
using UnityEngine;

namespace MUSCA.Gate3D.Tests
{
    public sealed class CombatFoundationTests
    {
        [Test]
        public void DamageClampsAtZeroAndReportsDeathOnce()
        {
            var health = new CombatHealth(100f);

            Assert.That(health.ApplyDamage(35f), Is.False);
            Assert.That(health.CurrentHealth, Is.EqualTo(65f));
            Assert.That(health.ApplyDamage(80f), Is.True);
            Assert.That(health.CurrentHealth, Is.EqualTo(0f));
            Assert.That(health.ApplyDamage(10f), Is.False);
        }

        [Test]
        public void ResetRestoresFullHealth()
        {
            var health = new CombatHealth(75f);
            health.ApplyDamage(25f);
            health.Reset();

            Assert.That(health.CurrentHealth, Is.EqualTo(75f));
            Assert.That(health.IsAlive, Is.True);
            Assert.That(health.Normalized, Is.EqualTo(1f));
        }

        [Test]
        public void AttackArcAcceptsForwardTargetAndRejectsRearTarget()
        {
            Vector3 origin = Vector3.zero;
            Vector3 forward = Vector3.forward;

            Assert.That(PlayerMeleeCombat.IsWithinAttackArc(
                origin, forward, new Vector3(0.4f, 0f, 1.2f), 1.8f, 0.12f), Is.True);
            Assert.That(PlayerMeleeCombat.IsWithinAttackArc(
                origin, forward, new Vector3(0f, 0f, -1f), 1.8f, 0.12f), Is.False);
        }

        [Test]
        public void AttackArcRejectsTargetOutsideRange()
        {
            Assert.That(PlayerMeleeCombat.IsWithinAttackArc(
                Vector3.zero, Vector3.forward, new Vector3(0f, 0f, 2.1f), 1.8f, 0.12f), Is.False);
        }
    }
}
