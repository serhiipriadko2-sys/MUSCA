import bpy, math, hashlib, json
from pathlib import Path

BASE = Path(r"C:\github\MUSCA\blender\gate-lab-v0.2")
OUT_BLEND = BASE / "GateLab_Form_v0.31.blend"
EXPORTS = BASE / "exports" / "v031"
EXPORTS.mkdir(parents=True, exist_ok=True)

player_col = bpy.data.collections.get("PLAYER_V03")
musca_col = bpy.data.collections.get("MUSCA_V03")
if player_col is None or musca_col is None:
    raise RuntimeError("v0.3 character collections missing")

SUIT = bpy.data.materials.get("V03_Suit")
ARMOR = bpy.data.materials.get("V03_Armor")
SKIN = bpy.data.materials.get("V03_Skin")
HAIR = bpy.data.materials.get("V03_Hair")
ORANGE = bpy.data.materials.get("V03_Orange")
CYAN = bpy.data.materials.get("V03_Cyan")
MCER = bpy.data.materials.get("V03_Ceramic")
MDARK = bpy.data.materials.get("V03_DarkMetal")
MGLASS = bpy.data.materials.get("V03_Glass")

def relink(obj, collection):
    for c in list(obj.users_collection):
        c.objects.unlink(obj)
    collection.objects.link(obj)
    return obj

def box(name, loc, dims, mat, collection, bevel=0.012):
    bpy.ops.mesh.primitive_cube_add(location=loc)
    o = bpy.context.object; o.name = name; o.dimensions = dims
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel:
        b = o.modifiers.new("Bevel", "BEVEL"); b.width = bevel; b.segments = 3
    if mat: o.data.materials.append(mat)
    return relink(o, collection)

def sphere(name, loc, scale, mat, collection):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=28, ring_count=14, location=loc)
    o = bpy.context.object; o.name = name; o.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if mat: o.data.materials.append(mat)
    return relink(o, collection)

def torus(name, loc, major, minor, mat, collection, rot=(0,0,0)):
    bpy.ops.mesh.primitive_torus_add(major_radius=major, minor_radius=minor, major_segments=36, minor_segments=10, location=loc, rotation=rot)
    o = bpy.context.object; o.name = name
    if mat: o.data.materials.append(mat)
    return relink(o, collection)

# Remove only prior v0.31 detail objects so the pass is idempotent.
for obj in list(bpy.data.objects):
    if obj.name.startswith(("P03_Detail_", "M03_Detail_")):
        bpy.data.objects.remove(obj, do_unlink=True)

px, py = 0.0, 5.65
box("P03_Detail_Collar", (px, py-0.035, 1.49), (0.25, 0.18, 0.08), ARMOR, player_col, 0.025)
box("P03_Detail_ChestLink", (px, py-0.188, 1.31), (0.09, 0.022, 0.10), CYAN, player_col, 0.008)
box("P03_Detail_PackSide_L", (px-0.22, py+0.17, 1.17), (0.09, 0.11, 0.28), ARMOR, player_col, 0.025)
box("P03_Detail_PackSide_R", (px+0.22, py+0.17, 1.17), (0.09, 0.11, 0.28), ARMOR, player_col, 0.025)
box("P03_Detail_PackAmber", (px+0.18, py+0.238, 1.27), (0.035, 0.025, 0.14), ORANGE, player_col, 0.008)
box("P03_Detail_WristFrame", (px-0.292, py-0.088, 0.95), (0.11, 0.055, 0.095), ARMOR, player_col, 0.014)
box("P03_Detail_Knee_L", (px-0.105, py-0.11, 0.43), (0.12, 0.055, 0.12), ARMOR, player_col, 0.025)
box("P03_Detail_Knee_R", (px+0.105, py-0.11, 0.43), (0.12, 0.055, 0.12), ARMOR, player_col, 0.025)

# Subtle facial/tech silhouette cues without turning the researcher into a soldier.
sphere("P03_Detail_EarLink", (px+0.108, py-0.072, 1.615), (0.025,0.018,0.025), CYAN, player_col)
box("P03_Detail_HairTie", (px, py+0.112, 1.705), (0.055,0.032,0.025), ARMOR, player_col, 0.008)

mx, my, mz = -0.52, 5.48, 1.42
# A layered camera/iris makes MUSCA read as a living sensor, not a glowing ball.
torus("M03_Detail_OpticHousing", (mx, my-0.150, mz), 0.055, 0.008, MDARK, musca_col, (math.radians(90),0,0))
torus("M03_Detail_Iris", (mx, my-0.160, mz), 0.035, 0.004, CYAN, musca_col, (math.radians(90),0,0))
box("M03_Detail_DorsalPanel", (mx, my+0.072, mz+0.055), (0.085,0.045,0.035), MCER, musca_col, 0.014)
box("M03_Detail_AmberStatus", (mx+0.060, my+0.080, mz+0.055), (0.018,0.014,0.026), ORANGE, musca_col, 0.005)

for side in (-1, 1):
    sphere(f"M03_Detail_WingJoint_{side}", (mx+side*0.055, my+0.020, mz+0.018), (0.018,0.018,0.018), MDARK, musca_col)
    sphere(f"M03_Detail_ManipulatorTip_{side}", (mx+side*0.070, my-0.030, mz-0.137), (0.012,0.010,0.014), ORANGE, musca_col)
    sphere(f"M03_Detail_SensorNode_{side}", (mx+side*0.064, my-0.015, mz+0.110), (0.011,0.011,0.011), CYAN, musca_col)

# Give the translucent wings a restrained technological edge instead of a fairy-like glow.
if MGLASS and MGLASS.use_nodes:
    p = MGLASS.node_tree.nodes.get("Principled BSDF")
    if p:
        p.inputs["Roughness"].default_value = 0.28
        if "Coat Weight" in p.inputs: p.inputs["Coat Weight"].default_value = 0.18
        if "Metallic" in p.inputs: p.inputs["Metallic"].default_value = 0.08

def export_group(prefix, path):
    bpy.ops.object.select_all(action='DESELECT')
    objs = [o for o in bpy.data.objects if o.name.startswith(prefix) and o.type in {'MESH','EMPTY'}]
    for o in objs:
        o.hide_set(False); o.select_set(True)
    if objs: bpy.context.view_layer.objects.active = objs[0]
    bpy.ops.export_scene.fbx(filepath=str(path), use_selection=True, object_types={'MESH','EMPTY'}, use_mesh_modifiers=True, add_leaf_bones=False)
    return len(objs)

def sha(path):
    h = hashlib.sha256()
    with open(path, 'rb') as f:
        for chunk in iter(lambda: f.read(1024*1024), b''): h.update(chunk)
    return h.hexdigest()

bpy.ops.wm.save_as_mainfile(filepath=str(OUT_BLEND))
researcher_fbx = EXPORTS / "Researcher_FormProxy_v0.31.fbx"
musca_fbx = EXPORTS / "MUSCA_FormProxy_v0.31.fbx"
researcher_count = export_group("P03_", researcher_fbx)
musca_count = export_group("M03_", musca_fbx)

receipt = {
    "schema": "musca.character-art-v031.v1",
    "status": "PASS",
    "researcher_objects": researcher_count,
    "musca_objects": musca_count,
    "artifacts": {}
}
for p in (OUT_BLEND, researcher_fbx, musca_fbx):
    receipt["artifacts"][p.name] = {"bytes": p.stat().st_size, "sha256": sha(p)}
out = BASE / "receipts" / "character-art-v031.json"
out.write_text(json.dumps(receipt, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print("MUSCA_CHARACTER_ART_V031=PASS")
print(str(out))
