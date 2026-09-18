import bpy, math, json, hashlib, argparse, sys
from pathlib import Path
from mathutils import Vector

SOURCE_ROOT = Path(__file__).resolve().parents[1]
parser = argparse.ArgumentParser()
parser.add_argument('--output-dir', type=Path, required=True)
options = parser.parse_args(sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else [])
ROOT = options.output_dir.resolve()
ROOT.mkdir(parents=True, exist_ok=False)
EXPORTS = ROOT / 'exports'
RENDERS = ROOT / 'renders'
RECEIPTS = ROOT / 'receipts'
for p in (EXPORTS, RENDERS, RECEIPTS): p.mkdir(parents=True, exist_ok=True)

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for datablocks in (bpy.data.meshes, bpy.data.curves, bpy.data.materials, bpy.data.cameras, bpy.data.lights):
    pass

scene = bpy.context.scene
try:
    bpy.context.preferences.filepaths.save_version = 0
except Exception:
    pass
scene.unit_settings.system = 'METRIC'
scene.unit_settings.scale_length = 1.0
scene.render.engine = 'BLENDER_EEVEE'
scene.render.resolution_x = 1600
scene.render.resolution_y = 900
scene.render.resolution_percentage = 70
scene.render.image_settings.file_format = 'PNG'
scene.render.film_transparent = False
scene.world.color = (0.004, 0.008, 0.012)
COLLECTION_NAMES = ['SHELL','GATE','STATIONS','ROUTE','PROPS','MUSCA_PROXY','PLAYER_PROXY','LIGHTS','CAMERAS','TEXT','SECTOR_B_PREVIEW']
collections = {}
for name in COLLECTION_NAMES:
    c = bpy.data.collections.new(name)
    scene.collection.children.link(c)
    collections[name] = c

def move_to_collection(obj, name):
    for c in list(obj.users_collection): c.objects.unlink(obj)
    collections[name].objects.link(obj)
    return obj

def make_mat(name, base, metallic=0.0, rough=0.45, emission=None, strength=0.0):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (*base, 1.0)
    bsdf.inputs['Metallic'].default_value = metallic
    bsdf.inputs['Roughness'].default_value = rough
    if emission is not None:
        if 'Emission Color' in bsdf.inputs: bsdf.inputs['Emission Color'].default_value = (*emission, 1.0)
        elif 'Emission' in bsdf.inputs: bsdf.inputs['Emission'].default_value = (*emission, 1.0)
        if 'Emission Strength' in bsdf.inputs: bsdf.inputs['Emission Strength'].default_value = strength
    return m

MAT_DARK = make_mat('M_DarkMetal',(0.025,0.04,0.05),0.72,0.28)
MAT_MID = make_mat('M_Panel',(0.10,0.15,0.18),0.55,0.34)
MAT_FLOOR = make_mat('M_Floor',(0.055,0.075,0.09),0.45,0.42)
MAT_CYAN = make_mat('M_Cyan',(0.01,0.16,0.22),0.15,0.25,(0.02,0.75,1.0),8.0)
MAT_AMBER = make_mat('M_Amber',(0.22,0.065,0.005),0.18,0.24,(1.0,0.22,0.01),7.0)
MAT_COBALT = make_mat('M_Cobalt',(0.01,0.055,0.22),0.18,0.24,(0.02,0.28,1.0),7.0)
MAT_WHITE = make_mat('M_Ceramic',(0.46,0.52,0.54),0.25,0.25)
MAT_GREEN = make_mat('M_Plant',(0.025,0.14,0.07),0.0,0.75)
MAT_BLACK = make_mat('M_Black',(0.008,0.01,0.012),0.25,0.5)
MAT_ORANGE = make_mat('M_AccentOrange',(0.35,0.055,0.005),0.38,0.28,(1.0,0.12,0.01),2.5)

def add_box(name, loc, dims, mat, coll='SHELL', bevel=0.08):
    bpy.ops.mesh.primitive_cube_add(location=loc)
    o = bpy.context.object; o.name = name; o.dimensions = dims
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel:
        mod=o.modifiers.new('Bevel','BEVEL'); mod.width=bevel; mod.segments=3
    o.data.materials.append(mat); move_to_collection(o, coll); return o

