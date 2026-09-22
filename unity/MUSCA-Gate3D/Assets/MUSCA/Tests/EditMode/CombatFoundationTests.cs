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

        [Test]
        public void StaminaSpendUsesDelayThenRegenerates()
        {
            var stamina = new CombatStamina(100f, 20f, 0.5f);

            Assert.That(stamina.TrySpend(28f), Is.True);
            Assert.That(stamina.CurrentStamina, Is.EqualTo(72f));

            stamina.Tick(0.25f);
            Assert.That(stamina.CurrentStamina, Is.EqualTo(72f));
            Assert.That(stamina.RegenDelayRemaining, Is.EqualTo(0.25f).Within(0.001f));

            stamina.Tick(0.25f);
            Assert.That(stamina.CurrentStamina, Is.EqualTo(72f));

            stamina.Tick(0.5f);
            Assert.That(stamina.CurrentStamina, Is.EqualTo(82f).Within(0.001f));
        }

        [Test]
        public void StaminaRejectsOverdraw()
        {
            var stamina = new CombatStamina(30f, 10f, 0f);
            Assert.That(stamina.TrySpend(28f), Is.True);
            Assert.That(stamina.TrySpend(28f), Is.False);
            Assert.That(stamina.CurrentStamina, Is.EqualTo(2f));
        }

        [Test]
        public void StaminaFrameSpikeConsumesDelayThenRegeneratesRemainder()
        {
            var stamina = new CombatStamina(100f, 20f, 0.5f);
            stamina.TrySpend(28f);

            stamina.Tick(1f);

            Assert.That(stamina.RegenDelayRemaining, Is.EqualTo(0f));
            Assert.That(stamina.CurrentStamina, Is.EqualTo(82f).Within(0.001f));
        }

        [Test]
        public void NeutralDodgeResolvesToBackstep()
        {
            Vector3 direction = PlayerDodgeController.ResolveDodgeDirection(
                0f, 0f, Vector3.forward, Vector3.right);

            Assert.That(direction, Is.EqualTo(Vector3.back));
            Assert.That(PlayerDodgeController.ClassifyDirection(
                direction, Vector3.forward, Vector3.right), Is.EqualTo(DodgeDirection.Backward));
        }

        [Test]
        public void DiagonalDodgeIsNormalizedAndClassified()
        {
            Vector3 direction = PlayerDodgeController.ResolveDodgeDirection(
                1f, 0.4f, Vector3.forward, Vector3.right);

            Assert.That(direction.magnitude, Is.EqualTo(1f).Within(0.001f));
            Assert.That(PlayerDodgeController.ClassifyDirection(
                direction, Vector3.forward, Vector3.right), Is.EqualTo(DodgeDirection.Right));
        }

        [Test]
        public void LockOnScorePrefersCenteredTarget()
        {
            float centered = PlayerLockOn.ScoreCandidate(8f, 5f, 14f, 82f);
            float peripheral = PlayerLockOn.ScoreCandidate(5f, 50f, 14f, 82f);

            Assert.That(centered, Is.LessThan(peripheral));
        }

        [Test]
        public void SentinelStrikeUsesCommittedTelegraphPoint()
        {
            Vector3 committed = new Vector3(0f, 0f, 1.5f);

            Assert.That(SentinelCombatBrain.IsPointInsideStrike(
                new Vector3(0.4f, 0f, 1.5f), committed, 0.95f), Is.True);
            Assert.That(SentinelCombatBrain.IsPointInsideStrike(
                new Vector3(1.2f, 0f, 1.5f), committed, 0.95f), Is.False);
        }

        [Test]
        public void KaelPredictionDoesNotLockBeforeMinimumHistory()
        {
            var model = new KaelPredictionModel(7, 4, 0.6f);
            model.Record(DodgeDirection.Right);
            model.Record(DodgeDirection.Right);
            model.Record(DodgeDirection.Right);

            KaelPredictionSnapshot snapshot = model.Snapshot();
            Assert.That(snapshot.Direction, Is.EqualTo(DodgeDirection.Right));
            Assert.That(snapshot.SampleCount, Is.EqualTo(3));
            Assert.That(snapshot.Confidence, Is.EqualTo(1f));
            Assert.That(snapshot.Locked, Is.False);
        }

        [Test]
        public void KaelPredictionLocksRepeatedRightHabit()
        {
            var model = new KaelPredictionModel(7, 4, 0.6f);
            for (int i = 0; i < 5; i++) model.Record(DodgeDirection.Right);
            model.Record(DodgeDirection.Left);

            KaelPredictionSnapshot snapshot = model.Snapshot();
            Assert.That(snapshot.Direction, Is.EqualTo(DodgeDirection.Right));
            Assert.That(snapshot.SampleCount, Is.EqualTo(6));
            Assert.That(snapshot.Confidence, Is.EqualTo(5f / 6f).Within(0.001f));
            Assert.That(snapshot.Locked, Is.True);
        }

        [Test]
        public void KaelPredictionSlidingWindowCanBeBrokenByNewHabit()
        {
            var model = new KaelPredictionModel(5, 4, 0.6f);
            for (int i = 0; i < 5; i++) model.Record(DodgeDirection.Right);
            for (int i = 0; i < 5; i++) model.Record(DodgeDirection.Left);

            KaelPredictionSnapshot snapshot = model.Snapshot();
            Assert.That(snapshot.Direction, Is.EqualTo(DodgeDirection.Left));
            Assert.That(snapshot.SampleCount, Is.EqualTo(5));
            Assert.That(snapshot.Confidence, Is.EqualTo(1f));
            Assert.That(snapshot.Locked, Is.True);
        }

        [Test]
        public void KaelPredictionDirectionUsesPlayerReferenceFrame()
        {
            Vector3 direction = KaelPredictionModel.ResolveWorldDirection(
                DodgeDirection.Right, Vector3.back, Vector3.left);

            Assert.That(direction, Is.EqualTo(Vector3.left));
        }

        [Test]
        public void DodgeCommittedStepConsumesFullRemainingTimeAcrossFrameSpike()
        {
            Assert.That(
                FirstPersonController.ComputeCommittedStepSeconds(0.28f, 0.5f),
                Is.EqualTo(0.28f).Within(0.0001f));
            Assert.That(
                FirstPersonController.ComputeCommittedStepSeconds(0.28f, 0.016f),
                Is.EqualTo(0.016f).Within(0.0001f));
        }

        [Test]
        public void DodgeProgressIsControlledEaseOutAndPreservesEndpoints()
        {
            float start = FirstPersonController.ComputeDodgeProgress(0f);
            float quarter = FirstPersonController.ComputeDodgeProgress(0.25f);
            float half = FirstPersonController.ComputeDodgeProgress(0.5f);
            float end = FirstPersonController.ComputeDodgeProgress(1f);

            Assert.That(start, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(end, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(quarter, Is.EqualTo(0.4375f).Within(0.0001f));
            Assert.That(half, Is.EqualTo(0.75f).Within(0.0001f));
            Assert.That(quarter, Is.LessThan(0.5f));
            Assert.That(quarter, Is.LessThan(half));
            Assert.That(half, Is.LessThan(end));
        }

        [Test]
        public void CameraRelativeMovementUsesCameraHeading()
        {
            GameObject cameraObject = new GameObject("qa-camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

            Vector3 movement = FirstPersonController.ResolveCameraRelativeMovement(
                Vector3.forward, camera, null);

            Assert.That(movement.x, Is.EqualTo(1f).Within(0.001f));
            Assert.That(movement.z, Is.EqualTo(0f).Within(0.001f));
            Object.DestroyImmediate(cameraObject);
        }

        [Test]
        public void ExternalForwardInputUsesPivotYaw()
        {
            GameObject bodyObject = new GameObject("qa-body");
            Vector3 forward = FirstPersonController.ResolveMovementDirection(
                Vector3.forward,
                bodyObject.transform,
                0f,
                true);

            Assert.That(forward.x, Is.EqualTo(0f).Within(0.001f));
            Assert.That(forward.z, Is.EqualTo(1f).Within(0.001f));
            Object.DestroyImmediate(bodyObject);
        }

        [Test]
        public void BodyFacingYawMatchesDesiredTravelDirection()
        {
            Assert.That(
                FirstPersonController.ComputeFacingYaw(Vector3.forward, 180f),
                Is.EqualTo(0f).Within(0.001f));
            Assert.That(
                FirstPersonController.ComputeFacingYaw(Vector3.back, 0f),
                Is.EqualTo(180f).Within(0.001f));
            Assert.That(
                FirstPersonController.ComputeFacingYaw(Vector3.zero, 37f),
                Is.EqualTo(37f).Within(0.001f));
        }

        [Test]
        public void PredictionBreakRequiresAppliedMiss()
        {
            Assert.That(SentinelCombatBrain.WasPredictionBroken(true, false), Is.True);
            Assert.That(SentinelCombatBrain.WasPredictionBroken(true, true), Is.False);
            Assert.That(SentinelCombatBrain.WasPredictionBroken(false, false), Is.False);
        }

        [Test]
        public void LockYawDeadZoneSuppressesMicroCorrection()
        {
            float velocity = 2f;
            float yaw = FirstPersonController.ComputeLockYawStep(
                10f, 10.05f, ref velocity, 0.10f, 420f, 0.12f, 1f / 60f);

            Assert.That(yaw, Is.EqualTo(10f).Within(0.0001f));
            Assert.That(velocity, Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void LockYawDampedStepMovesTowardTargetWithoutSnap()
        {
            float velocity = 0f;
            float first = FirstPersonController.ComputeLockYawStep(
                0f, 90f, ref velocity, 0.10f, 420f, 0.12f, 1f / 60f);
            float second = FirstPersonController.ComputeLockYawStep(
                first, 90f, ref velocity, 0.10f, 420f, 0.12f, 1f / 60f);

            Assert.That(first, Is.GreaterThan(0f));
            Assert.That(first, Is.LessThan(90f));
            Assert.That(second, Is.GreaterThan(first));
            Assert.That(second, Is.LessThan(90f));
        }

        [Test]
        public void JumpVelocityUsesHeightAndDownwardGravity()
        {
            float velocity = FirstPersonController.ComputeJumpVelocity(1.15f, -24f);

            Assert.That(velocity, Is.GreaterThan(7.4f));
            Assert.That(velocity, Is.LessThan(7.5f));
            Assert.That(FirstPersonController.ComputeJumpVelocity(0f, -24f), Is.EqualTo(0f));
            Assert.That(FirstPersonController.ComputeJumpVelocity(1f, 9.81f), Is.EqualTo(0f));
        }

        [Test]
        public void VerticalIntegrationPreservesJumpImpulseAcrossLongFrame()
        {
            float velocity = FirstPersonController.ComputeJumpVelocity(1.15f, -24f);
            float displacement = FirstPersonController.ComputeVerticalDisplacement(
                velocity, -24f, 0.28f);
            float finalVelocity = FirstPersonController.ComputeVerticalVelocity(
                velocity, -24f, 0.28f);

            Assert.That(displacement, Is.GreaterThan(1.10f));
            Assert.That(displacement, Is.LessThan(1.16f));
            Assert.That(finalVelocity, Is.GreaterThan(0f));
        }
    }
}
