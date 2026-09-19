import bpy, hashlib, json, math
from pathlib import Path
from mathutils import Vector

BASE = Path(__file__).resolve().parents[1]
OUT_BLEND = BASE / "Combat_Sentinel_v0.1.blend"
EXPORTS = BASE / "exports" / "combat_v01"
RENDERS = BASE / "renders" / "combat_v01"
RECEIPTS = BASE / "receipts"
for path in (EXPORTS, RENDERS, RECEIPTS):
    path.mkdir(parents=True, exist_ok=True)

bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene
scene.unit_settings.system = "METRIC"
scene.unit_settings.scale_length = 1.0
scene.render.engine = "BLENDER_EEVEE"
scene.render.resolution_x = 960
scene.render.resolution_y = 960
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = "PNG"

sentinel = bpy.data.collections.new("SENTINEL_V01")
preview = bpy.data.collections.new("PREVIEW_ONLY")
scene.collection.children.link(sentinel)
scene.collection.children.link(preview)

def material(name, base, metallic=0.0, roughness=0.4, emission=None, strength=0.0):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = (*base, 1.0)
    bsdf.inputs["Metallic"].default_value = metallic
    bsdf.inputs["Roughness"].default_value = roughness
    if emission:
        key = "Emission Color" if "Emission Color" in bsdf.inputs else "Emission"
        bsdf.inputs[key].default_value = (*emission, 1.0)
        if "Emission Strength" in bsdf.inputs:
            bsdf.inputs["Emission Strength"].default_value = strength
    return m

DARK = material("CombatV01_DarkMetal", (0.018, 0.024, 0.028), 0.78, 0.24)
CERAMIC = material("CombatV01_Ceramic", (0.46, 0.50, 0.50), 0.22, 0.31)
CYAN = material("CombatV01_Cyan", (0.01, 0.16, 0.22), 0.30, 0.20, (0.02, 0.65, 1.0), 4.2)
AMBER = material("CombatV01_Amber", (0.30, 0.07, 0.008), 0.28, 0.26, (1.0, 0.16, 0.01), 2.6)
FLOOR = material("CombatV01_Floor", (0.025, 0.032, 0.038), 0.58, 0.38)

def link(obj, collection=sentinel):
    for c in list(obj.users_collection):
        c.objects.unlink(obj)
    collection.objects.link(obj)
    return obj

def box(name, loc, dims, mat, bevel=0.03):
    bpy.ops.mesh.primitive_cube_add(location=loc)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = dims
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel:
        mod = obj.modifiers.new("Bevel", "BEVEL")
        mod.width = bevel
        mod.segments = 3
    obj.data.materials.append(mat)
    return link(obj)

def sphere(name, loc, scale, mat):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=28, ring_count=14, location=loc)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(mat)
    bpy.ops.object.shade_smooth()
    return link(obj)

def cylinder(name, loc, radius, depth, mat, rot=(0.0, 0.0, 0.0)):
    bpy.ops.mesh.primitive_cylinder_add(vertices=24, radius=radius, depth=depth, location=loc, rotation=rot)
    obj = bpy.context.object
    obj.name = name
    obj.data.materials.append(mat)
    return link(obj)

root = bpy.data.objects.new("SentinelV01_Root", None)
sentinel.objects.link(root)

parts = []
parts += [box("SV01_Torso", (0, 0, 1.20), (0.54, 0.34, 0.55), DARK, 0.07)]
parts += [box("SV01_ChestPlate", (0, -0.19, 1.27), (0.43, 0.07, 0.31), CERAMIC, 0.05)]
parts += [box("SV01_Pelvis", (0, 0, 0.86), (0.40, 0.29, 0.20), DARK, 0.05)]
parts += [sphere("SV01_Head", (0, -0.01, 1.62), (0.15, 0.13, 0.13), CERAMIC)]
parts += [sphere("SV01_OpticHousing", (0, -0.125, 1.62), (0.085, 0.036, 0.075), DARK)]
parts += [sphere("SV01_Optic", (0, -0.158, 1.62), (0.052, 0.012, 0.048), CYAN)]
parts += [box("SV01_Core", (0, -0.228, 1.25), (0.15, 0.025, 0.13), AMBER, 0.02)]

