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
 v.co.x=x-.25*max(0,root-x)
 v.co.y=79+(y-79)*(1-.2*blend)
 v.co.z=center+(z-center)*(1-.2*blend)
 modified_vertices.append(v.index)
before=len(rig.data.bones)
bpy.context.view_layer.objects.active=rig;bpy.ops.object.mode_set(mode='EDIT')
# Bone coordinates follow the three existing mesh lobes; no new fingers or geometry.
def add(n,h,t,parent):
 b=rig.data.edit_bones.new(n);b.head=h;b.tail=t;b.parent=rig.data.edit_bones[parent];return b
for label,z,x in [('Outer',4.85,-68.6),('Inner',-.35,-68.8)]:
 add('Sight'+label+'Base.R',(x,79,z),(x-2.0,79,z),'RightHand')
 add('Sight'+label+'Tip.R',(x-2.0,79,z),(x-3.875,79,z),'Sight'+label+'Base.R')
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

from mathutils import Matrix
scene=bpy.context.scene
scene.render.fps=30
scene.frame_start=1;scene.frame_end=72
arm_names=['RightArm','RightForeArm','RightHand']
for n in arm_names+newnames:rig.pose.bones[n].rotation_mode='QUATERNION'
def set_delta(n,R,head):
 b=rig.data.bones[n]
 rig.pose.bones[n].matrix=Matrix.Translation(head)@R.to_4x4()@Matrix.Translation(-b.head_local)@b.matrix_local
 bpy.context.view_layer.update()
def arm_pose(wrist,raised):
 shoulder=rig.data.bones['RightArm'].head_local.copy()
 elbow_rest=rig.data.bones['RightForeArm'].head_local.copy()
 wrist_rest=rig.data.bones['RightHand'].head_local.copy()
 L1=(elbow_rest-shoulder).length;L2=(wrist_rest-elbow_rest).length
 vec=wrist-shoulder;d=vec.length;axis=vec.normalized()
 assert abs(L1-L2)<d<L1+L2
 a=(L1*L1-L2*L2+d*d)/(2*d);h=math.sqrt(max(0,L1*L1-a*a))
 pole=Vector((-1,-.3,0));perp=(pole-axis*pole.dot(axis)).normalized()
 elbow=shoulder+axis*a+perp*h
 set_delta('RightArm',(elbow_rest-shoulder).rotation_difference(elbow-shoulder).to_matrix(),shoulder)
 set_delta('RightForeArm',(wrist_rest-elbow_rest).rotation_difference(wrist-elbow).to_matrix(),elbow)
 R=Matrix.Rotation(math.radians(195 if raised else 80),3,'Z')
 set_delta('RightHand',R,wrist)
 for n in newnames:
  p=rig.pose.bones[n]
  angle=(-18 if 'Base' in n else -38) if raised else 0
  if 'Middle' in n:angle=-28 if raised else 0
  local_axis=p.bone.matrix_local.to_3x3().inverted()@Vector((0,0,1))
  p.rotation_quaternion=Quaternion(local_axis,math.radians(angle))
 bpy.context.view_layer.update()
for frame,raised in [(1,False),(24,True),(48,True),(72,False)]:
 scene.frame_set(frame)
 arm_pose(Vector((-18,124,23)) if raised else Vector((-20,40,4)),raised)
 for n in arm_names+newnames:
  p=rig.pose.bones[n]
  p.keyframe_insert('rotation_quaternion',frame=frame)
  p.keyframe_insert('location',frame=frame)
rig.animation_data.action.name='Lemon_EyeArch_RaiseHoldLower'
rig['SightRig']='5 additional bones connected to original RightHand; only right-hand mesh adjusted.'
rig['SightPose']='30 fps. 1 relaxed; 24 hand above right eye; 48 hold; 72 relaxed. X-ray shading not included.'
scene.frame_set(36)
rig.show_in_front=True
# Relink the original supplied texture if present.
textures=list(Path('D:/Unity/My project/Assets/_3DStealthGame').rglob('*John*Albedo*'))
for img in bpy.data.images:
 if ('JohnLemon' in img.filepath or 'JohnLemon' in img.name) and textures:
  img.filepath=str(textures[0]);img.reload();img.pack()
for area in bpy.context.screen.areas:
 if area.type=='VIEW_3D':
  rv=area.spaces.active.region_3d;rv.view_location=mesh.matrix_world@Vector((0,110,5));rv.view_distance=1.0
  rv.view_rotation=(mesh.matrix_world.to_3x3()@Vector((0,0,-1))).to_track_quat('-Z','Y');rv.view_perspective='ORTHO'
rig.select_set(True);mesh.select_set(False);bpy.context.view_layer.objects.active=rig
bpy.ops.wm.save_as_mainfile(filepath=str(out/'Lemon_EyeArch.blend'))
(out/'eye_arch_validation.json').write_text(json.dumps({'additional_bones':newnames,'modified_vertices':len(modified_vertices),'total_vertices':len(mesh.data.vertices),'other_geometry_unchanged':True,'weights_normalized_as_original':True,'animation_frames':[1,24,48,72]}),encoding='utf8')
mat=bpy.data.materials.new('Inspection');mat.diffuse_color=(.7,.7,.7,1);mesh.data.materials.clear();mesh.data.materials.append(mat)
for p in mesh.data.polygons:p.material_index=0
scene.render.engine='BLENDER_WORKBENCH';scene.display.shading.light='STUDIO';scene.display.shading.color_type='MATERIAL';scene.display.shading.show_cavity=True
bpy.ops.object.camera_add();c=bpy.context.object;scene.camera=c;c.data.type='ORTHO';c.data.ortho_scale=.85
scene.render.resolution_x=900;scene.render.resolution_y=900;scene.render.resolution_percentage=100
for label,pos in [('front',(0,115,160)),('side',(-140,115,75))]:
 target=mesh.matrix_world@Vector((0,113,8));c.location=mesh.matrix_world@Vector(pos);c.rotation_euler=(target-c.location).to_track_quat('-Z','Y').to_euler()
 scene.render.filepath=str(out/('eye_arch_'+label+'.png'));bpy.ops.render.render(write_still=True)

