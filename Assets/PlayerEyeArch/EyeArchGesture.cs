using UnityEngine;
using UnityEngine.InputSystem;

namespace StealthGame
{
    [DefaultExecutionOrder(100)]
    public class EyeArchGesture : MonoBehaviour
    {
        [System.Serializable]
        public class BoneLink
        {
            public Transform source;
            public Transform target;
            public Quaternion rotationOffset;
            public Vector3 restLocalPosition;
            public Quaternion restLocalRotation;
            public bool gestureBone;
            public Vector3 raisedPosition;
            public Quaternion raisedRotation;
        }

        [Tooltip("Press once to raise the hand; press again to lower it.")]
        public UnityEngine.InputSystem.Key activationKey = UnityEngine.InputSystem.Key.E;
        [Min(0.05f)] public float blendDuration = 0.3f;
        public BoneLink[] bones;
        public Transform viewTransform;
        public Vector3 viewOffset = new Vector3(-0.08f, 0.08f, 0.35f);
        [Tooltip("Rotation of the raised hand around its wrist, in camera axes.")]
        public Vector3 handRotationOffset;
        [Range(0, .15f)] public float shoulderLift = .08f;
        [Range(1, 1.15f)] public float armReachScale = 1.08f;
        public bool ExternalControl { get; set; }
        public float PoseWeight => weight;
        public void SetRaised(bool raised) { requested = raised; }
        [SerializeField] private Renderer[] originalRenderers;
        [SerializeField] private GameObject gestureVisual;
        [SerializeField] private bool[] originalEnabled;
        private float weight;
        private bool requested;
        public bool IsRaised => weight > 0.95f;

        private void OnEnable()
        {
            if (originalRenderers != null)
            {
                bool capture = originalEnabled == null || originalEnabled.Length != originalRenderers.Length;
                if (capture) originalEnabled = new bool[originalRenderers.Length];
                for (int i = 0; i < originalRenderers.Length; i++)
                {
                    if (capture) originalEnabled[i] = originalRenderers[i].enabled;
                    originalRenderers[i].enabled = false;
                }
            }
            if (gestureVisual != null) gestureVisual.SetActive(true);
        }

        private void LateUpdate()
        {
            bool canReadInput = Application.isFocused && Time.timeScale > 0f &&
                Cursor.lockState == CursorLockMode.Locked && Keyboard.current != null &&
                true;
            if (!ExternalControl && canReadInput && Keyboard.current[activationKey].wasPressedThisFrame) requested = !requested;
            weight = Mathf.MoveTowards(weight, requested ? 1f : 0f, Time.deltaTime / Mathf.Max(0.05f, blendDuration));
            ApplyPose(weight);
        }

