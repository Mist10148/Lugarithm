using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds LevelSelect.unity — six entry rows (Tutorial + Levels 1–5) with
/// lock states, best scores, Back, and the shared Settings panel.
/// </summary>
public static class LevelSelectSceneBuilder
{
    public static void Build()
    {
        var scene = SceneBuilderUtil.NewScene();

        SceneBuilderUtil.CreateCamera2D("Main Camera", new Color(0.05f, 0.06f, 0.09f), 5f);
        SceneBuilderUtil.CreateGlobalLight2D();
        SceneBuilderUtil.CreateEventSystem();

        var canvas = UIFactory.CreateCanvas("LevelSelectCanvas");
        UIFactory.CreatePanel(canvas.transform, "Background",
                              Vector2.zero, Vector2.one, new Color(0.05f, 0.06f, 0.09f, 1f));

        // --- Header -----------------------------------------------------------------

        var header = UIFactory.CreateText(canvas.transform, "Header", "SELECT A LEG", 60f, UIFactory.Accent);
        UIFactory.Place(header, new Vector2(0.5f, 1f), new Vector2(0f, -56f), new Vector2(900f, 80f));
        header.fontStyle = FontStyles.Bold;

        var sub = UIFactory.CreateText(canvas.transform, "SubHeader",
                                       "Iloilo City  →  San Joaquin   ·   recover the journal pages",
                                       24f, UIFactory.TextDim);
        UIFactory.Place(sub, new Vector2(0.5f, 1f), new Vector2(0f, -118f), new Vector2(1000f, 40f));

        // --- Entry rows -------------------------------------------------------------

        var column = UIFactory.CreateRect(canvas.transform, "Entries",
                                          new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        UIFactory.Place(column, new Vector2(0.5f, 0.5f), new Vector2(0f, -60f), new Vector2(920f, 600f));
        UIFactory.AddVerticalLayout(column, 14f, align: TextAnchor.UpperCenter);

        var entries = new LevelSelectEntry[LevelLibrary.Count];
        for (int i = 0; i < LevelLibrary.Count; i++)
            entries[i] = BuildEntryRow(column, i);

        // --- Corner buttons -----------------------------------------------------------

        Button back = UIFactory.CreateButton(canvas.transform, "BackButton", "←  Back", new Vector2(180f, 54f));
        UIFactory.Place(back, new Vector2(0f, 1f), new Vector2(28f, -28f), new Vector2(180f, 54f));

        Button settings = UIFactory.CreateButton(canvas.transform, "SettingsButton", "Settings", new Vector2(180f, 54f));
        UIFactory.Place(settings, new Vector2(1f, 1f), new Vector2(-28f, -28f), new Vector2(180f, 54f));

        // --- Settings panel + manager ----------------------------------------------------

        SettingsPanel settingsPanel = SettingsPanelBuilder.Build(canvas.transform);

        var manager = canvas.gameObject.AddComponent<LevelSelectManager>();
        SceneBuilderUtil.WireArray(manager, "entries", entries);
        SceneBuilderUtil.Wire(manager, "backButton",          back);
        SceneBuilderUtil.Wire(manager, "settingsButton",      settings);
        SceneBuilderUtil.Wire(manager, "settingsPanel",       settingsPanel);
        SceneBuilderUtil.Wire(manager, "mainMenuSceneName",   "MainMenu");
        SceneBuilderUtil.Wire(manager, "manualSceneName",     "ManualDrive");
        SceneBuilderUtil.Wire(manager, "automationSceneName", "AutomationDrive");

        SceneBuilderUtil.SaveScene(scene, "LevelSelect");
    }

    // -------------------------------------------------------------------------

    static LevelSelectEntry BuildEntryRow(RectTransform parent, int index)
    {
        var row = UIFactory.CreateRect(parent, $"Entry_{index}",
                                       new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        row.sizeDelta = new Vector2(900f, 84f);
        UIFactory.SetLayoutSize(row, 900f, 84f);

        var face = row.gameObject.AddComponent<Image>();
        face.sprite = UIFactory.BuiltinSprite("UISprite.psd");
        face.type   = Image.Type.Sliced;
        face.color  = UIFactory.ButtonFace;

        var button = row.gameObject.AddComponent<Button>();
        button.targetGraphic = face;

        var name = UIFactory.CreateText(row, "Name", "", 30f, UIFactory.TextBright,
                                        TextAlignmentOptions.MidlineLeft);
        UIFactory.Place(name, new Vector2(0f, 0.5f), new Vector2(28f, 0f), new Vector2(520f, 70f));

        var best = UIFactory.CreateText(row, "Best", "", 22f, UIFactory.TextDim,
                                        TextAlignmentOptions.MidlineRight);
        UIFactory.Place(best, new Vector2(1f, 0.5f), new Vector2(-240f, 0f), new Vector2(200f, 70f));

        var status = UIFactory.CreateText(row, "Status", "", 24f, UIFactory.Accent,
                                          TextAlignmentOptions.MidlineRight);
        UIFactory.Place(status, new Vector2(1f, 0.5f), new Vector2(-28f, 0f), new Vector2(200f, 70f));

        var lockOverlay = UIFactory.CreatePanel(row, "LockOverlay",
                                                Vector2.zero, Vector2.one,
                                                new Color(0f, 0f, 0f, 0.55f));
        lockOverlay.GetComponent<Image>().raycastTarget = false;

        var entry = row.gameObject.AddComponent<LevelSelectEntry>();
        SceneBuilderUtil.Wire(entry, "nameLabel",   name);
        SceneBuilderUtil.Wire(entry, "statusLabel", status);
        SceneBuilderUtil.Wire(entry, "bestLabel",   best);
        SceneBuilderUtil.Wire(entry, "lockOverlay", lockOverlay.gameObject);
        SceneBuilderUtil.Wire(entry, "button",      button);

        return entry;
    }
}
