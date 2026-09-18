import bpy, json, hashlib
from pathlib import Path
from mathutils import Vector
SOURCE_ROOT=Path(__file__).resolve().parents[1]
ROOT=Path(bpy.data.filepath).resolve().parent
RECEIPTS=ROOT/'receipts'

def sha256(path):
    h=hashlib.sha256()
    with open(path,'rb') as f:
        for chunk in iter(lambda:f.read(1024*1024),b''): h.update(chunk)
    return h.hexdigest()

expected_collections=['SHELL','GATE','STATIONS','ROUTE','PROPS','MUSCA_PROXY','PLAYER_PROXY','LIGHTS','CAMERAS','TEXT','SECTOR_B_PREVIEW']
expected_objects=['Floor','GateDoor_L','GateDoor_R','GateFrame_L','GateFrame_R','AmberStation_Base','CobaltStation_Base','CAM_FUNCTION_SPAWN','CAM_FORM_HERO','MuscaBody','SectorBFloor']
checks={}
checks['collections_present']=all(n in bpy.data.collections for n in expected_collections)
checks['objects_present']=all(n in bpy.data.objects for n in expected_objects)
checks['amber_position']=tuple(round(v,3) for v in bpy.data.objects['AmberStation_Base'].location)==(-4.1,-7.2,0.22)
checks['cobalt_position']=tuple(round(v,3) for v in bpy.data.objects['CobaltStation_Base'].location)==(4.1,-7.2,0.22)
checks['gate_closed_width']=round(abs(bpy.data.objects['GateDoor_R'].location.x-bpy.data.objects['GateDoor_L'].location.x),2)==2.26
checks['spawn_camera_height']=round(bpy.data.objects['CAM_FUNCTION_SPAWN'].location.z,2)==1.68
blockers=[]
for cname in ('PROPS','STATIONS'):
    for o in bpy.data.collections[cname].objects:
        if o.type!='MESH': continue
        minx=o.location.x-o.dimensions.x/2; maxx=o.location.x+o.dimensions.x/2
        miny=o.location.y-o.dimensions.y/2; maxy=o.location.y+o.dimensions.y/2
        minz=o.location.z-o.dimensions.z/2; maxz=o.location.z+o.dimensions.z/2
        if maxx>-1.45 and minx<1.45 and maxy>-11.6 and miny<12 and minz<1.8 and maxz>0.05:
            blockers.append(o.name)
checks['route_clear']=not blockers
blend=ROOT/'GateLab_Form_v0.1.blend'
env=ROOT/'exports'/'GateLab_Form_v0.1.fbx'
musca=ROOT/'exports'/'MUSCA_FormProxy_v0.1.fbx'
checks['blend_exists']=blend.exists() and blend.stat().st_size>0
checks['environment_fbx_exists']=env.exists() and env.stat().st_size>0
checks['musca_fbx_exists']=musca.exists() and musca.stat().st_size>0

# Measure the reopened artifact, rather than just re-reading expected JSON values.
bpy.context.view_layer.update()
layout_path=SOURCE_ROOT.parents[1]/'prototype3d/room-evidence/gate-lab-v0.2/room-layout.json'
layout=json.loads(layout_path.read_text(encoding='utf-8'))
gate=next(o for o in layout['objects'] if o['id']=='gate-a1')
def close(actual, expected, tolerance=0.001):
    return len(actual)==len(expected) and all(abs(a-b)<=tolerance for a,b in zip(actual,expected))
def bounds(obj):
    evaluated=obj.evaluated_get(bpy.context.evaluated_depsgraph_get())
    points=[evaluated.matrix_world @ Vector(corner) for corner in evaluated.bound_box]
    return ([min(p[i] for p in points) for i in range(3)],
            [max(p[i] for p in points) for i in range(3)])
def blockers_in(lower,upper):
    found=[]
    for coll in ('SHELL','GATE','STATIONS','PROPS','MUSCA_PROXY','SECTOR_B_PREVIEW'):
        for obj in bpy.data.collections[coll].objects:
            if obj.type!='MESH' or obj.hide_render: continue
            lo,hi=bounds(obj)
            if all(hi[i]>lower[i]+0.001 and lo[i]<upper[i]-0.001 for i in range(3)):
                found.append(obj.name)
    return sorted(found)
