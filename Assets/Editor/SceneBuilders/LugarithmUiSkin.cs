using UnityEditor;
using UnityEngine;

/// <summary>Editor-time access to the blueprint-matched in-game UI component kit.</summary>
public static class LugarithmUiSkin
{
    const string Root = "Assets/UI/Sprites/LugarithmUi/Components";
    const string IconRoot = "Assets/UI/Sprites/LugarithmUi/Icons";
    const string SettingsRoot = "Assets/UI/Sprites/LugarithmUi/Settings";
    const string TutorialHudRoot = "Assets/UI/Sprites/LugarithmUi/TutorialHud/Parts";
    const string TutorialMinigameRoot = "Assets/UI/Sprites/LugarithmUi/TutorialMinigames";
    const string TutorialMinigamePartsRoot = "Assets/UI/Sprites/LugarithmUi/TutorialMinigames/Parts";
    const string JeepneyHudRoot = "Assets/UI/Sprites/LugarithmUi/JeepneyHud/Parts";
    const string JournalRoot = "Assets/UI/Sprites/LugarithmUi/Journal";

    public static readonly Color Plum = new Color32(31, 23, 43, 255);
    public static readonly Color PlumDeep = new Color32(12, 13, 25, 250);
    public static readonly Color PlumMuted = new Color32(83, 55, 91, 255);
    public static readonly Color Gold = new Color32(239, 169, 18, 255);
    public static readonly Color Cream = new Color32(242, 224, 174, 255);
    public static readonly Color MutedCream = new Color32(186, 163, 147, 255);
    public static readonly Color Error = new Color32(235, 55, 59, 255);
    public static readonly Color CodeCyan = new Color32(55, 211, 231, 255);

    public static Sprite WindowOuter => Load("window_outer");
    public static Sprite PanelInner => Load("panel_inner");
    public static Sprite TitleRibbon => Load("title_ribbon");
    public static Sprite CompactCard => Load("card_compact");
    public static Sprite ButtonNormal => Load("button_normal");
    public static Sprite ButtonPrimary => Load("button_primary");
    public static Sprite ButtonDisabled => Load("button_disabled");
    public static Sprite DangerFrame => Load("frame_danger");
    public static Sprite Tab => Load("tab");
    public static Sprite Segmented => Load("segmented");
    public static Sprite InputFrame => Load("input_frame");
    public static Sprite DialogueFrame => Load("dialogue_frame");
    public static Sprite SliderTrack => Load("slider_track");
    public static Sprite SliderKnob => Load("slider_knob");
    public static Sprite CheckboxOff => Load("checkbox_off");
    public static Sprite CheckboxOn => Load("checkbox_on");
    public static Sprite ScrollbarTrack => Load("scrollbar_track");
    public static Sprite ScrollbarHandle => Load("scrollbar_handle");
    public static Sprite PortraitFrame => Load("portrait_frame");
    public static Sprite IconSettings => LoadIcon("settings");
    public static Sprite IconCode => LoadIcon("code");
    public static Sprite IconOracle => LoadIcon("oracle");
    public static Sprite IconJournal => LoadIcon("journal");
    public static Sprite IconPause => LoadIcon("pause");
    public static Sprite IconControls => LoadIcon("controls");
    public static Sprite IconSteering => LoadIcon("steering");
    public static Sprite IconAudio => LoadIcon("audio");
    public static Sprite IconDialogue => LoadIcon("dialogue");
    public static Sprite IconRun => LoadIcon("run");
    public static Sprite IconReset => LoadIcon("reset");
    public static Sprite IconHint => LoadIcon("hint");
    public static Sprite IconAutopilot => LoadIcon("autopilot");
    public static Sprite IconClose => LoadIcon("close");
    public static Sprite IconCheck => LoadIcon("check");
    public static Sprite IconWarning => LoadIcon("warning");
    public static Sprite SettingsWindow => AssetDatabase.LoadAssetAtPath<Sprite>($"{SettingsRoot}/settings_window_generated.png");
    public static Sprite TutorialObjective => LoadTutorial("objective_card");
    public static Sprite TutorialBanner => LoadTutorial("location_banner");
    public static Sprite TutorialFooter => LoadTutorial("controls_footer");
    public static Sprite TutorialModeCard => LoadTutorial("mode_card");
    public static Sprite TutorialRailSettings => LoadTutorial("rail_settings");
    public static Sprite TutorialRailCode => LoadTutorial("rail_code");
    public static Sprite TutorialRailOracle => LoadTutorial("rail_oracle");
    public static Sprite TutorialRailJournal => LoadTutorial("rail_journal");
    public static Sprite TutorialRailPause => LoadTutorial("rail_pause");
    public static Sprite MinigameGarageMaze => LoadMinigame("garage_maze");
    public static Sprite MinigameCapizWindow => LoadMinigame("capiz_window");
    public static Sprite MinigameRouteLinks => LoadMinigame("route_links");
    public static Sprite MinigameCapizRoute => LoadMinigame("capiz_route");
    public static Sprite MinigameFirstRoute => LoadMinigame("first_route");
    public static Sprite MinigameStraightRoad => LoadMinigame("straight_road");
    public static Sprite MinigameResults => LoadMinigame("results_chroma");
    public static Sprite MinigameResultsOuterFrame => LoadMinigamePart("results_outer_frame");
    public static Sprite MinigameResultsTitleRibbon => LoadMinigamePart("results_title_ribbon");
    public static Sprite MinigameResultsDropdown => LoadMinigamePart("results_dropdown");
    public static Sprite MinigameResultsSolutionPanel => LoadMinigamePart("results_solution_panel");
    public static Sprite MinigameResultsContinueButton => LoadMinigamePart("results_continue_button");
    public static Sprite MinigameRoadOuterFrame => LoadMinigamePart("road_outer_frame");
    public static Sprite MinigameRoadTitleRibbon => LoadMinigamePart("road_title_ribbon");
    public static Sprite MinigameMazePreviewFrame => LoadMinigamePart("maze_preview_frame");
    public static Sprite MinigameEditorFrame => LoadMinigamePart("editor_frame");
    public static Sprite MinigameButtonPrimary => LoadMinigamePart("button_primary_wide");
    public static Sprite MinigameButtonSecondary => LoadMinigamePart("button_secondary");
    public static Sprite MinigameGarageOuterFrame => LoadMinigamePart("garage_outer_frame");
    public static Sprite MinigameCapizWindowOuterFrame => LoadMinigamePart("capiz_window_outer_frame");
    public static Sprite MinigameFirstRouteOuterFrame => LoadMinigamePart("first_route_outer_frame");
    public static Sprite MinigameRouteLinksOuterFrame => LoadMinigamePart("route_links_outer_frame");
    public static Sprite MinigameCapizRouteOuterFrame => LoadMinigamePart("capiz_route_outer_frame");
    public static Sprite JeepneyObjective => LoadJeepney("objective_card");
    public static Sprite JeepneyObjectiveStrip => LoadJeepney("objective_strip");
    public static Sprite JeepneyFrontSeat => LoadJeepney("front_seat");
    public static Sprite JeepneyActionCode => LoadJeepney("action_code");
    public static Sprite JeepneyActionRoute => LoadJeepney("action_route");
    public static Sprite JeepneyActionJournal => LoadJeepney("action_journal");
    public static Sprite JeepneyEditorShell => LoadJeepney("editor_shell");
    public static Sprite JeepneyMinimap => LoadJeepney("minimap_frame");
    public static Sprite JeepneyDialogue => LoadJeepney("dialogue_card");
    public static Sprite JournalBook => AssetDatabase.LoadAssetAtPath<Sprite>($"{JournalRoot}/journal_book.png");
    public static Texture2D JournalPageTurns => AssetDatabase.LoadAssetAtPath<Texture2D>($"{JournalRoot}/page_turn_sheet.png");
    public static Sprite JournalPart(string name) =>
        AssetDatabase.LoadAssetAtPath<Sprite>($"{JournalRoot}/Parts/{name}.png");

