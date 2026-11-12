"""Blender: blender -b -t 1 --python Tools/export_item_meshes.py -- <SS3D-Art root> [name ...]

Exports Playtest 1 item meshes from a local SS3D-Art checkout into Assets/Art. Each entry keeps only
the named objects, so a source's studio planes, alternate designs (toy guns, silencers) and siblings
in the same .blend are left out. Every kept object is baked into world space, shifted so the main
object's bounds centre is the origin, and unparented: Unity sees one child per mesh, all sharing the
main object's space. Shape keys are flattened to the mix the source shows. Objects listed under
`join` are merged into the main mesh. Palette UVs and material slots are untouched.

Unity-side recipes read these FBXs: SS3D -> Combat -> Run Content Prefab Recipes (guns, stun baton)
and SS3D -> Health -> Run Medical Item Recipes (gauze, defibrillator).
"""
import sys
from pathlib import Path
import bpy
from mathutils import Vector

# name: (source .blend under Assets/Models, main object, extra objects kept as their own mesh, objects joined into main)
ITEMS = {
    'C20R': ('Items/Weapons/Ranged/Physical/Rifles/C20R', 'C20R', ['C20RMagazine'], []),
    'Stechkin': ('Items/Weapons/Ranged/Physical/Handguns/Stechkin', 'Stechkin', ['StechkinMagazine'], ['StechkinSlide']),
    'SecurityPistol': ('Items/Weapons/Ranged/Physical/Handguns/SecurityPistol', 'SecurityPistol', ['SecurityPistolMagazine'], []),
    'StunBaton': ('Items/Weapons/Melee/Blunt/StunBaton', 'StunBaton', [], []),
    'Gauze': ('Items/Consumable/Chemical/Medications/Bandages', 'Gauze', [], []),
    'PortableDefibrillator': ('Items/Functional/Tools/Medical/PortableDefibrillator', 'PortableDefibrillator', [],
                              ['PortableDefibrillatorPaddle']),
}

args = sys.argv[sys.argv.index('--') + 1:]
source = Path(args[0])
wanted = args[1:] or list(ITEMS)
root = Path(__file__).resolve().parents[1]


def flatten(obj):
    """Applies the shape-key mix the source shows (a loaded magazine, a closed slide); Unity would import
    a mesh with shape keys as a skinned mesh."""
    if obj.data.shape_keys:
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.shape_key_remove(all=True, apply_mix=True)


def bake(obj, centre):
    """Writes world-space geometry relative to centre into the mesh and clears the object's transform."""
    world = [obj.matrix_world @ v.co for v in obj.data.vertices]
    obj.parent = None
    obj.matrix_world.identity()
    for vertex, position in zip(obj.data.vertices, world):
        vertex.co = position - centre


for name in wanted:
    relative, main_name, extras, joins = ITEMS[name]
    bpy.ops.wm.open_mainfile(filepath=str(source / 'Assets/Models' / (relative + '.blend')))
    main = bpy.data.objects[main_name]
    # Mesh data shared with an unexported object (a toy gun) must not be rewritten under it.
    for obj in [main] + [bpy.data.objects[n] for n in extras + joins]:
        obj.data = obj.data.copy()
        flatten(obj)
    points = [main.matrix_world @ v.co for v in main.data.vertices]
    centre = Vector([(min(p[i] for p in points) + max(p[i] for p in points)) / 2 for i in range(3)])
    kept = [main] + [bpy.data.objects[n] for n in extras]
    joined = [bpy.data.objects[n] for n in joins]
    for obj in kept + joined:
        bake(obj, centre)
    bpy.ops.object.select_all(action='DESELECT')
    if joined:
        for obj in [main] + joined:
            obj.select_set(True)
        bpy.context.view_layer.objects.active = main
        bpy.ops.object.join()
    bpy.ops.object.select_all(action='DESELECT')
    for obj in kept:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = main
    destination = root / 'Assets/Art/Models' / (str(Path(relative).parent / name) + '.fbx')
    destination.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.export_scene.fbx(filepath=str(destination), use_selection=True, object_types={'MESH'},
        axis_forward='-Z', axis_up='Y', apply_unit_scale=True, bake_space_transform=True,
        use_mesh_modifiers=True, add_leaf_bones=False, bake_anim=False, path_mode='STRIP')
    print('EXPORTED', destination.relative_to(root))
