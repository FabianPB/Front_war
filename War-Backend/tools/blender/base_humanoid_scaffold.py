"""
WAR · Base humanoid scaffold (Blender 4.0+)

Creates a fresh .blend scene pre-configured for the WAR base humanoid workflow:
  - Metric units (1 Blender unit = 1 meter)
  - Clean scene (default cube removed)
  - Proportion reference cubes at the correct heights for heads 1..7.75
  - Empty placeholders at hip / chest / shoulder / neck / head positions
  - Basic lighting and camera for silhouette testing
  - Collection hierarchy ready for the modeling workflow

Usage (inside Blender):
    - Open Blender → Scripting workspace → Open Text → load this file → Run Script
    - File will be untitled; save as Char_Base_M.blend or Char_Base_F.blend

Usage (headless):
    blender --background --factory-startup --python base_humanoid_scaffold.py \
            -- --gender male --output Char_Base_M.blend

Notes:
  - This script builds a PROPORTION SCAFFOLD, not a final mesh. You sculpt/retopo
    using the reference cubes as anatomical checkpoints.
  - Mixamo rig is added later via Mixamo web or Rigify; this scaffold only places
    empties where the rig's root/spine/neck/head should land.

Target specs (default = male):
  - Height:        1.80 m
  - Heads:         7.75
  - Head height:   ≈ 23.2 cm
Switch to female by passing --gender female (or editing GENDER below):
  - Height:        1.72 m
  - Heads:         7.5
  - Head height:   ≈ 22.9 cm
"""

import bpy
import sys
import argparse
from mathutils import Vector

# ══════════════════════════════════════════════════════════════════════════════
# CLI handling (headless)
# ══════════════════════════════════════════════════════════════════════════════
def parse_args():
    argv = sys.argv
    if "--" in argv:
        argv = argv[argv.index("--") + 1:]
    else:
        argv = []
    p = argparse.ArgumentParser()
    p.add_argument("--gender", choices=["male", "female"], default="male")
    p.add_argument("--output", default=None, help="optional .blend output path")
    return p.parse_args(argv)


ARGS = parse_args()
GENDER = ARGS.gender  # "male" | "female"

# ══════════════════════════════════════════════════════════════════════════════
# Proportions table (from docs/ART_PIPELINE.md · Paso 1)
# ══════════════════════════════════════════════════════════════════════════════
if GENDER == "male":
    HEIGHT_M = 1.80
    HEADS = 7.75
    SHOULDER_W = 0.48   # 2.1 × head
    HIP_W = 0.34        # 1.5 × head
    WAIST_W = 0.28      # 1.2 × head
else:
    HEIGHT_M = 1.72
    HEADS = 7.5
    SHOULDER_W = 0.39   # 1.7 × head
    HIP_W = 0.39        # 1.7 × head (curvy)
    WAIST_W = 0.22      # 0.95 × head

HEAD_H = HEIGHT_M / HEADS

# Landmarks along vertical axis (Z), measured from the floor (Z=0).
# Classic artistic landmarks at fractions of heads.
LANDMARKS = {
    "floor":          0.0,
    "ankle":          HEIGHT_M * 0.04,
    "knee":           HEIGHT_M * 0.26,
    "hip_pivot":      HEIGHT_M * 0.52,       # ~ Mixamo Hips bone position
    "navel":          HEIGHT_M * 0.60,
    "waist":          HEIGHT_M * 0.62,
    "solar_plexus":   HEIGHT_M * 0.68,
    "nipple":         HEIGHT_M * 0.73,
    "shoulder":       HEIGHT_M * 0.82,       # ~ Mixamo Shoulder bones
    "neck_base":      HEIGHT_M * 0.84,
    "chin":           HEIGHT_M * 0.87,
    "mouth":          HEIGHT_M * 0.90,
    "nose_tip":       HEIGHT_M * 0.92,
    "eye_line":       HEIGHT_M * 0.94,
    "brow":           HEIGHT_M * 0.955,
    "hair_line":      HEIGHT_M * 0.975,
    "crown":          HEIGHT_M,
}

# ══════════════════════════════════════════════════════════════════════════════
# Scene setup
# ══════════════════════════════════════════════════════════════════════════════
def wipe_scene():
    # Remove default cube / camera / light so we start clean
    for obj in list(bpy.data.objects):
        bpy.data.objects.remove(obj, do_unlink=True)
    # Also clear orphan data to avoid bloat
    for block in (bpy.data.meshes, bpy.data.materials, bpy.data.armatures,
                  bpy.data.cameras, bpy.data.lights):
        for item in list(block):
            if item.users == 0:
                block.remove(item)