left=bpy.data.objects['GateDoor_L']; right=bpy.data.objects['GateDoor_R']
checks['actual_gate_leaf_dimensions']=all(close(o.dimensions,(gate['size'][0]/2,gate['size'][2],gate['size'][1])) for o in (left,right))
checks['actual_gate_position']=all(close((o.location.y,o.location.z),(gate['position'][2],gate['position'][1])) for o in (left,right))
spawn=layout['spawn']['position']
checks['actual_spawn_position']=close(bpy.data.objects['CAM_FUNCTION_SPAWN'].location,(spawn[0],spawn[2],spawn[1]))
checks['canonical_camera']=bpy.context.scene.camera==bpy.data.objects['CAM_FUNCTION_SPAWN']
checks['player_hidden_in_spawn_render']=bpy.data.collections['PLAYER_PROXY'].hide_render
checks['metre_units']=bpy.context.scene.unit_settings.system=='METRIC' and abs(bpy.context.scene.unit_settings.scale_length-1)<1e-6
floor_lo,floor_hi=bounds(bpy.data.objects['Floor'])
containment=layout['containment']
checks['floor_covers_function_bounds']=close(floor_lo[:2],(containment['x_min'],containment['z_min'])) and close(floor_hi[:2],(containment['x_max'],containment['z_max']))
checks['sector_floor_connected']=bounds(bpy.data.objects['SectorBFloor'])[1][1]>=floor_lo[1]-0.001
checks['actual_gate_frame_opening']=abs(bounds(bpy.data.objects['GateFrame_R'])[0][0]-bounds(bpy.data.objects['GateFrame_L'])[1][0]-gate['size'][0])<0.001 and abs(bounds(bpy.data.objects['GateFrame_T'])[0][2]-gate['size'][1])<0.001
for prefix,identity in [('AmberStation','amber-station'),('CobaltStation','cobalt-station')]:
    station=next(o for o in layout['objects'] if o['id']==identity)
    centre=(station['position'][0],station['position'][2],station['size'][1]/2)
    size=(station['size'][0],station['size'][2],station['size'][1])
    station_objects=[o for o in bpy.data.objects if o.name.startswith(prefix) and o.type=='MESH']
    checks[prefix+'_inside_function_envelope']=all(all(lo[i]>=centre[i]-size[i]/2-0.001 and hi[i]<=centre[i]+size[i]/2+0.001 for i in range(3)) for lo,hi in map(bounds,station_objects))
actual_blockers=blockers_in((-1.45,-11.6,0.08),(1.45,12,1.8))
checks['all_geometry_route_clear']=not actual_blockers
original=(left.location.x,right.location.x)
ring=bpy.data.objects['GateRing']; seam=bpy.data.objects['GateSeam']
visible=(ring.hide_render,seam.hide_render)
try:
    left.location.x=-3.48; right.location.x=3.48
    ring.hide_render=True; seam.hide_render=True
    bpy.context.view_layer.update()
    opening_blockers=blockers_in((-2.25,-13.6,0.08),(2.25,-12.5,3.5))
    continuation_blockers=blockers_in((-1.45,-18,0.08),(1.45,-11.6,1.8))
finally:
    left.location.x,right.location.x=original
    ring.hide_render,seam.hide_render=visible
    bpy.context.view_layer.update()
checks['open_gate_volume_clear']=not opening_blockers
checks['sector_b_route_clear']=not continuation_blockers
receipt={
 'schema':'musca.blender-form-readback.v1',
 'status':'PASS' if all(checks.values()) else 'FAIL',
 'blender_version':bpy.app.version_string,
 'checks':checks,
 'route_blockers':blockers,
 'all_geometry_route_blockers':actual_blockers,
 'open_gate_blockers':opening_blockers,
 'sector_b_route_blockers':continuation_blockers,
 'source_function_layout_sha256':sha256(layout_path),
 'measurements':{'gate_leaf_dimensions':list(left.dimensions),'gate_leaf_position':list(left.location),'spawn':list(bpy.data.objects['CAM_FUNCTION_SPAWN'].location),'floor_bounds':[floor_lo,floor_hi]},
 'objects_total':len(bpy.data.objects),
 'blend_sha256':sha256(blend) if blend.exists() else None,
 'environment_fbx_sha256':sha256(env) if env.exists() else None,
 'musca_fbx_sha256':sha256(musca) if musca.exists() else None,
 'claim_boundary':'Read-back validates saved Form artifact structure only; no human Form approval or Unity integration claim.'
}
path=RECEIPTS/'form-readback.json'
path.write_text(json.dumps(receipt,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print('FORM_READBACK_STATUS='+receipt['status'])
print('FORM_READBACK='+str(path))
if receipt['status']!='PASS':
    raise RuntimeError('Form read-back failed: '+', '.join(k for k,v in checks.items() if not v))
