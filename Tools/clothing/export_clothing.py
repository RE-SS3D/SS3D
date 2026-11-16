"""Export job clothing from the SS3D-Art sources into rigged FBX sets for Unity.

Run headless (no UI, no Blender MCP needed):

    blender -b --python Tools/clothing/export_clothing.py -- [--art ../SS3D-Art-main] [--out Assets/Art/Models/Items/Clothing/Jobs] [--set Command ...]

Reads Tools/clothing/clothing_manifest.json. Every set is written as <out>/<set>.fbx holding
HumanArmature (from the HUMAN_BODY blend, the rig every in-game garment uses) plus, per item, a
skinned worn mesh named after the item and an optional static world mesh named <name>Folded.
Unity's ClothingImportSetup turns those into Items. A report of every exported mesh's bounds
is printed so a misplaced piece shows up without opening Blender.
"""

import argparse
import collections
import json
import os
import sys

import bmesh
import bpy
import numpy as np
from mathutils import Matrix, Vector
from mathutils import interpolate
from mathutils.bvhtree import BVHTree
from mathutils.kdtree import KDTree

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.dirname(os.path.dirname(HERE))

HB_REL = "Pulled from Google Drive/HUMAN_BODY_v4.0_rigged&Fixed.blend"
SYN_REL = "Pulled from Google Drive/Syndicate_Clothing (needs naming and exporting).blend"
DRIVE_REL = "Pulled from Google Drive/SS3D GoogleDrive-20261104T183245Z-1-001/SS3D GoogleDrive/Assets/Models/Items/Clothing"
FOLDED_REL = "Assets/Models/Items/Clothing/JumpsuitsFoldedTemporary.blend"
PALETTE = os.path.join(REPO, "Assets/Art/Textures/World/Palette05.png")
ARMATURE = "HumanArmature"
MORPH_KEYS = ["Female", "Breasts", "Fat", "Muscle", "Jaw Wide", "Jaw Thin"]  # the body's slider keys
FOLDED_TEMPLATE = "JumpsuitGreyFolded"
BODY = "00 Body - Human COMPLETE"


def parse_args():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    p = argparse.ArgumentParser()
    p.add_argument("--art", default=os.path.join(os.path.dirname(REPO), "SS3D-Art-main"))
    p.add_argument("--out", default=os.path.join(REPO, "Assets/Art/Models/Items/Clothing/Jobs"))
    p.add_argument("--set", action="append", help="export only these sets")
    return p.parse_args(argv)


# ---------------------------------------------------------------- scene helpers

def world_bounds(obj):
    pts = [obj.matrix_world @ v.co for v in obj.data.vertices]
    lo = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts)))
    hi = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
    return lo, hi


def link(obj):
    bpy.context.scene.collection.objects.link(obj)
    return obj


def bake_to_world(obj):
    """Unparents and applies the transform so mesh data is in world space at rest."""
    mw = obj.matrix_world.copy()
    obj.parent = None
    obj.matrix_world = mw
    shaping = [m for m in obj.modifiers if m.type != "ARMATURE"]
    if shaping:
        # Geometry Nodes or Solidify build the visible shape (the Syndicate belt is a scaled cube without
        # them); bake that shape into the mesh before the modifiers go. Shape keys can't survive this.
        for m in obj.modifiers:
            m.show_viewport = m.type != "ARMATURE"
        if obj.data.shape_keys:
            obj.shape_key_clear()
        bpy.context.view_layer.update()
        evaluated = obj.evaluated_get(bpy.context.evaluated_depsgraph_get())
        # The baked mesh keeps each vertex's group weights, and the object keeps the group names.
        baked = bpy.data.meshes.new_from_object(evaluated)
        keep_render_uv(baked, obj.data)
        if not baked.vertices:
            raise RuntimeError(f"{obj.name}: modifiers left no geometry")
        obj.data = baked
    for mod in list(obj.modifiers):
        obj.modifiers.remove(mod)
    me = obj.data
    if me.shape_keys:
        for kb in me.shape_keys.key_blocks:
            for i, v in enumerate(kb.data):
                v.co = mw @ v.co
    me.transform(mw)
    obj.matrix_world = Matrix.Identity(4)
    return obj


def copy_object(src, name):
    obj = src.copy()
    obj.data = src.data.copy()
    obj.name = name
    obj.data.name = name
    for coll in list(obj.users_collection):
        coll.objects.unlink(obj)
    link(obj)
    return bake_to_world(obj)