def set_units_metric():
    scene = bpy.context.scene
    scene.unit_settings.system = 'METRIC'
    scene.unit_settings.scale_length = 1.0
    scene.unit_settings.length_unit = 'METERS'


def ensure_collection(name, parent=None):
    coll = bpy.data.collections.get(name)
    if coll is None:
        coll = bpy.data.collections.new(name)
        (parent or bpy.context.scene.collection).children.link(coll)
    return coll


# ══════════════════════════════════════════════════════════════════════════════
# Helpers
# ══════════════════════════════════════════════════════════════════════════════
def add_reference_cube(name, loc, size, collection):
    mesh = bpy.data.meshes.new(name + "_mesh")
    obj = bpy.data.objects.new(name, mesh)
    collection.objects.link(obj)
    # Build an 8-vertex cube programmatically
    sx, sy, sz = size
    verts = [
        (-sx / 2, -sy / 2, 0),
        (sx / 2, -sy / 2, 0),
        (sx / 2,  sy / 2, 0),
        (-sx / 2,  sy / 2, 0),
        (-sx / 2, -sy / 2, sz),
        (sx / 2, -sy / 2, sz),
        (sx / 2,  sy / 2, sz),
        (-sx / 2,  sy / 2, sz),
    ]
    faces = [(0, 1, 2, 3), (4, 5, 6, 7),
             (0, 1, 5, 4), (2, 3, 7, 6),
             (1, 2, 6, 5), (0, 3, 7, 4)]
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj.location = Vector(loc)
    obj.display_type = 'WIRE'
    obj.hide_select = False
    return obj


def add_empty(name, loc, collection, size=0.08):
    empty = bpy.data.objects.new(name, None)
    empty.empty_display_type = 'SPHERE'
    empty.empty_display_size = size
    empty.location = Vector(loc)
    collection.objects.link(empty)
    return empty


def add_camera_and_lights():
    # Silhouette camera — orthographic, side view
    cam_data = bpy.data.cameras.new("SilhouetteCam")
    cam_data.type = 'ORTHO'
    cam_data.ortho_scale = 2.2
    cam = bpy.data.objects.new("SilhouetteCam", cam_data)
    cam.location = (3.5, 0.0, HEIGHT_M / 2)
    cam.rotation_euler = (1.5708, 0.0, 1.5708)  # 90° X, 90° Z
    bpy.context.scene.collection.objects.link(cam)
    bpy.context.scene.camera = cam

    # Three-point light rig (key + fill + rim)
    key = bpy.data.lights.new("KeyLight", type='AREA')
    key.energy = 800
    key.size = 2.0
    key_obj = bpy.data.objects.new("KeyLight", key)
    key_obj.location = (2.5, -1.5, 2.5)
    key_obj.rotation_euler = (0.9, 0.6, 0.0)
    bpy.context.scene.collection.objects.link(key_obj)

    fill = bpy.data.lights.new("FillLight", type='AREA')
    fill.energy = 300
    fill.size = 3.0
    fill_obj = bpy.data.objects.new("FillLight", fill)
    fill_obj.location = (-2.5, -1.0, 2.0)
    fill_obj.rotation_euler = (0.9, -0.6, 0.0)
    bpy.context.scene.collection.objects.link(fill_obj)

    rim = bpy.data.lights.new("RimLight", type='AREA')
    rim.energy = 600
    rim.size = 1.5
    rim_obj = bpy.data.objects.new("RimLight", rim)
    rim_obj.location = (-1.0, 3.0, HEIGHT_M + 0.5)
    rim_obj.rotation_euler = (-0.8, 0.0, 0.0)
    bpy.context.scene.collection.objects.link(rim_obj)


