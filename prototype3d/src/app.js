import * as THREE from 'three';
import { PointerLockControls } from 'three/addons/controls/PointerLockControls.js';
import { Gate3DState, OBJECTIVES, interactionHint, signalStrength } from './game-state.mjs';

const viewport = document.querySelector('#viewport');
const startOverlay = document.querySelector('#start-overlay');
const pauseOverlay = document.querySelector('#pause-overlay');
const resultOverlay = document.querySelector('#result-overlay');
const resultCard = document.querySelector('#result-card');
const resultTitle = document.querySelector('#result-title');
const resultText = document.querySelector('#result-text');
const interaction = document.querySelector('#interaction');
const companionLine = document.querySelector('#companion-line');
const objectivesEl = document.querySelector('#objectives');
const signalNumber = document.querySelector('#signal-number');
const signalBar = document.querySelector('#signal-bar');
const cellsEl = document.querySelector('#cells');
const cellsText = document.querySelector('#cells-text');
const logPanel = document.querySelector('#log-panel');
const logEntries = document.querySelector('#log-entries');

const scene = new THREE.Scene();
scene.background = new THREE.Color(0x030a10);
scene.fog = new THREE.FogExp2(0x061018, 0.027);
const camera = new THREE.PerspectiveCamera(72, innerWidth / innerHeight, 0.05, 100);
camera.position.set(0, 1.68, 12);
const renderer = new THREE.WebGLRenderer({ antialias: true, powerPreference: 'high-performance' });
renderer.setPixelRatio(Math.min(devicePixelRatio, 1.6));
renderer.setSize(innerWidth, innerHeight);
renderer.outputColorSpace = THREE.SRGBColorSpace;
renderer.shadowMap.enabled = true;
renderer.shadowMap.type = THREE.PCFSoftShadowMap;
viewport.appendChild(renderer.domElement);

const controls = new PointerLockControls(camera, renderer.domElement);
const clock = new THREE.Clock();
const keys = new Set();
const up = new THREE.Vector3(0, 1, 0);
const forward = new THREE.Vector3();
const right = new THREE.Vector3();
const candidate = new THREE.Vector3();
const stationPositions = {
  amber: new THREE.Vector3(-4.1, 0, -7.2),
  cobalt: new THREE.Vector3(4.1, 0, -7.2),
};
const gatePoint = new THREE.Vector3(0, 0, -11.6);
const PLAYER_RADIUS = 0.42;
const SPEED = 4.6;

let game = new Gate3DState({ mode: 'light_chemical', layout: 'a' });
let activeStation = null;
let gateLeft;
let gateRight;
let gateGlow;
let companion;
let scanPulse = null;
let scanPulseAge = 0;
let lastLogLength = 0;
const stationMeshes = new Map();
const colliders = [];
function material(color, emissive = 0x000000, roughness = 0.65, metalness = 0.35) {
  return new THREE.MeshStandardMaterial({ color, emissive, roughness, metalness });
}

function box(name, size, position, mat, { collider = false, cast = true } = {}) {
  const mesh = new THREE.Mesh(new THREE.BoxGeometry(...size), mat);
  mesh.name = name;
  mesh.position.set(...position);
  mesh.castShadow = cast;
  mesh.receiveShadow = true;
  scene.add(mesh);
  if (collider) {
    colliders.push({
      minX: position[0] - size[0] / 2 - PLAYER_RADIUS,
      maxX: position[0] + size[0] / 2 + PLAYER_RADIUS,
      minZ: position[2] - size[2] / 2 - PLAYER_RADIUS,
      maxZ: position[2] + size[2] / 2 + PLAYER_RADIUS,
      name,
    });
  }
  return mesh;
}

