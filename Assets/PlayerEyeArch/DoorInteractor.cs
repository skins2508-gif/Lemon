using UnityEngine;using UnityEngine.InputSystem;
namespace StealthGame
{
 [DisallowMultipleComponent]
 [RequireComponent(typeof(PlayerMovement))]
 public class DoorInteractor:MonoBehaviour
 {
  public Camera viewCamera;
  [Min(.5f)]public float interactionDistance=2.5f;
  public UnityEngine.InputSystem.Key interactionKey=UnityEngine.InputSystem.Key.F;
  public bool showPrompt=true;
  PlayerMovement player;
  Door target;
  GUIStyle promptStyle;
  void Awake(){player=GetComponent<PlayerMovement>();if(viewCamera==null)viewCamera=GetComponentInChildren<Camera>();}
  void Update()
  {
   target=null;if(viewCamera==null||Time.timeScale<=0||!Application.isFocused||Cursor.lockState!=CursorLockMode.Locked)return;
   var hits=Physics.RaycastAll(viewCamera.transform.position,viewCamera.transform.forward,interactionDistance,~0,QueryTriggerInteraction.Ignore);
   System.Array.Sort(hits,(a,b)=>a.distance.CompareTo(b.distance));
   foreach(var hit in hits){if(hit.transform.IsChildOf(transform))continue;target=hit.collider.GetComponentInParent<Door>();break;}
   if(target!=null&&Keyboard.current!=null&&Keyboard.current[interactionKey].wasPressedThisFrame)target.TryInteract(player);
  }
  void OnGUI()
  {
   if(!showPrompt||target==null||target.IsMoving)return;
   if(promptStyle==null){promptStyle=new GUIStyle(GUI.skin.label);promptStyle.alignment=TextAnchor.MiddleCenter;promptStyle.fontSize=20;promptStyle.normal.textColor=Color.white;}
   string action=target.IsOpen?"Close":target.CanOpen(player)?"Open":"Locked - key required";
   GUI.Label(new Rect(Screen.width*.5f-180,Screen.height*.76f,360,35),"["+interactionKey+"] "+action,promptStyle);
  }
 }
}
