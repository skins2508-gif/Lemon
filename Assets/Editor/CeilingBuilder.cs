using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Collections.Generic;
[InitializeOnLoad]
public static class CeilingBuilder
{
 const string Request="Temp/create_ceiling.request";
 static CeilingBuilder(){ EditorApplication.delayCall += () => { if(File.Exists(Request) && !EditorApplication.isPlaying){Build();File.Delete(Request);} }; }
 [MenuItem("Tools/Level/Add Room Ceilings")]
 public static void Build()
 {
  var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
  if(scene.path!="Assets/_3DStealthGame/Tutorial_Demo/Demo_Scenes/DemoScene.unity")throw new System.InvalidOperationException("Open DemoScene first.");
  if(GameObject.Find("Room Ceilings")!=null)throw new System.InvalidOperationException("Room Ceilings already exists; edit its panels instead.");
  var floors=new List<MeshRenderer>();var heights=new List<float>();
  foreach(var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
  {
   if(r.sharedMaterial!=null && r.sharedMaterial.name.StartsWith("Floor_") && r.bounds.size.y<.05f && r.bounds.size.x>.5f && r.bounds.size.z>.5f)floors.Add(r);
   if(r.name.StartsWith("Wall_") && r.bounds.size.y>2)heights.Add(r.bounds.max.y);
  }
  if(floors.Count==0||heights.Count==0)throw new System.InvalidOperationException("No room floors or walls found.");
  heights.Sort();float underside=heights[heights.Count/2]-.025f;
  const string folder="Assets/Environment/Ceilings";
  Directory.CreateDirectory(folder);AssetDatabase.Refresh();
  string materialPath=folder+"/Ceiling_WarmPlaster.mat";
  var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
  if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));material.name="Ceiling_WarmPlaster";material.SetColor("_BaseColor",new Color(.48f,.43f,.34f));material.SetFloat("_Smoothness",.12f);AssetDatabase.CreateAsset(material,materialPath);}
  Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Add room ceilings");
  var root=new GameObject("Room Ceilings");Undo.RegisterCreatedObjectUndo(root,"Add room ceilings");
  var report=new List<string>();int covered=0;
  foreach(var floor in floors)
  {
   Bounds bounds=floor.bounds;var panel=GameObject.CreatePrimitive(PrimitiveType.Cube);Undo.RegisterCreatedObjectUndo(panel,"Add ceiling panel");
   panel.name="Ceiling - "+floor.transform.parent.name;panel.transform.SetParent(root.transform,false);
   panel.transform.position=new Vector3(bounds.center.x,underside+.04f,bounds.center.z);
   panel.transform.localScale=new Vector3(bounds.size.x+.10f,.08f,bounds.size.z+.10f);
   panel.GetComponent<MeshRenderer>().sharedMaterial=material;
   var collider=panel.GetComponent<BoxCollider>();Physics.SyncTransforms();
   for(int x=-1;x<=1;x++)for(int z=-1;z<=1;z++)
   {
    var point=new Vector3(bounds.center.x+x*bounds.extents.x,1.2f,bounds.center.z+z*bounds.extents.z);
    RaycastHit hit;if(!collider.Raycast(new Ray(point,Vector3.up),out hit,5))throw new System.InvalidOperationException("Ceiling coverage gap: "+panel.name);covered++;
   }
   report.Add(panel.name+" | center="+panel.transform.position.ToString("F3")+" size="+panel.transform.localScale.ToString("F3"));
  }
  Undo.CollapseUndoOperations(group);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
  report.Insert(0,"panels="+floors.Count+" underside="+underside+" coverageChecks="+covered+" allPassed=true");
  File.WriteAllLines("Temp/ceiling_result.txt",report);
  Selection.activeGameObject=root;
  EditorApplication.delayCall+=Capture;
 }
 static void Capture()
 {
  var player=GameObject.Find("Player");var source=player!=null?player.GetComponentInChildren<Camera>():Camera.main;if(source==null)return;
  var obj=new GameObject("Ceiling verification camera");var cam=obj.AddComponent<Camera>();cam.CopyFrom(source);cam.enabled=false;
  cam.transform.position=source.transform.position;cam.transform.rotation=Quaternion.Euler(-30,source.transform.eulerAngles.y,0);
  var rt=RenderTexture.GetTemporary(1000,700,24);var previous=RenderTexture.active;Texture2D tex=null;
  try{cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;tex=new Texture2D(1000,700,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1000,700),0,0);tex.Apply();File.WriteAllBytes("Temp/ceiling_preview.png",tex.EncodeToPNG());}
  finally{RenderTexture.active=previous;cam.targetTexture=null;RenderTexture.ReleaseTemporary(rt);if(tex!=null)Object.DestroyImmediate(tex);Object.DestroyImmediate(obj);}
 }
}
