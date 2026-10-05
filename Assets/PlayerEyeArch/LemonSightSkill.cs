using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace StealthGame
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EyeArchGesture), typeof(PlayerMovement))]
    public class LemonSightSkill : MonoBehaviour
    {
        public enum SkillState { Ready, Settling, Raising, Active, Lowering }
        [Header("Input and timing")]
        public UnityEngine.InputSystem.Key activationKey = UnityEngine.InputSystem.Key.E;
        [Min(0.01f)] public float idleTransitionSeconds = .22f;
        [Min(0)] public float raisedPreviewSeconds = .18f;
        public bool hidePlayerInFirstPersonWhileActive = true;
        [Min(.1f)] public float durationSeconds = 8f;
        [Min(.1f)] public float rechargeSeconds = 12f;
        [Min(0)] public float rechargeDelay = 1f;
        [Range(.01f, 1)] public float minimumCharge = .15f;
        [Header("X-ray silhouettes")]
        public Renderer[] targets;
        public Shader silhouetteShader;
        public Color lemonColor = new Color(1f, .98f, .08f, 1f);
        public Color silhouetteColor = new Color(.85f, .72f, .01f, 1f);
        [Header("Lemon screen tint")]
        [Tooltip("Alpha controls the strength of the screen tint during the skill.")]
        public Color screenTintColor = new Color(1f, .98f, .65f, .22f);
        [Header("Skill audio")]
        public AudioClip skillSound;
        [Range(0, 1)] public float soundVolume = .6f;
        public bool loopSound = false;
        [Header("Gauge: top-left, reference resolution 1920 x 1080")]
        public Texture gaugeFrame;
        public Texture gaugeFill;
        public Vector2 gaugePosition = new Vector2(24, 24);
        public Vector2 gaugeSize = new Vector2(360, 82);
        [Tooltip("Fill rectangle normalized within the frame: x, y from bottom-left, width, height.")]
        public Rect fillArea = new Rect(.174f, .314f, .785f, .289f);
        [Header("Side occlusion (does not change FOV)")]
        [Range(0, .45f)] public float sideCoverWidth = .16f;
        public Color sideCoverColor = new Color(1f, .96f, .48f, .88f);
        public SkillState State { get; private set; }
        public float Charge { get; private set; } = 1f;
        EyeArchGesture gesture;
        PlayerMovement movement;
        Animator animator;
        Rigidbody body;
        AudioSource footsteps, sound;
        bool movementWasEnabled, locked;
        float animatorSpeed, timer, rechargeTimer;
        RigidbodyConstraints constraints;
        Material silhouette;
        Material invisible;
        readonly List<Renderer> overlays = new List<Renderer>();
        readonly List<Renderer> sources = new List<Renderer>();
        GameObject hud;
        RectTransform gauge, fillMask, fillRect, leftCover, rightCover;
        RawImage fillImage;
        Image leftImage, rightImage, screenTint;
        Camera viewCamera;
        Renderer[] playerRenderers;
        UnityEngine.Rendering.ShadowCastingMode[] savedShadowModes;
        bool cameraVisibilityOverridden;
        Texture2D neutralFill;

        void Awake()
        {
            gesture = GetComponent<EyeArchGesture>();
            movement = GetComponent<PlayerMovement>();
            animator = GetComponent<Animator>();
            body = GetComponent<Rigidbody>();
            footsteps = GetComponent<AudioSource>();
            viewCamera = GetComponentInChildren<Camera>();
            playerRenderers = GetComponentsInChildren<Renderer>(true);
            savedShadowModes = new UnityEngine.Rendering.ShadowCastingMode[playerRenderers.Length];
            sound = gameObject.AddComponent<AudioSource>();
            sound.playOnAwake = false; sound.spatialBlend = 0;
            CreateOverlays(); CreateHud();
        }
        void OnEnable()
        {
            if (gesture != null) gesture.ExternalControl = true;
            if (hud != null) hud.SetActive(true);
            UnityEngine.Rendering.RenderPipelineManager.beginCameraRendering += BeforeCamera;
            UnityEngine.Rendering.RenderPipelineManager.endCameraRendering += AfterCamera;
        }
        void BeforeCamera(UnityEngine.Rendering.ScriptableRenderContext context, Camera camera)
        {
            if (camera != viewCamera || State != SkillState.Active || !hidePlayerInFirstPersonWhileActive) return;
            for (int i = 0; i < playerRenderers.Length; i++)
            {
                if (playerRenderers[i] == null) continue;
                savedShadowModes[i] = playerRenderers[i].shadowCastingMode;
                playerRenderers[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
            }
            cameraVisibilityOverridden = true;
        }
        void AfterCamera(UnityEngine.Rendering.ScriptableRenderContext context, Camera camera)
        {
            if (camera == viewCamera) RestoreCameraVisibility();
        }
        void RestoreCameraVisibility()
        {
            if (!cameraVisibilityOverridden) return;
            for (int i = 0; i < playerRenderers.Length; i++)
                if (playerRenderers[i] != null) playerRenderers[i].shadowCastingMode = savedShadowModes[i];
            cameraVisibilityOverridden = false;
        }
        void Update()
        {
            if (Time.timeScale <= 0) return;
            if (Application.isFocused && Cursor.lockState == CursorLockMode.Locked && Keyboard.current != null && Keyboard.current[activationKey].wasPressedThisFrame)
                ToggleSkill();
            float dt = Time.deltaTime;
            timer += dt;
            if (State == SkillState.Settling && timer >= idleTransitionSeconds)
            {
                if (animator != null) animator.speed = 0;
                gesture.SetRaised(true); State = SkillState.Raising; timer = 0;
            }
            else if (State == SkillState.Raising && gesture.IsRaised && timer >= gesture.blendDuration + raisedPreviewSeconds)
            {
                State = SkillState.Active;
                sound.clip = skillSound; sound.loop = loopSound; sound.volume = soundVolume;
                if (skillSound != null) sound.Play();
            }
            else if (State == SkillState.Active)
            {
                Charge = Mathf.Max(0, Charge - dt / Mathf.Max(.1f, durationSeconds));
                if (Charge <= 0) EndSkill();
            }
            else if (State == SkillState.Lowering && gesture.PoseWeight <= .001f)
            {
                RestorePlayer(); State = SkillState.Ready; rechargeTimer = rechargeDelay;
            }
            else if (State == SkillState.Ready)
            {
                rechargeTimer -= dt;
                if (rechargeTimer <= 0) Charge = Mathf.Min(1, Charge + dt / Mathf.Max(.1f, rechargeSeconds));
            }
            sound.volume = soundVolume;
            UpdateVisuals();
        }
        public void ToggleSkill()
        {
            if (!isActiveAndEnabled) return;
            if (State == SkillState.Ready) BeginSkill();
            else if (State != SkillState.Lowering) EndSkill();
        }
        public bool BeginSkill()
        {
            if (!isActiveAndEnabled || State != SkillState.Ready || Charge < minimumCharge) return false;
            movementWasEnabled = movement.enabled; movement.enabled = false;
            if (footsteps != null) footsteps.Stop();
            if (body != null)
            {
                constraints = body.constraints;
                if (!body.isKinematic) { body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero; }
                body.constraints |= RigidbodyConstraints.FreezePosition;
            }
            if (animator != null)
            {
                animatorSpeed = animator.speed;
                animator.SetBool("IsWalking", false);
                if (animator.isActiveAndEnabled && animator.HasState(0, Animator.StringToHash("Base Layer.Idle")))
                    animator.CrossFadeInFixedTime("Base Layer.Idle", idleTransitionSeconds * .75f, 0);
            }
            locked = true; timer = 0; State = SkillState.Settling;
            return true;
        }
        public void EndSkill()
        {
            if (State == SkillState.Ready || State == SkillState.Lowering) return;
            if (animator != null) animator.speed = 0;
            gesture.SetRaised(false); sound.Stop(); State = SkillState.Lowering; timer = 0;
        }
        void RestorePlayer()
        {
            if (!locked) return;
            if (animator != null) animator.speed = animatorSpeed;
            if (body != null) body.constraints = constraints;
            if (movement != null) movement.enabled = movementWasEnabled;
            locked = false;
        }
        void CreateOverlays()
        {
            if (silhouetteShader == null) return;
            silhouette = new Material(silhouetteShader);
            silhouette.renderQueue = 5000;
            invisible = new Material(silhouetteShader);
            invisible.SetColor("_SightColor", Color.clear);
            if (targets == null) return;
            foreach (Renderer source in targets)
            {
                if (source == null) continue;
                GameObject go = new GameObject("Lemon Sight Silhouette");
                go.layer = source.gameObject.layer;
                go.transform.SetParent(source.transform, false);
                Renderer overlay;
                if (source is SkinnedMeshRenderer skin)
                {
                    var copy = go.AddComponent<SkinnedMeshRenderer>();
                    copy.sharedMesh = skin.sharedMesh; copy.bones = skin.bones; copy.rootBone = skin.rootBone;
                    copy.localBounds = skin.localBounds; copy.updateWhenOffscreen = true;
                    overlay = copy;
                }
                else if (source.TryGetComponent<MeshFilter>(out var filter))
                {
                    go.AddComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
                    overlay = go.AddComponent<MeshRenderer>();
                }
                else { Destroy(go); continue; }
                var materials = new Material[Mathf.Max(1, source.sharedMaterials.Length)];
                for (int i = 0; i < materials.Length; i++)
                {
                    var original = i < source.sharedMaterials.Length ? source.sharedMaterials[i] : null;
                    materials[i] = original != null && original.name.Contains("Light_Cone") ? invisible : silhouette;
                }
                overlay.sharedMaterials = materials;
                overlay.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                overlay.receiveShadows = false; overlay.enabled = false;
                overlays.Add(overlay); sources.Add(source);
            }
        }
        static RectTransform RectObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }
        void CreateHud()
        {
            hud = new GameObject("Lemon Sight HUD", typeof(Canvas), typeof(CanvasScaler));
            var canvas = hud.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 50;
            var scaler = hud.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
            var tintRect = RectObject("Lemon screen tint", hud.transform);
            tintRect.anchorMin = Vector2.zero; tintRect.anchorMax = Vector2.one;
            tintRect.offsetMin = tintRect.offsetMax = Vector2.zero;
            screenTint = tintRect.gameObject.AddComponent<Image>(); screenTint.raycastTarget = false;
            leftCover = RectObject("Left cover", hud.transform); leftImage = leftCover.gameObject.AddComponent<Image>(); leftImage.raycastTarget = false;
            rightCover = RectObject("Right cover", hud.transform); rightImage = rightCover.gameObject.AddComponent<Image>(); rightImage.raycastTarget = false;
            gauge = RectObject("Gauge", hud.transform); gauge.anchorMin = gauge.anchorMax = new Vector2(0, 1); gauge.pivot = new Vector2(0, 1);
            fillMask = RectObject("Fill mask", gauge); fillMask.gameObject.AddComponent<RectMask2D>(); fillMask.anchorMin = fillMask.anchorMax = Vector2.zero; fillMask.pivot = Vector2.zero;
            fillRect = RectObject("Fill", fillMask); fillRect.anchorMin = fillRect.anchorMax = Vector2.zero; fillRect.pivot = Vector2.zero;
            fillImage = fillRect.gameObject.AddComponent<RawImage>(); fillImage.texture = CreateNeutralFill(); fillImage.raycastTarget = false;
            var frame = RectObject("Frame", gauge); frame.anchorMin = Vector2.zero; frame.anchorMax = Vector2.one; frame.offsetMin = frame.offsetMax = Vector2.zero;
            var image = frame.gameObject.AddComponent<RawImage>(); image.texture = gaugeFrame; image.raycastTarget = false;
            UpdateVisuals();
        }
        Texture CreateNeutralFill()
        {
            if (gaugeFill == null) return null;
            // Preserve the supplied PNG and its alpha; tint a runtime grayscale copy.
            var rt = RenderTexture.GetTemporary(gaugeFill.width, gaugeFill.height, 0, RenderTextureFormat.ARGB32);
            var previous = RenderTexture.active;
            try
            {
                Graphics.Blit(gaugeFill, rt); RenderTexture.active = rt;
                neutralFill = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
                neutralFill.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                var pixels = neutralFill.GetPixels32();
                for (int i = 0; i < pixels.Length; i++)
                {
                    byte value = (byte)Mathf.Max(pixels[i].r, Mathf.Max(pixels[i].g, pixels[i].b));
                    pixels[i] = new Color32(value, value, value, pixels[i].a);
                }
                neutralFill.SetPixels32(pixels); neutralFill.Apply(); return neutralFill;
            }
            finally { RenderTexture.active = previous; RenderTexture.ReleaseTemporary(rt); }
        }
        void UpdateVisuals()
        {
            bool active = State == SkillState.Active;
            if (silhouette != null) silhouette.SetColor("_SightColor", silhouetteColor);
            for (int i = 0; i < overlays.Count; i++)
            {
                if (overlays[i] == null) continue;
                var source = sources[i];
                overlays[i].enabled = active && source != null && source.enabled && source.gameObject.activeInHierarchy;
                if (active && source is SkinnedMeshRenderer skin && overlays[i] is SkinnedMeshRenderer copy && skin.sharedMesh != null)
                    for (int b = 0; b < skin.sharedMesh.blendShapeCount; b++) copy.SetBlendShapeWeight(b, skin.GetBlendShapeWeight(b));
            }
            if (gauge == null) return;
            gauge.anchoredPosition = new Vector2(gaugePosition.x, -gaugePosition.y); gauge.sizeDelta = gaugeSize;
            Vector2 size = Vector2.Scale(gaugeSize, fillArea.size);
            fillMask.anchoredPosition = Vector2.Scale(gaugeSize, fillArea.position);
            fillMask.sizeDelta = new Vector2(size.x * Charge, size.y); fillRect.sizeDelta = size;
            fillImage.color = lemonColor;
            Color tint = screenTintColor;
            tint.a *= State == SkillState.Ready ? 0 : (gesture != null ? gesture.PoseWeight : 0);
            screenTint.color = tint;
            float cover = sideCoverWidth * (gesture != null ? gesture.PoseWeight : 0);
            leftCover.anchorMin = Vector2.zero; leftCover.anchorMax = new Vector2(cover, 1);
            rightCover.anchorMin = new Vector2(1 - cover, 0); rightCover.anchorMax = Vector2.one;
            leftCover.offsetMin = leftCover.offsetMax = rightCover.offsetMin = rightCover.offsetMax = Vector2.zero;
            leftImage.color = rightImage.color = sideCoverColor;
        }
        void OnDisable()
        {
            UnityEngine.Rendering.RenderPipelineManager.beginCameraRendering -= BeforeCamera;
            UnityEngine.Rendering.RenderPipelineManager.endCameraRendering -= AfterCamera;
            RestoreCameraVisibility();
            RestorePlayer(); State = SkillState.Ready;
            if (gesture != null) { gesture.SetRaised(false); gesture.ExternalControl = false; gesture.ApplyPose(0); }
            if (sound != null) sound.Stop();
            foreach (var overlay in overlays) if (overlay != null) overlay.enabled = false;
            if (hud != null) hud.SetActive(false);
        }
        void OnDestroy()
        {
            foreach (var overlay in overlays) if (overlay != null) Destroy(overlay.gameObject);
            if (silhouette != null) Destroy(silhouette);
            if (invisible != null) Destroy(invisible);
            if (hud != null) Destroy(hud);
            if (sound != null) Destroy(sound);
            if (neutralFill != null) Destroy(neutralFill);
        }
    }
}
