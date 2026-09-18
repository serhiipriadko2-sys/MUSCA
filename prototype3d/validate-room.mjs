import { readFile } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const ROOT = path.dirname(fileURLToPath(import.meta.url));
const ROOM = path.join(ROOT, 'room-evidence', 'gate-lab-v0.2');

async function json(name) {
  return JSON.parse(await readFile(path.join(ROOM, name), 'utf8'));
}

function check(condition, message, findings) {
  if (!condition) findings.push(message);
}

const [brief, layout, openings, props, reviews] = await Promise.all([
  json('room-brief.json'),
  json('room-layout.json'),
  json('openings.json'),
  json('props.json'),
  json('milestone-reviews.json'),
]);

const findings = [];
const id = 'gate-lab-v0.2';
for (const [name, doc] of Object.entries({ brief, layout, openings, props, reviews })) {
  check(doc.room_id === id, `${name}: room_id mismatch`, findings);
}
check(brief.purpose && brief.runtime_surface, 'brief: purpose/runtime missing', findings);
check(brief.primary_arrival.clear_width_m >= 2.4, 'brief: arrival width below 2.4m', findings);
check(brief.paid_service_budget.credits === 0, 'brief: unexpected paid budget', findings);
const objectIds = layout.objects.map((object) => object.id);
check(new Set(objectIds).size === objectIds.length, 'layout: duplicate object id', findings);
check(objectIds.includes(brief.hero_object), 'layout: hero object missing', findings);
check(layout.route.waypoints.length >= 4, 'layout: route too short', findings);
check(layout.symmetry === undefined || brief.symmetry.mode === 'bilateral', 'layout: symmetry conflict', findings);

const amber = layout.objects.find((object) => object.id === 'amber-station');
const cobalt = layout.objects.find((object) => object.id === 'cobalt-station');
check(Boolean(amber && cobalt), 'layout: reagent station missing', findings);
if (amber && cobalt) {
  check(Math.abs(amber.position[0] + cobalt.position[0]) < 1e-9, 'layout: reagent x positions not mirrored', findings);
  check(amber.position[2] === cobalt.position[2], 'layout: reagent z positions not aligned', findings);
}

const arrival = openings.openings.find((opening) => opening.id === 'arrival-hall');
const gate = openings.openings.find((opening) => opening.id === 'gate-a1-opening');
check(Boolean(arrival && gate), 'openings: required opening missing', findings);
check(arrival?.width_m >= 2.4, 'openings: arrival aperture too narrow', findings);
check(gate?.destination, 'openings: gate destination missing', findings);
check(props.paid_generation === false, 'props: paid generation must be false at Function gate', findings);
check(reviews.reviews.function.human_approval === null, 'reviews: Function approval must remain pending', findings);
check(reviews.reviews.form.status === 'blocked_pending_function_approval', 'reviews: Form gate must remain blocked', findings);
const report = {
  status: findings.length ? 'FAIL' : 'PASS',
  gate: 'function',
  function_status: reviews.reviews.function.status,
  human_approval: reviews.reviews.function.human_approval,
  findings,
};

console.log(JSON.stringify(report, null, 2));
if (findings.length) process.exit(1);