def add_cyl(name, loc, radius, depth, mat, coll='PROPS', vertices=48):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=loc)
    o=bpy.context.object; o.name=name; o.data.materials.append(mat); move_to_collection(o,coll); return o

def add_uvsphere(name, loc, scale, mat, coll='PROPS'):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=40, ring_count=20, location=loc)
    o=bpy.context.object; o.name=name; o.scale=scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    o.data.materials.append(mat); move_to_collection(o,coll); return o

def add_text(name, body, loc, scale, mat, rot=(math.radians(90),0,math.radians(180))):
    bpy.ops.object.text_add(location=loc, rotation=rot)
    o=bpy.context.object; o.name=name; o.data.body=body; o.data.align_x='CENTER'; o.data.align_y='CENTER'
    o.data.size=scale; o.data.extrude=0.015; o.data.bevel_depth=0.004; o.data.materials.append(mat); move_to_collection(o,'TEXT'); return o
# Frozen Function envelope mapped as Function X/Z -> Blender X/Y, height -> Z.
ROOM_X = 7.15; SOUTH_Y = 13.4; NORTH_Y = -13.0; CEILING_Z = 4.6
add_box('Floor',(0,0.2,-0.12),(14.3,26.4,0.24),MAT_FLOOR,'SHELL',0.03)
add_box('Ceiling',(0,0.2,CEILING_Z),(14.3,26.4,0.22),MAT_DARK,'SHELL',0.03)
add_box('Wall_W',(-ROOM_X-0.13,0,2.25),(0.26,26.8,4.5),MAT_DARK,'SHELL',0.03)
add_box('Wall_E',( ROOM_X+0.13,0,2.25),(0.26,26.8,4.5),MAT_DARK,'SHELL',0.03)
# North gate wall leaves exact 4.5 x 3.5 m opening.
side_w=(14.3-4.5)/2
add_box('NorthWall_L',(-(4.5/2+side_w/2),NORTH_Y-0.13,2.25),(side_w,0.26,4.5),MAT_DARK,'SHELL')
add_box('NorthWall_R',((4.5/2+side_w/2),NORTH_Y-0.13,2.25),(side_w,0.26,4.5),MAT_DARK,'SHELL')
add_box('NorthWall_Top',(0,NORTH_Y-0.13,4.0),(4.5,0.26,1.0),MAT_DARK,'SHELL')
# South arrival wall leaves exact 4.0 x 3.2 m opening.
side_s=(14.3-4.0)/2
add_box('SouthWall_L',(-(2.0+side_s/2),SOUTH_Y+0.13,2.25),(side_s,0.26,4.5),MAT_DARK,'SHELL')
add_box('SouthWall_R',((2.0+side_s/2),SOUTH_Y+0.13,2.25),(side_s,0.26,4.5),MAT_DARK,'SHELL')
add_box('SouthWall_Top',(0,SOUTH_Y+0.13,3.85),(4.0,0.26,1.3),MAT_DARK,'SHELL')
# Floor panel rhythm and ceiling ribs reinforce the approved axial route.
for y in [11,8,5,2,-1,-4,-7,-10]:
    add_box(f'FloorPanel_{y:+}',(0,y,0.015),(13.1,2.35,0.035),MAT_MID,'SHELL',0.015)
    add_box(f'CeilingRib_{y:+}',(0,y,4.28),(13.7,0.30,0.34),MAT_MID,'SHELL',0.05)
    add_box(f'CeilingCyan_{y:+}',(0,y-0.08,4.08),(4.4,0.08,0.035),MAT_CYAN,'SHELL',0.01)
