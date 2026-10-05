using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;using System.IO;
[InitializeOnLoad] public static class RestoreLemonSkillAudio
{
 static RestoreLemonSkillAudio(){EditorApplication.delayCall+=()=>{if(!File.Exists("Temp/restore_lemon_audio.request")||EditorApplication.isPlaying)return;var p=GameObject.Find("Player");if(p==null)return;var s=p.GetComponent<StealthGame.LemonSightSkill>();if(s==null)return;Undo.RecordObject(s,"Restore E skill volume");s.soundVolume=.6f;EditorUtility.SetDirty(s);PrefabUtility.RecordPrefabInstancePropertyModifications(s);EditorSceneManager.MarkSceneDirty(p.scene);EditorSceneManager.SaveScene(p.scene);File.WriteAllText("Temp/restore_lemon_audio.result","soundVolume="+s.soundVolume);File.Delete("Temp/restore_lemon_audio.request");};}
}
