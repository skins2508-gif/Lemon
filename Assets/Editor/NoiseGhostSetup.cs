using UnityEngine;using UnityEngine.AI;using UnityEditor;using UnityEditor.SceneManagement;using System.IO;using System.Collections.Generic;using StealthGame;
[InitializeOnLoad]public static class NoiseGhostSetup
{
 const string Root="Assets/NoiseGhost/";
 static NoiseGhostSetup(){EditorApplication.delayCall+=()=>{if(!File.Exists("Temp/noise_ghost_setup.request")||EditorApplication.isPlaying)return;File.Delete("Temp/noise_ghost_setup.request");try{Run();}catch(System.Exception e){File.WriteAllText("Temp/noise_ghost_setup_error.txt",e.ToString());Debug.LogException(e);}};}
 static void Run()
 {
  var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(!scene.path.EndsWith("DemoScene.unity"))throw new System.Exception("DemoScene required");
  if(GameObject.Find("Footstep Ghost System")!=null)throw new System.Exception("System already exists");
  var sources=new List<NavMeshBuildSource>();var marks=new List<NavMeshBuildMarkup>();foreach(var door in Object.FindObjectsByType<Door>(FindObjectsSortMode.None))marks.Add(new NavMeshBuildMarkup{root=door.transform,ignoreFromBuild=true});
  NavMeshBuilder.CollectSources(GameObject.Find("Level A").transform,~0,NavMeshCollectGeometry.RenderMeshes,0,marks,sources);
  var settings=NavMesh.GetSettingsByIndex(0);settings.agentRadius=.23f;settings.agentHeight=1.35f;settings.agentClimb=.22f;settings.agentSlope=45;settings.overrideVoxelSize=true;settings.voxelSize=.06f;settings.minRegionArea=.15f;
  var data=NavMeshBuilder.BuildNavMeshData(settings,sources,new Bounds(new Vector3(-4,1.2f,3),new Vector3(52,5,28)),Vector3.zero,Quaternion.identity);
  if(data==null)throw new System.Exception("Navigation build failed");AssetDatabase.CreateAsset(data,Root+"FootstepNavigation.asset");var navInstance=NavMesh.AddNavMeshData(data);
  try{
   var system=new GameObject("Footstep Ghost System");Undo.RegisterCreatedObjectUndo(system,"Add two hearing ghosts");system.AddComponent<NoiseGhostNavigation>().navigationData=data;
   var player=GameObject.Find("Player");var noise=player.GetComponent<PlayerFootstepNoise>();if(noise==null)noise=Undo.AddComponent<PlayerFootstepNoise>(player);if(player.GetComponent<FootstepNoiseHud>()==null)Undo.AddComponent<FootstepNoiseHud>(player);
   var model=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"Art/FunnyCuteGhost.fbx");if(model==null)throw new System.Exception("Missing ghost FBX");
   var white=new Material(Shader.Find("Universal Render Pipeline/Lit"));white.name="Ghost Pearl";white.color=new Color(.86f,.90f,.94f);white.SetFloat("_Smoothness",.35f);white.EnableKeyword("_EMISSION");white.SetColor("_EmissionColor",new Color(.10f,.13f,.16f));AssetDatabase.CreateAsset(white,Root+"Art/GhostPearl.mat");
   var black=new Material(white);black.name="Ghost Face";black.color=new Color(.012f,.009f,.021f);black.SetColor("_EmissionColor",Color.black);AssetDatabase.CreateAsset(black,Root+"Art/GhostFace.mat");
   var boundary=GameObject.Find("Door_Red").transform.position;var report=new List<string>{"nav sources="+sources.Count};
   var triangulation=NavMesh.CalculateTriangulation();var candidates=new List<Vector3>();
   for(int i=0;i<triangulation.indices.Length;i+=3){var p=(triangulation.vertices[triangulation.indices[i]]+triangulation.vertices[triangulation.indices[i+1]]+triangulation.vertices[triangulation.indices[i+2]])/3;if(p.y>.35f||p.y<-.2f)continue;bool near=false;foreach(var q in candidates)if((q-p).sqrMagnitude<2.25f){near=true;break;}if(!near)candidates.Add(p);}
   foreach(bool positive in new[]{false,true})
   {
    var g=new GameObject(positive?"Footstep Ghost - Red Door East":"Footstep Ghost - Red Door West");g.transform.SetParent(system.transform);Vector3 proposed=positive?new Vector3(-1.3f,0,5.6f):new Vector3(-4.8f,0,5.6f);
    if(!NavMesh.SamplePosition(proposed,out var spawn,2,NavMesh.AllAreas))throw new System.Exception("No navigation near red door "+positive);g.transform.position=spawn.position;
    var visual=(GameObject)PrefabUtility.InstantiatePrefab(model);visual.name="Funny Cute Ghost Visual";visual.transform.SetParent(g.transform,false);visual.transform.localRotation=Quaternion.Euler(0,180,0);
    var renderers=visual.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);float scale=1.20f/bounds.size.y;visual.transform.localScale=Vector3.one*scale;
    bounds=renderers[0].bounds;foreach(var r in renderers){bounds.Encapsulate(r.bounds);r.sharedMaterial=r.name=="Sphere"?white:black;GameObjectUtility.SetStaticEditorFlags(r.gameObject,0);}
    visual.transform.position+=new Vector3(g.transform.position.x-bounds.center.x,g.transform.position.y+.15f-bounds.min.y,g.transform.position.z-bounds.center.z);
    var agent=g.AddComponent<NavMeshAgent>();agent.agentTypeID=settings.agentTypeID;agent.radius=.23f;agent.height=1.35f;agent.speed=.7f;agent.acceleration=5;agent.angularSpeed=180;agent.stoppingDistance=.2f;agent.autoBraking=true;
    var ghost=g.AddComponent<FootstepGhost>();ghost.playerNoise=noise;ghost.gameEnding=Object.FindFirstObjectByType<GameEnding>();ghost.visual=visual.transform;ghost.boundaryPoint=boundary;ghost.boundaryNormal=Vector3.right;ghost.positiveSide=positive;
    var points=new List<Vector3>();foreach(var point in candidates){if(!ghost.InRegion(point))continue;var path=new NavMeshPath();if(!NavMesh.CalculatePath(spawn.position,point,NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete)continue;bool allowed=true;foreach(var c in path.corners)if(!ghost.InRegion(c)){allowed=false;break;}if(allowed)points.Add(point);}
    if(points.Count<3)throw new System.Exception("Too few patrol destinations: "+positive+" "+points.Count);ghost.patrolPoints=points.ToArray();
    // Keep the two regional instances editable; prefab stores the model and tuning defaults.
    PrefabUtility.SaveAsPrefabAsset(g,Root+(positive?"HearingGhostEast.prefab":"HearingGhostWest.prefab"));
    report.Add(g.name+" spawn="+spawn.position+" patrol points="+points.Count);
   }
   var sight=player.GetComponent<LemonSightSkill>();Undo.RecordObject(sight,"Add hearing ghosts to lemon sight");var targets=new List<Renderer>(sight.targets);targets.AddRange(system.GetComponentsInChildren<Renderer>());sight.targets=targets.ToArray();PrefabUtility.RecordPrefabInstancePropertyModifications(sight);
   AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);File.WriteAllLines("Temp/noise_ghost_setup_result.txt",report);
  }finally{navInstance.Remove();}
 }
}
