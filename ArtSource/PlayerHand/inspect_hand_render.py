import bpy
from mathutils import Vector
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.fbx(filepath='D:/Unity/My project/Assets/_3DStealthGame/Art/Models/Characters/Players/JohnLemon_Model.fbx')
m=next(o for o in bpy.context.scene.objects if o.type=='MESH')
mat=bpy.data.materials.new('Inspection');mat.diffuse_color=(0.65,0.65,0.65,1);m.data.materials.clear();m.data.materials.append(mat)
for p in m.data.polygons:p.material_index=0
scene=bpy.context.scene
scene.render.engine='BLENDER_WORKBENCH';scene.display.shading.light='STUDIO';scene.display.shading.color_type='MATERIAL';scene.display.shading.show_cavity=True
bpy.ops.object.camera_add();c=bpy.context.object;scene.camera=c;c.data.type='ORTHO';c.data.ortho_scale=.20
c.location=m.matrix_world@Vector((-68,98,24));target=m.matrix_world@Vector((-68,79,2));c.rotation_euler=(target-c.location).to_track_quat('-Z','Y').to_euler()
scene.render.resolution_x=900;scene.render.resolution_y=700;scene.render.resolution_percentage=100
scene.render.filepath='D:/Unity/My project/ArtSource/PlayerHand/hand_inspection.png';bpy.ops.render.render(write_still=True)
