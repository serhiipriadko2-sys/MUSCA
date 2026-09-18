import bpy, math
from pathlib import Path
from mathutils import Vector

BASE = Path(r"C:\github\MUSCA\blender\gate-lab-v0.2")
OUT = BASE / "GateLab_Form_v0.3.blend"
RENDERS = BASE / "renders" / "v03"
RENDERS.mkdir(parents=True, exist_ok=True)
scene = bpy.context.scene

for name in ("VISUAL_V03", "MUSCA_V03", "LIGHTS_V03", "CAMERAS_V03"):
    old = bpy.data.collections.get(name)
    if old:
        for obj in list(old.objects):
            bpy.data.objects.remove(obj, do_unlink=True)
        bpy.data.collections.remove(old)

cols = {}
for name in ("VISUAL_V03", "MUSCA_V03", "LIGHTS_V03", "CAMERAS_V03"):
    c = bpy.data.collections.new(name)
    scene.collection.children.link(c)
    cols[name] = c

def link(obj, coll="VISUAL_V03"):
    for c in list(obj.users_collection): c.objects.unlink(obj)
    cols[coll].objects.link(obj)
    return obj
def mat(name, base, metallic=0.0, rough=0.4, emission=None, strength=0.0, transmission=0.0):
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = (*base, 1.0)
    bsdf.inputs["Metallic"].default_value = metallic
    bsdf.inputs["Roughness"].default_value = rough
    for key in ("Transmission Weight", "Transmission"):
        if key in bsdf.inputs: bsdf.inputs[key].default_value = transmission
    if emission is not None:
        for key in ("Emission Color", "Emission"):
            if key in bsdf.inputs: bsdf.inputs[key].default_value = (*emission, 1.0)
        if "Emission Strength" in bsdf.inputs: bsdf.inputs["Emission Strength"].default_value = strength
    return m

M_DARK = mat("V03_DarkMetal", (0.018,0.028,0.034), 0.82, 0.24)
M_PANEL = mat("V03_Panel", (0.075,0.105,0.12), 0.62, 0.30)
M_CERAMIC = mat("V03_Ceramic", (0.52,0.56,0.56), 0.18, 0.28)
M_CYAN = mat("V03_Cyan", (0.01,0.12,0.18), 0.22, 0.22, (0.02,0.65,1.0), 4.0)
M_CYAN_SOFT = mat("V03_CyanSoft", (0.02,0.10,0.13), 0.12, 0.28, (0.05,0.42,0.62), 1.8)
M_WHITE_LIGHT = mat("V03_WhiteLight", (0.65,0.69,0.67), 0.05, 0.22, (0.82,0.92,0.95), 5.5)
M_AMBER = mat("V03_Amber", (0.24,0.065,0.004), 0.18, 0.22, (1.0,0.22,0.015), 5.0)
M_COBALT = mat("V03_Cobalt", (0.008,0.035,0.19), 0.20, 0.22, (0.02,0.24,1.0), 5.0)
M_GLASS = mat("V03_Glass", (0.04,0.09,0.12), 0.05, 0.08, transmission=0.78)
def box(name, loc, dims, material, bevel=0.04, coll="VISUAL_V03"):
    bpy.ops.mesh.primitive_cube_add(location=loc)
    o = bpy.context.object; o.name = name; o.dimensions = dims
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel:
        b = o.modifiers.new("Bevel", "BEVEL"); b.width = bevel; b.segments = 3
    o.data.materials.append(material); return link(o, coll)

def cyl(name, loc, radius, depth, material, vertices=48, coll="VISUAL_V03"):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=loc)
    o = bpy.context.object; o.name = name; o.data.materials.append(material); return link(o, coll)

def sphere(name, loc, scale, material, coll="MUSCA_V03"):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=32, ring_count=16, location=loc)
    o=bpy.context.object; o.name=name; o.scale=scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    o.data.materials.append(material); return link(o, coll)

