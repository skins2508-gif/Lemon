using UnityEngine;using UnityEditor;using System.IO;using System.Collections.Generic;
[InitializeOnLoad]public static class DoorMeshInspect{
 [System.Serializable]class Data{public Vector3[] vertices;public int[] triangles;public Vector2[] uv;public string[] doors;}
 static DoorMeshInspect(){EditorApplication.delayCall+=()=>{if(!File.Exists("Temp/door_mesh_inspect.request")||EditorApplication.isPlaying)return;File.Delete("Temp/door_mesh_inspect.request");var g=GameObject.Find("Bathroom Entrance Door");var m=g.GetComponent<MeshFilter>().sharedMesh;var lines=new List<string>();foreach(var d in Object.FindObjectsByType<StealthGame.Door>(FindObjectsSortMode.None))lines.Add(d.name+" pos="+d.transform.position+" rot="+d.transform.eulerAngles+" scale="+d.transform.localScale);File.WriteAllText("Temp/door_mesh.json",JsonUtility.ToJson(new Data{vertices=m.vertices,triangles=m.triangles,uv=m.uv,doors=lines.ToArray()}));};}
}
