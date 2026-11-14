"""Blender: blender -b -t 1 --python Tools/export_syndicate_bomb.py -- <Syndicate_Bomb.blend>

Exports the Syndicate bomb and its delivery beacon into Assets/Art/Models/Items/Weapons/Explosive/.

- SyndicateBomb.fbx: the contributed model (object `Syndicate_Bomb.001`), baked into world space and
  shifted so its bounds centre is the origin. The modelers-kit room planes in the same file are dropped.
- SyndicateBombBeacon.fbx: a placeholder handheld beacon generated here (a dark box, a stub antenna and a
  red lamp), until someone models one. Its faces take their colours from the bomb's own Palette05 UVs:
  the body from the bomb's casing, the lamp from its red core.

Unity side: SS3D -> Structural Damage -> Run Content Prefab Recipes (SyndicateBombPrefabSetup).
"""
import sys
from pathlib import Path
import bmesh
import bpy
from mathutils import Vector

source = Path(sys.argv[sys.argv.index('--') + 1])
root = Path(__file__).resolve().parents[1]
out = root / 'Assets/Art/Models/Items/Weapons/Explosive'

bpy.ops.wm.open_mainfile(filepath=str(source))
bomb = bpy.data.objects['Syndicate_Bomb.001']
for obj in list(bpy.data.objects):
    if obj is not bomb:
        bpy.data.objects.remove(obj)


def bake(obj):
    """Writes world-space geometry relative to the bounds centre into the mesh and clears the transform."""
    world = [obj.matrix_world @ v.co for v in obj.data.vertices]
    centre = Vector([(min(p[i] for p in world) + max(p[i] for p in world)) / 2 for i in range(3)])
    obj.parent = None
    obj.matrix_world.identity()
    for vertex, position in zip(obj.data.vertices, world):
        vertex.co = position - centre


def export(obj, name):
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    destination = out / (name + '.fbx')
    bpy.ops.export_scene.fbx(filepath=str(destination), use_selection=True, object_types={'MESH'},
        axis_forward='-Z', axis_up='Y', apply_unit_scale=True, bake_space_transform=True,
        use_mesh_modifiers=True, add_leaf_bones=False, bake_anim=False, path_mode='STRIP')
    print('EXPORTED', destination.relative_to(root))


def palette_uvs(obj):
    """(dark casing UV, red core UV) sampled from the bomb's faces against its palette texture."""
    image = next(n.image for m in obj.data.materials if m and m.node_tree
                 for n in m.node_tree.nodes if n.type == 'TEX_IMAGE' and n.image)
    width, height = image.size
    pixels = image.pixels[:]
    uv_layer = obj.data.uv_layers.active.data
    dark = red = None
    for poly in obj.data.polygons:
        uv = sum((uv_layer[i].uv for i in poly.loop_indices), Vector((0, 0))) / poly.loop_total
        x = min(width - 1, max(0, int(uv.x * width)))
        y = min(height - 1, max(0, int(uv.y * height)))
        r, g, b = pixels[(y * width + x) * 4:(y * width + x) * 4 + 3]
        if red is None and r > 0.35 and g < 0.12 and b < 0.12:
            red = uv.copy()
        if dark is None and r + g + b < 0.3:
            dark = uv.copy()
    if dark is None or red is None:
        raise RuntimeError('palette colours not found on the bomb')
    return dark, red


bake(bomb)
bomb.name = 'SyndicateBomb'
bomb.data.name = 'SyndicateBomb'
dark, red = palette_uvs(bomb)
export(bomb, 'SyndicateBomb')

# Placeholder beacon: 6 x 3 x 10 cm body, a stub antenna and a red lamp on top (Blender Z up).
mesh = bpy.data.meshes.new('SyndicateBombBeacon')
beacon = bpy.data.objects.new('SyndicateBombBeacon', mesh)
bpy.context.scene.collection.objects.link(beacon)
beacon.data.materials.append(bomb.data.materials[0])
bm = bmesh.new()
uv = bm.loops.layers.uv.new('UVMap')


def box(size, centre, colour):
    made = bmesh.ops.create_cube(bm, size=1.0)
    verts = made['verts']
    bmesh.ops.scale(bm, vec=Vector(size), verts=verts)
    bmesh.ops.translate(bm, vec=Vector(centre), verts=verts)
    faces = {f for v in verts for f in v.link_faces}
    for face in faces:
        for loop in face.loops:
            loop[uv].uv = colour


box((0.06, 0.03, 0.10), (0, 0, 0.05), dark)
box((0.008, 0.008, 0.06), (0.018, 0, 0.13), dark)
box((0.022, 0.016, 0.012), (-0.01, 0, 0.106), red)
bm.to_mesh(mesh)
bm.free()
bake(beacon)
export(beacon, 'SyndicateBombBeacon')