def torus(name, loc, major, minor, material, rot=(math.radians(90),0,0)):
    bpy.ops.mesh.primitive_torus_add(major_radius=major, minor_radius=minor, major_segments=64,
                                    minor_segments=12, location=loc, rotation=rot)
    o=bpy.context.object; o.name=name; o.data.materials.append(material); return link(o)

def replace_material(obj, material):
    if obj.data and hasattr(obj.data, "materials"):
        obj.data.materials.clear(); obj.data.materials.append(material)

def add_area(name, loc, energy, size, color):
    d=bpy.data.lights.new(name,"AREA"); d.energy=energy; d.shape="RECTANGLE"; d.size=size; d.color=color
    o=bpy.data.objects.new(name,d); o.location=loc; cols["LIGHTS_V03"].objects.link(o); return o
# Base scene retune: neutral lab first, cyan only for navigation and active technology.
for obj in bpy.data.objects:
    n = obj.name
    if n.startswith("WallPanel_") or n.startswith("CeilingRib_"):
        replace_material(obj, M_PANEL)
    elif n in {"Wall_W","Wall_E","NorthWall_L","NorthWall_R","NorthWall_Top","SouthWall_L","SouthWall_R","SouthWall_Top"}:
        replace_material(obj, M_DARK)
    elif n.startswith("CeilingCyan_"):
        replace_material(obj, M_WHITE_LIGHT)
    elif n.startswith("Route_") or n in {"GateSeam","GateBeacon","SignalBeaconBody"} or n.startswith("BeaconWing_"):
        replace_material(obj, M_CYAN)

# Reduce the inherited orange wall glow; keep amber for the actual reagent station.
old_orange = bpy.data.materials.get("M_AccentOrange")
if old_orange and old_orange.use_nodes:
    p = old_orange.node_tree.nodes.get("Principled BSDF")
    if p:
        p.inputs["Base Color"].default_value = (0.11,0.025,0.004,1)
        if "Emission Strength" in p.inputs: p.inputs["Emission Strength"].default_value = 0.45

# Architectural framing: recess lines and restrained luminous floor edges.
for side in (-1,1):
    x = side*6.60
    for i,y in enumerate([10.2,6.6,3.0,-0.6,-4.2,-9.8]):
        box(f"V03_WallFrame_{side}_{i}_A", (x,y-1.31,2.15), (0.07,0.06,3.25), M_DARK, 0.015)
        box(f"V03_WallFrame_{side}_{i}_B", (x,y+1.31,2.15), (0.07,0.06,3.25), M_DARK, 0.015)
        box(f"V03_WallService_{side}_{i}", (side*6.50,y,0.72), (0.05,1.75,0.055), M_CYAN_SOFT, 0.01)
for y in [11,8,5,2,-1,-4,-7,-10]:
    box(f"V03_FloorEdgeL_{y}", (-2.35,y,0.045), (0.035,2.05,0.025), M_CYAN_SOFT, 0.006)
    box(f"V03_FloorEdgeR_{y}", ( 2.35,y,0.045), (0.035,2.05,0.025), M_CYAN_SOFT, 0.006)
# Gate A-1: industrial circular language on top of the frozen 4.5 x 3.5 m Function opening.
if bpy.data.objects.get("GateRing"):
    bpy.data.objects["GateRing"].scale = (1.85,1.85,1.85)
torus("V03_GateOuterRing", (0,-12.66,1.76), 1.62, 0.095, M_PANEL)
torus("V03_GateInnerRing", (0,-12.61,1.76), 1.28, 0.035, M_CYAN_SOFT)
for x in (-1.82,1.82):
    box(f"V03_GatePylon_{x}", (x,-12.58,1.77), (0.22,0.34,3.15), M_PANEL, 0.06)
for z,w in [(0.40,2.45),(3.12,2.45),(3.52,1.65)]:
    box(f"V03_GateRib_{z}", (0,-12.56,z), (w,0.22,0.11), M_PANEL, 0.035)
