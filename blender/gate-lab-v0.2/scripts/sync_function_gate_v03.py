import bpy
from pathlib import Path

BASE=Path(r"C:\github\MUSCA\blender\gate-lab-v0.2")
left=bpy.data.objects['GateDoor_L']; right=bpy.data.objects['GateDoor_R']
for o,loc in ((left,(-1.13,-13.25,1.76)),(right,(1.13,-13.25,1.76))):
    o.location=loc
    o.dimensions=(2.25,0.34,3.5)
    bpy.context.view_layer.objects.active=o
    o.select_set(True)
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    o.select_set(False)

top=bpy.data.objects['GateFrame_T']; top.location.z=3.66; top.dimensions=(5.18,0.42,0.32)
bpy.context.view_layer.objects.active=top; top.select_set(True); bpy.ops.object.transform_apply(location=False,rotation=False,scale=True); top.select_set(False)

# Keep dynamic Form decoration flush with the player-facing side of the corrected door leaves.
for o in bpy.data.objects:
    if o.get('gate_dynamic_child'):
        mw=o.matrix_world.copy(); mw.translation.y=-13.04; o.matrix_world=mw

bpy.context.scene['function_gate_sync']='gate-lab-v0.2'
bpy.ops.wm.save_as_mainfile(filepath=str(BASE/'GateLab_Form_v0.3.blend'))
print('MUSCA_FUNCTION_GATE_SYNC_V03 PASS')
