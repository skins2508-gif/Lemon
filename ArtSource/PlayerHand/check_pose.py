import bpy
bpy.ops.wm.open_mainfile(filepath='D:/Unity/My project/ArtSource/PlayerHand/Lemon_Character_CircleHand.blend')
r=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
for n in ['RightShoulder','RightArm','RightForeArm','RightHand','Head','RightEye','LeftEye','RightEyebrow']:
 b=r.data.bones[n]; print(n,tuple(b.head_local),tuple(b.tail_local))