        // Kept public so editor verification can inspect the exact runtime pose without entering Play mode.
        public void ApplyPose(float amount)
        {
            if (bones == null) return;
            Vector3 wrist = Vector3.zero;
            foreach (BoneLink bone in bones)
                if (bone.target != null && bone.target.name == "RightHand") wrist = bone.raisedPosition;
            foreach (BoneLink bone in bones)
            {
                if (bone.target == null) continue;
                if (bone.source != null)
                {
                    bone.target.position = bone.source.position;
                    bone.target.rotation = bone.source.rotation * bone.rotationOffset;
                }
                else
                {
                    bone.target.localPosition = bone.restLocalPosition;
                    bone.target.localRotation = bone.restLocalRotation;
                }
            }
            // Solve the elbow from the original segment lengths so moving the hand
            // toward the forehead bends the arm instead of stretching the sleeve.
            BoneLink arm = null, forearm = null, handLink = null;
            foreach (BoneLink bone in bones)
            {
                if (bone.target == null) continue;
                if (bone.target.name == "RightArm") arm = bone;
                if (bone.target.name == "RightForeArm") forearm = bone;
                if (bone.target.name == "RightHand") handLink = bone;
            }
            Vector3 shoulder = Vector3.zero, elbow = Vector3.zero, goal = Vector3.zero;
            Quaternion armRotation = Quaternion.identity, forearmRotation = Quaternion.identity;
            bool solveArm = arm != null && forearm != null && handLink != null;
            if (solveArm)
            {
                Vector3 oldShoulder = arm.target.position;
                shoulder = oldShoulder + transform.up * shoulderLift;
                Vector3 oldElbow = forearm.target.position, oldWrist = handLink.target.position;
                float upper = Vector3.Distance(oldShoulder, oldElbow) * armReachScale, lower = Vector3.Distance(oldElbow, oldWrist) * armReachScale;
                goal = transform.TransformPoint(wrist);
                if (viewTransform != null)
                    goal = viewTransform.TransformPoint(wrist - transform.InverseTransformPoint(viewTransform.position) + viewOffset);
                Vector3 axis = (goal - shoulder).normalized;
                float distance = Mathf.Clamp(Vector3.Distance(goal, shoulder), Mathf.Abs(upper - lower) + .001f, upper + lower - .001f);
                goal = shoulder + axis * distance;
                float along = (upper * upper - lower * lower + distance * distance) / (2 * distance);
                Vector3 pole = transform.TransformDirection(new Vector3(1, -.8f, .2f));
                Vector3 outward = Vector3.ProjectOnPlane(pole, axis).normalized;
                elbow = shoulder + axis * along + outward * Mathf.Sqrt(Mathf.Max(0, upper * upper - along * along));
                armRotation = Quaternion.FromToRotation(oldElbow - oldShoulder, elbow - shoulder) * arm.target.rotation;
                forearmRotation = Quaternion.FromToRotation(oldWrist - oldElbow, goal - elbow) * forearm.target.rotation;
            }
            foreach (BoneLink bone in bones)
            {
                if (!bone.gestureBone || bone.target == null) continue;
                Vector3 raisedPosition = transform.TransformPoint(bone.raisedPosition);
                Quaternion raisedRotation = transform.rotation * bone.raisedRotation;
                bool hand = bone.target.name == "RightHand" || bone.target.name == "RightHand_end" || bone.target.name.StartsWith("Sight") || bone.target.name.StartsWith("RightHandEND");
                Vector3 posePosition = bone.raisedPosition;
                Quaternion poseRotation = bone.raisedRotation;
                if (hand)
                {
                    Quaternion adjustment = Quaternion.Euler(handRotationOffset);
                    posePosition = wrist + adjustment * (posePosition - wrist);
                    poseRotation = adjustment * poseRotation;
                    raisedPosition = transform.TransformPoint(posePosition);
                    raisedRotation = transform.rotation * poseRotation;
                }
                if (viewTransform != null && bone.target.name != "RightArm")
                {
                    float follow = bone.target.name == "RightForeArm" ? 0.5f : 1f;
                    Vector3 eye = transform.InverseTransformPoint(viewTransform.position);
                    Vector3 cameraPosition = viewTransform.TransformPoint(posePosition - eye + viewOffset);
                    raisedPosition = Vector3.Lerp(raisedPosition, cameraPosition, follow);
                    raisedRotation = Quaternion.Slerp(raisedRotation, viewTransform.rotation * poseRotation, follow);
                }
                if (solveArm)
                {
                    if (bone == arm) { raisedPosition = shoulder; raisedRotation = armRotation; }
                    else if (bone == forearm) { raisedPosition = elbow; raisedRotation = forearmRotation; }
                    else if (hand)
                    {
                        Quaternion reference = viewTransform != null ? viewTransform.rotation : transform.rotation;
                        raisedPosition = goal + reference * (posePosition - wrist);
                    }
                }
                bone.target.position = Vector3.Lerp(bone.target.position, raisedPosition, amount);
                bone.target.rotation = Quaternion.Slerp(bone.target.rotation, raisedRotation, amount);
            }
        }

        private void OnDisable()
        {
            weight = 0f;
            requested = false;
            if (originalEnabled != null)
                for (int i = 0; i < originalEnabled.Length; i++)
                    if (originalRenderers[i] != null) originalRenderers[i].enabled = originalEnabled[i];
            if (gestureVisual != null) gestureVisual.SetActive(false);
        }
    }
}