def join(objs, name):
    bpy.ops.object.select_all(action="DESELECT")
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    bpy.ops.object.join()
    obj = bpy.context.view_layer.objects.active
    obj.name = name
    obj.data.name = name
    return obj


def import_drive(drive_root, spec, name):
    path, obj_name = spec.rsplit("#", 1)
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=os.path.join(drive_root, path))
    new = set(bpy.data.objects) - before
    picked = None
    for o in new:
        if o.name.split(".")[0] == obj_name or o.name == obj_name:
            picked = o
    if picked is None:
        raise RuntimeError(f"{name}: no object '{obj_name}' in {path} (found {[o.name for o in new]})")
    picked.name = name
    picked.data = picked.data.copy()
    picked.data.name = name
    bake_to_world(picked)
    for o in new:
        if o is not picked:
            bpy.data.objects.remove(o, do_unlink=True)
    for g in list(picked.vertex_groups):
        picked.vertex_groups.remove(g)
    return picked


def keep_render_uv(mesh, source):
    """Keep only the UV map the source renders with. Geometry Nodes leave extra maps (GeometrySource11),
    an evaluated copy can list one first, and Unity reads the first map as the palette UVs."""
    render = next((l.name for l in source.uv_layers if l.active_render), None)
    for layer in list(mesh.uv_layers):
        if render is not None and layer.name != render:
            mesh.uv_layers.remove(layer)


def evaluated_world(obj, name):
    """A static copy of what the object shows: every modifier applied (its armature at rest), in world space."""
    bpy.context.view_layer.update()
    evaluated = obj.evaluated_get(bpy.context.evaluated_depsgraph_get())
    mesh = bpy.data.meshes.new_from_object(evaluated)
    mesh.transform(evaluated.matrix_world)
    mesh.name = name
    keep_render_uv(mesh, obj.data)
    copy = bpy.data.objects.new(name, mesh)
    return link(copy)


def import_syndicate(syn_path, spec, name, hb_body):
    """The Syndicate file poses its garments with its own rig, 4 m behind the origin, and sites the belt
    as a static object in that space. Load the whole file, show each piece as posed at rest, then move
    the lot onto the HUMAN_BODY body by the offset between the two bodies."""
    names = spec.split("+")
    with bpy.data.libraries.load(syn_path) as (src, dst):
        dst.objects = list(src.objects)
    loaded = [link(o) for o in dst.objects if o is not None]
    for o in loaded:
        if o.type == "ARMATURE":
            o.data.pose_position = "REST"
    by_name = {o.name: o for o in loaded}
    body = evaluated_world(by_name["Human"], "SyndicateBody")
    (slo, shi), (hlo, hhi) = world_bounds(body), world_bounds(hb_body)
    offset = (hlo + hhi) / 2 - (slo + shi) / 2
    parts = []
    for part in names:
        o = evaluated_world(by_name[part], part + "_baked")
        o.data.transform(Matrix.Translation(offset))
        parts.append(o)
    for o in loaded + [body]:
        bpy.data.objects.remove(o, do_unlink=True)
    return join(parts, name) if len(parts) > 1 else _rename(parts[0], name)


def _rename(obj, name):
    obj.name = name
    obj.data.name = name
    return obj


def ungrid(obj):
    """Drive sheets lay pieces out on a 1 m grid in x and y; snap the piece back over the body."""
    lo, hi = world_bounds(obj)
    c = (lo + hi) / 2
    shift = Vector((-round(c.x), -round(c.y), 0.0))
    obj.data.transform(Matrix.Translation(shift))


# ---------------------------------------------------------------- weights and morphs

def transfer_weights(obj, ref):
    for g in ref.vertex_groups:
        if g.name not in obj.vertex_groups:
            obj.vertex_groups.new(name=g.name)
    mod = obj.modifiers.new("WeightTransfer", "DATA_TRANSFER")
    mod.object = ref
    mod.use_vert_data = True
    mod.data_types_verts = {"VGROUP_WEIGHTS"}
    mod.vert_mapping = "POLYINTERP_NEAREST"
    mod.layers_vgroup_select_src = "ALL"
    mod.layers_vgroup_select_dst = "NAME"
    apply_modifier(obj, mod)


