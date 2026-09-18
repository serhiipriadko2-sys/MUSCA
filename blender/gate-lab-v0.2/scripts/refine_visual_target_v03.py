import bpy, math
from pathlib import Path
from mathutils import Vector

BASE = Path(r"C:\github\MUSCA\blender\gate-lab-v0.2")
scene = bpy.context.scene

old = bpy.data.collections.get("PLAYER_V03")
if old:
    for obj in list(old.objects): bpy.data.objects.remove(obj, do_unlink=True)
    bpy.data.collections.remove(old)
player_col = bpy.data.collections.new("PLAYER_V03")
scene.collection.children.link(player_col)

def mat(name, base, metallic=0.0, rough=0.4, emission=None, strength=0.0):
    m=bpy.data.materials.get(name) or bpy.data.materials.new(name); m.use_nodes=True
    p=m.node_tree.nodes.get("Principled BSDF")
    p.inputs["Base Color"].default_value=(*base,1); p.inputs["Metallic"].default_value=metallic; p.inputs["Roughness"].default_value=rough
    if emission:
        for k in ("Emission Color","Emission"):
            if k in p.inputs: p.inputs[k].default_value=(*emission,1)
        if "Emission Strength" in p.inputs: p.inputs["Emission Strength"].default_value=strength
    return m

SUIT=mat("V03_Suit",(0.34,0.37,0.37),0.12,0.42)
ARMOR=mat("V03_Armor",(0.018,0.025,0.028),0.72,0.25)
SKIN=mat("V03_Skin",(0.34,0.17,0.11),0.0,0.55)
HAIR=mat("V03_Hair",(0.018,0.010,0.008),0.0,0.70)
ORANGE=mat("V03_Orange",(0.28,0.055,0.004),0.32,0.30,(0.8,0.08,0.005),0.55)
CYAN=bpy.data.materials.get("V03_Cyan")
def link(o):
    for c in list(o.users_collection): c.objects.unlink(o)
    player_col.objects.link(o); return o

def box(name, loc, dims, material, bevel=0.04):
    bpy.ops.mesh.primitive_cube_add(location=loc)
    o=bpy.context.object; o.name=name; o.dimensions=dims
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel:
        m=o.modifiers.new("Bevel","BEVEL"); m.width=bevel; m.segments=3
    o.data.materials.append(material); return link(o)

def sphere(name, loc, scale, material):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=32, ring_count=16, location=loc)
    o=bpy.context.object; o.name=name; o.scale=scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    o.data.materials.append(material); return link(o)

def cyl(name, loc, radius, depth, material, rot=(0,0,0)):
    bpy.ops.mesh.primitive_cylinder_add(vertices=24, radius=radius, depth=depth, location=loc, rotation=rot)
    o=bpy.context.object; o.name=name; o.data.materials.append(material); return link(o)

# Retire the construction dummy; the new silhouette remains a proxy, not a final character asset.
old_player=bpy.data.collections.get("PLAYER_PROXY")
if old_player:
    old_player.hide_viewport=True; old_player.hide_render=True

px,py=0.0,5.65
box("P03_Torso",(px,py,1.18),(0.38,0.23,0.54),SUIT,0.08)
box("P03_ChestArmor",(px,py-0.125,1.24),(0.31,0.055,0.27),ARMOR,0.04)
box("P03_Pelvis",(px,py,0.85),(0.34,0.21,0.20),ARMOR,0.06)
sphere("P03_Head",(px,py-0.005,1.61),(0.105,0.095,0.125),SKIN)
sphere("P03_HairCap",(px,py+0.025,1.67),(0.112,0.100,0.095),HAIR)
sphere("P03_Bun",(px,py+0.105,1.74),(0.060,0.055,0.060),HAIR)
for sx in (-1,1):
    box(f"P03_UpperArm_{sx}",(px+sx*0.265,py,1.18),(0.10,0.13,0.43),SUIT,0.045)
    box(f"P03_Shoulder_{sx}",(px+sx*0.245,py-0.01,1.37),(0.15,0.17,0.12),ARMOR,0.045)
    box(f"P03_Forearm_{sx}",(px+sx*0.285,py-0.015,0.91),(0.09,0.12,0.35),ARMOR,0.035)
    box(f"P03_Thigh_{sx}",(px+sx*0.105,py,0.58),(0.15,0.18,0.47),SUIT,0.055)
    box(f"P03_Shin_{sx}",(px+sx*0.105,py,0.27),(0.14,0.17,0.36),ARMOR,0.045)
    box(f"P03_Boot_{sx}",(px+sx*0.105,py-0.035,0.09),(0.17,0.28,0.18),ARMOR,0.045)

box("P03_Backpack",(px,py+0.145,1.20),(0.31,0.18,0.50),ARMOR,0.065)
box("P03_PackCore",(px,py+0.245,1.26),(0.13,0.035,0.18),CYAN,0.02)
for sx in (-1,1):
    box(f"P03_Harness_{sx}",(px+sx*0.13,py-0.145,1.18),(0.035,0.035,0.46),ARMOR,0.012)
box("P03_BeltAccent",(px+0.17,py-0.12,0.86),(0.055,0.045,0.11),ORANGE,0.012)
box("P03_WristDisplay",(px-0.29,py-0.085,0.95),(0.085,0.045,0.075),CYAN,0.012)
# Rebuild MUSCA as a compact technological chimera beside the left shoulder.
mcol=bpy.data.collections.get("MUSCA_V03")
if mcol:
    for obj in list(mcol.objects): bpy.data.objects.remove(obj, do_unlink=True)
