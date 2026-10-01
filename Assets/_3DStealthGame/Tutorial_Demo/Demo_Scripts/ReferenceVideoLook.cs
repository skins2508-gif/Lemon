using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace StealthGame
{
    /// <summary>
    /// Keeps the game presentation identical to the 16:9 Unity Play reference,
    /// including its barrel distortion and horror color treatment.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class ReferenceVideoLook : MonoBehaviour
    {
        const float TargetAspect = 16f / 9f;
        const float CameraTransitionDuration = 32f;

        [SerializeField, Range(0f, 1f)]
        float m_AnimationProgress;

        Camera m_Camera;
        Animator m_CameraAnimator;
        Transform m_Player;
        LensDistortion m_Lens;
        Vignette m_Vignette;
        Bloom m_Bloom;
        ColorAdjustments m_Color;
        Vector3 m_LastPlayerPosition;
        float m_TraveledDistance;
        float m_MovementTime;
        float m_CurrentSpeed;
        int m_LastWidth;
        int m_LastHeight;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void RegisterSceneCallback()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "DemoScene")
                return;

            Camera camera = Camera.main;
            if (camera != null && camera.GetComponent<ReferenceVideoLook>() == null)
                camera.gameObject.AddComponent<ReferenceVideoLook>();
        }

        void Awake()
        {
            m_Camera = GetComponent<Camera>();
            ApplyReferenceLook();
            ConfigureCameraAnimator();
            GameObject player = GameObject.Find("Player");
            if (player != null)
            {
                m_Player = player.transform;
                m_LastPlayerPosition = m_Player.position;
            }
            UpdateAspect(force: true);

#if !UNITY_EDITOR
            Screen.SetResolution(1280, 720, FullScreenMode.Windowed);
#endif
        }

        void Update()
        {
            AnimateLookFromMovement();
            UpdateAspect(force: false);
        }

        void OnGUI()
        {
            Rect viewport = m_Camera.rect;
            if (viewport == new Rect(0f, 0f, 1f, 1f))
                return;

            Color previousColor = GUI.color;
            GUI.color = Color.black;

            float left = viewport.x * Screen.width;
            float right = (1f - viewport.xMax) * Screen.width;
            float bottom = viewport.y * Screen.height;
            float top = (1f - viewport.yMax) * Screen.height;

            if (left > 0f)
                GUI.DrawTexture(new Rect(0f, 0f, left, Screen.height), Texture2D.whiteTexture);
            if (right > 0f)
                GUI.DrawTexture(new Rect(Screen.width - right, 0f, right, Screen.height), Texture2D.whiteTexture);
            if (top > 0f)
                GUI.DrawTexture(new Rect(0f, 0f, Screen.width, top), Texture2D.whiteTexture);
            if (bottom > 0f)
                GUI.DrawTexture(new Rect(0f, Screen.height - bottom, Screen.width, bottom), Texture2D.whiteTexture);

            GUI.color = previousColor;
        }

        void ApplyReferenceLook()
        {
            m_Camera.allowHDR = true;
            m_Camera.fieldOfView = 60f;

            UniversalAdditionalCameraData cameraData =
                m_Camera.GetUniversalAdditionalCameraData();
            cameraData.renderPostProcessing = true;
            cameraData.volumeLayerMask = 1;

            Volume globalVolume = FindFirstObjectByType<Volume>();
            if (globalVolume == null || globalVolume.sharedProfile == null)
                return;

            VolumeProfile profile = globalVolume.sharedProfile;

            if (profile.TryGet(out m_Lens))
            {
                m_Lens.active = true;
                m_Lens.intensity.Override(0.28f);
                m_Lens.xMultiplier.Override(1f);
                m_Lens.yMultiplier.Override(1f);
                m_Lens.center.Override(new Vector2(0.5f, 0.5f));
                m_Lens.scale.Override(1.1f);
            }

            if (profile.TryGet(out m_Vignette))
            {
                m_Vignette.active = true;
                m_Vignette.color.Override(Color.black);
                m_Vignette.center.Override(new Vector2(0.5f, 0.5f));
                m_Vignette.intensity.Override(0.363f);
                m_Vignette.smoothness.Override(0.3f);
            }

            if (profile.TryGet(out m_Bloom))
            {
                m_Bloom.active = true;
                m_Bloom.intensity.Override(0.2f);
                m_Bloom.scatter.Override(0.5f);
            }

            if (profile.TryGet(out Tonemapping tonemapping))
            {
                tonemapping.active = true;
                tonemapping.mode.Override(TonemappingMode.ACES);
            }

            if (profile.TryGet(out m_Color))
            {
                m_Color.active = true;
                m_Color.postExposure.Override(0.63f);
                m_Color.colorFilter.Override(new Color(1.08f, 0.78f, 1.05f));
                m_Color.contrast.Override(8f);
                m_Color.saturation.Override(8f);
            }
        }

        void ConfigureCameraAnimator()
        {
            RuntimeAnimatorController controller =
                Resources.Load<RuntimeAnimatorController>("Camera_32s");
            if (controller == null)
                return;

            m_CameraAnimator = GetComponent<Animator>();
            if (m_CameraAnimator == null)
                m_CameraAnimator = gameObject.AddComponent<Animator>();

            m_CameraAnimator.runtimeAnimatorController = controller;
            m_CameraAnimator.updateMode = AnimatorUpdateMode.Normal;
            m_CameraAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            m_CameraAnimator.Play("Camera Look 32 Seconds", 0, 0f);
        }

        void AnimateLookFromMovement()
        {
            if (m_Player == null)
                return;

            float moved = Vector3.Distance(m_Player.position, m_LastPlayerPosition);
            m_LastPlayerPosition = m_Player.position;

            float targetSpeed = Time.deltaTime > 0f ? moved / Time.deltaTime : 0f;
            m_CurrentSpeed = Mathf.Lerp(m_CurrentSpeed, targetSpeed, 1f - Mathf.Exp(-8f * Time.deltaTime));

            // The reference travels from the purple hall into warm and then green/cyan rooms.
            // Distance, rather than elapsed time, keeps that transition tied to actual movement.
            if (moved > 0.0001f)
            {
                m_TraveledDistance += moved;
                m_MovementTime = Mathf.Min(m_MovementTime + Time.deltaTime, CameraTransitionDuration);
            }

            if (m_CameraAnimator != null)
                m_CameraAnimator.speed = targetSpeed > 0.01f ? 1f : 0f;

            float route = m_CameraAnimator != null
                ? Mathf.Clamp01(m_AnimationProgress)
                : Mathf.Clamp01(m_MovementTime / CameraTransitionDuration);
            float blend = route * route * (3f - 2f * route);
            float movement = Mathf.Clamp01(m_CurrentSpeed / 1.2f);
            float pulse = Mathf.Sin(m_TraveledDistance * 1.35f) * movement;

            if (m_Color != null)
            {
                Color purple = new Color(1.08f, 0.78f, 1.05f);
                Color warm = new Color(1.02f, 0.94f, 0.84f);
                Color cyanGreen = new Color(0.72f, 1.08f, 0.92f);
                Color routeColor = blend < 0.5f
                    ? Color.Lerp(purple, warm, blend * 2f)
                    : Color.Lerp(warm, cyanGreen, (blend - 0.5f) * 2f);

                m_Color.colorFilter.value = Color.Lerp(m_Color.colorFilter.value, routeColor,
                    1f - Mathf.Exp(-3.5f * Time.deltaTime));
                m_Color.hueShift.Override(Mathf.Lerp(-7f, 10f, blend) + pulse * 2f);
                m_Color.postExposure.value = 0.63f + pulse * 0.08f;
                m_Color.contrast.value = 8f + movement * 4f;
                m_Color.saturation.value = 8f + movement * 5f;
            }

            if (m_Lens != null)
            {
                m_Lens.intensity.value = 0.28f + movement * 0.035f + pulse * 0.012f;
                m_Lens.scale.value = 1.1f + movement * 0.012f;
            }

            if (m_Bloom != null)
                m_Bloom.intensity.value = 0.2f + movement * 0.08f + Mathf.Max(0f, pulse) * 0.03f;

            if (m_Vignette != null)
                m_Vignette.intensity.value = 0.363f + movement * 0.025f;
        }

        void UpdateAspect(bool force)
        {
            if (!force && m_LastWidth == Screen.width && m_LastHeight == Screen.height)
                return;

            m_LastWidth = Screen.width;
            m_LastHeight = Screen.height;

            float windowAspect = Screen.height > 0
                ? (float)Screen.width / Screen.height
                : TargetAspect;
            float scaleHeight = windowAspect / TargetAspect;

            if (scaleHeight < 1f)
            {
                m_Camera.rect = new Rect(0f, (1f - scaleHeight) * 0.5f, 1f, scaleHeight);
            }
            else
            {
                float scaleWidth = 1f / scaleHeight;
                m_Camera.rect = new Rect((1f - scaleWidth) * 0.5f, 0f, scaleWidth, 1f);
            }
        }
    }
}
