using UnityEngine;
using UnityEngine.InputSystem;

namespace StealthGame
{
    /// <summary>
    /// Add beside PlayerMovement. Assign a child camera or a dedicated child pivot.
    /// PlayerMovement owns physics, animation, audio and the existing key inventory.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerMovement), typeof(Rigidbody))]
    public class FirstPersonController : MonoBehaviour
    {
        [Header("View")]
        [Tooltip("Child camera or dedicated child pivot. Position it at eye height in the scene.")]
        [SerializeField] private Transform viewTransform;
        [SerializeField] private bool hidePlayerMesh = false;
        [Tooltip("Degrees per pixel of mouse movement. Mouse delta is not multiplied by deltaTime.")]
        [SerializeField, Min(0f)] private float horizontalSensitivity = 0.1f;
        [SerializeField, Min(0f)] private float verticalSensitivity = 0.1f;
        [SerializeField] private bool invertY;
        [SerializeField, Range(-89f, 0f)] private float minimumPitch = -85f;
        [SerializeField, Range(0f, 89f)] private float maximumPitch = 85f;

        [Header("Cursor")]
        [Tooltip("Escape releases the cursor; left click captures it again.")]
        [SerializeField] private bool lockCursorOnEnable = true;

        private float yaw;
        private float pitch;
        private bool hasFocus;
        private bool ownsCursor;
        private Renderer[] playerRenderers;
        private UnityEngine.Rendering.ShadowCastingMode[] originalShadowModes;

        public Vector2 MovementInput { get; private set; }
        public Quaternion BodyRotation => Quaternion.Euler(0f, yaw, 0f);

        private void OnEnable()
        {
            if (viewTransform == null || viewTransform == transform || !viewTransform.IsChildOf(transform))
            {
                Debug.LogWarning("FirstPersonController needs a child camera or child view pivot assigned.", this);
                enabled = false;
                return;
            }

            yaw = transform.eulerAngles.y;
            pitch = Mathf.Clamp(Mathf.DeltaAngle(0f, viewTransform.eulerAngles.x), minimumPitch, maximumPitch);
            hasFocus = Application.isFocused;
            if (hidePlayerMesh)
            {
                playerRenderers = GetComponentsInChildren<Renderer>();
                originalShadowModes = new UnityEngine.Rendering.ShadowCastingMode[playerRenderers.Length];
                for (int i = 0; i < playerRenderers.Length; i++)
                {
                    originalShadowModes[i] = playerRenderers[i].shadowCastingMode;
                    playerRenderers[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
                }
            }
            if (lockCursorOnEnable && hasFocus)
                CaptureCursor();
        }

        private void Update()
        {
            MovementInput = Vector2.zero;
            if (!hasFocus || Time.timeScale == 0f)
                return;

            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                ReleaseCursor();
                return;
            }

            if (Cursor.lockState != CursorLockMode.Locked)
            {
                if (mouse != null && mouse.leftButton.wasPressedThisFrame)
                    CaptureCursor();
                return;
            }

            if (keyboard != null)
            {
                MovementInput = Vector2.ClampMagnitude(new Vector2(
                    (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f),
                    (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f)), 1f);
            }

            if (mouse == null)
                return;

            Vector2 delta = mouse.delta.ReadValue();
            yaw = Mathf.Repeat(yaw + delta.x * horizontalSensitivity, 360f);
            pitch = Mathf.Clamp(pitch + delta.y * verticalSensitivity * (invertY ? 1f : -1f),
                minimumPitch, maximumPitch);
        }

        private void LateUpdate()
        {
            // World rotation avoids waiting for the next physics step to turn the view.
            viewTransform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        private void OnApplicationFocus(bool focused)
        {
            hasFocus = focused;
            if (!focused)
            {
                MovementInput = Vector2.zero;
                ReleaseCursor();
            }
        }

        private void OnDisable()
        {
            MovementInput = Vector2.zero;
            ReleaseCursor();
            if (playerRenderers != null)
            {
                for (int i = 0; i < playerRenderers.Length; i++)
                    if (playerRenderers[i] != null)
                        playerRenderers[i].shadowCastingMode = originalShadowModes[i];
                playerRenderers = null;
            }
        }

        private void CaptureCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            ownsCursor = true;
        }

        private void ReleaseCursor()
        {
            if (!ownsCursor)
                return;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            ownsCursor = false;
        }
    }
}
