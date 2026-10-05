import bpy
bpy.ops.wm.open_mainfile(filepath='D:/Unity/My project/ArtSource/PlayerHand/Lemon_EyeArch.blend')
bpy.context.scene.frame_set(1)
for o in bpy.context.scene.objects:o.select_set(o.type in {'ARMATURE','MESH'})
bpy.ops.export_scene.fbx(filepath='D:/Unity/My project/Assets/PlayerEyeArch/Lemon_EyeArch.fbx',use_selection=True,object_types={'ARMATURE','MESH'},add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=False,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,axis_forward='-Z',axis_up='Y')
