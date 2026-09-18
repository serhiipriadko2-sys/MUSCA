import bpy, math
from pathlib import Path
from mathutils import Vector

BASE=Path(r"C:\github\MUSCA\blender\gate-lab-v0.2")
scene=bpy.context.scene
V=bpy.data.collections.get("VISUAL_V03")
if V is None:
    V=bpy.data.collections.new("VISUAL_V03"); scene.collection.children.link(V)

def link(o):
    for c in list(o.users_collection): c.objects.unlink(o)
    V.objects.link(o); return o

def box(name,loc,dims,mat,bevel=0.04):
    old=bpy.data.objects.get(name)
    if old: bpy.data.objects.remove(old,do_unlink=True)
    bpy.ops.mesh.primitive_cube_add(location=loc); o=bpy.context.object; o.name=name; o.dimensions=dims
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    if bevel: b=o.modifiers.new("Bevel","BEVEL"); b.width=bevel; b.segments=3
    o.data.materials.append(mat); return link(o)

def text(name,body,loc,size,mat):
    old=bpy.data.objects.get(name)
    if old: bpy.data.objects.remove(old,do_unlink=True)
    bpy.ops.object.text_add(location=loc,rotation=(math.radians(90),0,math.radians(180)))
    o=bpy.context.object; o.name=name; o.data.body=body; o.data.align_x='CENTER'; o.data.align_y='CENTER'; o.data.size=size
    o.data.extrude=0.008; o.data.bevel_depth=0.002; o.data.materials.append(mat); link(o)
    bpy.context.view_layer.objects.active=o; o.select_set(True); bpy.ops.object.convert(target='MESH'); return o
M_DARK=bpy.data.materials.get("V03_DarkMetal")
M_PANEL=bpy.data.materials.get("V03_Panel")
M_CYAN=bpy.data.materials.get("V03_Cyan")
M_CYAN_SOFT=bpy.data.materials.get("V03_CyanSoft")
M_WHITE=bpy.data.materials.get("V03_WhiteLight")
M_CER=bpy.data.materials.get("V03_Ceramic")

# Gate signage closer to the approved gameplay reference.
box("V03_GateSignPanel",(0,-12.44,4.12),(3.7,0.16,0.56),M_DARK,0.05)
text("V03_GateTitle","ШЛЮЗ A-1",(0,-12.34,4.23),0.28,M_WHITE)
text("V03_GateSubtitle","НЕЙТРАЛЬНЫЙ РЕАГЕНТ",(0,-12.33,4.00),0.105,M_CYAN_SOFT)

# Foreground scanner console for depth and a concrete research affordance.
box("V03_ScannerBase",(-4.65,4.35,0.38),(1.35,0.85,0.76),M_DARK,0.08)
box("V03_ScannerStem",(-4.65,4.38,0.92),(0.82,0.55,0.44),M_PANEL,0.06)
screen=box("V03_ScannerScreen",(-4.65,4.08,1.18),(1.10,0.08,0.58),M_DARK,0.035)
screen.rotation_euler[0]=math.radians(-18)
box("V03_ScannerGlow",(-4.65,4.025,1.20),(0.88,0.018,0.38),M_CYAN_SOFT,0.008).rotation_euler[0]=math.radians(-18)
box("V03_SampleCanister",(-5.25,4.65,0.57),(0.46,0.46,1.14),M_PANEL,0.06)
box("V03_SampleGlow",(-5.25,4.40,0.65),(0.20,0.025,0.62),M_WHITE,0.008)
# Turn MUSCA's optic toward the third-person camera; keep the body beside the shoulder.
core=bpy.data.objects.get("M03_Core"); optic=bpy.data.objects.get("M03_Optic")
if core: core.location.y=5.568
if optic: optic.location.y=5.612
old=bpy.data.objects.get("M03_OpticRing")
if old: bpy.data.objects.remove(old,do_unlink=True)
bpy.ops.mesh.primitive_torus_add(major_radius=0.055,minor_radius=0.008,major_segments=48,minor_segments=10,
                                 location=(-0.52,5.628,1.42),rotation=(math.radians(90),0,0))
ring=bpy.context.object; ring.name="M03_OpticRing"; ring.data.materials.append(M_PANEL)
mcol=bpy.data.collections.get("MUSCA_V03")
for c in list(ring.users_collection): c.objects.unlink(ring)
mcol.objects.link(ring)
for sx in (-1,1):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=24,ring_count=12,location=(-0.52+sx*0.078,5.50,1.415))
    pod=bpy.context.object; pod.name=f"M03_SidePod_{sx}"; pod.scale=(0.028,0.040,0.032)
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True); pod.data.materials.append(M_DARK)
    for c in list(pod.users_collection): c.objects.unlink(pod)
    mcol.objects.link(pod)
    box(f"M03_PodGlow_{sx}",(-0.52+sx*0.080,5.535,1.415),(0.018,0.010,0.018),M_CYAN,0.003)

# Less flat lighting: neutral base, cyan reserved for the path/gate, local reagent accents.
for o in bpy.data.objects:
    if o.type!='LIGHT': continue
    if o.name.startswith("CeilingArea_"): o.data.energy=250
    if o.name.startswith("V03_Key_"): o.data.energy=245
    if o.name=="GateCyan": o.data.energy=390
scene.view_settings.exposure=-0.15
cam=bpy.data.objects.get("CAM_V03_HERO")
if cam:
    cam.location=(0.0,10.85,1.72); cam.data.lens=33
    direction=Vector((0,-9.0,1.56))-cam.location
    cam.rotation_euler=direction.to_track_quat('-Z','Y').to_euler(); scene.camera=cam

for window in bpy.context.window_manager.windows:
    for area in window.screen.areas:
        if area.type=='VIEW_3D':
            area.spaces.active.region_3d.view_perspective='CAMERA'
            area.spaces.active.shading.type='MATERIAL'

scene["visual_polish"]="gameplay-reference-composition"
bpy.ops.wm.save_as_mainfile(filepath=str(BASE/"GateLab_Form_v0.3.blend"))
print("MUSCA_VISUAL_V03_POLISH saved")