# ══════════════════════════════════════════════════════════════════════════════
# Build scaffold
# ══════════════════════════════════════════════════════════════════════════════
def main():
    wipe_scene()
    set_units_metric()

    bpy.context.scene.render.engine = 'CYCLES'
    bpy.context.scene.cycles.device = 'GPU'

    # Collections
    coll_ref = ensure_collection(f"_Reference_{GENDER.upper()}")
    coll_landmarks = ensure_collection(f"_Landmarks_{GENDER.upper()}")
    coll_mesh = ensure_collection(f"_Mesh_{GENDER.upper()}")

    # ── Body-block references (for proportions / silhouette) ─────────────
    # Torso block
    torso_size = (SHOULDER_W, HIP_W * 0.6,
                  LANDMARKS["shoulder"] - LANDMARKS["hip_pivot"])
    add_reference_cube("Ref_Torso", (0, 0, LANDMARKS["hip_pivot"]),
                       torso_size, coll_ref)

    # Hip block
    hip_size = (HIP_W, HIP_W * 0.6, LANDMARKS["hip_pivot"] * 0.25)
    add_reference_cube("Ref_Hip", (0, 0, LANDMARKS["hip_pivot"] * 0.9),
                       hip_size, coll_ref)

    # Head block — sphere-ish cube, 1 head tall & wide
    head_size = (HEAD_H * 0.80, HEAD_H * 0.85, HEAD_H)
    add_reference_cube("Ref_Head", (0, 0, LANDMARKS["chin"]),
                       head_size, coll_ref)

    # Leg block (upper + lower as single for simplicity)
    leg_size = (HIP_W * 0.35, HIP_W * 0.35,
                LANDMARKS["hip_pivot"] - LANDMARKS["ankle"])
    add_reference_cube("Ref_LegL", (-HIP_W * 0.25, 0, LANDMARKS["ankle"]),
                       leg_size, coll_ref)
    add_reference_cube("Ref_LegR", (HIP_W * 0.25, 0, LANDMARKS["ankle"]),
                       leg_size, coll_ref)

    # Arms — A-pose, 45° down-outward
    arm_length = HEAD_H * 3.0
    # Left arm (viewer's right): from shoulder, going 45° down+out
    import math
    sin45 = math.sin(math.radians(45))
    cos45 = math.cos(math.radians(45))
    arm_offset_x = arm_length * sin45
    arm_offset_z = arm_length * cos45
    shoulder_z = LANDMARKS["shoulder"]

    arm_size = (HIP_W * 0.22, HIP_W * 0.22, arm_length)
    # Left arm
    add_reference_cube("Ref_ArmL",
                       (-(SHOULDER_W / 2 + arm_offset_x / 2), 0,
                        shoulder_z - arm_offset_z),
                       arm_size, coll_ref)
    # Right arm (mirror)
    add_reference_cube("Ref_ArmR",
                       (SHOULDER_W / 2 + arm_offset_x / 2, 0,
                        shoulder_z - arm_offset_z),
                       arm_size, coll_ref)

    # Set references to wireframe + transparent
    for obj in coll_ref.objects:
        obj.display_type = 'WIRE'
        obj.hide_select = False

    # ── Anatomical landmarks (empties for alignment) ─────────────────────
    for name, z in LANDMARKS.items():
        add_empty(f"LM_{name}", (0, 0, z), coll_landmarks)

    # Extra Mixamo-critical landmarks (L/R pairs)
    add_empty("LM_shoulder_L", (-SHOULDER_W / 2, 0, LANDMARKS["shoulder"]), coll_landmarks)
    add_empty("LM_shoulder_R", ( SHOULDER_W / 2, 0, LANDMARKS["shoulder"]), coll_landmarks)
    add_empty("LM_hip_L", (-HIP_W / 2, 0, LANDMARKS["hip_pivot"]), coll_landmarks)
    add_empty("LM_hip_R", ( HIP_W / 2, 0, LANDMARKS["hip_pivot"]), coll_landmarks)
    add_empty("LM_knee_L", (-HIP_W / 2, 0, LANDMARKS["knee"]), coll_landmarks)
    add_empty("LM_knee_R", ( HIP_W / 2, 0, LANDMARKS["knee"]), coll_landmarks)
    add_empty("LM_ankle_L", (-HIP_W / 2, 0, LANDMARKS["ankle"]), coll_landmarks)
    add_empty("LM_ankle_R", ( HIP_W / 2, 0, LANDMARKS["ankle"]), coll_landmarks)

    # ── Mixamo placeholder skeleton hint ─────────────────────────────────
    # We DO NOT create a full armature — Mixamo auto-rig does that after FBX upload.
    # But we leave a root empty at (0,0,0) so the artist aligns the mesh origin there.
    root = add_empty("LM_root_origin", (0, 0, 0), coll_landmarks, size=0.15)
    root.show_name = True

    add_camera_and_lights()

    # Sensible viewport defaults
    for area in bpy.context.screen.areas:
        if area.type == 'VIEW_3D':
            for space in area.spaces:
                if space.type == 'VIEW_3D':
                    space.clip_end = 1000.0
                    space.shading.type = 'SOLID'

    print(f"[WAR] Base humanoid scaffold for {GENDER.upper()} ready.")
    print(f"      Height={HEIGHT_M}m, heads={HEADS}, head_h={HEAD_H:.3f}m")
    print(f"      Shoulder_w={SHOULDER_W}m, hip_w={HIP_W}m, waist_w={WAIST_W}m")
    print("      Collections: _Reference, _Landmarks, _Mesh")
    print("      Next steps: sculpt/block-out inside _Mesh collection, using")
    print("                  the Reference cubes as proportional guides.")

    if ARGS.output:
        bpy.ops.wm.save_as_mainfile(filepath=ARGS.output)
        print(f"      Saved to {ARGS.output}")


if __name__ == "__main__":
    main()
