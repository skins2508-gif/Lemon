using UnityEngine;
using UnityEngine.AI;

namespace StealthGame
{
    [DisallowMultipleComponent, RequireComponent(typeof(NavMeshAgent))]
    public class FootstepGhost : MonoBehaviour
    {
        public enum Behaviour { Patrol, Investigate, Search }
        [Header("References")]
        public PlayerFootstepNoise playerNoise;
        public GameEnding gameEnding;
        public Transform visual;
        [Header("Region: plane through the closed red door")]
        public Vector3 boundaryPoint;
        public Vector3 boundaryNormal = Vector3.right;
        public bool positiveSide;
        public Vector3[] patrolPoints;
        [Header("Hearing")]
        [Min(.1f)] public float hearingDistance = 14;
        [Range(0,1)] public float throughWallMultiplier = .55f;
        [Min(.1f)] public float memorySeconds = 5;
        [Min(.1f)] public float searchSeconds = 4;
        [Header("Movement")]
        [Min(.1f)] public float patrolSpeed = .7f;
        [Min(.1f)] public float investigateSpeed = 1.2f;
        [Min(.1f)] public float patrolPause = 1;
        [Header("Contact")]
        public bool catchPlayerOnContact = true;
        [Min(.1f)] public float catchDistance = .55f;
        [Header("Floating")]
        public float floatAmplitude = .06f;
        public float floatFrequency = 1.2f;
        public Behaviour State { get; private set; }
        public Vector3 LastHeardPosition { get; private set; }
        public int HeardCount { get; private set; }
        NavMeshAgent agent;
        Vector3 visualOrigin, lastPosition;
        float nextPatrol, heardAt, searchUntil, blockedTime, nextDoorCheck;
        int nextPoint;
        bool caught;
        void Awake(){agent=GetComponent<NavMeshAgent>();if(visual!=null)visualOrigin=visual.localPosition;lastPosition=transform.position;}
        void OnEnable(){if(playerNoise!=null)playerNoise.Stepped+=Hear;}
        void OnDisable(){if(playerNoise!=null)playerNoise.Stepped-=Hear;}
        void Start(){nextPoint=Random.Range(0,Mathf.Max(1,patrolPoints==null?0:patrolPoints.Length));agent.autoRepath=false;agent.speed=patrolSpeed;}
        public bool InRegion(Vector3 point){float side=Vector3.Dot(point-boundaryPoint,boundaryNormal.normalized);return positiveSide?side>=.08f:side<=-.08f;}
        public bool TryRoute(Vector3 destination, out NavMeshPath path)
        {
            path=new NavMeshPath();
            if(!agent.isOnNavMesh || !NavMesh.SamplePosition(destination,out var hit,1,agent.areaMask) || !InRegion(hit.position))return false;
            if(!agent.CalculatePath(hit.position,path)||path.status!=NavMeshPathStatus.PathComplete)return false;
            foreach(var corner in path.corners)if(!InRegion(corner))return false;
            return path.corners.Length>0;
        }
        public void Hear(Vector3 position,float loudness)
        {
            if(!isActiveAndEnabled||loudness<=0||!InRegion(position))return;
            float range=hearingDistance*Mathf.Clamp01(loudness);
            if(Vector3.Distance(transform.position,position)>range)return;
            Vector3 ear=transform.position+Vector3.up*.8f, source=position+Vector3.up*.5f;
            if(Obstructed(ear,source))range*=throughWallMultiplier;
            if(Vector3.Distance(transform.position,position)>range||!TryRoute(position,out var path))return;
            LastHeardPosition=position;HeardCount++;heardAt=Time.time;State=Behaviour.Investigate;
            agent.speed=investigateSpeed;agent.isStopped=false;agent.SetPath(path);
        }
        bool Obstructed(Vector3 from,Vector3 to)
        {
            var delta=to-from;
            foreach(var hit in Physics.RaycastAll(from,delta.normalized,delta.magnitude,~0,QueryTriggerInteraction.Ignore))
                if(!hit.transform.IsChildOf(transform) && (playerNoise==null||!hit.transform.IsChildOf(playerNoise.transform)))return true;
            return false;
        }
        void Search(){State=Behaviour.Search;searchUntil=Time.time+searchSeconds;agent.ResetPath();agent.isStopped=false;}
        void Update()
        {
            if(Time.timeScale<=0||!agent.isOnNavMesh)return;
            if(visual!=null)visual.localPosition=visualOrigin+Vector3.up*(Mathf.Sin(Time.time*floatFrequency*Mathf.PI*2)*floatAmplitude);
            if(catchPlayerOnContact&&!caught&&playerNoise!=null&&Vector3.Distance(transform.position,playerNoise.transform.position)<catchDistance&&!Obstructed(transform.position+Vector3.up*.7f,playerNoise.transform.position+Vector3.up*.7f))
            {caught=true;if(gameEnding!=null)gameEnding.CaughtPlayer();}
            if(State==Behaviour.Investigate && (Time.time-heardAt>memorySeconds || (!agent.pathPending && agent.remainingDistance<.3f)))Search();
            if(State==Behaviour.Search){transform.Rotate(Vector3.up,45*Time.deltaTime);if(Time.time>=searchUntil){State=Behaviour.Patrol;nextPatrol=Time.time;}return;}
            if(State==Behaviour.Patrol&&!agent.pathPending&&(!agent.hasPath||agent.remainingDistance<.3f)&&Time.time>=nextPatrol)
            {
                agent.speed=patrolSpeed;nextPatrol=Time.time+patrolPause;
                if(patrolPoints!=null)for(int i=0;i<patrolPoints.Length;i++){var p=patrolPoints[nextPoint++%patrolPoints.Length];if(Vector3.Distance(p,transform.position)<1||!TryRoute(p,out var path))continue;agent.isStopped=false;agent.SetPath(path);break;}
            }
            CheckDoor();
            if(agent.hasPath && Vector3.Distance(lastPosition,transform.position)<.003f)blockedTime+=Time.deltaTime;else blockedTime=0;
            lastPosition=transform.position;
            if(blockedTime>3){agent.ResetPath();agent.isStopped=false;blockedTime=0;if(State==Behaviour.Investigate)Search();else nextPatrol=Time.time;}
        }
        void CheckDoor()
        {
            if(Time.time<nextDoorCheck)return;nextDoorCheck=Time.time+.1f;
            if(!agent.hasPath)return;
            var direction=agent.steeringTarget-transform.position;direction.y=0;
            if(direction.sqrMagnitude<.01f)return;
            bool blocked=false;
            foreach(var hit in Physics.SphereCastAll(transform.position+Vector3.up*.75f,.17f,direction.normalized,.75f,~0,QueryTriggerInteraction.Ignore))
            {
                var door=hit.collider.GetComponentInParent<Door>();if(door==null)continue;
                if(!door.IsOpen){if(!door.requiresKey)door.TryOpenForNoiseGhost(transform.position);blocked=true;}
                else if(door.IsMoving)blocked=true;
            }
            agent.isStopped=blocked;
        }
    }
}