function canvasLabel(text, color = '#d8f5ff', width = 512, height = 128) {
  const canvas = document.createElement('canvas');
  canvas.width = width;
  canvas.height = height;
  const ctx = canvas.getContext('2d');
  ctx.clearRect(0, 0, width, height);
  ctx.font = '700 54px Segoe UI, Arial';
  ctx.textAlign = 'center';
  ctx.textBaseline = 'middle';
  ctx.fillStyle = color;
  ctx.fillText(text, width / 2, height / 2);
  const texture = new THREE.CanvasTexture(canvas);
  texture.colorSpace = THREE.SRGBColorSpace;
  const sprite = new THREE.Sprite(new THREE.SpriteMaterial({ map: texture, transparent: true, depthWrite: false }));
  sprite.scale.set(4.4, 1.1, 1);
  return sprite;
}

function addStrip(x, y, z, sx, sy, sz, color) {
  return box('light-strip', [sx, sy, sz], [x, y, z], material(0x07131c, color, 0.35, 0.25), { cast: false });
}

function buildRoom() {
  box('floor', [16, 0.22, 30], [0, -0.12, 0], material(0x15222a, 0x000000, 0.88, 0.25), { cast: false });
  box('left-wall', [0.45, 5.2, 30], [-7.78, 2.55, 0], material(0x101c24, 0x000000, 0.72, 0.55), { collider: true });
  box('right-wall', [0.45, 5.2, 30], [7.78, 2.55, 0], material(0x101c24, 0x000000, 0.72, 0.55), { collider: true });
  box('ceiling', [16, 0.3, 30], [0, 5.05, 0], material(0x08131a, 0x000000, 0.9, 0.2), { cast: false });
  for (let z = 12; z >= -12; z -= 3) {
    box('floor-panel', [15.1, 0.025, 0.045], [0, 0.015, z], material(0x2b4652, 0x000000, 0.8, 0.3), { cast: false });
    box('ceiling-beam', [15.2, 0.22, 0.34], [0, 4.72, z], material(0x24343d, 0x000000, 0.6, 0.55));
  }
  for (let z = 11.5; z >= -10.5; z -= 2.6) {
    addStrip(-7.47, 2.5, z, 0.05, 1.4, 0.12, 0x1ca8d1);
    addStrip(7.47, 2.5, z, 0.05, 1.4, 0.12, 0x1ca8d1);
  }
  box('back-left', [5.3, 5.2, 0.5], [-5.35, 2.55, -13.75], material(0x101c24), { collider: true });
  box('back-right', [5.3, 5.2, 0.5], [5.35, 2.55, -13.75], material(0x101c24), { collider: true });
  box('gate-header', [5.4, 1.25, 0.7], [0, 4.38, -13.55], material(0x1b2b35, 0x0a1c25, 0.5, 0.65));
  box('gate-frame-l', [0.72, 4.0, 0.75], [-2.72, 2.0, -13.55], material(0x253944, 0x08222c, 0.42, 0.72));
  box('gate-frame-r', [0.72, 4.0, 0.75], [2.72, 2.0, -13.55], material(0x253944, 0x08222c, 0.42, 0.72));
  gateLeft = box('gate-panel-left', [2.25, 3.5, 0.34], [-1.13, 1.76, -13.25], material(0x273844, 0x062738, 0.42, 0.72));
  gateRight = box('gate-panel-right', [2.25, 3.5, 0.34], [1.13, 1.76, -13.25], material(0x273844, 0x062738, 0.42, 0.72));
  gateGlow = addStrip(0, 3.72, -13.02, 0.16, 1.2, 0.12, 0x45d9ff);
  const gateLabel = canvasLabel('ШЛЮЗ A-1', '#d7f7ff');
  gateLabel.position.set(0, 4.25, -12.95);
  gateLabel.scale.set(3.5, 0.85, 1);
  scene.add(gateLabel);

  for (let z = 9.5; z >= -8.5; z -= 1.45) {
    const arrow = new THREE.Mesh(new THREE.ConeGeometry(0.28, 0.7, 3), material(0x0b4a61, 0x1cbde8, 0.34, 0.18));
    arrow.rotation.set(Math.PI / 2, 0, Math.PI);
    arrow.position.set(0, 0.035, z);
    arrow.scale.set(1.3, 1, 1);
    scene.add(arrow);
  }
}
function buildStation(reagent, color, emissive) {
  const p = stationPositions[reagent];
  const group = new THREE.Group();
  group.name = `${reagent}-station`;
  group.position.copy(p);
  const baseMat = material(0x18252d, 0x07131a, 0.56, 0.58);
  const base = new THREE.Mesh(new THREE.BoxGeometry(2.15, 0.8, 1.55), baseMat);
  base.position.y = 0.4;
  base.castShadow = base.receiveShadow = true;
  group.add(base);
  const column = new THREE.Mesh(new THREE.CylinderGeometry(0.48, 0.58, 2.3, 20), new THREE.MeshStandardMaterial({
    color, emissive, emissiveIntensity: 1.2, metalness: 0.15, roughness: 0.25, transparent: true, opacity: 0.84,
  }));
  column.position.y = 1.75;
  column.castShadow = true;
  group.add(column);
  const cage = new THREE.Mesh(new THREE.CylinderGeometry(0.68, 0.68, 2.65, 20, 1, true), material(0x263944, emissive, 0.5, 0.7));
  cage.position.y = 1.76;
  group.add(cage);
  const label = canvasLabel(reagent === 'amber' ? 'AMBER · 9' : 'COBALT · 4', reagent === 'amber' ? '#ffb13b' : '#59b7ff');
  label.position.set(0, 3.25, 0);
  label.scale.set(2.7, 0.68, 1);
  group.add(label);
  scene.add(group);
  colliders.push({ minX: p.x - 1.35, maxX: p.x + 1.35, minZ: p.z - 1.1, maxZ: p.z + 1.1, name: `${reagent}-station` });
  stationMeshes.set(reagent, { group, column });
}
function buildCompanion() {
  companion = new THREE.Group();
  const body = new THREE.Mesh(new THREE.SphereGeometry(0.22, 18, 14), material(0x122630, 0x22d4ff, 0.28, 0.66));
  companion.add(body);
  const eye = new THREE.Mesh(new THREE.SphereGeometry(0.085, 12, 10), material(0x86ecff, 0x47e6ff, 0.15, 0.1));
  eye.position.set(0, 0, -0.19);
  companion.add(eye);
  for (const side of [-1, 1]) {
    const wing = new THREE.Mesh(new THREE.CircleGeometry(0.26, 20), new THREE.MeshBasicMaterial({ color: 0x83dff0, transparent: true, opacity: 0.26, side: THREE.DoubleSide }));
    wing.scale.set(1.65, 0.52, 1);
    wing.position.set(side * 0.25, 0.04, 0.02);
    wing.rotation.y = Math.PI / 2;
    companion.add(wing);
  }
  const light = new THREE.PointLight(0x3fdcff, 1.4, 4.5, 2);
  companion.add(light);
  companion.position.set(-1.1, 1.4, 10.8);
  scene.add(companion);
}

