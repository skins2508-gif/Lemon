using UnityEngine;using UnityEngine.AI;using UnityEditor;using System.IO;using System.Collections.Generic;using System.Reflection;using StealthGame;
[InitializeOnLoad]public static class NoiseGhostPlayCheck
{
 static int phase;static double started;static readonly List<string> results=new List<string>();static FootstepGhost[] ghosts;static Vector3[] starts;static PlayerFootstepNoise noise;static GameObject cameraObject;
 static NoiseGhostPlayCheck(){if(File.Exists("Temp/noise_ghost_play.request"))EditorApplication.update+=Step;}
 static void Assert(bool value,string name){results.Add(name+"="+value);if(!value)throw new System.Exception(name);}
 static void Finish(string error=null){if(error!=null)results.Add("FAIL="+error);File.WriteAllLines("Temp/noise_ghost_play_result.txt",results);File.Delete("Temp/noise_ghost_play.request");EditorApplication.update-=Step;EditorApplication.isPlaying=false;}
 static void Step()
 {
  if(!File.Exists("Temp/noise_ghost_play.request")){EditorApplication.update-=Step;return;}if(!EditorApplication.isPlaying){if(!EditorApplication.isPlayingOrWillChangePlaymode)EditorApplication.isPlaying=true;return;}if(EditorApplication.isCompiling)return;
  try{
   if(phase==0){var player=GameObject.Find("Player");if(player==null)return;foreach(var o in Object.FindObjectsByType<Observer>(FindObjectsSortMode.None))o.enabled=false;noise=player.GetComponent<PlayerFootstepNoise>();ghosts=Object.FindObjectsByType<FootstepGhost>(FindObjectsSortMode.None);Assert(ghosts.Length==2,"exactly_two_ghosts");starts=new Vector3[2];
    for(int i=0;i<ghosts.Length;i++){var ghost=ghosts[i];ghost.catchPlayerOnContact=false;Assert(ghost.GetComponent<NavMeshAgent>().isOnNavMesh,"on_navmesh_"+i);Assert(ghost.InRegion(ghost.transform.position),"correct_region_"+i);starts[i]=ghost.transform.position;results.Add("patrol_points_"+i+"="+ghost.patrolPoints.Length);}
    var body=player.GetComponent<Rigidbody>();body.constraints=RigidbodyConstraints.FreezeAll;body.useGravity=false;
    var fixedMethod=typeof(PlayerFootstepNoise).GetMethod("FixedUpdate",BindingFlags.Instance|BindingFlags.NonPublic);var input=typeof(FirstPersonController).GetField("<MovementInput>k__BackingField",BindingFlags.Instance|BindingFlags.NonPublic);var controller=player.GetComponent<FirstPersonController>();var old=body.position;int steps=0;noise.Stepped+=(p,l)=>steps++;float half=0;
    for(int i=0;i<200;i++){input.SetValue(controller,Vector2.up);body.position+=Vector3.forward*.02f;fixedMethod.Invoke(noise,null);if(i==49)half=noise.Level;}
    Assert(noise.Level>.95f&&half>.2f&&half<noise.Level,"continuous_walking_builds_noise");Assert(steps>3,"footsteps_emit_events");
    for(int i=0;i<150;i++){input.SetValue(controller,Vector2.zero);fixedMethod.Invoke(noise,null);}Assert(noise.Level<.01f,"stopping_cools_meter");
    for(int i=0;i<40;i++){input.SetValue(controller,Vector2.up);fixedMethod.Invoke(noise,null);}Assert(noise.Level<.01f,"pushing_without_movement_is_silent");
    player.GetComponent<PlayerMovement>().enabled=false;controller.enabled=false;body.position=old;
    // Capture the model with the actual HUD, using a temporary camera and light only during this play session.
    cameraObject=new GameObject("Temporary ghost verification camera");var cam=cameraObject.AddComponent<Camera>();cam.CopyFrom(player.GetComponentInChildren<Camera>());player.GetComponentInChildren<Camera>().enabled=false;
    var preview=Object.Instantiate(ghosts[0].visual.gameObject);preview.name="Temporary model preview";preview.transform.rotation=Quaternion.Euler(0,180,0);preview.transform.localScale=ghosts[0].visual.lossyScale;var rs=preview.GetComponentsInChildren<Renderer>();var bounds=rs[0].bounds;foreach(var r in rs)bounds.Encapsulate(r.bounds);preview.transform.position+=new Vector3(100-bounds.center.x,.15f-bounds.min.y,100-bounds.center.z);
    cam.transform.position=new Vector3(100,1,102.5f);cam.transform.LookAt(new Vector3(100,.75f,100));cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.025f,.035f,.04f);var lamp=cameraObject.AddComponent<Light>();lamp.type=LightType.Point;lamp.range=5;lamp.intensity=1.5f;
    typeof(PlayerFootstepNoise).GetField("<Level>k__BackingField",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(noise,.30f);noise.enabled=false;
    started=EditorApplication.timeSinceStartup;phase=1;
   }else if(phase==1&&EditorApplication.timeSinceStartup-started>1){ScreenCapture.CaptureScreenshot("Temp/noise_ghost_segmented.png");Assert(GameObject.Find("Noise segment 20")!=null,"segmented_meter_created");
    for(int i=0;i<ghosts.Length;i++){var ghost=ghosts[i];int heard=ghost.HeardCount;Vector3 opposite=ghost.boundaryPoint+Vector3.right*(ghost.positiveSide?-1:1);ghost.Hear(opposite,1);Assert(ghost.HeardCount==heard,"opposite_region_ignored_"+i);ghost.Hear(ghost.transform.position+Vector3.forward*.05f,0);Assert(ghost.HeardCount==heard,"silence_ignored_"+i);ghost.Hear(ghost.transform.position,1);Assert(ghost.HeardCount==heard+1,"nearby_step_heard_"+i);ghost.memorySeconds=.3f;ghost.searchSeconds=.3f;}
    started=EditorApplication.timeSinceStartup;phase=2;
   }else if(phase==2&&EditorApplication.timeSinceStartup-started>5){for(int i=0;i<ghosts.Length;i++){Assert(ghosts[i].State==FootstepGhost.Behaviour.Patrol,"returns_to_patrol_"+i);Assert(ghosts[i].InRegion(ghosts[i].transform.position),"stays_in_region_"+i);Assert(Vector3.Distance(starts[i],ghosts[i].transform.position)>.1f,"patrol_moves_"+i);}
    var locked=GameObject.Find("Door_Red").GetComponent<Door>();Assert(!locked.TryOpenForNoiseGhost(locked.transform.position)&&!locked.IsOpen,"ghost_cannot_unlock_red_door");var ordinary=GameObject.Find("Bathroom Entrance Door").GetComponent<Door>();Assert(ordinary.TryOpenForNoiseGhost(ordinary.transform.position),"ghost_can_open_ordinary_door");Assert(Mathf.Abs(noise.GetComponent<LemonSightSkill>().soundVolume-.6f)<.001f,"E_skill_volume_preserved");Finish();}
  }catch(System.Exception e){Finish(e.ToString());}
 }
}