else:
    mcol=bpy.data.collections.new("MUSCA_V03"); scene.collection.children.link(mcol)

def mlink(o):
    for c in list(o.users_collection): c.objects.unlink(o)
    mcol.objects.link(o); return o

def msphere(name,loc,scale,material):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=36, ring_count=18, location=loc)
    o=bpy.context.object; o.name=name; o.scale=scale
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True); o.data.materials.append(material); return mlink(o)

def mbox(name,loc,dims,material,bevel=0.015):
    bpy.ops.mesh.primitive_cube_add(location=loc); o=bpy.context.object; o.name=name; o.dimensions=dims
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    if bevel: b=o.modifiers.new("Bevel","BEVEL"); b.width=bevel; b.segments=3
    o.data.materials.append(material); return mlink(o)

MCER=bpy.data.materials.get("V03_Ceramic"); MDARK=bpy.data.materials.get("V03_DarkMetal")
MGLASS=bpy.data.materials.get("V03_Glass"); MPANEL=bpy.data.materials.get("V03_Panel")
mx,my,mz=-0.52,5.48,1.42
msphere("M03_Shell",(mx,my,mz),(0.085,0.105,0.070),MCER)
msphere("M03_Core",(mx,my-0.088,mz),(0.067,0.045,0.058),MDARK)
msphere("M03_Optic",(mx,my-0.132,mz),(0.047,0.020,0.047),CYAN)
def wing(name, side, zoff, reach, sweep):
    sx=1 if side>0 else -1
    verts=[(mx+sx*0.045,my+0.010,mz+zoff),(mx+sx*reach,my+sweep,mz+zoff+0.025),
           (mx+sx*(reach*0.92),my+sweep+0.055,mz+zoff-0.018),(mx+sx*0.055,my+0.035,mz+zoff-0.025)]
    mesh=bpy.data.meshes.new(name+"Mesh"); mesh.from_pydata(verts,[],[[0,1,2,3]]); mesh.update()
    o=bpy.data.objects.new(name,mesh); mcol.objects.link(o); o.data.materials.append(MGLASS)
    sol=o.modifiers.new("WingSkin","SOLIDIFY"); sol.thickness=0.004
    bev=o.modifiers.new("WingEdge","BEVEL"); bev.width=0.006; bev.segments=2
    return o

for side in (-1,1):
    wing(f"M03_WingUpper_{side}",side,0.030,0.205,0.050)
    wing(f"M03_WingLower_{side}",side,-0.020,0.175,0.085)
    for zoff,reach,sweep in [(0.030,0.205,0.050),(-0.020,0.175,0.085)]:
        mbox(f"M03_Spar_{side}_{zoff}",(mx+side*reach*0.55,my+sweep*0.55,mz+zoff),(reach*0.78,0.007,0.009),MPANEL,0.003).rotation_euler[2]=math.radians(-14*side)

for side in (-1,1):
    ant=mbox(f"M03_Antenna_{side}",(mx+side*0.050,my-0.012,mz+0.105),(0.008,0.008,0.115),MDARK,0.003)
    ant.rotation_euler[1]=math.radians(18*side)
    msphere(f"M03_AntennaTip_{side}",(mx+side*0.073,my-0.020,mz+0.165),(0.010,0.010,0.010),CYAN)
    claw=mbox(f"M03_Manipulator_{side}",(mx+side*0.052,my-0.025,mz-0.092),(0.014,0.014,0.095),MDARK,0.004)
    claw.rotation_euler[1]=math.radians(20*side)
# Material realism: darker floor and subtle micro-surface instead of flat plastic.
floor=bpy.data.materials.get("M_Floor")
if floor and floor.use_nodes:
    p=floor.node_tree.nodes.get("Principled BSDF"); p.inputs["Base Color"].default_value=(0.020,0.030,0.038,1); p.inputs["Roughness"].default_value=0.34

def micro(material, scale=55.0, strength=0.10):
    if not material or not material.use_nodes: return
    nt=material.node_tree; bsdf=nt.nodes.get("Principled BSDF")
    for n in list(nt.nodes):
        if n.label=="MUSCA_V03_MICRO": nt.nodes.remove(n)
    noise=nt.nodes.new("ShaderNodeTexNoise"); noise.label="MUSCA_V03_MICRO"; noise.inputs["Scale"].default_value=scale; noise.inputs["Detail"].default_value=2.0; noise.inputs["Roughness"].default_value=0.55
    bump=nt.nodes.new("ShaderNodeBump"); bump.label="MUSCA_V03_MICRO"; bump.inputs["Strength"].default_value=strength; bump.inputs["Distance"].default_value=0.035
    nt.links.new(noise.outputs["Fac"],bump.inputs["Height"]); nt.links.new(bump.outputs["Normal"],bsdf.inputs["Normal"])
for m,s,st in [(bpy.data.materials.get("V03_DarkMetal"),70,0.07),(bpy.data.materials.get("V03_Panel"),48,0.10),(floor,85,0.08),(SUIT,90,0.08)]:
    micro(m,s,st)

# Bring the third-person camera closer to the approved gameplay reference.
cam=bpy.data.objects.get("CAM_V03_HERO")
if cam:
    cam.location=(0.0,9.65,1.72); cam.data.lens=34
    direction=Vector((0,-8.8,1.58))-cam.location; cam.rotation_euler=direction.to_track_quat('-Z','Y').to_euler(); scene.camera=cam
scene.view_settings.exposure=-0.05
bpy.ops.wm.save_as_mainfile(filepath=str(BASE/"GateLab_Form_v0.3.blend"))
print("MUSCA_VISUAL_V03_REFINE saved")