function buildSign(text, position, color = '#9ee7ff') {
  const sprite = canvasLabel(text, color);
  sprite.position.set(...position);
  sprite.scale.set(3.4, 0.85, 1);
  scene.add(sprite);
}

buildRoom();
buildStation('amber', 0xffa21f, 0xff8a00);
buildStation('cobalt', 0x228cff, 0x1a8eff);
buildCompanion();
buildSign('СВЕТ ≠ СОСТАВ', [-5.4, 3.3, -1.8], '#ffcc73');
buildSign('НАБЛЮДАЙ → РЕШАЙ', [5.15, 3.3, -1.8], '#8de6ff');
const hemi = new THREE.HemisphereLight(0x8fdcff, 0x071018, 1.65);
scene.add(hemi);
const keyLight = new THREE.DirectionalLight(0xe6f8ff, 2.1);
keyLight.position.set(4, 8, 7);
keyLight.castShadow = true;
keyLight.shadow.mapSize.set(1536, 1536);
keyLight.shadow.camera.left = -12;
keyLight.shadow.camera.right = 12;
keyLight.shadow.camera.top = 18;
keyLight.shadow.camera.bottom = -18;
scene.add(keyLight);
const gateLight = new THREE.PointLight(0x37cfff, 18, 16, 2);
gateLight.position.set(0, 3.3, -11.2);
scene.add(gateLight);
const warmLight = new THREE.PointLight(0xffa52c, 8, 8, 2);
warmLight.position.set(-4.1, 2.4, -6.9);
scene.add(warmLight);
const coolLight = new THREE.PointLight(0x319dff, 9, 8, 2);
coolLight.position.set(4.1, 2.4, -6.9);
scene.add(coolLight);

