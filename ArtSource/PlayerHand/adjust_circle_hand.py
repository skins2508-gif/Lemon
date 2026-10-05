import bpy, math, json
from mathutils import Vector,Quaternion
from pathlib import Path
out=Path('D:/Unity/My project/ArtSource/PlayerHand')
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.fbx(filepath='D:/Unity/My project/Assets/_3DStealthGame/Art/Models/Characters/Players/JohnLemon_Model.fbx')
rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE');mesh=next(o for o in bpy.context.scene.objects if o.type=='MESH')
rig.animation_data_clear()
for p in rig.pose.bones:p.matrix_basis.identity()
original={v.index:[(g.group,g.weight) for g in v.groups] for v in mesh.data.vertices}
hand=mesh.vertex_groups['RightHand'];hi=hand.index
rest_positions={v.index:v.co.copy() for v in mesh.data.vertices}
modified_vertices=[]
for v in mesh.data.vertices:
 w=next((g.weight for g in v.groups if g.group==hi),0)
 x,y,z=v.co
 if w<=0 or x>-67.4:continue
 label='Outer' if z>3.6 else ('Inner' if z<.9 else 'Middle')
 if label=='Middle':continue
 root=-68.6 if label=='Outer' else -68.8
 center=4.85 if label=='Outer' else -.35
 blend=max(0,min(1,(root+.7-x)/1.2))
 v.co.x=x-.8*max(0,root-x)
 v.co.y=79+(y-79)*(1-.42*blend)
 v.co.z=center+(z-center)*(1-.42*blend)
 modified_vertices.append(v.index)
before=len(rig.data.bones)
bpy.context.view_layer.objects.active=rig;bpy.ops.object.mode_set(mode='EDIT')
# Bone coordinates follow the three existing mesh lobes; no new fingers or geometry.
def add(n,h,t,parent):
 b=rig.data.edit_bones.new(n);b.head=h;b.tail=t;b.parent=rig.data.edit_bones[parent];return b
for label,z,x in [('Outer',4.85,-68.6),('Inner',-.35,-68.8)]:
 add('Sight'+label+'Base.R',(x,79,z),(x-2.88,79,z),'RightHand')
 add('Sight'+label+'Tip.R',(x-2.88,79,z),(x-5.58,79,z),'Sight'+label+'Base.R')
add('SightMiddle.R',(-69.5,79,2.2),(-72.6,79,2.2),'RightHand')
bpy.ops.object.mode_set(mode='OBJECT')
newnames=[b.name for b in rig.data.bones if b.name.startswith('Sight')]
groups={n:mesh.vertex_groups.new(name=n) for n in newnames}
changed=[]
for v in mesh.data.vertices:
 w=next((g.weight for g in v.groups if g.group==hi),0)
 if w<=0 or v.co.x>-67.4:continue
 x,y,z=rest_positions[v.index]
 label='Outer' if z>3.6 else ('Inner' if z<.9 else 'Middle')
 root=-68.6 if label=='Outer' else (-68.8 if label=='Inner' else -69.5)
 influence=max(0,min(1,(root+1.1-x)/1.4))
 if influence==0:continue
 if label=='Middle':dist={'SightMiddle.R':1}
 else:
  tip=max(0,min(1,(root-1.1-x)/1.0))
  dist={f'Sight{label}Base.R':1-tip,f'Sight{label}Tip.R':tip}
 hand.add([v.index],w*(1-influence),'REPLACE')
 for n,f in dist.items():
  if f>0:groups[n].add([v.index],w*influence*f,'REPLACE')
 changed.append(v.index)
# Only authorized right-hand vertices adjusted; other geometry stays exact.
for v in mesh.data.vertices:
 if v.index not in modified_vertices:assert (v.co-rest_positions[v.index]).length < 1e-6
assert len(rig.data.bones)==before+5
for v in mesh.data.vertices:
 assert abs(sum(g.weight for g in v.groups)-sum(w for _,w in original[v.index]))<1e-5
 if v.index not in changed:assert [(g.group,g.weight) for g in v.groups]==original[v.index]
rig['SightRig']='5 added bones: outer 2, inner 2, middle 1; weighted to original right hand only.'
rig['SightPose']='Frame 1: adjusted open hand; frame 30: circular gesture. Not a finished first-person animation.'
for n in newnames:
 p=rig.pose.bones[n];p.rotation_mode='QUATERNION';p.rotation_quaternion=Quaternion();p.keyframe_insert('rotation_quaternion',frame=1)
 angle= 0 if 'Base' in n else (-85 if 'OuterTip' in n else (85 if 'InnerTip' in n else -170))
 axis=Vector((0,1,0)) if 'Middle' not in n else Vector((0,0,1))
 p.rotation_quaternion=Quaternion(p.bone.matrix_local.to_3x3().inverted()@axis,math.radians(angle));p.keyframe_insert('rotation_quaternion',frame=30)
rig.animation_data.action.name='RightHand_Circle_Test'
scene=bpy.context.scene;scene.frame_start=1;scene.frame_end=30;scene.frame_set(30)
rig.show_in_front=True
for o in bpy.context.selected_objects:o.select_set(False)
rig.select_set(True);bpy.context.view_layer.objects.active=rig
for area in bpy.context.screen.areas:
 if area.type=='VIEW_3D':
  rv=area.spaces.active.region_3d;rv.view_location=mesh.matrix_world@Vector((-68,79,2));rv.view_distance=.23
  direction=mesh.matrix_world.to_3x3()@Vector((0,-1,0));rv.view_rotation=direction.to_track_quat('-Z','Y');rv.view_perspective='ORTHO'
bpy.ops.wm.save_as_mainfile(filepath=str(out/'Lemon_Character_CircleHand.blend'))
(out/'circle_hand_validation.json').write_text(json.dumps({'added_bones':newnames,'weighted_vertices':len(changed),'modified_mesh_vertices':len(modified_vertices),'original_vertex_count':len(mesh.data.vertices),'non_hand_weights_unchanged':True,'weight_sums_preserved':True}),encoding='utf8')
# Neutral inspection render, only after the saved character copy.
mat=bpy.data.materials.new('Inspection');mat.diffuse_color=(.65,.65,.65,1);mesh.data.materials.clear();mesh.data.materials.append(mat)
for p in mesh.data.polygons:p.material_index=0
scene.render.engine='BLENDER_WORKBENCH';scene.display.shading.light='STUDIO';scene.display.shading.color_type='MATERIAL';scene.display.shading.show_cavity=True
bpy.ops.object.camera_add();c=bpy.context.object;scene.camera=c;c.data.type='ORTHO';c.data.ortho_scale=.17
c.location=mesh.matrix_world@Vector((-69,105,2));target=mesh.matrix_world@Vector((-69,79,2));c.rotation_euler=(target-c.location).to_track_quat('-Z','Y').to_euler()
scene.render.resolution_x=900;scene.render.resolution_y=700;scene.render.resolution_percentage=100;scene.render.filepath=str(out/'circle_hand_adjusted.png');bpy.ops.render.render(write_still=True)