# Side-wall language: recessed modules, cyan service strips, sparse biophilic pockets.
for side in (-1,1):
    x=side*6.86
    for idx,y in enumerate([10.2,6.6,3.0,-0.6,-4.2,-9.8]):
        add_box(f'WallPanel_{side}_{idx}',(x,y,2.15),(0.18,2.65,3.55),MAT_MID,'SHELL',0.05)
        add_box(f'WallLight_{side}_{idx}',(side*6.72,y+0.85,2.7),(0.08,0.12,1.65),MAT_CYAN,'SHELL',0.015)
        add_box(f'WallTrim_{side}_{idx}',(side*6.66,y-0.95,1.05),(0.10,0.44,1.25),MAT_ORANGE,'SHELL',0.02)

# Railings at the decision bay echo the reference without touching the axial route.
for side in (-1,1):
    x=side*5.5
    for y in [-1.5,0.0,1.5,3.0]:
        add_cyl(f'RailPost_{side}_{y}',(x,y,0.55),0.045,1.1,MAT_MID,'PROPS',24)
    for y in [-0.75,0.75,2.25]:
        bar=add_box(f'RailBar_{side}_{y}',(x,y,0.92),(0.08,1.5,0.08),MAT_MID,'PROPS',0.02)

# Planters: deliberately side-loaded; no route blockage.
for side,y in [(-1,4.2),(1,4.2),(-1,-2.6),(1,-2.6)]:
    x=side*5.75
    add_box(f'Planter_{side}_{y}',(x,y,0.34),(1.55,1.2,0.68),MAT_DARK,'PROPS',0.08)
    for j,dx in enumerate([-0.42,0,0.42]):
        stem=add_cyl(f'PlantStem_{side}_{y}_{j}',(x+dx,y,0.9),0.05,1.1,MAT_GREEN,'PROPS',16)
        for k,z in enumerate([1.1,1.35,1.58]):
            leaf=add_uvsphere(f'Leaf_{side}_{y}_{j}_{k}',(x+dx+0.1*side,y+0.08*(k-1),z),(0.18,0.32,0.08),MAT_GREEN,'PROPS')
            leaf.rotation_euler[1]=math.radians(20*side)
# Hero gate A-1: exact opening retained, Form adds frame, signage and readable center motif.
add_box('GateFrame_L',(-2.42,-12.86,1.86),(0.34,0.42,3.72),MAT_MID,'GATE',0.06)
add_box('GateFrame_R',( 2.42,-12.86,1.86),(0.34,0.42,3.72),MAT_MID,'GATE',0.06)
add_box('GateFrame_T',(0,-12.86,3.66),(5.18,0.42,0.32),MAT_MID,'GATE',0.06)
# Match the Unity Function leaves, including its 0.01 m centre seam.
add_box('GateDoor_L',(-1.13,-13.25,1.76),(2.25,0.34,3.5),MAT_DARK,'GATE',0.04)
add_box('GateDoor_R',( 1.13,-13.25,1.76),(2.25,0.34,3.5),MAT_DARK,'GATE',0.04)
add_box('GateSeam',(0,-12.74,1.75),(0.08,0.06,3.1),MAT_CYAN,'GATE',0.01)
add_box('GateBeacon',(0,-12.70,3.88),(0.16,0.14,0.12),MAT_CYAN,'GATE',0.02)
# Circular visual anchor.
bpy.ops.mesh.primitive_torus_add(major_radius=0.72, minor_radius=0.055, major_segments=64, minor_segments=12, location=(0,-12.72,1.75), rotation=(math.radians(90),0,0))
ring=bpy.context.object; ring.name='GateRing'; ring.data.materials.append(MAT_CYAN); move_to_collection(ring,'GATE')
add_text('GateLabel','GATE A-1',(0,-12.62,4.18),0.34,MAT_CYAN,rot=(math.radians(90),0,math.radians(180)))

