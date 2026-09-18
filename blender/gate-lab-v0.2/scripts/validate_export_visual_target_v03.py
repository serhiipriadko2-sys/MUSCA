import bpy, json, hashlib
from pathlib import Path
from mathutils import Vector

ROOT=Path(bpy.data.filepath).resolve().parent
EXPORTS=ROOT/'exports'/'v03'; EXPORTS.mkdir(parents=True,exist_ok=True)
RECEIPTS=ROOT/'receipts'; RECEIPTS.mkdir(parents=True,exist_ok=True)
REPO=ROOT.parents[1]
LAYOUT=REPO/'prototype3d'/'room-evidence'/'gate-lab-v0.2'/'room-layout.json'
layout=json.loads(LAYOUT.read_text(encoding='utf-8'))

def sha(path):
    h=hashlib.sha256()
    with open(path,'rb') as f:
        for chunk in iter(lambda:f.read(1024*1024),b''): h.update(chunk)
    return h.hexdigest()

def bounds(obj):
    ev=obj.evaluated_get(bpy.context.evaluated_depsgraph_get())
    pts=[ev.matrix_world@Vector(c) for c in ev.bound_box]
    return ([min(p[i] for p in pts) for i in range(3)],[max(p[i] for p in pts) for i in range(3)])

def overlap(obj,lo,hi):
    a,b=bounds(obj)
    return all(b[i]>lo[i]+0.001 and a[i]<hi[i]-0.001 for i in range(3))
def static_env_objects():
    names=['SHELL','GATE','STATIONS','ROUTE','PROPS','TEXT','SECTOR_B_PREVIEW','VISUAL_V03']
    seen=[]
    for cname in names:
        c=bpy.data.collections.get(cname)
        if not c: continue
        for o in c.objects:
            if o.type not in {'MESH','EMPTY'}: continue
            if o.get('v03_export_exclude'): continue
            if o.name.startswith(('M03_','MUSCA03_','P03_')): continue
            if o not in seen: seen.append(o)
    return seen

def select_objects(objects):
    bpy.ops.object.select_all(action='DESELECT')
    for o in objects:
        o.hide_set(False); o.select_set(True)
    if objects: bpy.context.view_layer.objects.active=objects[0]

checks={}
checks['room_id']=layout.get('room_id')=='gate-lab-v0.2'
checks['metre_units']=bpy.context.scene.unit_settings.system=='METRIC' and abs(bpy.context.scene.unit_settings.scale_length-1.0)<1e-6
left=bpy.data.objects['GateDoor_L']; right=bpy.data.objects['GateDoor_R']
gate=next(x for x in layout['objects'] if x['id']=='gate-a1')
checks['gate_leaf_dimensions']=all(all(abs(a-b)<0.002 for a,b in zip(o.dimensions,(2.25,0.34,3.5))) for o in (left,right))
checks['gate_leaf_positions']=abs(left.location.x+1.13)<0.002 and abs(right.location.x-1.13)<0.002 and abs(left.location.y+13.25)<0.002
checks['spawn_camera']=all(abs(a-b)<0.002 for a,b in zip(bpy.data.objects['CAM_FUNCTION_SPAWN'].location,(0,12,1.68)))
checks['amber_base']=all(abs(a-b)<0.002 for a,b in zip(bpy.data.objects['AmberStation_Base'].location,(-4.1,-7.2,0.22)))
checks['cobalt_base']=all(abs(a-b)<0.002 for a,b in zip(bpy.data.objects['CobaltStation_Base'].location,(4.1,-7.2,0.22)))
env_objs=static_env_objects()
route_lo=(-1.45,-11.6,0.08); route_hi=(1.45,12.0,1.80)
route_blockers=[o.name for o in env_objs if o.name not in {'Floor'} and overlap(o,route_lo,route_hi)]
# Floor and route markers intentionally occupy the floor plane below the player's clearance volume.
route_blockers=[n for n in route_blockers if not n.startswith(('Route_','V03_FloorEdge'))]
checks['static_route_clear']=not route_blockers

