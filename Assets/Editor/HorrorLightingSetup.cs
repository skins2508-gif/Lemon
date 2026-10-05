using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.IO;
[InitializeOnLoad]
public static class HorrorLightingSetup
{
 static HorrorLightingSetup(){EditorApplication.delayCall+=()=>{if(File.Exists("Temp/darken_screen.request")&&!EditorApplication.isPlaying){Apply();File.Delete("Temp/darken_screen.request");}};}
 static void Apply()
 {
  var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
  if(scene.path!="Assets/_3DStealthGame/Tutorial_Demo/Demo_Scenes/DemoScene.unity")return;
  var player=GameObject.Find("Player");var camera=player.GetComponentInChildren<Camera>();
  Capture(camera,"Temp/horror_before.png");
  const string folder="Assets/Environment/Lighting";Directory.CreateDirectory(folder);AssetDatabase.Refresh();
  const string path=folder+"/HorrorDarkness.asset";
  var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
  if(profile==null){profile=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(profile,path);}
  ColorAdjustments color;if(!profile.TryGet(out color)){color=profile.Add<ColorAdjustments>(false);AssetDatabase.AddObjectToAsset(color,profile);}
  Undo.RecordObject(color,"Darken horror screen");color.postExposure.Override(-.5f);color.contrast.Override(10f);
  var go=GameObject.Find("Horror Screen Darkness");if(go==null){go=new GameObject("Horror Screen Darkness");Undo.RegisterCreatedObjectUndo(go,"Add horror screen darkness");}
  var volume=go.GetComponent<Volume>();if(volume==null)volume=Undo.AddComponent<Volume>(go);Undo.RecordObject(volume,"Configure horror darkness");volume.isGlobal=true;volume.weight=1;volume.priority=100;volume.sharedProfile=profile;
  var data=camera.GetUniversalAdditionalCameraData();Undo.RecordObject(data,"Enable screen color grading");data.renderPostProcessing=true;
  EditorUtility.SetDirty(color);EditorUtility.SetDirty(profile);EditorUtility.SetDirty(volume);EditorUtility.SetDirty(data);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
  File.WriteAllText("Temp/horror_darkness_result.txt","saved=true exposure=-0.5 contrast=10 camera="+camera.transform.localPosition+" fov="+camera.fieldOfView);
  EditorApplication.delayCall+=()=>Capture(camera,"Temp/horror_after.png");Selection.activeGameObject=go;
 }
 static void Capture(Camera cam,string path)
 {
  var rt=RenderTexture.GetTemporary(1280,720,24);var old=cam.targetTexture;var previous=RenderTexture.active;Texture2D tex=null;
  try{cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;tex=new Texture2D(1280,720,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1280,720),0,0);tex.Apply();File.WriteAllBytes(path,tex.EncodeToPNG());}
  finally{cam.targetTexture=old;RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);if(tex!=null)Object.DestroyImmediate(tex);}
 }
}