# Sector B preview exists only beyond the approved north opening; it does not alter Function circulation.
add_box('SectorBFloor',(0,-16.45,-0.10),(4.68,6.9,0.20),MAT_FLOOR,'SECTOR_B_PREVIEW',0.03)
add_box('SectorBCeiling',(0,-16.45,3.85),(4.68,6.7,0.18),MAT_DARK,'SECTOR_B_PREVIEW',0.03)
add_box('SectorBWallL',(-2.34,-16.45,1.9),(0.18,6.7,3.8),MAT_MID,'SECTOR_B_PREVIEW',0.03)
add_box('SectorBWallR',(2.34,-16.45,1.9),(0.18,6.7,3.8),MAT_MID,'SECTOR_B_PREVIEW',0.03)
add_box('SectorBPortal',(0,-19.72,1.9),(4.25,0.20,3.8),MAT_DARK,'SECTOR_B_PREVIEW',0.04)
for iy,y in enumerate([-14.0,-15.5,-17.0,-18.5]):
    add_box(f'SectorBLight_{iy}',(0,y,3.68),(2.8,0.10,0.06),MAT_CYAN,'SECTOR_B_PREVIEW',0.01)
    add_box(f'SectorBRoute_{iy}',(0,y,0.025),(0.8,0.10,0.03),MAT_CYAN,'SECTOR_B_PREVIEW',0.01)
add_text('SectorBBackLabel','SECTOR B',(0,-19.58,2.35),0.28,MAT_CYAN,rot=(math.radians(90),0,math.radians(180)))

# Central signal marker preserves spawn -> gate readability.
add_box('SignalBeaconBody',(0,-11.2,3.3),(0.16,0.16,1.0),MAT_CYAN,'GATE',0.02)
for i,z in enumerate([2.82,3.25,3.68]):
    add_box(f'BeaconWing_{i}',(0,-11.2,z),(1.1-0.2*i,0.10,0.045),MAT_CYAN,'GATE',0.01)
def build_station(prefix, x, mat, label):
    y=-7.2
    add_box(prefix+'_Base',(x,y,0.22),(1.95,1.38,0.44),MAT_DARK,'STATIONS',0.08)
    add_box(prefix+'_Plinth',(x,y,0.55),(1.55,1.02,0.26),MAT_MID,'STATIONS',0.05)
    add_cyl(prefix+'_Core',(x,y,1.58),0.43,1.78,mat,'STATIONS',48)
    add_cyl(prefix+'_CapBottom',(x,y,0.76),0.54,0.18,MAT_MID,'STATIONS',48)
    add_cyl(prefix+'_CapTop',(x,y,2.48),0.54,0.18,MAT_MID,'STATIONS',48)
    for a in (0,90,180,270):
        r=0.54; rad=math.radians(a)
        add_cyl(prefix+f'_Cage{a}',(x+r*math.cos(rad),y+r*math.sin(rad),1.61),0.035,1.63,MAT_MID,'STATIONS',16)
    add_box(prefix+'_Header',(x,y,2.88),(1.72,0.82,0.34),MAT_DARK,'STATIONS',0.05)
    add_box(prefix+'_HeaderGlow',(x,y+0.43,2.88),(1.42,0.05,0.11),mat,'STATIONS',0.01)
    add_box(prefix+'_Console',(x,y+0.68,1.02),(1.08,0.18,0.56),MAT_DARK,'STATIONS',0.04)
    add_box(prefix+'_ConsoleGlow',(x,y+0.77,1.06),(0.82,0.01,0.30),MAT_CYAN,'STATIONS',0.005)
    add_text(prefix+'_Label',label,(x,y+0.48,3.11),0.25,mat,rot=(math.radians(90),0,math.radians(180)))
    # Slim side pylons keep the total footprint within the Function station volume.
    for sx in (-0.80,0.80):
        add_box(prefix+f'_Pylon{sx}',(x+sx,y,1.55),(0.14,0.96,2.35),MAT_MID,'STATIONS',0.04)

build_station('AmberStation',-4.1,MAT_AMBER,'AMBER')
build_station('CobaltStation',4.1,MAT_COBALT,'COBALT')
# Route markers: thin emissive chevrons; collision-neutral and centered on approved axis.
route_y=[11.5,10.2,8.9,7.6,6.3,5.0,3.7,2.4,1.1,-0.2,-1.5,-2.8,-4.1,-5.4,-6.3,-8.2,-9.4,-10.6,-11.5]
for i,y in enumerate(route_y):
    for side,ang in [(-1,-28),(1,28)]:
        bar=add_box(f'Route_{i}_{side}',(side*0.18,y,0.035),(0.46,0.10,0.035),MAT_CYAN,'ROUTE',0.01)
        bar.rotation_euler[2]=math.radians(ang)