# Verify the actual runtime open offset: each leaf moves 2 m outward, and its parented visual decor must follow.
orig=(left.location.x,right.location.x)
left.location.x=orig[0]-2.0; right.location.x=orig[1]+2.0
bpy.context.view_layer.update()
open_lo=(-1.45,-13.62,0.08); open_hi=(1.45,-12.42,1.80)
open_blockers=[o.name for o in env_objs if overlap(o,open_lo,open_hi)]
open_blockers=[n for n in open_blockers if n not in {'Floor'} and not n.startswith(('Route_','V03_FloorEdge'))]
left.location.x,right.location.x=orig; bpy.context.view_layer.update()
checks['runtime_open_volume_clear']=not open_blockers

# Dynamic gate decoration must be parented to a moving leaf.
dyn=[o for o in bpy.data.objects if o.get('gate_dynamic_child')]
checks['dynamic_gate_decor_parented']=len(dyn)>=10 and all(o.parent in (left,right) for o in dyn)
checks['legacy_static_ring_excluded']=all(bpy.data.objects[n].get('v03_export_exclude') for n in ('GateRing','GateSeam'))

exports={}
env_fbx=EXPORTS/'GateLab_Form_v0.3.fbx'
select_objects(env_objs)
try:
    bpy.ops.export_scene.fbx(filepath=str(env_fbx),use_selection=True,object_types={'MESH','EMPTY'},use_mesh_modifiers=True,add_leaf_bones=False)
    exports['environment']='PASS'
except Exception as e: exports['environment']='FAIL: '+repr(e)
musca_fbx=EXPORTS/'MUSCA_FormProxy_v0.3.fbx'
musca_objs=[o for o in bpy.data.objects if o.name.startswith('M03_') and o.type in {'MESH','EMPTY'}]
select_objects(musca_objs)
try:
    bpy.ops.export_scene.fbx(filepath=str(musca_fbx),use_selection=True,object_types={'MESH','EMPTY'},use_mesh_modifiers=True,add_leaf_bones=False)
    exports['musca']='PASS'
except Exception as e: exports['musca']='FAIL: '+repr(e)

player_fbx=EXPORTS/'Researcher_FormProxy_v0.3.fbx'
player_objs=[o for o in bpy.data.objects if o.name.startswith('P03_') and o.type in {'MESH','EMPTY'}]
select_objects(player_objs)
try:
    bpy.ops.export_scene.fbx(filepath=str(player_fbx),use_selection=True,object_types={'MESH','EMPTY'},use_mesh_modifiers=True,add_leaf_bones=False)
    exports['researcher']='PASS'
except Exception as e: exports['researcher']='FAIL: '+repr(e)

artifacts={}
for p in (Path(bpy.data.filepath),env_fbx,musca_fbx,player_fbx):
    if p.exists(): artifacts[p.name]={'bytes':p.stat().st_size,'sha256':sha(p)}
checks['exports_exist']=len(artifacts)==4 and all(v=='PASS' for v in exports.values())
status='PASS' if all(checks.values()) else 'FAIL'
receipt={
    'schema':'musca.blender-form-v03-readback.v1','status':status,'blender_version':bpy.app.version_string,
    'source_function_layout_sha256':sha(LAYOUT),'checks':checks,'route_blockers':route_blockers,
    'open_gate_blockers':open_blockers,'dynamic_gate_children':[o.name for o in dyn],
    'exports':exports,'artifacts':artifacts,'objects_total':len(bpy.data.objects),
    'claim_boundary':'Validates Form v0.3 geometry/export against frozen Gate Lab Function only; not human art approval or Unity runtime integration.'
}
out=RECEIPTS/'form-v03-readback.json'; out.write_text(json.dumps(receipt,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print('FORM_V03_STATUS='+status); print('FORM_V03_RECEIPT='+str(out)); print('FORM_V03_ROUTE_BLOCKERS='+json.dumps(route_blockers)); print('FORM_V03_OPEN_BLOCKERS='+json.dumps(open_blockers))
if status!='PASS': raise RuntimeError('Form v0.3 validation failed: '+', '.join(k for k,v in checks.items() if not v))
