using System;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Entry points for regenerating all generated content: placeholder art, the
/// five scenes, and the build-settings scene list. Everything is rebuilt from
/// scratch each run, so re-running is always safe.
/// Menu: Lugarithm → … ; batch: -executeMethod BuildPipelineEntry.BuildAllBatch
/// </summary>
public static class BuildPipelineEntry
{
    static readonly string[] SceneNames =
    {
        "Bootstrap", "MainMenu", "LevelSelect", "ManualDrive", "AutomationDrive",
    };

    // -------------------------------------------------------------------------
    // Menu items

    [MenuItem("Lugarithm/Build All Scenes")]
    public static void BuildAll()
    {
        PlaceholderArtGenerator.GenerateAll();

        BootstrapSceneBuilder.Build();
        MainMenuSceneBuilder.Build();
        LevelSelectSceneBuilder.Build();
        ManualDriveSceneBuilder.Build();
        AutomationDriveSceneBuilder.Build();

        UpdateBuildSettings();
        AssetDatabase.SaveAssets();

        Debug.Log($"[Lugarithm] Scene build complete: {SceneNames.Length} scenes");
    }

    [MenuItem("Lugarithm/Generate Placeholder Art")]
    public static void GenerateArt()
    {
        PlaceholderArtGenerator.GenerateAll();
    }

    [MenuItem("Lugarithm/Build Scene/Bootstrap")]
    public static void BuildBootstrap() => BootstrapSceneBuilder.Build();

    [MenuItem("Lugarithm/Build Scene/MainMenu")]
    public static void BuildMainMenu() => MainMenuSceneBuilder.Build();

    [MenuItem("Lugarithm/Build Scene/LevelSelect")]
    public static void BuildLevelSelect() => LevelSelectSceneBuilder.Build();

    [MenuItem("Lugarithm/Build Scene/ManualDrive")]
    public static void BuildManualDrive() => ManualDriveSceneBuilder.Build();

    [MenuItem("Lugarithm/Build Scene/AutomationDrive")]
    public static void BuildAutomationDrive() => AutomationDriveSceneBuilder.Build();

    // -------------------------------------------------------------------------
    // Batch mode

    /// <summary>
    /// Batch entry: Unity.exe -batchmode -projectPath … -executeMethod
    /// BuildPipelineEntry.BuildAllBatch (no -quit needed — exits itself).
    /// </summary>
    public static void BuildAllBatch()
    {
        try
        {
            BuildAll();
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            Debug.LogError("[Lugarithm] Scene build FAILED: " + e);
            EditorApplication.Exit(1);
        }
    }

    // -------------------------------------------------------------------------

    static void UpdateBuildSettings()
    {
        var list = new EditorBuildSettingsScene[SceneNames.Length];
        for (int i = 0; i < SceneNames.Length; i++)
            list[i] = new EditorBuildSettingsScene($"{SceneBuilderUtil.ScenesDir}/{SceneNames[i]}.unity", true);

        EditorBuildSettings.scenes = list;
    }
}