# Functional labels remain spatially anchored but secondary to geometry.
add_text('ArrivalLabel','SECTOR A',(0,13.05,3.7),0.30,MAT_CYAN,rot=(math.radians(90),0,math.radians(180)))

# Player proxy for Form composition only; not a gameplay character asset.
add_uvsphere('PlayerHead',(0,5.8,2.05),(0.22,0.20,0.26),MAT_WHITE,'PLAYER_PROXY')
add_box('PlayerTorso',(0,5.8,1.35),(0.58,0.34,0.95),MAT_DARK,'PLAYER_PROXY',0.12)
add_box('PlayerBackpack',(0,5.98,1.42),(0.46,0.28,0.68),MAT_MID,'PLAYER_PROXY',0.08)
for sx in (-0.18,0.18):
    add_cyl(f'PlayerLeg_{sx}',(sx,5.8,0.55),0.095,0.9,MAT_DARK,'PLAYER_PROXY',24)
    add_cyl(f'PlayerArm_{sx}',(sx*1.65,5.8,1.28),0.07,0.8,MAT_DARK,'PLAYER_PROXY',20)
add_box('PlayerPackGlow',(0,6.14,1.5),(0.16,0.04,0.22),MAT_CYAN,'PLAYER_PROXY',0.02)
# MUSCA Form proxy: readable companion silhouette, not a final biological/mechanical claim.
MAT_WING = make_mat('M_Wing',(0.06,0.18,0.24),0.15,0.18,(0.08,0.55,0.9),2.2)
mx,my,mz=2.05,6.20,1.62
add_uvsphere('MuscaBody',(mx,my,mz),(0.24,0.30,0.20),MAT_WHITE,'MUSCA_PROXY')
add_uvsphere('MuscaCore',(mx,my-0.34,mz),(0.15,0.09,0.15),MAT_DARK,'MUSCA_PROXY')
add_uvsphere('MuscaEye',(mx,my-0.46,mz),(0.10,0.04,0.10),MAT_CYAN,'MUSCA_PROXY')
for sx,sy,rz in [(-0.28,0.02,28),(0.28,0.02,-28),(-0.24,0.14,48),(0.24,0.14,-48)]:
    wing=add_uvsphere(f'MuscaWing_{sx}_{sy}',(mx+sx,my+sy,mz+0.08),(0.22,0.10,0.02),MAT_WING,'MUSCA_PROXY')
    wing.rotation_euler[2]=math.radians(rz)
for sx in (-0.16,0.16):
    for dy in (-0.10,0.12):
        leg=add_cyl(f'MuscaLeg_{sx}_{dy}',(mx+sx,my+dy,mz-0.28),0.014,0.24,MAT_MID,'MUSCA_PROXY',12)
        leg.rotation_euler[1]=math.radians(28*sx/abs(sx))

# Lighting helpers.
def add_area(name, loc, energy, size, color=(1,1,1)):
    data=bpy.data.lights.new(name,'AREA'); data.energy=energy; data.shape='RECTANGLE'; data.size=size; data.color=color
    o=bpy.data.objects.new(name,data); o.location=loc; collections['LIGHTS'].objects.link(o); return o

def add_point(name, loc, energy, color):
    data=bpy.data.lights.new(name,'POINT'); data.energy=energy; data.color=color; data.shadow_soft_size=1.2
    o=bpy.data.objects.new(name,data); o.location=loc; collections['LIGHTS'].objects.link(o); return o

try:
    scene.view_settings.look = 'AgX - Medium High Contrast'
except Exception:
    pass
scene.view_settings.exposure = 0.4
for i,y in enumerate([10.0,6.0,2.0,-2.0,-6.0,-10.0]):
    a=add_area(f'CeilingArea_{i}',(0,y,4.16),520,4.2,(0.68,0.86,1.0))
    a.rotation_euler=(0,0,0)
