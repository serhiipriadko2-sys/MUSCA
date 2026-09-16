using System;
using System.Collections.Generic;

namespace MUSCA.Gate3D
{
    public enum SensorMode
    {
        Light,
        LightChemical
    }

    public enum Reagent
    {
        Amber,
        Cobalt
    }

    public enum GateOutcome
    {
        Pending,
        Opened,
        Sealed
    }

    public enum EvidenceLabel
    {
        Fact,
        Hypothesis,
        Interpretation,
        Unknown
    }

    public readonly struct EvidenceEntry
    {
        public EvidenceEntry(EvidenceLabel label, string text)
        {
            Label = label;
            Text = text ?? throw new ArgumentNullException(nameof(text));
        }

        public EvidenceLabel Label { get; }
        public string Text { get; }
    }

    public sealed class GateSnapshot
    {
        public GateSnapshot(
            SensorMode mode,
            int cells,
            bool scanned,
            bool reachedGate,
            bool seenAmber,
            bool seenCobalt,
            GateOutcome outcome,
            Reagent? selected,
            IReadOnlyList<EvidenceEntry> evidence)
        {
            Mode = mode;
            Cells = cells;
            Scanned = scanned;
            ReachedGate = reachedGate;
            SeenAmber = seenAmber;
            SeenCobalt = seenCobalt;
            Outcome = outcome;
            Selected = selected;
            Evidence = evidence;
        }

        public SensorMode Mode { get; }
        public int Cells { get; }
        public bool Scanned { get; }
        public bool ReachedGate { get; }
        public bool SeenAmber { get; }
        public bool SeenCobalt { get; }
        public GateOutcome Outcome { get; }
        public Reagent? Selected { get; }
        public IReadOnlyList<EvidenceEntry> Evidence { get; }

        public bool ObjectiveComplete(int index)
        {
            return index switch
            {
                0 => ReachedGate,
                1 => SeenAmber && SeenCobalt,
                2 => Scanned || Selected.HasValue,
                3 => Selected.HasValue,
                4 => Outcome == GateOutcome.Opened,
                _ => throw new ArgumentOutOfRangeException(nameof(index))
            };
        }
    }

    public sealed class GateDomain
    {
        private readonly Reagent _neutral;
        private readonly HashSet<Reagent> _seen = new HashSet<Reagent>();
        private readonly List<EvidenceEntry> _evidence = new List<EvidenceEntry>();

        public GateDomain(SensorMode mode, Reagent neutral)
        {
            Mode = mode;
            _neutral = neutral;
            Reset();
        }

        public SensorMode Mode { get; }
        public int Cells { get; private set; }
        public bool Scanned { get; private set; }
        public bool ReachedGate { get; private set; }
        public GateOutcome Outcome { get; private set; }
        public Reagent? Selected { get; private set; }
        public IReadOnlyList<EvidenceEntry> Evidence => _evidence;

        public void Reset()
        {
            Cells = 2;
            Scanned = false;
            ReachedGate = false;
            Outcome = GateOutcome.Pending;
            Selected = null;
            _seen.Clear();
            _evidence.Clear();
            _evidence.Add(new EvidenceEntry(EvidenceLabel.Fact, "Световой сигнал обнаружен впереди."));
            _evidence.Add(new EvidenceEntry(EvidenceLabel.Hypothesis, "Источник сигнала связан со шлюзом; путь нужно проверить."));
        }

        public bool MarkReachedGate()
        {
            if (ReachedGate)
            {
                return false;
            }

            ReachedGate = true;
            _evidence.Add(new EvidenceEntry(EvidenceLabel.Fact, "Источник сигнала локализован у шлюза."));
            return true;
        }

        public bool ObserveStation(Reagent reagent)
        {
            return _seen.Add(reagent);
        }

        public bool Scan()
        {
            if (Outcome != GateOutcome.Pending || Mode != SensorMode.LightChemical || Scanned || Cells < 1)
            {
                return false;
            }

            Cells -= 1;
            Scanned = true;
            foreach (Reagent reagent in Enum.GetValues(typeof(Reagent)))
            {
                string state = reagent == _neutral ? "нейтральный" : "реактивный";
                _evidence.Add(new EvidenceEntry(EvidenceLabel.Fact, $"{DisplayName(reagent)}: {state} по химической пробе."));
            }

            _evidence.Add(new EvidenceEntry(EvidenceLabel.Interpretation, $"Проба выделяет {DisplayName(_neutral)} как подходящий реагент."));
            return true;
        }

        public GateOutcome Choose(Reagent reagent)
        {
            if (Outcome != GateOutcome.Pending)
            {
                return Outcome;
            }

            Selected = reagent;
            Outcome = reagent == _neutral ? GateOutcome.Opened : GateOutcome.Sealed;

            if (Outcome == GateOutcome.Opened)
            {
                _evidence.Add(new EvidenceEntry(EvidenceLabel.Fact, $"{DisplayName(reagent)} принят шлюзом."));
                _evidence.Add(new EvidenceEntry(EvidenceLabel.Interpretation, "Проход разблокирован; решение подтвердилось действием мира."));
            }
            else
            {
                Cells = 0;
                _evidence.Add(new EvidenceEntry(EvidenceLabel.Fact, $"{DisplayName(reagent)} вызвал блокировку шлюза."));
                _evidence.Add(new EvidenceEntry(EvidenceLabel.Interpretation, "Выбранный реагент оказался реактивным."));
            }

            return Outcome;
        }

        public GateSnapshot Snapshot()
        {
            return new GateSnapshot(
                Mode,
                Cells,
                Scanned,
                ReachedGate,
                _seen.Contains(Reagent.Amber),
                _seen.Contains(Reagent.Cobalt),
                Outcome,
                Selected,
                _evidence.ToArray());
        }

        public static string DisplayName(Reagent reagent)
        {
            return reagent == Reagent.Amber ? "Янтарный реагент" : "Кобальтовый реагент";
        }
    }
}
