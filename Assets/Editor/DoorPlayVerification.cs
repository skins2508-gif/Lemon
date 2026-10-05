using UnityEngine;using UnityEditor;using System.IO;using System.Collections.Generic;
[InitializeOnLoad]public static class DoorPlayVerification
{
 static int phase;static double started;static StealthGame.Door door;static StealthGame.PlayerMovement player;static Vector3 position,hinge;static Quaternion rotation;static List<string> results=new List<string>();
 static DoorPlayVerification(){if(File.Exists("Temp/door_play_verify.request"))EditorApplication.update+=Step;}
 static void Finish(string error=null){if(error!=null)results.Add("FAIL="+error);File.WriteAllLines("Temp/door_play_verification.txt",results);File.Delete("Temp/door_play_verify.request");EditorApplication.update-=Step;EditorApplication.isPlaying=false;}
 static void Step()
 {
  if(!File.Exists("Temp/door_play_verify.request")){EditorApplication.update-=Step;return;}
  if(!EditorApplication.isPlaying){if(!EditorApplication.isPlayingOrWillChangePlaymode)EditorApplication.isPlaying=true;return;}
  if(EditorApplication.isCompiling)return;
  try{
   if(phase==0){var p=GameObject.Find("Player");if(p==null)return;player=p.GetComponent<StealthGame.PlayerMovement>();player.enabled=false;p.GetComponent<StealthGame.FirstPersonController>().enabled=false;foreach(var observer in Object.FindObjectsByType<StealthGame.Observer>(FindObjectsSortMode.None))observer.enabled=false;
    foreach(var candidate in Object.FindObjectsByType<StealthGame.Door>(FindObjectsSortMode.None))if(candidate.requiresKey){door=candidate;break;}
    position=door.transform.position;rotation=door.transform.rotation;hinge=door.transform.TransformPoint(door.localHinge);p.transform.position=position+door.transform.forward*2;
    results.Add("locked_without_key="+(!door.TryInteract(player)&&!door.IsOpen));var audio=door.GetComponent<AudioSource>();results.Add("locked_sound_started="+(audio!=null&&audio.isPlaying));player.AddKey(door.KeyName);results.Add("key_does_not_auto_open="+(!door.IsOpen));results.Add("interaction_opens="+door.TryInteract(player));results.Add("opening_sound_started="+(audio!=null&&audio.isPlaying));started=EditorApplication.timeSinceStartup;phase=1;
   }else if(phase==1&&EditorApplication.timeSinceStartup-started>1.1){results.Add("rotation_completed="+(!door.IsMoving&&Quaternion.Angle(rotation,door.transform.rotation)>90));results.Add("hinge_stays_fixed="+(Vector3.Distance(hinge,door.transform.TransformPoint(door.localHinge))<.001f));results.Add("interact_closes="+door.TryInteract(player));started=EditorApplication.timeSinceStartup;phase=2;
   }else if(phase==2&&EditorApplication.timeSinceStartup-started>1.1){results.Add("closed_pose_restored="+(!door.IsOpen&&!door.IsMoving&&Vector3.Distance(position,door.transform.position)<.001f&&Quaternion.Angle(rotation,door.transform.rotation)<.01f));results.Add("collider_preserved="+door.GetComponent<Collider>().enabled);results.Add("skill_volume_unchanged="+(Mathf.Abs(player.GetComponent<StealthGame.LemonSightSkill>().soundVolume-.6f)<.001f));Finish();}
  }catch(System.Exception e){Finish(e.Message);}
 }
}