add_point('GateCyan',(0,-11.65,2.25),820,(0.05,0.65,1.0))
add_point('AmberGlow',(-4.1,-6.85,1.7),420,(1.0,0.22,0.02))
add_point('CobaltGlow',(4.1,-6.85,1.7),420,(0.04,0.28,1.0))
add_area('WarmFill',(-5.8,1.2,3.0),260,3.0,(1.0,0.45,0.18)).rotation_euler=(math.radians(70),0,math.radians(-12))
add_area('CoolFill',(5.8,1.2,3.0),260,3.0,(0.18,0.55,1.0)).rotation_euler=(math.radians(70),0,math.radians(12))

def look_at(obj, target):
    direction = Vector(target) - obj.location
    obj.rotation_euler = direction.to_track_quat('-Z','Y').to_euler()

def add_camera(name, loc, target, lens=34):
    data=bpy.data.cameras.new(name); data.lens=lens; data.sensor_width=36
    o=bpy.data.objects.new(name,data); o.location=loc; collections['CAMERAS'].objects.link(o); look_at(o,target); return o

cam_spawn=add_camera('CAM_FUNCTION_SPAWN',(0,12.0,1.68),(0,-11.5,1.75),30)
cam_hero=add_camera('CAM_FORM_HERO',(2.7,10.8,2.75),(0,-7.8,1.55),34)
cam_station=add_camera('CAM_FORM_STATION',(-0.4,-1.2,2.0),(-4.1,-7.2,1.55),42)
cam_gate=add_camera('CAM_FORM_GATE',(0,-2.0,1.8),(0,-15.0,1.85),32)
# Convert signage to meshes so engine export does not depend on Blender font objects.
for obj in list(collections['TEXT'].objects):
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True); bpy.context.view_layer.objects.active=obj
    bpy.ops.object.convert(target='MESH')

BLEND = ROOT / 'GateLab_Form_v0.1.blend'

def set_collection_render(name, visible):
    collections[name].hide_render = not visible

def render(camera, filename, player=True):
    set_collection_render('PLAYER_PROXY', player)
    scene.camera=camera
    scene.render.filepath=str(RENDERS/filename)
    bpy.ops.render.render(write_still=True)

render(cam_spawn,'form_spawn.png',player=False)
render(cam_hero,'form_hero.png',player=True)
render(cam_station,'form_amber.png',player=False)
# World-response preview uses the same door leaves, temporarily translated outward.
left=bpy.data.objects['GateDoor_L']; right=bpy.data.objects['GateDoor_R']
lx,rx=left.location.x,right.location.x
ring=bpy.data.objects['GateRing']; seam=bpy.data.objects['GateSeam']
left.location.x=-3.48; right.location.x=3.48
ring.hide_render=True; seam.hide_render=True
render(cam_gate,'form_gate_open.png',player=False)
left.location.x=lx; right.location.x=rx
ring.hide_render=False; seam.hide_render=False
set_collection_render('PLAYER_PROXY', False)
scene.camera = cam_spawn
scene.render.filepath = str(RENDERS / 'form_spawn.png')
bpy.context.view_layer.update()
bpy.ops.wm.save_as_mainfile(filepath=str(BLEND))
def select_collections(names):
    bpy.ops.object.select_all(action='DESELECT')
    selected=[]
    for name in names:
        for obj in collections[name].objects:
            if obj.type in {'MESH','EMPTY'}:
                obj.hide_set(False); obj.select_set(True); selected.append(obj)
    if selected: bpy.context.view_layer.objects.active=selected[0]
    return selected

exports={}
env_fbx=EXPORTS/'GateLab_Form_v0.1.fbx'
try:
    select_collections(['SHELL','GATE','STATIONS','ROUTE','PROPS','TEXT','SECTOR_B_PREVIEW'])
    bpy.ops.export_scene.fbx(filepath=str(env_fbx),use_selection=True,object_types={'MESH','EMPTY'},use_mesh_modifiers=True,add_leaf_bones=False)
    exports['environment_fbx']='PASS'
