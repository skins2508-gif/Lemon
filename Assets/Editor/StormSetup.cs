using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Collections.Generic;
[InitializeOnLoad] public static class StormSetup
{
 static StormSetup(){EditorApplication.delayCall+=()=>{if(!EditorApplication.isPlaying && File.Exists("Temp/storm_apply.request")){Apply();File.Delete("Temp/storm_apply.request");}};}
 static void Apply()
 {
  var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(!scene.path.EndsWith("DemoScene.unity"))throw new System.InvalidOperationException("Open DemoScene.");
  var clip=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_3DStealthGame/Audio/비천둥소리.mp3");if(clip==null)throw new System.InvalidOperationException("Storm audio missing.");
  var report=new List<string>();var lights=Object.FindObjectsByType<Light>(FindObjectsSortMode.None);var candidates=new List<Light>();var kept=new List<Light>();
  foreach(var light in lights){if(light.enabled && light.name=="Exit_Spot_Light")kept.Add(light);else if(light.enabled && light.name=="Spot Light")candidates.Add(light);}
  candidates.Sort((a,b)=>a.transform.position.x.CompareTo(b.transform.position.x));
  foreach(var light in candidates){bool near=false;foreach(var other in kept){Vector3 delta=light.transform.position-other.transform.position;delta.y=0;if(delta.magnitude<5.5f){near=true;break;}}if(!near)kept.Add(light);}
  Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Minimal lights and storm ambience");
  foreach(var light in lights){Undo.RecordObject(light,"Reduce lights");report.Add("BEFORE light "+light.name+" at "+light.transform.position+" enabled="+light.enabled+" intensity="+light.intensity);light.enabled=kept.Contains(light);if(light.enabled){light.intensity=light.name=="Exit_Spot_Light"?3f:1.6f;light.range=Mathf.Min(light.range,5.5f);}EditorUtility.SetDirty(light);PrefabUtility.RecordPrefabInstancePropertyModifications(light);}
  int reduced=0;foreach(var source in Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
  {
   if(source.name=="Rain and Thunder")continue;Undo.RecordObject(source,"Lower other sounds");report.Add("BEFORE audio "+source.name+" volume="+source.volume);source.volume*=.25f;
   if(source.clip!=null && source.clip.name=="SFXBuzzingLight"){bool lit=false;foreach(var light in source.GetComponentsInChildren<Light>())if(light.enabled)lit=true;if(!lit)source.volume=0;}
   EditorUtility.SetDirty(source);PrefabUtility.RecordPrefabInstancePropertyModifications(source);reduced++;
  }
  var skill=Object.FindFirstObjectByType<StealthGame.LemonSightSkill>();if(skill!=null){Undo.RecordObject(skill,"Lower skill audio");skill.soundVolume*=.25f;EditorUtility.SetDirty(skill);PrefabUtility.RecordPrefabInstancePropertyModifications(skill);}
  var go=GameObject.Find("Rain and Thunder");if(go==null){go=new GameObject("Rain and Thunder");Undo.RegisterCreatedObjectUndo(go,"Add rain and thunder");}
  var storm=go.GetComponent<AudioSource>();if(storm==null)storm=Undo.AddComponent<AudioSource>(go);Undo.RecordObject(storm,"Configure storm audio");storm.clip=clip;storm.volume=.8f;storm.loop=true;storm.playOnAwake=true;storm.spatialBlend=0;storm.priority=32;EditorUtility.SetDirty(storm);
  Undo.CollapseUndoOperations(group);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
  report.Insert(0,"lights="+kept.Count+"/"+lights.Length+" otherAudioReduced="+reduced+" stormVolume="+storm.volume+" loop="+storm.loop+" clipSeconds="+clip.length);
  File.WriteAllLines("Temp/storm_result.txt",report);Selection.activeGameObject=go;
  EditorApplication.delayCall+=Capture;
 }
 static void Capture()
 {
  var cam=GameObject.Find("Player").GetComponentInChildren<Camera>();var rt=RenderTexture.GetTemporary(1280,720,24);var old=cam.targetTexture;var previous=RenderTexture.active;Texture2D tex=null;
  try{cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;tex=new Texture2D(1280,720,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1280,720),0,0);tex.Apply();File.WriteAllBytes("Temp/storm_preview.png",tex.EncodeToPNG());}
  finally{cam.targetTexture=old;RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);if(tex!=null)Object.DestroyImmediate(tex);}
 }
}
