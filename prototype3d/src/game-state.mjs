export const REAGENTS = Object.freeze(['amber', 'cobalt']);
export const MODES = Object.freeze(['light', 'light_chemical']);

const HIDDEN_LAYOUTS = Object.freeze({
  a: Object.freeze({ neutral: 'cobalt' }),
  b: Object.freeze({ neutral: 'amber' }),
});

export const OBJECTIVES = Object.freeze([
  'Дойти до источника сигнала',
  'Осмотреть станции AMBER и COBALT',
  'Решить: просканировать состав или рискнуть',
  'Выбрать нейтральный реагент',
  'Открыть шлюз',
]);

export class Gate3DState {
  constructor({ mode = 'light_chemical', layout = 'a' } = {}) {
    if (!MODES.includes(mode)) throw new TypeError('unknown sensor mode');
    if (!Object.hasOwn(HIDDEN_LAYOUTS, layout)) throw new TypeError('unknown layout');
    this._layout = layout;
    this.mode = mode;
    this.reset();
  }

  reset() {
    this.cells = 2;
    this.scanned = false;
    this.seenStations = new Set();
    this.reachedGate = false;
    this.selected = null;
    this.outcome = 'pending';
    this.log = [
      { label: 'FACT', text: 'Световой сигнал обнаружен впереди.' },
      { label: 'HYP', text: 'Источник сигнала связан со шлюзом; путь нужно проверить.' },
    ];
  }

  markReachedGate() {
    if (this.reachedGate) return false;
    this.reachedGate = true;
    this.log.push({ label: 'FACT', text: 'Источник сигнала локализован у шлюза.' });
    return true;
  }

  observeStation(reagent) {
    if (!REAGENTS.includes(reagent)) throw new TypeError('unknown reagent');
    const before = this.seenStations.size;
    this.seenStations.add(reagent);
    return this.seenStations.size !== before;
  }

  scan() {
    if (this.outcome !== 'pending') return { accepted: false, reason: 'finished' };
    if (this.mode !== 'light_chemical') return { accepted: false, reason: 'mode' };
    if (this.scanned) return { accepted: false, reason: 'already_scanned' };
    if (this.cells < 1) return { accepted: false, reason: 'resource' };
    this.cells -= 1;
    this.scanned = true;
    const neutral = HIDDEN_LAYOUTS[this._layout].neutral;
    for (const reagent of REAGENTS) {
      const state = reagent === neutral ? 'нейтральный' : 'реактивный';
      this.log.push({ label: 'FACT', text: `${displayName(reagent)}: ${state} по химической пробе.` });
    }
    this.log.push({ label: 'INTERP', text: `Проба выделяет ${displayName(neutral)} как подходящий реагент.` });
    return { accepted: true };
  }

  choose(reagent) {
    if (!REAGENTS.includes(reagent)) throw new TypeError('unknown reagent');
    if (this.outcome !== 'pending') return { accepted: false, reason: 'finished' };
    this.selected = reagent;
    const neutral = HIDDEN_LAYOUTS[this._layout].neutral;
    this.outcome = reagent === neutral ? 'opened' : 'sealed';
    if (this.outcome === 'opened') {
      this.log.push({ label: 'FACT', text: `${displayName(reagent)} принят шлюзом.` });
      this.log.push({ label: 'INTERP', text: 'Проход разблокирован; решение подтвердилось действием мира.' });
    } else {
      this.cells = 0;
      this.log.push({ label: 'FACT', text: `${displayName(reagent)} вызвал блокировку шлюза.` });
      this.log.push({ label: 'INTERP', text: 'Выбранный реагент оказался реактивным.' });
    }
    return { accepted: true, outcome: this.outcome };
  }

  objectiveStates() {
    const stationsKnown = this.seenStations.size === REAGENTS.length;
    return [
      this.reachedGate,
      stationsKnown,
      this.scanned || this.selected !== null,
      this.selected !== null,
      this.outcome === 'opened',
    ];
  }

  playerView() {
    return {
      mode: this.mode,
      cells: this.cells,
      scanned: this.scanned,
      seenStations: [...this.seenStations].sort(),
      reachedGate: this.reachedGate,
      selected: this.selected,
      outcome: this.outcome,
      objectives: this.objectiveStates(),
      log: this.log.map((entry) => ({ ...entry })),
    };
  }
}

export function displayName(reagent) {
  return reagent === 'amber' ? 'Янтарный реагент' : 'Кобальтовый реагент';
}

export function signalStrength(positionZ) {
  const spawnZ = 12;
  const sourceZ = -11;
  const progress = Math.max(0, Math.min(1, (spawnZ - positionZ) / (spawnZ - sourceZ)));
  return Math.round(2 + progress * 8);
}

export function interactionHint(distance, reagent, state) {
  if (state.outcome !== 'pending' || distance > 2.4) return null;
  const scan = state.mode === 'light_chemical' && !state.scanned ? 'Q — химическая проба · ' : '';
  return `${scan}E — выбрать ${reagent.toUpperCase()}`;
}
