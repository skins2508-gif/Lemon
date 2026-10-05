using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;using System.IO;using System.Collections.Generic;
[InitializeOnLoad]public static class WindowStormSetup
{
 static WindowStormSetup(){EditorApplication.delayCall+=()=>{if(File.Exists("Temp/window_storm_build.request")&&!EditorApplication.isPlaying){Build();File.Delete("Temp/window_storm_build.request");}};}
 static void Build()
 {
  var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(!scene.path.EndsWith("DemoScene.unity"))throw new System.Exception("Open DemoScene.");
  if(GameObject.Find("Window Storm Effects")!=null)throw new System.Exception("Window effects already exist.");
  const string folder="Assets/Environment/Storm/";var shader=Shader.Find("StealthGame/RainOutsideWindow");if(shader==null||ShaderUtil.ShaderHasError(shader))throw new System.Exception("Rain shader is not ready.");
  var mat=new Material(shader);AssetDatabase.CreateAsset(mat,folder+"RainOutsideWindow.mat");
  var cookie=new Texture2D(256,256,TextureFormat.RGBA32,false,true);cookie.name="Arched window frame cookie";cookie.wrapMode=TextureWrapMode.Clamp;cookie.filterMode=FilterMode.Bilinear;var pixels=new Color[256*256];
  for(int y=0;y<256;y++)for(int x=0;x<256;x++){float u=(x+.5f)/256,v=(y+.5f)/256;bool hole=u>.08f&&u<.92f&&v>.08f&&v<.92f;hole&=Mathf.Abs(u-.5f)>.025f&&Mathf.Abs(v-.43f)>.025f&&Mathf.Abs(v-.70f)>.018f;if(v>.70f)hole&=Mathf.Pow((u-.5f)/.42f,2)+Mathf.Pow((v-.70f)/.22f,2)<1;pixels[y*256+x]=hole?Color.white:new Color(0,0,0,1);}
  cookie.SetPixels(pixels);cookie.Apply();AssetDatabase.CreateAsset(cookie,folder+"WindowFrameCookie.asset");
  Undo.IncrementCurrentGroup();int undo=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Add window rain and lightning");
  var root=new GameObject("Window Storm Effects");Undo.RegisterCreatedObjectUndo(root,"Window storm effects");var controller=Undo.AddComponent<StealthGame.WindowStorm>(root);
  controller.stormAudio=GameObject.Find("Rain and Thunder").GetComponent<AudioSource>();controller.viewer=GameObject.Find("Player").GetComponentInChildren<Camera>().transform;
  var lights=new List<Light>();var panes=new List<Renderer>();var windows=new List<MeshRenderer>();
  foreach(var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))if(r.name.StartsWith("Wall_Window"))windows.Add(r);
  foreach(var wall in windows)
  {
   var frame=new GameObject("Storm - "+wall.name);frame.transform.SetParent(root.transform);frame.transform.SetPositionAndRotation(wall.transform.position,wall.transform.rotation);frame.transform.localScale=wall.transform.lossyScale;
   var bounds=wall.GetComponent<MeshFilter>().sharedMesh.bounds;int count=wall.name.Contains("Tripple")?3:1;float width=wall.name.Contains("Double")?1.08f:.70f;
   for(int i=0;i<count;i++)
   {
    float x=bounds.center.x+(i-(count-1)*.5f);
    var pane=GameObject.CreatePrimitive(PrimitiveType.Quad);pane.name="Rain outside pane "+(i+1);Object.DestroyImmediate(pane.GetComponent<Collider>());pane.transform.SetParent(frame.transform,false);pane.transform.localPosition=new Vector3(x,1.22f,-.32f);pane.transform.localScale=new Vector3(width,1.28f,1);
    var renderer=pane.GetComponent<MeshRenderer>();renderer.sharedMaterial=mat;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;var block=new MaterialPropertyBlock();block.SetFloat("_Seed",panes.Count*.618f);renderer.SetPropertyBlock(block);panes.Add(renderer);
    var flash=new GameObject("Lightning through window");flash.transform.SetParent(frame.transform,false);flash.transform.localPosition=new Vector3(x,1.55f,.27f);flash.transform.localRotation=Quaternion.LookRotation(new Vector3(0,-.42f,1));var light=flash.AddComponent<Light>();light.type=LightType.Spot;light.spotAngle=75;light.innerSpotAngle=65;light.range=9;light.color=new Color(.68f,.8f,1);light.intensity=0;light.enabled=false;light.shadows=LightShadows.Soft;light.shadowStrength=1;light.shadowBias=.025f;light.shadowNormalBias=.08f;light.shadowNearPlane=.05f;light.cookie=cookie;lights.Add(light);
   }
  }
  controller.rainWindows=panes.ToArray();controller.windowLights=lights.ToArray();EditorUtility.SetDirty(controller);AssetDatabase.SaveAssets();Undo.CollapseUndoOperations(undo);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
  bool timing=controller.FlashAt(0)==0&&controller.FlashAt(6.77f)>.99f&&controller.FlashAt(7.5f)==0;
  File.WriteAllText("Temp/window_storm_result.txt","windows="+windows.Count+" rainPanes="+panes.Count+" lightningLights="+lights.Count+" timingChecks="+timing+" shaderErrors="+ShaderUtil.ShaderHasError(shader));
  Selection.activeGameObject=root;EditorApplication.delayCall+=()=>Preview(controller,windows);
 }
 static void Preview(StealthGame.WindowStorm controller,List<MeshRenderer> windows)
 {
  MeshRenderer nearest=null;float distance=float.MaxValue;foreach(var w in windows){float d=(w.bounds.center-controller.viewer.position).sqrMagnitude;if(d<distance){distance=d;nearest=w;}}
  var obj=new GameObject("Window storm preview camera");var cam=obj.AddComponent<Camera>();cam.CopyFrom(controller.viewer.GetComponent<Camera>());cam.enabled=false;
  Vector3 center=nearest.bounds.center;cam.transform.position=center+nearest.transform.forward*2.2f+nearest.transform.right*.65f;cam.transform.LookAt(center);
  var oldViewer=controller.viewer;controller.viewer=cam.transform;controller.ApplyVisuals(1);
  var rt=RenderTexture.GetTemporary(1280,720,24);var previous=RenderTexture.active;Texture2D tex=null;
  try{cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;tex=new Texture2D(1280,720,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1280,720),0,0);tex.Apply();File.WriteAllBytes("Temp/window_storm_preview.png",tex.EncodeToPNG());}
  finally{controller.ApplyVisuals(0);controller.viewer=oldViewer;RenderTexture.active=previous;cam.targetTexture=null;RenderTexture.ReleaseTemporary(rt);if(tex!=null)Object.DestroyImmediate(tex);Object.DestroyImmediate(obj);}
 }
}
