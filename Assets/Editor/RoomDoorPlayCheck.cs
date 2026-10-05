using UnityEngine;using UnityEditor;using System.IO;using System.Collections.Generic;
[InitializeOnLoad]public static class RoomDoorPlayCheck
{
 static int phase,index;static double time;static StealthGame.PlayerMovement player;static StealthGame.Door door;static Vector3 position,hinge;static Quaternion rotation;static readonly List<string> report=new List<string>();
 static readonly string[] names={"Purple Room Entrance Left","Purple Room Entrance Right","Bedroom 01 Entrance Door","Bedroom 02 Entrance Door","Bathroom Entrance Door","Door_Red","Door_Blue"};
 static RoomDoorPlayCheck(){if(File.Exists("Temp/room_door_play.request"))EditorApplication.update+=Step;}
 static void Finish(string error=null){if(error!=null)report.Add("FAIL="+error);File.WriteAllLines("Temp/room_door_play_result.txt",report);File.Delete("Temp/room_door_play.request");EditorApplication.update-=Step;EditorApplication.isPlaying=false;}
 static void Check(bool ok,string label){if(!ok)throw new System.Exception(door.name+": "+label);report.Add(door.name+": "+label+" PASS");}
 static void Step(){if(!File.Exists("Temp/room_door_play.request")){EditorApplication.update-=Step;return;}if(!EditorApplication.isPlaying){if(!EditorApplication.isPlayingOrWillChangePlaymode)EditorApplication.isPlaying=true;return;}if(EditorApplication.isCompiling)return;
  try{
   if(player==null){var p=GameObject.Find("Player");if(p==null)return;player=p.GetComponent<StealthGame.PlayerMovement>();player.enabled=false;p.GetComponent<StealthGame.FirstPersonController>().enabled=false;foreach(var o in Object.FindObjectsByType<StealthGame.Observer>(FindObjectsSortMode.None))o.enabled=false;}
   if(phase==0){door=GameObject.Find(names[index]).GetComponent<StealthGame.Door>();position=door.transform.position;rotation=door.transform.rotation;hinge=door.transform.TransformPoint(door.localHinge);player.transform.position=position-door.transform.forward*3;
    if(door.requiresKey){Check(!door.TryInteract(player)&&!door.IsOpen,"no key blocks");player.AddKey(door.KeyName);Check(!door.IsOpen,"key alone keeps closed");}
    Check(door.TryInteract(player),"interaction opens");Check(door.GetComponent<AudioSource>().isPlaying,"sound starts");time=EditorApplication.timeSinceStartup;phase=1;
   }else if(phase==1&&EditorApplication.timeSinceStartup-time>1.1){Check(!door.IsMoving&&Quaternion.Angle(rotation,door.transform.rotation)>90,"opening completes");Check(Vector3.Distance(hinge,door.transform.TransformPoint(door.localHinge))<.001f,"hinge fixed");Check(door.GetComponent<MeshRenderer>().enabled,"open door visible");Check(door.TryInteract(player),"interaction closes");time=EditorApplication.timeSinceStartup;phase=2;
   }else if(phase==2&&EditorApplication.timeSinceStartup-time>1.1){Check(!door.IsMoving&&!door.IsOpen&&Vector3.Distance(position,door.transform.position)<.001f,"closed pose restored");index++;phase=0;if(index==names.Length){report.Add("E skill volume="+player.GetComponent<StealthGame.LemonSightSkill>().soundVolume);Finish();}}
  }catch(System.Exception e){Finish(e.ToString());}
 }
}