for side in (-1, 1):
    x = side * 0.33
    parts += [box(f"SV01_Shoulder_{side}", (x, -0.01, 1.38), (0.18, 0.27, 0.16), CERAMIC, 0.05)]
    parts += [cylinder(f"SV01_UpperArm_{side}", (x, 0.0, 1.12), 0.075, 0.34, DARK)]
    parts += [box(f"SV01_Forearm_{side}", (x, -0.02, 0.88), (0.13, 0.17, 0.28), CERAMIC, 0.04)]
    parts += [box(f"SV01_Hand_{side}", (x, -0.035, 0.69), (0.12, 0.15, 0.12), DARK, 0.035)]
    lx = side * 0.13
    parts += [box(f"SV01_Thigh_{side}", (lx, 0.0, 0.59), (0.18, 0.24, 0.42), DARK, 0.055)]
    parts += [box(f"SV01_Knee_{side}", (lx, -0.12, 0.40), (0.17, 0.11, 0.16), CERAMIC, 0.04)]
    parts += [box(f"SV01_Shin_{side}", (lx, 0.0, 0.23), (0.16, 0.22, 0.31), CERAMIC, 0.045)]
    parts += [box(f"SV01_Foot_{side}", (lx, -0.055, 0.07), (0.19, 0.31, 0.14), DARK, 0.04)]
    parts += [box(f"SV01_Telegraph_{side}", (side * 0.285, -0.162, 1.39), (0.025, 0.02, 0.12), AMBER, 0.006)]

for obj in parts:
    obj.parent = root

# Preview-only floor and lighting; never exported to Unity.
bpy.ops.mesh.primitive_plane_add(size=7.0, location=(0, 0, 0))
floor = link(bpy.context.object, preview)
floor.name = "PreviewFloor"
floor.data.materials.append(FLOOR)

world = scene.world or bpy.data.worlds.new("CombatWorld")
scene.world = world
world.color = (0.008, 0.012, 0.018)

def add_area(name, loc, energy, color, size):
    data = bpy.data.lights.new(name, "AREA")
    data.energy = energy
    data.color = color
    data.shape = "DISK"
    data.size = size
    obj = bpy.data.objects.new(name, data)
    preview.objects.link(obj)
    obj.location = loc
    return obj

key = add_area("Preview_Key", (2.4, -3.1, 3.5), 900, (0.70, 0.86, 1.0), 3.0)
fill = add_area("Preview_Fill", (-2.2, -1.5, 2.2), 620, (1.0, 0.28, 0.08), 2.2)
for lamp, target in ((key, Vector((0, 0, 1.0))), (fill, Vector((0, 0, 1.15)))):
    lamp.rotation_euler = (target - lamp.location).to_track_quat('-Z', 'Y').to_euler()

cam_data = bpy.data.cameras.new("CombatPreviewCamera")
cam = bpy.data.objects.new("CombatPreviewCamera", cam_data)
preview.objects.link(cam)
cam.location = (3.0, -4.8, 2.35)
cam.rotation_euler = (Vector((0, 0, 0.95)) - cam.location).to_track_quat('-Z', 'Y').to_euler()
cam.data.lens = 52
scene.camera = cam
scene.render.filepath = str(RENDERS / "sentinel_v01.png")

def sha(path):
    h = hashlib.sha256()
    with open(path, "rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            h.update(chunk)
    return h.hexdigest()

bpy.ops.wm.save_as_mainfile(filepath=str(OUT_BLEND))
bpy.ops.object.select_all(action='DESELECT')
export_objects = [root] + [o for o in parts if o.type == 'MESH']
for obj in export_objects:
    obj.hide_set(False)
    obj.select_set(True)
bpy.context.view_layer.objects.active = root
fbx = EXPORTS / "Sentinel_FormProxy_v0.1.fbx"
bpy.ops.export_scene.fbx(filepath=str(fbx), use_selection=True,
    object_types={'MESH', 'EMPTY'}, use_mesh_modifiers=True, add_leaf_bones=False)

bpy.ops.render.render(write_still=True)
mins = [min((obj.dimensions[i] * -0.5 + obj.location[i]) for obj in parts) for i in range(3)]
maxs = [max((obj.dimensions[i] * 0.5 + obj.location[i]) for obj in parts) for i in range(3)]
checks = {
    "object_count": len(parts) >= 20,
    "height_proxy": 1.65 <= maxs[2] <= 1.90,
    "feet_near_floor": -0.02 <= mins[2] <= 0.08,
    "fbx_exists": fbx.exists() and fbx.stat().st_size > 0,
}
status = "PASS" if all(checks.values()) else "FAIL"
receipt = {
    "schema": "musca.combat-sentinel-v01.v1",
    "status": status,
    "claim_boundary": "Procedural combat proxy only; not final enemy art, rig, animation, AI, or game-feel approval.",
    "objects": len(parts),
    "bounds_min": mins,
    "bounds_max": maxs,
    "checks": checks,
    "artifacts": {}
}
for artifact in (OUT_BLEND, fbx, RENDERS / "sentinel_v01.png"):
    receipt["artifacts"][artifact.name] = {
        "bytes": artifact.stat().st_size,
        "sha256": sha(artifact)
    }
out = RECEIPTS / "combat-sentinel-v01.json"
out.write_text(json.dumps(receipt, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print("MUSCA_COMBAT_SENTINEL_V01=" + status)
print(str(out))
if status != "PASS":
    raise RuntimeError("Combat sentinel validation failed")
