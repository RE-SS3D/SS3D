"""Blender: blender -b -t 1 --python Tools/export_firelock.py -- <SS3D-Art>/Assets/Models/Structures/Doors/Firelock.blend

Exports the external airlock (the SS3D-Art file calls it Firelock) to Assets/Art/Models/Structures/Doors/Firelock.fbx:
the `FirelockArmature` rig, the skinned `Firelock` mesh and its one action, baked as the clip `Open`. The door
sinks into the floor as it opens: latches flip, the top half telescopes into the bottom, then the stack drops
about 1.07 m, leaving a 9 cm lip. The modelers-kit room planes in the same file are dropped.

Unity side: AirlockSkinSetup.BuildExternalAirlock (SS3D.Systems.StructuralDamage.Editor) builds the prefab and
its Opening/Closing clips from this file.
"""
import sys
from pathlib import Path
import bpy

source = Path(sys.argv[sys.argv.index('--') + 1])
root = Path(__file__).resolve().parents[1]
destination = root / 'Assets/Art/Models/Structures/Doors/Firelock.fbx'

bpy.ops.wm.open_mainfile(filepath=str(source))
armature = bpy.data.objects['FirelockArmature']
mesh = bpy.data.objects['Firelock']
for obj in list(bpy.data.objects):
    if obj not in (armature, mesh):
        bpy.data.objects.remove(obj)

action = armature.animation_data.action
action.name = 'Open'
for other in list(bpy.data.actions):
    if other is not action:
        bpy.data.actions.remove(other)

scene = bpy.context.scene
scene.frame_start, scene.frame_end = (int(f) for f in action.frame_range)

bpy.ops.object.select_all(action='DESELECT')
armature.select_set(True)
mesh.select_set(True)
bpy.context.view_layer.objects.active = armature
bpy.ops.export_scene.fbx(
    filepath=str(destination), use_selection=True, object_types={'ARMATURE', 'MESH'},
    axis_forward='-Z', axis_up='Y', apply_scale_options='FBX_SCALE_UNITS',
    use_mesh_modifiers=False, mesh_smooth_type='FACE', add_leaf_bones=False, use_armature_deform_only=True,
    bake_anim=True, bake_anim_use_all_actions=False, bake_anim_use_nla_strips=False,
    bake_anim_force_startend_keying=True, bake_anim_simplify_factor=0.0, path_mode='STRIP')
print('EXPORTED', destination.relative_to(root))
