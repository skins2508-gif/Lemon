using System;
using UnityEngine;

namespace StealthGame
{
    [DisallowMultipleComponent, RequireComponent(typeof(PlayerMovement))]
    public class PlayerFootstepNoise : MonoBehaviour
    {
        [Header("Footsteps (independent of audio volume)")]
        [Min(.1f)] public float strideDistance = .6f;
        [Range(0, 1)] public float startingLoudness = .15f;
        [Min(.1f)] public float buildupSeconds = 4f;
        [Min(.01f)] public float minimumSpeed = .08f;
        [Min(.1f)] public float meterFallSpeed = .45f;
        public float Level { get; private set; }
        public event Action<Vector3, float> Stepped;
        PlayerMovement movement;
        FirstPersonController controller;
        Rigidbody body;
        Vector3 previous;
        float distance;
        bool walking;

        void Awake() { movement = GetComponent<PlayerMovement>(); controller = GetComponent<FirstPersonController>(); body = GetComponent<Rigidbody>(); }
        void OnEnable() { previous = transform.position; distance = 0; walking = false; Level = 0; }
        void FixedUpdate()
        {
            Vector3 now = body != null ? body.position : transform.position;
            float travelled = Vector3.ProjectOnPlane(now - previous, Vector3.up).magnitude;
            previous = now;
            bool input = controller != null && controller.isActiveAndEnabled
                ? controller.MovementInput.sqrMagnitude > .01f
                : movement.MoveAction != null && movement.MoveAction.enabled && movement.MoveAction.ReadValue<Vector2>().sqrMagnitude > .01f;
            bool moving = movement.isActiveAndEnabled && input && travelled > minimumSpeed * Time.fixedDeltaTime && travelled < .5f;
            if (!moving) { walking = false; distance = 0; Level = Mathf.MoveTowards(Level, 0, meterFallSpeed * Time.fixedDeltaTime); return; }
            float speedRatio = Mathf.Clamp01(travelled / Time.fixedDeltaTime / Mathf.Max(.1f, movement.walkSpeed));
            Level = Mathf.Clamp01(Mathf.Max(startingLoudness, Level) + Time.fixedDeltaTime * speedRatio / Mathf.Max(.1f, buildupSeconds));
            distance += travelled;
            if (!walking || distance >= strideDistance)
            {
                distance = 0;
                Stepped?.Invoke(now + Vector3.up * .15f, Level);
            }
            walking = true;
        }
    }
}