def bind_to_bone(obj, bone):
    g = obj.vertex_groups.new(name=bone)
    g.add(list(range(len(obj.data.vertices))), 1.0, "REPLACE")


def apply_modifier(obj, mod):
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.modifier_apply(modifier=mod.name)


def tidy_weights(obj):
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.vertex_group_clean(group_select_mode="ALL", limit=0.01)
    bpy.ops.object.vertex_group_limit_total(group_select_mode="ALL", limit=4)
    bpy.ops.object.vertex_group_normalize_all(lock_active=False)
    unweighted = [v for v in obj.data.vertices if not v.groups]
    if unweighted:
        # Source files leave the odd stray vertex unweighted; it would stay at the origin in Unity.
        weighted = [v for v in obj.data.vertices if v.groups]
        if not weighted:
            raise RuntimeError(f"{obj.name}: no vertex has a bone weight")
        tree = KDTree(len(weighted))
        for i, v in enumerate(weighted):
            tree.insert(v.co, i)
        tree.balance()
        for v in unweighted:
            _, i, _ = tree.find(v.co)
            for g in weighted[i].groups:
                obj.vertex_groups[g.group].add([v.index], g.weight, "REPLACE")
        print(f"CLOTHING note {obj.name}: {len(unweighted)} unweighted vertices took their neighbour's weights")


def bake_morphs(obj, body, keys):
    """Follow the body sliders: each vertex takes the body key's offset at the nearest point of the body surface.

    A direct transfer instead of a Surface Deform bake: applying that modifier as a shape key in background
    mode recorded empty keys. Reference Jumpsuit's own keys are unusable as a source (every key carries the
    same 1.48 m stray offset), so the body is the source; its keys move 1-6 cm.
    """
    kbs = body.data.shape_keys.key_blocks
    basis = kbs["Basis"]
    mw = body.matrix_world
    rest = [mw @ basis.data[i].co for i in range(len(basis.data))]
    bm = bmesh.new()
    bm.from_mesh(body.data)
    bmesh.ops.triangulate(bm, faces=bm.faces[:])
    bm.verts.ensure_lookup_table()
    for v in bm.verts:
        v.co = rest[v.index]
    bm.faces.ensure_lookup_table()
    tree = BVHTree.FromBMesh(bm)
    # Per garment vertex: the body triangle it sits over and the barycentric weights of its corners.
    hits = []
    for v in obj.data.vertices:
        loc, _, face_index, _ = tree.find_nearest(v.co)
        face = bm.faces[face_index]
        corners = [c.co for c in face.verts]
        weights = interpolate.poly_3d_calc(corners, loc)
        hits.append(([c.index for c in face.verts], weights))
    bm.free()

    if obj.data.shape_keys is None:
        obj.shape_key_add(name="Basis", from_mix=False)
    for key in keys:
        if key not in kbs:
            continue
        moved = kbs[key].data
        delta = [(mw @ moved[i].co) - rest[i] for i in range(len(rest))]
        block = obj.shape_key_add(name=key, from_mix=False)
        for i, (ids, weights) in enumerate(hits):
            offset = Vector((0.0, 0.0, 0.0))
            for corner, w in zip(ids, weights):
                offset += delta[corner] * w
            block.data[i].co = obj.data.vertices[i].co + offset


FIT_MARGIN = 0.006
FIT_REGION_Z = (0.75, 1.30)   # chest to hips; the neck hem and cuffs tuck into the skin on purpose
FIT_REGION_X = 0.25           # torso only, not the arms


def fit_over_body(obj, body):
    """Push torso vertices that sit inside or flush with the body out to FIT_MARGIN, so less body shows
    through. The Drive jumpsuits are cut tighter than the body the game uses: 86 torso body vertices poked
    through the Captain's chest, 26 after this, against 18 for the in-game grey jumpsuit. Every shape key
    moves by the same amount, so the slider deltas are unchanged.

    Pushing from the body's side instead (move the garment face above each poking body vertex) made it
    worse: the nearest face is often an inner hem or cuff, and the pushes pile up."""
    bm = bmesh.new()
    bm.from_mesh(body.data)
    bm.transform(body.matrix_world)
    tree = BVHTree.FromBMesh(bm)
    keys = obj.data.shape_keys.key_blocks if obj.data.shape_keys else []
    moved = 0
    for v in obj.data.vertices:
        p = v.co
        if not (FIT_REGION_Z[0] < p.z < FIT_REGION_Z[1] and abs(p.x) < FIT_REGION_X):
            continue
        loc, normal, _, _ = tree.find_nearest(p)
        depth = (p - loc).dot(normal)
        if depth >= FIT_MARGIN:
            continue
        push = normal * (FIT_MARGIN - depth)
        for kb in keys:
            kb.data[v.index].co += push
        v.co += push
        moved += 1
    bm.free()
    return moved


