import bpy,json
bpy.ops.import_scene.fbx(filepath='D:/Unity/My project/Assets/_3DStealthGame/Art/Models/Characters/Players/JohnLemon_Model.fbx')
for o in bpy.context.scene.objects:
 if o.type=='ARMATURE':
  print('ARM',o.name,[(b.name,list(b.head_local),list(b.tail_local)) for b in o.data.bones if 'Hand' in b.name])
 if o.type=='MESH':
  print('MESH',o.name,len(o.data.vertices),[(g.index,g.name) for g in o.vertex_groups if 'Hand' in g.name])
  for g in o.vertex_groups:
   if 'Hand' in g.name:
    vs=[list(v.co) for v in o.data.vertices if any(w.group==g.index and w.weight>0.2 for w in v.groups)]
    print('HANDVERTS',g.name,json.dumps(vs))