function blocked(x, z) {
  if (x < -7.15 || x > 7.15 || z > 13.4 || z < -13.0) return true;
  for (const c of colliders) {
    if (x > c.minX && x < c.maxX && z > c.minZ && z < c.maxZ) return true;
  }
  if (game.playerView().outcome !== 'opened' && z < -12.45 && Math.abs(x) < 2.4) return true;
  return false;
}
function updateMovement(dt) {
  if (!controls.isLocked || game.playerView().outcome !== 'pending') return;
  const fb = (keys.has('KeyW') ? 1 : 0) - (keys.has('KeyS') ? 1 : 0);
  const lr = (keys.has('KeyD') ? 1 : 0) - (keys.has('KeyA') ? 1 : 0);
  if (fb === 0 && lr === 0) return;
  camera.getWorldDirection(forward);
  forward.y = 0;
  forward.normalize();
  right.crossVectors(forward, up).normalize();
  candidate.copy(camera.position);
  candidate.addScaledVector(forward, fb * SPEED * dt);
  candidate.addScaledVector(right, lr * SPEED * dt);
  if (!blocked(candidate.x, camera.position.z)) camera.position.x = candidate.x;
  if (!blocked(camera.position.x, candidate.z)) camera.position.z = candidate.z;
}

function nearestStation() {
  let best = null;
  let bestDistance = Infinity;
  for (const [name, position] of Object.entries(stationPositions)) {
    const dx = camera.position.x - position.x;
    const dz = camera.position.z - position.z;
    const distance = Math.hypot(dx, dz);
    if (distance < bestDistance) {
      best = name;
      bestDistance = distance;
    }
  }
  return { reagent: best, distance: bestDistance };
}

function updateSpatialState() {
  const gateDistance = Math.hypot(camera.position.x - gatePoint.x, camera.position.z - gatePoint.z);
  if (gateDistance < 6.4) game.markReachedGate();
  const nearest = nearestStation();
  activeStation = nearest.distance < 2.7 ? nearest.reagent : null;
  if (activeStation) game.observeStation(activeStation);
  const hint = activeStation ? interactionHint(nearest.distance, activeStation, game.playerView()) : null;
  interaction.textContent = hint || (gateDistance < 4.8 ? 'Шлюз ждёт решение у одной из станций.' : '');
  interaction.classList.toggle('hidden', !interaction.textContent);
}
function updateHud() {
  const view = game.playerView();
  const strength = signalStrength(camera.position.z);
  signalNumber.textContent = String(strength);
  signalBar.style.width = `${strength * 10}%`;
  cellsText.textContent = `${view.cells}/2`;
  cellsEl.replaceChildren();
  for (let i = 0; i < 2; i += 1) {
    const cell = document.createElement('i');
    cell.className = `cell${i < view.cells ? ' full' : ''}`;
    cellsEl.appendChild(cell);
  }
  objectivesEl.replaceChildren();
  const states = view.objectives;
  const firstUndone = states.findIndex((done) => !done);
  OBJECTIVES.forEach((objective, index) => {
    const li = document.createElement('li');
    li.textContent = objective;
    if (states[index]) li.classList.add('done');
    else if (index === firstUndone) li.classList.add('active');
    objectivesEl.appendChild(li);
  });

  if (view.log.length !== lastLogLength) {
    lastLogLength = view.log.length;
    logEntries.replaceChildren();
    for (const entry of view.log) {
      const node = document.createElement('div');
      node.className = 'log-entry';
      node.dataset.label = entry.label;
      node.innerHTML = `<strong>${entry.label}</strong>${entry.text}`;
      logEntries.appendChild(node);
    }
    logEntries.scrollTop = logEntries.scrollHeight;
  }
}
function spawnScanPulse() {
  if (scanPulse) scene.remove(scanPulse);
  scanPulse = new THREE.Mesh(
    new THREE.RingGeometry(0.35, 0.45, 48),
    new THREE.MeshBasicMaterial({ color: 0x56e3ff, transparent: true, opacity: 0.9, side: THREE.DoubleSide }),
  );
  scanPulse.rotation.x = -Math.PI / 2;
  const p = activeStation ? stationPositions[activeStation] : camera.position;
  scanPulse.position.set(p.x, 0.08, p.z);
  scanPulseAge = 0;
  scene.add(scanPulse);
}

