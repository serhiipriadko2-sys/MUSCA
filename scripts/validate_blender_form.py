"""Static integrity gate for the checked-in Blender Form candidate.

This does not execute Blender. It verifies that checked-in binary/render artifacts
match the locally produced Blender receipts and that lifecycle boundaries remain honest.
"""
from __future__ import annotations
import hashlib, json
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]
FORM=ROOT/'blender'/'gate-lab-v0.2'
EVIDENCE=ROOT/'prototype3d'/'room-evidence'/'gate-lab-v0.2'

def sha256(path: Path) -> str:
    h=hashlib.sha256()
    with path.open('rb') as f:
        for chunk in iter(lambda:f.read(1024*1024),b''): h.update(chunk)
    return h.hexdigest()

def load(path: Path):
    return json.loads(path.read_text(encoding='utf-8'))

build=load(FORM/'receipts'/'form-build.json')
readback=load(FORM/'receipts'/'form-readback.json')
visual=load(FORM/'receipts'/'form-visual-qc.json')
reviews=load(EVIDENCE/'milestone-reviews.json')
errors=[]
for name,doc in [('build',build),('readback',readback)]:
    if doc.get('status')!='PASS': errors.append(f'{name} receipt not PASS')
if visual.get('status')!='PASS_AS_FORM_CANDIDATE': errors.append('visual receipt not candidate PASS')
def artifact_path(name: str) -> Path:
    if name.endswith('.blend'): return FORM/name
    if name.endswith('.fbx'): return FORM/'exports'/name
    if name.endswith('.png'): return FORM/'renders'/name
    raise ValueError(name)

for name,meta in build.get('artifacts',{}).items():
    p=artifact_path(name)
    if not p.exists():
        errors.append(f'missing artifact: {name}'); continue
    if p.stat().st_size != meta.get('bytes'): errors.append(f'byte mismatch: {name}')
    if sha256(p) != meta.get('sha256'): errors.append(f'hash mismatch: {name}')

for name,digest in visual.get('reviewed_renders',{}).items():
    p=artifact_path(name)
    if not p.exists() or sha256(p)!=digest: errors.append(f'visual QC hash mismatch: {name}')

blend=FORM/'GateLab_Form_v0.1.blend'
env=FORM/'exports'/'GateLab_Form_v0.1.fbx'
musca=FORM/'exports'/'MUSCA_FormProxy_v0.1.fbx'
if readback.get('blend_sha256') != sha256(blend): errors.append('readback blend hash mismatch')
if readback.get('environment_fbx_sha256') != sha256(env): errors.append('readback environment hash mismatch')
if readback.get('musca_fbx_sha256') != sha256(musca): errors.append('readback MUSCA hash mismatch')

function=reviews['reviews']['function']; form=reviews['reviews']['form']
if function.get('status')!='approved': errors.append('Function gate not approved')
if not function.get('human_approval'): errors.append('Function human approval receipt missing')
if form.get('status')!='ready_for_human_approval': errors.append('Form lifecycle is not ready_for_human_approval')
if form.get('human_approval') is not None: errors.append('Form human approval must remain null')
backups=list(FORM.rglob('*.blend[0-9]'))
if backups: errors.append('Blender backup files tracked/present: '+','.join(str(p) for p in backups))
result={'status':'PASS' if not errors else 'FAIL','errors':errors,'checked_artifacts':len(build.get('artifacts',{}))}
print(json.dumps(result,ensure_ascii=False,indent=2))
raise SystemExit(0 if not errors else 1)