# ---------------------------------------------------------------- folded world mesh

class Palette:
    def __init__(self, path):
        img = bpy.data.images.load(path)
        self.w, self.h = img.size
        self.px = np.array(img.pixels[:]).reshape(self.h, self.w, 4)[:, :, :3]

    def at(self, uv):
        x = min(self.w - 1, max(0, int(uv[0] * self.w)))
        y = min(self.h - 1, max(0, int(uv[1] * self.h)))
        return self.px[y, x]


def face_uv(bm, face):
    layer = bm.loops.layers.uv.active
    c = Vector((0.0, 0.0))
    for loop in face.loops:
        c += loop[layer].uv
    return c / len(face.loops)


def dominant_uvs(obj, palette):
    """Colour tiers of a mesh by area, each with a UV that samples it, darkest last."""
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    area = collections.Counter()
    uv_of = {}
    for f in bm.faces:
        c = face_uv(bm, f)
        key = tuple(np.round(palette.at(c), 2))
        area[key] += f.calc_area()
        uv_of.setdefault(key, c.copy())
    return [(np.array(k), uv_of[k]) for k, _ in area.most_common()]


def auto_folded(template, worn, name, palette):
    """The grey folded pile in the worn garment's colours: its main, second and darkest tiers.

    Rebuilds the in-game Security pile exactly; Engineering and Botany were picked by eye, so a
    set can override the result with an explicit folded source when the auto colours read wrong.
    """
    skin = [c for c, _ in dominant_uvs(bpy.data.objects[BODY], palette)[:3]]
    # Skip skin showing through a neckline (the medical V-neck); white scrubs keep their white.
    target = [t for t in dominant_uvs(worn, palette)
              if min(np.abs(t[0] - c).max() for c in skin) > 0.08]
    if not target:
        raise RuntimeError(f"{name}: worn mesh has no colour to fold with")
    main, second = target[0], target[1] if len(target) > 1 else target[0]
    darkest = min(target[:4], key=lambda t: t[0].sum())
    tiers = [t for t in dominant_uvs(template, palette)]
    mapping = {}
    for rank, (colour, _) in enumerate(tiers):
        if (abs(colour - 0.8) < 0.02).all():
            continue  # labels and buttons stay white
        mapping[tuple(colour)] = [main, second, darkest][min(rank, 2)][1]
    obj = template.copy()
    obj.data = template.data.copy()
    obj.name = obj.data.name = name
    link(obj)
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    layer = bm.loops.layers.uv.active
    for f in bm.faces:
        c = face_uv(bm, f)
        uv = mapping.get(tuple(np.round(palette.at(c), 2)))
        if uv is None:
            continue
        for loop in f.loops:
            loop[layer].uv = loop[layer].uv - c + uv
    bm.to_mesh(obj.data)
    return obj


# ---------------------------------------------------------------- main

def single_material(obj, material):
    keep_render_uv(obj.data, obj.data)
    # Garments are palette-textured; leftover vertex colours (Geometry Nodes sources carry alpha 0..1) only
    # risk tinting or fading them in the toon shader.
    for attr in list(obj.data.color_attributes):
        obj.data.color_attributes.remove(attr)
    obj.data.materials.clear()
    obj.data.materials.append(material)
    for poly in obj.data.polygons:
        poly.material_index = 0


