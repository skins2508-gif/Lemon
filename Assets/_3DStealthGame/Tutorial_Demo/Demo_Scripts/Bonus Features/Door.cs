using UnityEngine;
namespace StealthGame
{
 [DisallowMultipleComponent]
 public class Door : MonoBehaviour
 {
  public string KeyName="key1";
  public bool requiresKey=true;
  [Header("Opening")]
  public Vector3 localHinge=new Vector3(-.65f,0,.18f);
  [Range(45,140)] public float openAngle=100f;
  [Min(.1f)] public float openingSeconds=.65f;
  public bool openAwayFromPlayer=true;
  [Tooltip("When opening away from the player is disabled, choose the inward swing direction.")]
  public bool reverseOpeningDirection;
  [Header("Sounds")]
  public AudioClip openingSound;
  public AudioClip lockedSound;
  [Range(0,1)] public float soundVolume=.6f;
  [Min(.1f)] public float lockedSoundCooldown=.7f;
  public bool IsOpen {get;private set;}
  public bool IsMoving=>Mathf.Abs(angle-targetAngle)>.1f;
  Quaternion closedRotation;
  Vector3 closedPosition;
  float angle,targetAngle,lastLockedSound=-100f;
  AudioSource audioSource;
  bool initialized;
  public bool CanOpen(PlayerMovement player)=>!requiresKey || (player!=null && player.OwnKey(KeyName));
  void Awake(){Initialize();}
  void Initialize()
  {
   if(initialized)return;initialized=true;closedPosition=transform.localPosition;closedRotation=transform.localRotation;
   audioSource=gameObject.AddComponent<AudioSource>();audioSource.playOnAwake=false;audioSource.spatialBlend=1;audioSource.minDistance=1;audioSource.maxDistance=10;audioSource.rolloffMode=AudioRolloffMode.Linear;
  }
  public bool TryInteract(PlayerMovement player)
  {
   if(player==null||!isActiveAndEnabled||IsMoving)return false;
   Initialize();
   if(!IsOpen && !CanOpen(player))
   {
    if(Time.unscaledTime-lastLockedSound>=lockedSoundCooldown){Play(lockedSound);lastLockedSound=Time.unscaledTime;}
    return false;
   }
   if(IsOpen)
   {
    // Keep an occupied doorway open instead of closing onto the player.
    var box=GetComponent<BoxCollider>();
    if(box!=null){var parent=transform.parent;var center=closedPosition+closedRotation*Vector3.Scale(box.center,transform.localScale);if(parent!=null)center=parent.TransformPoint(center);Vector3 offset=player.transform.position-center;offset.y=0;if(offset.magnitude<.7f)return false;}
    IsOpen=false;targetAngle=0;
   }
   else
   {
    float sign=reverseOpeningDirection?-1:1;
    if(openAwayFromPlayer){float side=Vector3.Dot(player.transform.position-transform.position,transform.forward);sign=(side>=0?1:-1)*(localHinge.x<=0?1:-1);}
    targetAngle=openAngle*sign;IsOpen=true;Play(openingSound);
   }
   return true;
  }
  void Play(AudioClip clip){if(clip!=null)audioSource.PlayOneShot(clip,soundVolume);}
  public bool TryOpenForNoiseGhost(Vector3 visitorPosition)
  {
   if(requiresKey||!isActiveAndEnabled||IsMoving)return false;
   if(IsOpen)return true;
   Initialize();float sign=reverseOpeningDirection?-1:1;
   if(openAwayFromPlayer)sign=(Vector3.Dot(visitorPosition-transform.position,transform.forward)>=0?1:-1)*(localHinge.x<=0?1:-1);
   targetAngle=openAngle*sign;IsOpen=true;Play(openingSound);return true;
  }
  void Update()
  {
   if(!initialized||!IsMoving)return;
   angle=Mathf.MoveTowards(angle,targetAngle,openAngle/Mathf.Max(.1f,openingSeconds)*Time.deltaTime);
   Quaternion rotation=Quaternion.Euler(0,angle,0);Vector3 hinge=Vector3.Scale(localHinge,transform.localScale);
   transform.localPosition=closedPosition+closedRotation*(hinge-rotation*hinge);transform.localRotation=closedRotation*rotation;
  }
 }
}
