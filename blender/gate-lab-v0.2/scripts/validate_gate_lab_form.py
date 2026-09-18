import bpy, json, hashlib
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
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
receipt={
 'schema':'musca.blender-form-readback.v1',
 'status':'PASS' if all(checks.values()) else 'FAIL',
 'blender_version':bpy.app.version_string,
 'checks':checks,
 'route_blockers':blockers,
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
