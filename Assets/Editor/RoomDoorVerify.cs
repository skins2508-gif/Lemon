using UnityEngine;using UnityEditor;using System.IO;using System.Collections.Generic;
[InitializeOnLoad]public static class RoomDoorVerify
{
 static RoomDoorVerify(){EditorApplication.delayCall+=()=>{if(File.Exists("Temp/room_door_verify.request")&&!EditorApplication.isPlaying){File.Delete("Temp/room_door_verify.request");Run();}};}
 static void Run()
 {
  var go=new GameObject("Temporary room door verification camera");var c=go.AddComponent<Camera>();c.CopyFrom(GameObject.Find("Player").GetComponentInChildren<Camera>());c.enabled=false;
  var states=new Dictionary<Renderer,bool>();foreach(var r in GameObject.Find("Player").GetComponentsInChildren<Renderer>()){states[r]=r.enabled;r.enabled=false;}
  var results=new List<string>();
  try{
   foreach(var name in new[]{"Purple Room Entrance Left","Purple Room Entrance Right","Bedroom 01 Entrance Door","Bedroom 02 Entrance Door","Bathroom Entrance Door","Door_Red","Door_Blue"})
   {
    var d=GameObject.Find(name).GetComponent<StealthGame.Door>();var t=d.transform;var pos=t.position;var rot=t.rotation;var r=d.GetComponent<MeshRenderer>();var center=r.bounds.center;var f=t.forward;var hinge=t.TransformPoint(d.localHinge);
    try{
     c.transform.position=center-f*1.65f+Vector3.up*.1f;c.transform.LookAt(center);Capture(c,"Temp/"+name.Replace(" ","_")+"_closed.png");
     var q=Quaternion.Euler(0,d.reverseOpeningDirection?-d.openAngle:d.openAngle,0);t.position=hinge+q*(pos-hinge);t.rotation=q*rot;
     c.transform.position=center-f*1.65f+Vector3.up*.1f;c.transform.LookAt(center+f*.6f);Capture(c,"Temp/"+name.Replace(" ","_")+"_open.png");
     results.Add(name+" rendererEnabled="+r.enabled+" opaque="+(r.sharedMaterial.GetFloat("_Surface")==0)+" bothFaces="+(r.sharedMaterial.GetFloat("_Cull")==0)+" keyRequired="+d.requiresKey+" sound="+d.openingSound.name);
    }finally{t.SetPositionAndRotation(pos,rot);}
   }
   File.WriteAllLines("Temp/room_door_visual_check.txt",results);
  }finally{foreach(var p in states)p.Key.enabled=p.Value;Object.DestroyImmediate(go);}
 }
 static void Capture(Camera c,string path){var rt=RenderTexture.GetTemporary(960,720,24);var old=RenderTexture.active;Texture2D tex=null;try{c.targetTexture=rt;c.Render();RenderTexture.active=rt;tex=new Texture2D(960,720,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,960,720),0,0);tex.Apply();File.WriteAllBytes(path,tex.EncodeToPNG());}finally{c.targetTexture=null;RenderTexture.active=old;RenderTexture.ReleaseTemporary(rt);if(tex!=null)Object.DestroyImmediate(tex);}}
}