def build_item(item, ctx):
    name = item["name"]
    kind, spec = item["src"].split(":", 1)
    if kind == "hb":
        worn = copy_object(bpy.data.objects[spec], name)
    elif kind == "drive":
        worn = import_drive(ctx["drive"], spec, name)
    elif kind == "syn":
        worn = import_syndicate(ctx["syn"], spec, name, bpy.data.objects[BODY])
    else:
        raise RuntimeError(f"{name}: unknown source kind {kind}")
    if item.get("grid"):
        ungrid(worn)

    weights = item["weights"]
    if weights.startswith("transfer:"):
        for g in list(worn.vertex_groups):
            worn.vertex_groups.remove(g)
        transfer_weights(worn, bpy.data.objects[weights.split(":", 1)[1]])
    elif weights.startswith("bone:"):
        for g in list(worn.vertex_groups):
            worn.vertex_groups.remove(g)
        bind_to_bone(worn, weights.split(":", 1)[1])
    elif weights != "keep":
        raise RuntimeError(f"{name}: unknown weights {weights}")
    tidy_weights(worn)
    if item.get("morphs"):
        if worn.data.shape_keys:
            worn.shape_key_clear()
        bake_morphs(worn, bpy.data.objects[BODY], MORPH_KEYS)
        moved = fit_over_body(worn, bpy.data.objects[BODY])
        if moved:
            print(f"CLOTHING note {name}: pushed {moved} vertices clear of the body")
    single_material(worn, ctx["material"])

    folded = None
    source = item.get("folded")
    if source == "auto":
        folded = auto_folded(ctx["folded_template"], worn, name + "Folded", ctx["palette"])
    elif source:
        fkind, fspec = source.split(":", 1)
        folded = (copy_object(bpy.data.objects[fspec], name + "Folded") if fkind == "hb"
                  else import_drive(ctx["drive"], fspec, name + "Folded"))
        for g in list(folded.vertex_groups):
            folded.vertex_groups.remove(g)
        if folded.data.shape_keys:
            folded.shape_key_clear()
    if folded is not None:
        single_material(folded, ctx["material"])

    arm = bpy.data.objects[ARMATURE]
    worn.parent = arm
    worn.matrix_parent_inverse = arm.matrix_world.inverted()
    mod = worn.modifiers.new("Armature", "ARMATURE")
    mod.object = arm
    return [o for o in (worn, folded) if o is not None]


def export_set(set_name, objs, out_dir):
    os.makedirs(out_dir, exist_ok=True)
    path = os.path.join(out_dir, set_name + ".fbx")
    bpy.ops.object.select_all(action="DESELECT")
    bpy.data.objects[ARMATURE].select_set(True)
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = bpy.data.objects[ARMATURE]
    bpy.ops.export_scene.fbx(
        filepath=path, use_selection=True, object_types={"ARMATURE", "MESH"},
        use_mesh_modifiers=False, mesh_smooth_type="FACE", add_leaf_bones=False,
        bake_anim=False, use_armature_deform_only=False, apply_scale_options="FBX_SCALE_UNITS")
    return path


def main():
    args = parse_args()
    manifest = json.load(open(os.path.join(HERE, "clothing_manifest.json")))
    hb = os.path.join(args.art, HB_REL)
    bpy.ops.wm.open_mainfile(filepath=hb)
    for o in bpy.data.objects:  # the rig must be at rest for world-space bakes
        if o.type == "ARMATURE":
            o.data.pose_position = "REST"
    with bpy.data.libraries.load(os.path.join(args.art, FOLDED_REL)) as (src, dst):
        dst.objects = [FOLDED_TEMPLATE]
    template = dst.objects[0]
    material = bpy.data.materials.get("Palette05") or bpy.data.materials.new("Palette05")
    ctx = {"drive": os.path.join(args.art, DRIVE_REL), "syn": os.path.join(args.art, SYN_REL),
           "palette": Palette(PALETTE), "folded_template": template, "material": material}

    report = []
    for set_name, items in manifest["sets"].items():
        if args.set and set_name not in args.set:
            continue
        objs = []
        for item in items:
            built = build_item(item, ctx)
            objs.extend(built)
            for o in built:
                lo, hi = world_bounds(o)
                keys = [k.name for k in o.data.shape_keys.key_blocks] if o.data.shape_keys else []
                report.append(f"{set_name:12s} {o.name:24s} v={len(o.data.vertices):5d} "
                              f"min=({lo.x:+.3f},{lo.y:+.3f},{lo.z:+.3f}) max=({hi.x:+.3f},{hi.y:+.3f},{hi.z:+.3f}) "
                              f"groups={len(o.vertex_groups)} keys={keys[1:]}")
        path = export_set(set_name, objs, args.out)
        report.append(f"WROTE {os.path.relpath(path, REPO)}")
    print("\n".join("CLOTHING " + r for r in report))


if __name__ == "__main__":
    main()
