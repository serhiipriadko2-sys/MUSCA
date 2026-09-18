import bpy, math
from pathlib import Path

BASE=Path(r"C:\github\MUSCA\blender\gate-lab-v0.2")
scene=bpy.context.scene
V=bpy.data.collections.get("VISUAL_V03")
L=bpy.data.objects["GateDoor_L"]; R=bpy.data.objects["GateDoor_R"]
MP=bpy.data.materials.get("V03_Panel"); MC=bpy.data.materials.get("V03_CyanSoft")

for name in ["V03_GateOuterRing","V03_GateInnerRing"]:
    o=bpy.data.objects.get(name)
    if o: bpy.data.objects.remove(o,do_unlink=True)
for o in list(bpy.data.objects):
    if o.name.startswith("V03_GateRib_") or o.name.startswith("V03_GateOuterArc_") or o.name.startswith("V03_GateInnerArc_") or o.name.startswith("V03_DoorRib_"):
        bpy.data.objects.remove(o,do_unlink=True)

# Legacy static motif is kept in the base artifact but excluded from the v0.3 visual/export layer.
for name in ("GateRing","GateSeam"):
    o=bpy.data.objects.get(name)
    if o:
        o.hide_render=True
        o["v03_export_exclude"]=True

def parent_keep_world(o,parent):
    world=o.matrix_world.copy(); o.parent=parent; o.matrix_world=world
    o["gate_dynamic_child"]=True
    return o
def arc(name, side, radius, bevel, mat, parent):
    curve=bpy.data.curves.new(name+"Curve",'CURVE'); curve.dimensions='3D'; curve.bevel_depth=bevel; curve.bevel_resolution=3
    spline=curve.splines.new('POLY'); steps=24; spline.points.add(steps)
    a0,a1=((math.pi/2,3*math.pi/2) if side<0 else (-math.pi/2,math.pi/2))
    for i,p in enumerate(spline.points):
        a=a0+(a1-a0)*i/steps
        p.co=(radius*math.cos(a),0,radius*math.sin(a),1)
    o=bpy.data.objects.new(name,curve); o.location=(0,-12.55,1.76); V.objects.link(o); o.data.materials.append(mat)
    bpy.context.view_layer.objects.active=o; o.select_set(True); bpy.ops.object.convert(target='MESH'); o=bpy.context.object
    parent_keep_world(o,parent); return o

arc("V03_GateOuterArc_L",-1,1.62,0.090,MP,L)
arc("V03_GateOuterArc_R", 1,1.62,0.090,MP,R)
arc("V03_GateInnerArc_L",-1,1.28,0.032,MC,L)
arc("V03_GateInnerArc_R", 1,1.28,0.032,MC,R)

def box(name,loc,dims,mat,parent):
    bpy.ops.mesh.primitive_cube_add(location=loc); o=bpy.context.object; o.name=name; o.dimensions=dims
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    b=o.modifiers.new("Bevel","BEVEL"); b.width=0.025; b.segments=2
    o.data.materials.append(mat)
    for c in list(o.users_collection): c.objects.unlink(o)
    V.objects.link(o); return parent_keep_world(o,parent)

for z,w in [(0.40,2.45),(3.12,2.45),(3.52,1.65)]:
    half=max(0.20,w/2-0.06); cx=w/4+0.03
    box(f"V03_DoorRib_L_{z}",(-cx,-12.54,z),(half,0.16,0.10),MP,L)
    box(f"V03_DoorRib_R_{z}",( cx,-12.54,z),(half,0.16,0.10),MP,R)
scene["gate_dynamic_visuals"]="split-and-parented-to-door-leaves"
bpy.ops.wm.save_as_mainfile(filepath=str(BASE/"GateLab_Form_v0.3.blend"))
print("MUSCA_GATE_DYNAMIC_V03_PASS")
