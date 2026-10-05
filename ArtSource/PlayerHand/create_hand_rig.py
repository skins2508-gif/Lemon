import bpy
from mathutils import Quaternion
from pathlib import Path
name = 'Lemon_RightHand_Rig'
if bpy.data.objects.get(name):
    raise RuntimeError('Rig already exists; refusing to overwrite')
arm = bpy.data.armatures.new(name)
rig = bpy.data.objects.new(name, arm)
bpy.context.scene.collection.objects.link(rig)
for obj in bpy.context.selected_objects:
    obj.select_set(False)
rig.select_set(True)
bpy.context.view_layer.objects.active = rig
rig.show_in_front = True
arm.display_type = 'OCTAHEDRAL'
bpy.ops.object.mode_set(mode='EDIT')
def bone(name, head, tail, parent=None, connected=False):
    b = arm.edit_bones.new(name)
    b.head, b.tail = head, tail
    if parent:
        b.parent = arm.edit_bones[parent]
        b.use_connect = connected
    return b
bone('Wrist.R', (0,-0.055,0), (0,0,0))
bone('Palm.R', (0,0,0), (0,0.085,0), 'Wrist.R', True)
fingers = {
 'Thumb': [(-0.025,0.018,0),(-0.055,0.037,0),(-0.075,0.061,0),(-0.083,0.086,0)],
 'Index': [(-0.031,0.082,0),(-0.035,0.121,0),(-0.036,0.147,0),(-0.036,0.168,0)],
 'Middle': [(-0.008,0.09,0),(-0.008,0.133,0),(-0.008,0.163,0),(-0.008,0.185,0)],
 'Ring': [(0.015,0.084,0),(0.018,0.124,0),(0.019,0.151,0),(0.019,0.172,0)],
 'Pinky': [(0.035,0.07,0),(0.043,0.102,0),(0.046,0.123,0),(0.048,0.141,0)]}
for finger, points in fingers.items():
    parent = 'Palm.R'
    for i in range(3):
        n = f'{finger}.{i+1:02d}.R'
        bone(n, points[i], points[i+1], parent, i>0)
        parent = n
bpy.ops.object.mode_set(mode='OBJECT')
rig['purpose'] = 'First-person right hand skeleton for Lemon x-ray gesture. Mesh and skin weights not yet attached.'
rig['controls'] = 'Pose Mode: rotate finger segments locally. Thumb + Index form the viewing aperture.'
rig['units'] = 'Meters; wrist-to-middle-fingertip 0.185 m. Fit to final hand mesh before skinning.'
bpy.context.scene.unit_settings.system = 'METRIC'
for area in bpy.context.screen.areas:
    if area.type == 'VIEW_3D':
        area.spaces.active.region_3d.view_rotation = Quaternion((1,0,0,0))
        area.spaces.active.region_3d.view_location = (0,0.065,0)
        area.spaces.active.region_3d.view_distance = 0.4
        area.spaces.active.region_3d.view_perspective = 'ORTHO'
assert len(arm.bones) == 17
for f in fingers:
    assert arm.bones[f'{f}.03.R'].parent.name == f'{f}.02.R'
bpy.ops.wm.save_as_mainfile(filepath='D:/Unity/My project/ArtSource/PlayerHand/Lemon_RightHand_Rig.blend', copy=True)
Path('D:/Unity/My project/ArtSource/PlayerHand/validation.txt').write_text('PASS: 17 bones; Wrist/Palm + 5 fingers x 3 segments. Parent chains checked. Original Unity assets untouched. No mesh or animation yet.',encoding='utf8')
print('LEMON_HAND_RIG_CREATED: 17 bones, saved separate blend copy')