except Exception as exc:
    exports['environment_fbx']='FAIL: '+repr(exc)

musca_fbx=EXPORTS/'MUSCA_FormProxy_v0.1.fbx'
try:
    select_collections(['MUSCA_PROXY'])
    bpy.ops.export_scene.fbx(filepath=str(musca_fbx),use_selection=True,object_types={'MESH','EMPTY'},use_mesh_modifiers=True,add_leaf_bones=False)
    exports['musca_fbx']='PASS'
except Exception as exc:
    exports['musca_fbx']='FAIL: '+repr(exc)
repo=SOURCE_ROOT.parents[1]
layout_path=repo/'prototype3d'/'room-evidence'/'gate-lab-v0.2'/'room-layout.json'
layout=json.loads(layout_path.read_text(encoding='utf-8'))
checks={
 'room_id': layout.get('room_id')=='gate-lab-v0.2',
 'gate_size': next(o for o in layout['objects'] if o['id']=='gate-a1')['size']==[4.5,3.5,0.34],
 'amber_position': next(o for o in layout['objects'] if o['id']=='amber-station')['position']==[-4.1,0,-7.2],
 'cobalt_position': next(o for o in layout['objects'] if o['id']=='cobalt-station')['position']==[4.1,0,-7.2],
 'spawn': layout['spawn']['position']==[0,1.68,12],
}
blockers=[]
for cname in ('PROPS','STATIONS'):
    for o in collections[cname].objects:
        if o.type!='MESH': continue
        minx=o.location.x-o.dimensions.x/2; maxx=o.location.x+o.dimensions.x/2
        miny=o.location.y-o.dimensions.y/2; maxy=o.location.y+o.dimensions.y/2
        minz=o.location.z-o.dimensions.z/2; maxz=o.location.z+o.dimensions.z/2
        if maxx>-1.45 and minx<1.45 and maxy>-11.6 and miny<12 and minz<1.8 and maxz>0.05:
            blockers.append(o.name)
checks['route_clear']=len(blockers)==0

def sha256(path):
    h=hashlib.sha256()
    with open(path,'rb') as f:
        for chunk in iter(lambda:f.read(1024*1024),b''): h.update(chunk)
    return h.hexdigest()
artifact_paths=[BLEND,env_fbx,musca_fbx,RENDERS/'form_spawn.png',RENDERS/'form_hero.png',RENDERS/'form_amber.png',RENDERS/'form_gate_open.png']
artifacts={}
for p in artifact_paths:
    if p.exists(): artifacts[p.name]={'bytes':p.stat().st_size,'sha256':sha256(p)}

receipt={
 'schema':'musca.blender-form-build.v1',
 'room_id':'gate-lab-v0.2',
 'status':'PASS' if all(checks.values()) and all(v=='PASS' for v in exports.values()) else 'FAIL',
 'blender_version':bpy.app.version_string,
 'source_function_layout_sha256':sha256(layout_path),
 'checks':checks,
 'route_blockers':blockers,
 'exports':exports,
 'collections':{n:len(c.objects) for n,c in collections.items()},
 'objects_total':len(bpy.data.objects),
 'meshes_total':len(bpy.data.meshes),
 'materials_total':len(bpy.data.materials),
 'artifacts':artifacts,
 'claim_boundary':'Form authoring artifact only; no Form human approval, Unity import approval, playtest evidence, or release claim.'
}
receipt_path=RECEIPTS/'form-build.json'
receipt_path.write_text(json.dumps(receipt,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print('FORM_BUILD_STATUS='+receipt['status'])
print('FORM_RECEIPT='+str(receipt_path))
print('FORM_OBJECTS='+str(receipt['objects_total']))
print('FORM_BLOCKERS='+json.dumps(blockers))
print('FORM_EXPORTS='+json.dumps(exports))
if receipt['status'] != 'PASS' or len(artifacts) != len(artifact_paths):
    raise RuntimeError('Form build postconditions failed; inspect the receipt')
