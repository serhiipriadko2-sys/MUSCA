using NUnit.Framework;

namespace MUSCA.Gate3D.Tests
{
    public sealed class GateDomainTests
    {
        [Test]
        public void HiddenAnswerDoesNotChangeInitialPlayerSnapshot()
        {
            GateSnapshot cobaltNeutral = new GateDomain(SensorMode.LightChemical, Reagent.Cobalt).Snapshot();
            GateSnapshot amberNeutral = new GateDomain(SensorMode.LightChemical, Reagent.Amber).Snapshot();

            Assert.That(cobaltNeutral.Cells, Is.EqualTo(amberNeutral.Cells));
            Assert.That(cobaltNeutral.Scanned, Is.EqualTo(amberNeutral.Scanned));
            Assert.That(cobaltNeutral.Outcome, Is.EqualTo(amberNeutral.Outcome));
            Assert.That(cobaltNeutral.Evidence.Count, Is.EqualTo(amberNeutral.Evidence.Count));
            for (int i = 0; i < cobaltNeutral.Evidence.Count; i++)
            {
                Assert.That(cobaltNeutral.Evidence[i].Text, Is.EqualTo(amberNeutral.Evidence[i].Text));
            }
        }

        [Test]
        public void ChemicalScanCostsOneCellAndAddsEvidence()
        {
            var domain = new GateDomain(SensorMode.LightChemical, Reagent.Cobalt);
            Assert.That(domain.Scan(), Is.True);
            Assert.That(domain.Cells, Is.EqualTo(1));
            Assert.That(domain.Scanned, Is.True);
            Assert.That(domain.Evidence[^1].Text, Does.Contain("Кобальтовый"));
        }

        [Test]
        public void ScanIsRejectedInLightOnlyModeWithoutResourceCost()
        {
            var domain = new GateDomain(SensorMode.Light, Reagent.Cobalt);
            Assert.That(domain.Scan(), Is.False);
            Assert.That(domain.Cells, Is.EqualTo(2));
            Assert.That(domain.Scanned, Is.False);
        }

        [Test]
        public void RepeatedScanDoesNotSpendTwice()
        {
            var domain = new GateDomain(SensorMode.LightChemical, Reagent.Cobalt);
            Assert.That(domain.Scan(), Is.True);
            Assert.That(domain.Scan(), Is.False);
            Assert.That(domain.Cells, Is.EqualTo(1));
        }

        [Test]
        public void NeutralChoiceOpensAndReactiveChoiceSeals()
        {
            var opened = new GateDomain(SensorMode.LightChemical, Reagent.Cobalt);
            var sealedRun = new GateDomain(SensorMode.LightChemical, Reagent.Cobalt);

            Assert.That(opened.Choose(Reagent.Cobalt), Is.EqualTo(GateOutcome.Opened));
            Assert.That(opened.Cells, Is.EqualTo(2));
            Assert.That(sealedRun.Choose(Reagent.Amber), Is.EqualTo(GateOutcome.Sealed));
            Assert.That(sealedRun.Cells, Is.EqualTo(0));
        }

        [Test]
        public void ObjectiveChainCompletesOnlyAfterWorldOpens()
        {
            var domain = new GateDomain(SensorMode.LightChemical, Reagent.Cobalt);
            domain.MarkReachedGate();
            domain.ObserveStation(Reagent.Amber);
            domain.ObserveStation(Reagent.Cobalt);
            domain.Scan();
            domain.Choose(Reagent.Cobalt);
            GateSnapshot snapshot = domain.Snapshot();

            for (int i = 0; i < 5; i++)
            {
                Assert.That(snapshot.ObjectiveComplete(i), Is.True, $"objective {i}");
            }
        }

        [Test]
        public void ResetRestoresPreregisteredResourcesAndPendingState()
        {
            var domain = new GateDomain(SensorMode.LightChemical, Reagent.Cobalt);
            domain.Scan();
            domain.Choose(Reagent.Amber);
            domain.Reset();

            Assert.That(domain.Cells, Is.EqualTo(2));
            Assert.That(domain.Scanned, Is.False);
            Assert.That(domain.Outcome, Is.EqualTo(GateOutcome.Pending));
            Assert.That(domain.Selected, Is.Null);
        }
    }
}
