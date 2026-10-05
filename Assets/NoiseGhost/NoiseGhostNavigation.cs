using UnityEngine;
using UnityEngine.AI;
namespace StealthGame
{
    [ExecuteAlways, DefaultExecutionOrder(-1000)]
    public class NoiseGhostNavigation : MonoBehaviour
    {
        public NavMeshData navigationData;
        NavMeshDataInstance instance;
        void OnEnable(){if(navigationData!=null)instance=NavMesh.AddNavMeshData(navigationData);}
        void OnDisable(){if(instance.valid)instance.Remove();}
    }
}