    public static Sprite JournalHeritageCard => JournalPart("heritage_card");
    public static Sprite JournalHeritageCardSelected => JournalPart("heritage_card_selected");
    public static Sprite JournalHeritageCardLocked => JournalPart("heritage_card_locked");
    public static Sprite JournalCodingRow => JournalPart("coding_row");
    public static Sprite JournalCodingRowSelected => JournalPart("coding_row_selected");
    public static Sprite JournalOracleTopicRow => JournalPart("oracle_topic_row");
    public static Sprite JournalTitleRibbon => JournalPart("title_ribbon");
    public static Sprite JournalAssistantRibbon => JournalPart("assistant_ribbon");
    public static Sprite JournalSeparator => JournalPart("separator");
    public static Sprite JournalOracleBanner => JournalPart("oracle_banner");
    public static Sprite JournalPlayerMessage => JournalPart("player_message_frame");
    public static Sprite JournalOracleMessage => JournalPart("oracle_message_frame");
    public static Sprite JournalInput => JournalPart("input_frame");
    public static Sprite JournalSendSeal => JournalPart("send_seal");
    public static Sprite JournalFeather => JournalPart("feather");
    public static Sprite JournalClose => JournalPart("close_button");
    public static Sprite JournalScrollbarTrack => JournalPart("scrollbar_track");
    public static Sprite JournalScrollbarThumb => JournalPart("scrollbar_thumb");

    static Sprite Load(string name)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>($"{Root}/{name}.png");
    }

    static Sprite LoadIcon(string name)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>($"{IconRoot}/{name}.png");
    }

    static Sprite LoadTutorial(string name)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>($"{TutorialHudRoot}/{name}.png");
    }

    static Sprite LoadMinigame(string name)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>($"{TutorialMinigameRoot}/{name}.png");
    }

    static Sprite LoadMinigamePart(string name)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>($"{TutorialMinigamePartsRoot}/{name}.png");
    }

    static Sprite LoadJeepney(string name)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>($"{JeepneyHudRoot}/{name}.png");
    }
}
