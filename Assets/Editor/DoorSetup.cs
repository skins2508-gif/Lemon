using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;using System.IO;using System.Collections.Generic;
[InitializeOnLoad]public static class DoorSetup
{
 static DoorSetup(){EditorApplication.delayCall+=()=>{if(File.Exists("Temp/door_build.request")&&!EditorApplication.isPlaying){Build();File.Delete("Temp/door_build.request");}};}
 static void Build()
 {
  var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(!scene.path.EndsWith("DemoScene.unity"))throw new System.Exception("Open DemoScene.");
  var open=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_3DStealthGame/Audio/문여는소리.mp3");var locked=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_3DStealthGame/Audio/잠긴문소리.mp3");if(open==null||locked==null)throw new System.Exception("Door audio missing.");
  var normals=new List<MeshRenderer>();var doors=new List<StealthGame.Door>(Object.FindObjectsByType<StealthGame.Door>(FindObjectsSortMode.None));
  foreach(var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))if((r.name=="Door"||r.name.StartsWith("Door ("))&&r.GetComponent<StealthGame.Door>()==null)normals.Add(r);
  Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Configure F key doors");
  foreach(var r in normals){var door=Undo.AddComponent<StealthGame.Door>(r.gameObject);door.requiresKey=false;door.KeyName="";doors.Add(door);}
  foreach(var d in doors)
  {
   Undo.RecordObject(d,"Configure door interaction");var mesh=d.GetComponent<MeshFilter>().sharedMesh;d.localHinge=new Vector3(mesh.bounds.min.x,0,mesh.bounds.center.z);
   foreach(var other in doors)if(other!=d && Vector3.Distance(other.transform.position,d.transform.position)<1.6f){var delta=d.transform.InverseTransformPoint(other.transform.position);if(delta.x<0)d.localHinge.x=mesh.bounds.max.x;}
   d.openingSound=open;d.lockedSound=locked;d.soundVolume=.6f;
   if(d.GetComponent<Collider>()==null){var box=Undo.AddComponent<BoxCollider>(d.gameObject);box.center=mesh.bounds.center;box.size=mesh.bounds.size;}
   Undo.RecordObject(d.gameObject,"Allow door movement");GameObjectUtility.SetStaticEditorFlags(d.gameObject,0);
   EditorUtility.SetDirty(d);PrefabUtility.RecordPrefabInstancePropertyModifications(d);
  }
  var p=GameObject.Find("Player");var interactor=p.GetComponent<StealthGame.DoorInteractor>();if(interactor==null)interactor=Undo.AddComponent<StealthGame.DoorInteractor>(p);Undo.RecordObject(interactor,"Set door camera");interactor.viewCamera=p.GetComponentInChildren<Camera>();EditorUtility.SetDirty(interactor);
  Undo.CollapseUndoOperations(group);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
  var results=new List<string>();results.Add("doors="+doors.Count+" normal="+normals.Count+" locked="+(doors.Count-normals.Count)+" openingClip="+open.name+" lockedClip="+locked.name);
  var testPlayer=new GameObject("Temporary door test player");var testDoor=new GameObject("Temporary door test");
  try{var movement=testPlayer.AddComponent<StealthGame.PlayerMovement>();var d=testDoor.AddComponent<StealthGame.Door>();d.requiresKey=true;d.KeyName="test-red";testPlayer.transform.position=new Vector3(0,0,2);results.Add("no_key_blocked="+(!d.TryInteract(movement)&&!d.IsOpen));movement.AddKey("wrong-key");results.Add("wrong_key_blocked="+(!d.TryInteract(movement)&&!d.IsOpen));movement.AddKey("test-red");results.Add("matching_key_opens="+(d.TryInteract(movement)&&d.IsOpen));var normal=new GameObject("Temporary normal door");try{var n=normal.AddComponent<StealthGame.Door>();n.requiresKey=false;results.Add("normal_opens="+n.TryInteract(movement));}finally{Object.DestroyImmediate(normal);}results.Add("collision_auto_open_removed="+(typeof(StealthGame.Door).GetMethod("OnCollisionEnter",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)==null));}
  finally{Object.DestroyImmediate(testDoor);Object.DestroyImmediate(testPlayer);}
  File.WriteAllLines("Temp/door_result.txt",results);Selection.activeGameObject=p;
 }
}
