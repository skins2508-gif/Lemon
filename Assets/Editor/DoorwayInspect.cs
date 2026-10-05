using UnityEngine;using UnityEditor;using System.IO;using System.Collections.Generic;
[InitializeOnLoad]public static class DoorwayInspect{
static DoorwayInspect(){EditorApplication.delayCall+=()=>{if(!File.Exists("Temp/doorway_inspect.request")||EditorApplication.isPlaying)return;var a=new List<string>();foreach(var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)){if(!t.name.StartsWith("Wall_Door"))continue;var rs=t.GetComponentsInChildren<Renderer>();a.Add(t.name+" pos="+t.position+" rot="+t.eulerAngles+" scale="+t.lossyScale+" children="+rs.Length);foreach(var r in rs)a.Add("  "+r.name+" center="+r.bounds.center+" size="+r.bounds.size);}File.WriteAllLines("Temp/doorways.txt",a);File.Delete("Temp/doorway_inspect.request");};}
}