function showResult() {
  const view = game.playerView();
  const success = view.outcome === 'opened';
  resultCard.classList.toggle('success', success);
  resultCard.classList.toggle('failure', !success);
  resultTitle.textContent = success ? 'ШЛЮЗ ОТКРЫТ' : 'ШЛЮЗ ЗАБЛОКИРОВАН';
  resultText.textContent = success
    ? `Решение подтверждено миром. Остаток ресурса: ${view.cells}/2.`
    : 'Реактивный реагент заблокировал проход. Гипотеза не подтвердилась.';
  resultOverlay.classList.remove('hidden');
  controls.unlock();
}

function scan() {
  if (!activeStation) {
    companionLine.textContent = 'MUSCA: подойди к станции реагента, чтобы взять химическую пробу.';
    return;
  }
  const result = game.scan();
  if (!result.accepted) return;
  spawnScanPulse();
  companionLine.textContent = 'ISKRA: проба дала новый факт. Теперь выбор можно обосновать наблюдением.';
  updateHud();
}

function chooseActive() {
  if (!activeStation) {
    companionLine.textContent = 'MUSCA: взаимодействовать можно только рядом со станцией.';
    return;
  }
  const result = game.choose(activeStation);
  if (!result.accepted) return;
  companionLine.textContent = result.outcome === 'opened'
    ? 'MUSCA: шлюз отвечает. Проход открыт.'
    : 'ISKRA: мир опроверг решение. Шлюз заблокирован.';
  updateHud();
  setTimeout(showResult, 550);
}
function resetPrototype() {
  game = new Gate3DState({ mode: 'light_chemical', layout: 'a' });
  camera.position.set(0, 1.68, 12);
  camera.rotation.set(0, 0, 0);
  activeStation = null;
  lastLogLength = 0;
  gateGlow.material.emissive.setHex(0x45d9ff);
  resultOverlay.classList.add('hidden');
  interaction.classList.add('hidden');
  companionLine.textContent = 'MUSCA: сигнал обнаружен впереди.';
  updateHud();
}

function toggleLog() {
  logPanel.classList.toggle('hidden');
}

addEventListener('keydown', (event) => {
  if (event.code === 'Tab') {
    event.preventDefault();
    toggleLog();
    return;
  }
  if (event.code === 'KeyR') {
    resetPrototype();
    return;
  }
  if (event.repeat) return;
  keys.add(event.code);
  if (event.code === 'KeyQ') scan();
  if (event.code === 'KeyE') chooseActive();
});
addEventListener('keyup', (event) => keys.delete(event.code));

controls.addEventListener('lock', () => {
  startOverlay.classList.add('hidden');
  pauseOverlay.classList.add('hidden');
});
controls.addEventListener('unlock', () => {
  keys.clear();
  if (game.playerView().outcome === 'pending' && startOverlay.classList.contains('hidden')) {
    pauseOverlay.classList.remove('hidden');
  }
});
document.querySelector('#start-button').addEventListener('click', () => controls.lock());
document.querySelector('#resume-button').addEventListener('click', () => controls.lock());
document.querySelector('#restart-button').addEventListener('click', () => {
  resetPrototype();
  controls.lock();
});
document.querySelector('#toggle-log').addEventListener('click', toggleLog);
document.querySelector('#close-log').addEventListener('click', toggleLog);

