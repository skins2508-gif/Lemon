using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Collections.Generic;

[InitializeOnLoad]
public static class RoomDoorBuild
{
 const string Folder="Assets/Environment/Doors";
 static RoomDoorBuild(){EditorApplication.delayCall+=()=>{if(File.Exists("Temp/room_door_build.request")&&!EditorApplication.isPlaying){File.Delete("Temp/room_door_build.request");try{Build();}catch(System.Exception e){File.WriteAllText("Temp/room_door_build_error.txt",e.ToString());Debug.LogException(e);}}};}
 static Material Solid(Material source)
 {
  if(AssetDatabase.GetAssetPath(source).StartsWith(Folder+"/"))return source;
  string path=Folder+"/"+source.name+"_DoorTwoSided.mat";
  var m=AssetDatabase.LoadAssetAtPath<Material>(path);
  if(m!=null)return m;
  m=new Material(source);m.name=source.name+"_DoorTwoSided";
  m.SetFloat("_Cull",0);m.SetFloat("_Surface",0);m.SetFloat("_ZWrite",1);
  m.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");m.SetOverrideTag("RenderType","Opaque");m.renderQueue=2000;m.doubleSidedGI=true;
  AssetDatabase.CreateAsset(m,path);return m;
 }
 static void Build()
 {
  var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(!scene.path.EndsWith("DemoScene.unity"))throw new System.Exception("DemoScene required");
  Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
  var report=new List<string>();
  foreach(var d in Object.FindObjectsByType<StealthGame.Door>(FindObjectsSortMode.None))
  {
   var r=d.GetComponent<MeshRenderer>();if(r==null)continue;
   Undo.RecordObject(r,"Make both door faces visible");var mats=r.sharedMaterials;for(int i=0;i<mats.Length;i++)mats[i]=Solid(mats[i]);r.sharedMaterials=mats;
   report.Add(d.name+" scale="+d.transform.localScale+" worldScale="+d.transform.lossyScale+" opaqueBothFaces="+(mats[0].GetFloat("_Cull")==0));
   PrefabUtility.RecordPrefabInstancePropertyModifications(r);
  }
  var duplicate=GameObject.Find("Reading Room Entrance Door");if(duplicate!=null)Undo.DestroyObjectImmediate(duplicate);
  Create("Purple Room Entrance Left",new Vector3(-15.14f,.035f,-1.515f),270,true,report,1.10f);
  Create("Purple Room Entrance Right",new Vector3(-15.14f,.035f,-.40f),270,false,report,1.10f,true);
  foreach(var color in new[]{"Red","Blue"}){var m=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/"+color+"_DoorTwoSided.mat");m.SetFloat("_Smoothness",.22f);m.SetFloat("_Glossiness",.22f);EditorUtility.SetDirty(m);report.Add(color+" door mirror-like reflection removed; original key color preserved.");}
  AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
  File.WriteAllLines("Temp/room_door_build_result.txt",report);
 }
 static void Create(string name,Vector3 center,float yaw,bool reverse,List<string> report,float width=1.20f,bool rightHinge=false)
 {
  if(GameObject.Find(name)!=null)throw new System.Exception("Door already exists: "+name);
  var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_3DStealthGame/Prefabs/Environment/Decorations/Corridors/Door.prefab");
  var g=(GameObject)PrefabUtility.InstantiatePrefab(prefab);Undo.RegisterCreatedObjectUndo(g,"Add room entrance door");g.name=name;
  var mesh=g.GetComponent<MeshFilter>().sharedMesh;var b=mesh.bounds;
  g.transform.rotation=Quaternion.Euler(0,yaw,0);g.transform.localScale=new Vector3(width/b.size.x,2.20f/b.size.y,1);
  g.transform.position=center-g.transform.rotation*Vector3.Scale(new Vector3(b.center.x,b.min.y,b.center.z),g.transform.localScale);
  GameObjectUtility.SetStaticEditorFlags(g,0);
  var r=g.GetComponent<MeshRenderer>();r.sharedMaterial=Solid(r.sharedMaterial);
  var box=g.GetComponent<BoxCollider>();if(box==null)box=g.AddComponent<BoxCollider>();box.center=b.center;box.size=b.size;
  var d=g.AddComponent<StealthGame.Door>();d.requiresKey=false;d.KeyName="";d.localHinge=new Vector3(rightHinge?b.max.x:b.min.x,0,b.center.z);d.openAwayFromPlayer=false;d.reverseOpeningDirection=reverse;d.openAngle=95;
  d.openingSound=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_3DStealthGame/Audio/문여는소리.mp3");d.lockedSound=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_3DStealthGame/Audio/잠긴문소리.mp3");d.soundVolume=.6f;
  // Sample the entire leaf sweep against every straight patrol segment, including spawn-to-first waypoint.
  float clearance=float.MaxValue;var hinge=g.transform.TransformPoint(d.localHinge);
  foreach(var patrol in Object.FindObjectsByType<StealthGame.WaypointPatrol>(FindObjectsSortMode.None))
  {
   var points=new List<Vector3>{patrol.transform.position};foreach(var t in patrol.waypoints)if(t!=null)points.Add(t.position);if(patrol.waypoints.Length>0)points.Add(patrol.waypoints[0].position);
   var col=patrol.GetComponent<Collider>();float radius=col!=null?Mathf.Max(col.bounds.extents.x,col.bounds.extents.z):.35f;
   for(int a=0;a<=95;a++)for(int n=0;n<=60;n++)
   {
    Vector3 closed=g.transform.TransformPoint(new Vector3(Mathf.Lerp(b.min.x,b.max.x,n/60f),0,b.center.z));Vector3 p=hinge+Quaternion.Euler(0,reverse?-a:a,0)*(closed-hinge);
    for(int k=1;k<points.Count;k++){var v=points[k]-points[k-1];v.y=0;var w=p-points[k-1];w.y=0;float t=v.sqrMagnitude>0?Mathf.Clamp01(Vector3.Dot(w,v)/v.sqrMagnitude):0;clearance=Mathf.Min(clearance,(w-v*t).magnitude-radius-b.extents.z-.02f);}
   }
  }
  if(clearance<.1f){Object.DestroyImmediate(g);throw new System.Exception(name+" obstructs patrol: "+clearance);}
  PrefabUtility.RecordPrefabInstancePropertyModifications(g.transform);PrefabUtility.RecordPrefabInstancePropertyModifications(r);
  report.Add(name+" bounds="+r.bounds+" minimumPatrolClearance="+clearance);
 }
}
