using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;using System.IO;using System.Collections.Generic;
[InitializeOnLoad]public static class RoomDoorFit
{
 static RoomDoorFit(){EditorApplication.delayCall+=()=>{if(!File.Exists("Temp/room_door_fit.request")||EditorApplication.isPlaying)return;File.Delete("Temp/room_door_fit.request");try{Run();}catch(System.Exception e){File.WriteAllText("Temp/room_door_fit_error.txt",e.ToString());Debug.LogException(e);}};}
 static void Run(){var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(!scene.path.EndsWith("DemoScene.unity"))throw new System.Exception("DemoScene required");var report=new List<string>();
  foreach(var name in new[]{"Purple Room Entrance Left","Purple Room Entrance Right","Bedroom 01 Entrance Door","Bedroom 02 Entrance Door","Bathroom Entrance Door"}){
   var g=GameObject.Find(name);var t=g.transform;var mesh=g.GetComponent<MeshFilter>().sharedMesh;var b=mesh.bounds;var center=t.TransformPoint(b.center);Undo.RecordObject(t,"Fit door panel to doorway");
   var scale=t.localScale;scale.x=name.StartsWith("Purple")?1.04f:1.17f;t.localScale=scale;t.position+=center-t.TransformPoint(b.center);
   PrefabUtility.RecordPrefabInstancePropertyModifications(t);report.Add(name+" actual surface width >= "+(1.09f*scale.x));
   var door=g.GetComponent<StealthGame.Door>();float clearance=float.MaxValue;var hinge=t.TransformPoint(door.localHinge);
   foreach(var p in Object.FindObjectsByType<StealthGame.WaypointPatrol>(FindObjectsSortMode.None)){var pts=new List<Vector3>{p.transform.position};foreach(var w in p.waypoints)if(w!=null)pts.Add(w.position);if(p.waypoints.Length>0)pts.Add(p.waypoints[0].position);var col=p.GetComponent<Collider>();float radius=col==null?.35f:Mathf.Max(col.bounds.extents.x,col.bounds.extents.z);
    for(int a=0;a<=95;a++)for(int n=0;n<=80;n++){var closed=t.TransformPoint(new Vector3(Mathf.Lerp(b.min.x,b.max.x,n/80f),0,b.center.z));var point=hinge+Quaternion.Euler(0,door.reverseOpeningDirection?-a:a,0)*(closed-hinge);for(int k=1;k<pts.Count;k++){var v=pts[k]-pts[k-1];v.y=0;var delta=point-pts[k-1];delta.y=0;float f=v.sqrMagnitude>0?Mathf.Clamp01(Vector3.Dot(delta,v)/v.sqrMagnitude):0;clearance=Mathf.Min(clearance,(delta-v*f).magnitude-radius-b.extents.z-.02f);}}}
   report.Add("sweep patrol clearance="+clearance);if(clearance<.1f)throw new System.Exception("Patrol overlap: "+name);
  }
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);File.WriteAllLines("Temp/room_door_fit_result.txt",report);
 }
}
