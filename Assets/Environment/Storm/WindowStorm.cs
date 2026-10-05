using UnityEngine;
namespace StealthGame
{
 [DisallowMultipleComponent]
 public class WindowStorm : MonoBehaviour
 {
  public AudioSource stormAudio;
  public Transform viewer;
  public Light[] windowLights;
  public Renderer[] rainWindows;
  [Tooltip("Lightning times in seconds within the looping rain/thunder clip.")]
  public float[] lightningTimes = {6.77f,14.77f,26.97f,34.57f,51.32f};
  [Min(0)] public float lightningIntensity=18f;
  [Range(1,8)] public int maximumNearbyLights=4;
  [Min(1)] public float visibleDistance=18f;
  [Range(.1f,3)] public float rainSpeed=1f;
  [Range(.1f,2)] public float rainDensity=1f;
  [Range(0,3)] public float windowFlashBrightness=1f;
  MaterialPropertyBlock block;
  public float FlashAt(float clipTime)
  {
   float flash=0;
   if(lightningTimes==null)return 0;
   foreach(float cue in lightningTimes)
   {
    float t=clipTime-cue;
    if(t>=0 && t<.60f)
    {
     float a=Mathf.Clamp01(1-t/.13f);
     float b=t>=.19f?Mathf.Clamp01(1-(t-.19f)/.22f)*.7f:0;
     float c=t>=.46f?Mathf.Clamp01(1-(t-.46f)/.14f)*.25f:0;
     flash=Mathf.Max(flash,Mathf.Max(a,Mathf.Max(b,c)));
    }
   }
   return flash;
  }
  void Update(){ApplyVisuals(stormAudio!=null && stormAudio.isPlaying?FlashAt(stormAudio.time):0);}
  public void ApplyVisuals(float flash)
  {
   if(block==null)block=new MaterialPropertyBlock();
   if(rainWindows!=null)foreach(var r in rainWindows)if(r!=null){r.GetPropertyBlock(block);block.SetFloat("_Flash",flash*windowFlashBrightness);block.SetFloat("_RainSpeed",rainSpeed);block.SetFloat("_RainDensity",rainDensity);r.SetPropertyBlock(block);}
   if(windowLights==null)return;
   for(int i=0;i<windowLights.Length;i++)
   {
    var light=windowLights[i];if(light==null)continue;
    bool show=flash>.005f && viewer!=null;
    if(show)
    {
     float distance=(light.transform.position-viewer.position).sqrMagnitude;int rank=0;
     if(distance>visibleDistance*visibleDistance)show=false;
     else for(int j=0;j<windowLights.Length;j++)if(j!=i&&windowLights[j]!=null){float other=(windowLights[j].transform.position-viewer.position).sqrMagnitude;if(other<distance || (Mathf.Approximately(other,distance)&&j<i))rank++;}
     show=show&&rank<maximumNearbyLights;
    }
    light.enabled=show;light.intensity=show?lightningIntensity*flash:0;
   }
  }
  void OnDisable(){ApplyVisuals(0);}
 }
}
