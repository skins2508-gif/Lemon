#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
internal static class JohnLemonProjectBootstrap
{
    private const string GameScenePath =
        "Assets/_3DStealthGame/Tutorial_Demo/Demo_Scenes/DemoScene.unity";

    private const string DefaultSampleScenePath = "Assets/Scenes/SampleScene.unity";

    static JohnLemonProjectBootstrap()
    {
        EditorApplication.delayCall += ConfigureProject;
    }

    private static void ConfigureProject()
    {
        var gameScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(GameScenePath);
        if (gameScene == null)
        {
            Debug.LogError($"John Lemon game scene was not found at {GameScenePath}.");
            return;
        }

        // The reference video starts directly in the distorted, color-graded game view.
        if (EditorSceneManager.playModeStartScene != gameScene)
        {
            EditorSceneManager.playModeStartScene = gameScene;
        }

        // Replace the untouched template scene in the editor with the actual game menu.
        var activeScene = EditorSceneManager.GetActiveScene();
        if (!EditorApplication.isPlayingOrWillChangePlaymode &&
            activeScene.path == DefaultSampleScenePath &&
            !activeScene.isDirty)
        {
            EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
        }
    }

    [MenuItem("John Lemon/Open Game Scene")]
    private static void OpenGameScene()
    {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
        }
    }
}
#endif
