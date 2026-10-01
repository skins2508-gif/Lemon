#if UNITY_EDITOR
using StealthGame;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Callbacks;
using UnityEngine;

[InitializeOnLoad]
internal static class CameraAnimator32SecondGenerator
{
    const string RootFolder = "Assets/_3DStealthGame/Resources";
    const string ClipPath = RootFolder + "/Camera_32s.anim";
    const string ControllerPath = RootFolder + "/Camera_32s.controller";
    const string StateName = "Camera Look 32 Seconds";
    const float Duration = 32f;

    static CameraAnimator32SecondGenerator()
    {
        EditorApplication.delayCall += Generate;
    }

    [DidReloadScripts]
    static void OnScriptsReloaded()
    {
        EditorApplication.delayCall += Generate;
    }

    [MenuItem("John Lemon/Rebuild 32 Second Camera Animator")]
    static void Generate()
    {
        EnsureFolder("Assets/_3DStealthGame", "Resources");

        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath);
        if (clip == null)
        {
            clip = new AnimationClip
            {
                name = "Camera 32 Second Look",
                frameRate = 60f,
                wrapMode = WrapMode.ClampForever
            };
            AssetDatabase.CreateAsset(clip, ClipPath);
        }

        AnimationCurve progress = AnimationCurve.Linear(0f, 0f, Duration, 1f);
        progress.preWrapMode = WrapMode.ClampForever;
        progress.postWrapMode = WrapMode.ClampForever;
        clip.SetCurve(string.Empty, typeof(ReferenceVideoLook),
            "m_AnimationProgress", progress);
        EditorUtility.SetDirty(clip);

        AnimatorController controller =
            AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
            controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        AnimatorState state = null;
        foreach (ChildAnimatorState child in stateMachine.states)
        {
            if (child.state.name == StateName)
            {
                state = child.state;
                break;
            }
        }

        if (state == null)
            state = stateMachine.AddState(StateName);

        state.motion = clip;
        state.speed = 1f;
        state.writeDefaultValues = true;
        stateMachine.defaultState = state;
        EditorUtility.SetDirty(controller);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    static void EnsureFolder(string parent, string child)
    {
        string path = parent + "/" + child;
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder(parent, child);
    }
}
#endif