box("V03_GateHeader", (0,-12.52,4.13), (4.8,0.32,0.42), M_DARK, 0.07)
box("V03_GateHeaderGlow", (0,-12.33,4.13), (2.35,0.035,0.055), M_CYAN, 0.01)

# Station detailing stays within the existing station footprints.
def detail_station(prefix, x, glow):
    y=-7.2
    cyl(prefix+"_V03Glass", (x,y,1.62), 0.50, 1.76, M_GLASS, 64)
    for z in (0.82,1.18,2.06,2.44):
        bpy.ops.mesh.primitive_torus_add(major_radius=0.54, minor_radius=0.035, major_segments=48,
                                        minor_segments=10, location=(x,y,z))
        o=bpy.context.object; o.name=f"{prefix}_V03Ring_{z}"; o.data.materials.append(M_PANEL); link(o)
    box(prefix+"_V03ScreenFrame", (x,y+0.79,1.03), (1.10,0.10,0.55), M_PANEL, 0.035)
    box(prefix+"_V03Screen", (x,y+0.845,1.05), (0.83,0.018,0.32), M_CYAN_SOFT, 0.006)
    for sx in (-0.72,0.72):
        box(prefix+f"_V03SideRail_{sx}", (x+sx,y,1.60), (0.065,0.70,2.25), M_DARK, 0.025)
    box(prefix+"_V03GlowFoot", (x,y+0.36,0.43), (1.35,0.08,0.06), glow, 0.01)

detail_station("AmberStation", -4.1, M_AMBER)
detail_station("CobaltStation", 4.1, M_COBALT)
# Human scale correction for the composition proxy only.
player = bpy.data.collections.get("PLAYER_PROXY")
if player:
    for o in player.objects:
        if not o.get("v03_scaled"):
            o.location.x *= 0.76
            o.location.z *= 0.76
            o.scale *= 0.76
            o["v03_scaled"] = True

# Hide the old oversized MUSCA proxy; replace it with a compact 15 cm-class body.
old_musca = bpy.data.collections.get("MUSCA_PROXY")
if old_musca:
    old_musca.hide_render = True
    old_musca.hide_viewport = True

mx,my,mz = -0.78,5.55,1.38
sphere("MUSCA03_Body", (mx,my,mz), (0.075,0.095,0.062), M_CERAMIC)
sphere("MUSCA03_Core", (mx,my-0.078,mz), (0.058,0.040,0.052), M_DARK)
sphere("MUSCA03_Eye", (mx,my-0.113,mz), (0.041,0.018,0.041), M_CYAN)
torus("MUSCA03_EyeRing", (mx,my-0.128,mz), 0.050, 0.008, M_PANEL, rot=(math.radians(90),0,0))
for sx in (-1,1):
    cyl(f"MUSCA03_AntennaStem_{sx}", (mx+sx*0.035,my-0.005,mz+0.095), 0.004, 0.095, M_DARK, 12, "MUSCA_V03")
    tip=sphere(f"MUSCA03_AntennaTip_{sx}", (mx+sx*0.055,my-0.018,mz+0.145), (0.010,0.010,0.010), M_CYAN, "MUSCA_V03")
for sx in (-1,1):
    for dz in (-0.018,0.030):
        wing=box(f"MUSCA03_Wing_{sx}_{dz}", (mx+sx*0.105,my+0.018,mz+dz), (0.115,0.010,0.044), M_GLASS, 0.012, "MUSCA_V03")
        wing.rotation_euler[2]=math.radians(-18*sx)
        wing.rotation_euler[1]=math.radians(8+dz*120)
