using UnityEngine;using UnityEditor;using System.IO;using System.Collections.Generic;
[InitializeOnLoad]public static class RoomDoorSurvey
{
 [System.Serializable]public class Item{public string name,path,material;public Vector3 center,size,position,rotation;}
 [System.Serializable]public class Route{public string name;public Vector3 start;public Vector3[] points;public float radius;}
 [System.Serializable]public class Survey{public List<Item> items=new List<Item>();public List<Route> routes=new List<Route>();}
 static RoomDoorSurvey(){EditorApplication.delayCall+=()=>{if(File.Exists("Temp/room_door_survey.request")&&!EditorApplication.isPlaying){Run();File.Delete("Temp/room_door_survey.request");}};}
 static string Path(Transform t){return t.parent==null?t.name:Path(t.parent)+"/"+t.name;}
 static void Run(){var s=new Survey();foreach(var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)){if(r.name.StartsWith("Ceiling")||r.name.StartsWith("Rain"))continue;if(r.sharedMaterial==null)continue;if(r.name.StartsWith("Wall")||r.name.Contains("Door")||r.sharedMaterial.name.StartsWith("Floor_"))s.items.Add(new Item{name=r.name,path=Path(r.transform),material=r.sharedMaterial.name,center=r.bounds.center,size=r.bounds.size,position=r.transform.position,rotation=r.transform.eulerAngles});}foreach(var p in Object.FindObjectsByType<StealthGame.WaypointPatrol>(FindObjectsSortMode.None)){var points=new List<Vector3>();foreach(var t in p.waypoints)if(t!=null)points.Add(t.position);var col=p.GetComponent<Collider>();s.routes.Add(new Route{name=p.name,start=p.transform.position,points=points.ToArray(),radius=col!=null?Mathf.Max(col.bounds.extents.x,col.bounds.extents.z):.35f});}File.WriteAllText("Temp/room_door_survey.json",JsonUtility.ToJson(s,true));}
}
