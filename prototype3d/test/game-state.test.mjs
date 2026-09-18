import test from 'node:test';
import assert from 'node:assert/strict';
import { Gate3DState, interactionHint, signalStrength } from '../src/game-state.mjs';

test('initial player view hides hidden layout and answer', () => {
  const a = new Gate3DState({ layout: 'a' }).playerView();
  const b = new Gate3DState({ layout: 'b' }).playerView();
  assert.deepEqual(a, b);
  assert.equal('layout' in a, false);
  assert.equal('neutral' in a, false);
});

test('chemical scan costs one cell and reveals evidence', () => {
  const state = new Gate3DState({ mode: 'light_chemical', layout: 'a' });
  assert.deepEqual(state.scan(), { accepted: true });
  assert.equal(state.playerView().cells, 1);
  assert.equal(state.playerView().scanned, true);
  assert.match(state.playerView().log.at(-1).text, /Кобальтовый/);
});

test('repeat scan is rejected without extra cost', () => {
  const state = new Gate3DState({ mode: 'light_chemical', layout: 'a' });
  state.scan();
  assert.equal(state.scan().accepted, false);
  assert.equal(state.playerView().cells, 1);
});

test('light-only mode cannot scan', () => {
  const state = new Gate3DState({ mode: 'light', layout: 'a' });
  assert.deepEqual(state.scan(), { accepted: false, reason: 'mode' });
  assert.equal(state.playerView().cells, 2);
});

test('neutral reagent opens while reactive reagent seals', () => {
  const opened = new Gate3DState({ layout: 'a' });
  const sealed = new Gate3DState({ layout: 'a' });
  assert.equal(opened.choose('cobalt').outcome, 'opened');
  assert.equal(opened.playerView().cells, 2);
  assert.equal(sealed.choose('amber').outcome, 'sealed');
  assert.equal(sealed.playerView().cells, 0);
});

test('objective chain advances from exploration to opened gate', () => {
  const state = new Gate3DState({ layout: 'a' });
  state.markReachedGate();
  state.observeStation('amber');
  state.observeStation('cobalt');
  state.scan();
  state.choose('cobalt');
  assert.deepEqual(state.playerView().objectives, [true, true, true, true, true]);
});

test('interaction hint is contextual and player safe', () => {
  const state = new Gate3DState({ layout: 'a' });
  assert.equal(interactionHint(5, 'amber', state.playerView()), null);
  assert.match(interactionHint(1.5, 'amber', state.playerView()), /Q/);
  state.scan();
  assert.doesNotMatch(interactionHint(1.5, 'amber', state.playerView()), /Q/);
});

test('signal strength grows monotonically toward the gate', () => {
  const samples = [12, 8, 4, 0, -4, -8, -11].map(signalStrength);
  assert.equal(samples[0], 2);
  assert.equal(samples.at(-1), 10);
  for (let i = 1; i < samples.length; i += 1) assert.ok(samples[i] >= samples[i - 1]);
});