# Small articulated manipulators and wing spars make MUSCA read as technology, not fairy wings.
for sx in (-1,1):
    arm=cyl(f"MUSCA03_Arm_{sx}", (mx+sx*0.045,my-0.015,mz-0.082), 0.007, 0.085, M_DARK, 12, "MUSCA_V03")
    arm.rotation_euler[1]=math.radians(24*sx)
    claw=sphere(f"MUSCA03_Claw_{sx}", (mx+sx*0.064,my-0.025,mz-0.125), (0.014,0.010,0.018), M_PANEL, "MUSCA_V03")
for sx in (-1,1):
    spar=box(f"MUSCA03_WingSpar_{sx}", (mx+sx*0.105,my+0.010,mz+0.005), (0.120,0.006,0.008), M_PANEL, 0.003, "MUSCA_V03")
    spar.rotation_euler[2]=math.radians(-18*sx)

# Retune inherited lighting to neutral laboratory illumination.
for o in bpy.data.objects:
    if o.type != "LIGHT": continue
    if o.name.startswith("CeilingArea_"):
        o.data.energy = 360; o.data.color = (0.82,0.90,0.92)
    elif o.name == "GateCyan":
        o.data.energy = 520
    elif o.name in {"WarmFill","CoolFill"}:
        o.data.energy = 120
for i,y in enumerate([9.5,5.5,1.5,-2.5,-6.5,-10.0]):
    a=add_area(f"V03_Key_{i}", (0,y,4.05), 390, 3.8, (0.86,0.91,0.92))
    a.rotation_euler=(0,0,0)
add_area("V03_GateFill", (0,-10.8,3.2), 290, 2.4, (0.25,0.65,0.95)).rotation_euler=(math.radians(25),0,0)
add_area("V03_AmberFill", (-4.1,-6.2,2.6), 210, 1.7, (1.0,0.34,0.08)).rotation_euler=(math.radians(35),0,0)
add_area("V03_CobaltFill", (4.1,-6.2,2.6), 210, 1.7, (0.12,0.34,1.0)).rotation_euler=(math.radians(35),0,0)
def look_at(obj, target):
    direction = Vector(target) - obj.location
    obj.rotation_euler = direction.to_track_quat('-Z','Y').to_euler()

def camera(name, loc, target, lens=32):
    d=bpy.data.cameras.new(name); d.lens=lens; d.sensor_width=36
    o=bpy.data.objects.new(name,d); o.location=loc; cols["CAMERAS_V03"].objects.link(o); look_at(o,target); return o

cam_hero = camera("CAM_V03_HERO", (0.0,11.8,2.05), (0,-9.0,1.65), 31)
cam_gate = camera("CAM_V03_GATE", (0.0,-1.5,2.15), (0,-12.8,1.9), 36)
cam_station = camera("CAM_V03_STATION", (-0.2,-1.4,1.9), (-4.1,-7.2,1.55), 44)
cam_musca = camera("CAM_V03_MUSCA", (0.10,6.55,1.62), (mx,my,mz), 58)
scene.camera = cam_hero
scene.render.engine = 'BLENDER_EEVEE'
scene.render.resolution_x = 1600
scene.render.resolution_y = 900
scene.render.resolution_percentage = 65
scene.render.image_settings.file_format = 'PNG'
scene.world.color = (0.006,0.010,0.013)
try: scene.view_settings.look = 'AgX - Medium High Contrast'
except Exception: pass
scene.view_settings.exposure = 0.15

# Put the visible editor viewport into the new hero camera so the user sees the pass immediately.
for window in bpy.context.window_manager.windows:
    for area in window.screen.areas:
        if area.type == 'VIEW_3D':
            space = area.spaces.active
            space.region_3d.view_perspective = 'CAMERA'
            space.shading.type = 'MATERIAL'
# Save as a new file: the accepted v0.1 artifact remains untouched.
scene["visual_target"] = "ISKRA_MUSCA_visual_target_v1"
scene["visual_pass"] = "form-v0.3"
bpy.ops.wm.save_as_mainfile(filepath=str(OUT))
print(f"MUSCA_VISUAL_V03_PASS saved={OUT} objects={len(scene.objects)}")
