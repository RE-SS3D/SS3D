"""Blender: -b -t 1 --python Tools/export_energy_weapons.py -- <SS3D-Art root>.
Export only the named weapon mesh, excluding the source's studio and alternate designs.
Bake authored scale, centre the mesh, and keep palette UVs/material slots intact.
"""
import sys
from pathlib import Path
import bpy
from mathutils import Vector

source = Path(sys.argv[sys.argv.index('--') + 1])
root = Path(__file__).resolve().parents[1]
for category, name in [('EnergyRifles', 'IonRifle'), ('EnergyRifles', 'LaserRifle'), ('EnergyHandguns', 'Taser')]:
    relative = Path('Models/Items/Weapons/Ranged/Energy') / category / (name + '.blend')
    bpy.ops.wm.open_mainfile(filepath=str(source / 'Assets' / relative))
    obj = bpy.data.objects[name]
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    # World-space geometry retains source proportions without its display pedestal offset.
    vertices = [obj.matrix_world @ v.co for v in obj.data.vertices]
    centre = Vector([(min(v[i] for v in vertices) + max(v[i] for v in vertices)) / 2 for i in range(3)])
    obj.parent = None
    obj.matrix_world.identity()
    for vertex, world in zip(obj.data.vertices, vertices):
        vertex.co = world - centre
    destination = root / 'Assets/Art' / relative.with_suffix('.fbx')
    destination.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.export_scene.fbx(filepath=str(destination), use_selection=True, object_types={'MESH'},
        axis_forward='-Z', axis_up='Y', apply_unit_scale=True, bake_space_transform=True,
        use_mesh_modifiers=True, add_leaf_bones=False, bake_anim=False, path_mode='STRIP')
    print('EXPORTED', destination)