function animateEffects(dt, time) {
  const view = game.playerView();
  const openAmount = view.outcome === 'opened' ? 1 : 0;
  gateLeft.position.x += ((-1.13 - openAmount * 1.65) - gateLeft.position.x) * Math.min(1, dt * 4.5);
  gateRight.position.x += ((1.13 + openAmount * 1.65) - gateRight.position.x) * Math.min(1, dt * 4.5);
  if (view.outcome === 'sealed') gateGlow.material.emissive.setHex(0xff3d48);
  else if (view.outcome === 'opened') gateGlow.material.emissive.setHex(0x35f2a7);

  for (const [name, data] of stationMeshes.entries()) {
    const selected = activeStation === name;
    data.column.material.emissiveIntensity = (selected ? 2.2 : 1.1) + Math.sin(time * 0.003 + (name === 'amber' ? 0 : 1.5)) * 0.25;
  }

  if (scanPulse) {
    scanPulseAge += dt;
    scanPulse.scale.setScalar(1 + scanPulseAge * 4.6);
    scanPulse.material.opacity = Math.max(0, 0.9 - scanPulseAge * 0.9);
    if (scanPulseAge > 1) {
      scene.remove(scanPulse);
      scanPulse = null;
    }
  }
}
function updateCompanion(dt, time) {
  camera.getWorldDirection(forward);
  forward.y = 0;
  forward.normalize();
  right.crossVectors(forward, up).normalize();
  const target = camera.position.clone()
    .addScaledVector(right, -1.05)
    .addScaledVector(forward, -1.15);
  target.y = 1.35 + Math.sin(time * 0.0035) * 0.12;
  companion.position.lerp(target, Math.min(1, dt * 3.8));
  companion.lookAt(camera.position.x, companion.position.y, camera.position.z);
}

function loop(time) {
  const dt = Math.min(clock.getDelta(), 0.05);
  updateMovement(dt);
  updateSpatialState();
  updateHud();
  updateCompanion(dt, time);
  animateEffects(dt, time);
  renderer.render(scene, camera);
}
renderer.setAnimationLoop(loop);

addEventListener('resize', () => {
  camera.aspect = innerWidth / innerHeight;
  camera.updateProjectionMatrix();
  renderer.setSize(innerWidth, innerHeight);
});

updateHud();
window.__MUSCA3D__ = Object.freeze({
  version: '0.2.0-function-graybox',
  getPlayerState: () => structuredClone(game.playerView()),
  getTelemetry: () => ({
    position: [camera.position.x, camera.position.y, camera.position.z],
    signal: signalStrength(camera.position.z),
    activeStation,
    renderer: {
      drawCalls: renderer.info.render.calls,
      triangles: renderer.info.render.triangles,
    },
  }),
  reset: resetPrototype,
});

const qaView = new URLSearchParams(location.search).get('qa');
if (qaView) {
  const views = {
    spawn: [0, 1.68, 9.5],
    decision: [0, 1.68, -1.8],
    gate: [0, 1.68, -5.1],
  };
  const selected = views[qaView] || views.spawn;
  camera.position.set(...selected);
  camera.lookAt(0, 1.7, -12.5);
  startOverlay.classList.add('hidden');
  pauseOverlay.classList.add('hidden');
  companionLine.textContent = 'QA VIEW · interactive graybox uses the same runtime scene.';
}

if (qaView === 'gate') {
  camera.position.set(0, 1.68, -5.5);
  camera.lookAt(0, 1.7, -12.8);
}
if (qaView === 'amber') {
  camera.position.set(-2.15, 1.68, -5.85);
  camera.lookAt(-4.1, 1.7, -7.2);
}
const qaState = new URLSearchParams(location.search).get('state');
if (qaState === 'opened') {
  game.markReachedGate();
  game.observeStation('amber');
  game.observeStation('cobalt');
  game.scan();
  game.choose('cobalt');
  updateHud();
}
