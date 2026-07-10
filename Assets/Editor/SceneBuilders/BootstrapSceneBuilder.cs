using UnityEngine;
using UnityEditor;

/// <summary>
/// Builds Bootstrap.unity — the first-loaded scene. Hosts the persistent
/// managers (GameManager, SettingsManager, SceneTransitionManager + fade
/// canvas) and the splash sequence that leads to the Main Menu.
/// </summary>
public static class BootstrapSceneBuilder
{
    public static void Build()
    {
        var scene = SceneBuilderUtil.NewScene();

        SceneBuilderUtil.CreateCamera2D("Main Camera", new Color(0.04f, 0.05f, 0.07f), 5f);
        SceneBuilderUtil.CreateGlobalLight2D();
        SceneBuilderUtil.CreateEventSystem();

        // --- Persistent managers ------------------------------------------------

        new GameObject("GameManager").AddComponent<GameManager>();
        new GameObject("SettingsManager").AddComponent<SettingsManager>();
        new GameObject("LocalizationManager").AddComponent<LocalizationManager>();

        // Background music (seamless loop, volume driven by the Music setting).
        var audioGo = new GameObject("AudioManager");
        var audioMgr = audioGo.AddComponent<AudioManager>();
        var musicSource = audioGo.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.playOnAwake = false;
        musicSource.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(
            "Assets/Audio/Music/bgmusic/Lakbay_Pamana_Seamless_Loop.mp3");
        SceneBuilderUtil.Wire(audioMgr, "musicSource", musicSource);

        var transitionGo = new GameObject("SceneTransition");
        var transition = transitionGo.AddComponent<SceneTransitionManager>();

        var transitionCanvas = UIFactory.CreateCanvas("TransitionCanvas", 1000);
        transitionCanvas.transform.SetParent(transitionGo.transform, false);

        var fade = UIFactory.CreatePanel(transitionCanvas.transform, "Fade",
                                         Vector2.zero, Vector2.one, Color.black);
        var fadeGroup = fade.gameObject.AddComponent<CanvasGroup>();
        fadeGroup.alpha = 0f;
        fadeGroup.blocksRaycasts = false;

        var loading = UIFactory.CreatePanel(transitionCanvas.transform, "LoadingPanel",
                                            Vector2.zero, Vector2.one, Color.black);
        var loadingText = UIFactory.CreateText(loading, "Label", "Loading…", 40f, UIFactory.TextDim);
        UIFactory.Place(loadingText, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(400f, 80f));
        loading.gameObject.SetActive(false);

        SceneBuilderUtil.Wire(transition, "fadeGroup",    fadeGroup);
        SceneBuilderUtil.Wire(transition, "loadingPanel", loading.gameObject);

        // --- Almanac overlay (persistent DontDestroyOnLoad book) ----------------
        AlmanacOverlayBuilder.Build(null);

        // --- Badge unlock overlay (persistent DontDestroyOnLoad) ----------------
        BadgeUnlockBuilder.Build();

        // --- Splash --------------------------------------------------------------

        var splashCanvas = UIFactory.CreateCanvas("SplashCanvas", 10);
        UIFactory.CreatePanel(splashCanvas.transform, "Background",
                              Vector2.zero, Vector2.one, new Color(0.04f, 0.05f, 0.07f, 1f));

        // Logo group
        var logoRoot = UIFactory.CreateRect(splashCanvas.transform, "LogoGroup",
                                            Vector2.zero, Vector2.one);
        var logoGroup = logoRoot.gameObject.AddComponent<CanvasGroup>();

        var titleText = UIFactory.CreateText(logoRoot, "Title", "LUGARITHM", 120f, UIFactory.Accent);
        UIFactory.Place(titleText, new Vector2(0.5f, 0.5f), new Vector2(0f, 70f), new Vector2(1200f, 150f));
        titleText.fontStyle = TMPro.FontStyles.Bold;

        var subtitleText = UIFactory.CreateText(logoRoot, "Subtitle",
                                                "A Heritage Jeepney Road Trip", 34f, UIFactory.TextBright);
        UIFactory.Place(subtitleText, new Vector2(0.5f, 0.5f), new Vector2(0f, -30f), new Vector2(1200f, 60f));

        // Team group
        var teamRoot = UIFactory.CreateRect(splashCanvas.transform, "TeamGroup",
                                            Vector2.zero, Vector2.one);
        var teamGroup = teamRoot.gameObject.AddComponent<CanvasGroup>();

        var teamText = UIFactory.CreateText(teamRoot, "TeamName", "A  CYFER  GAME", 28f, UIFactory.TextDim);
        UIFactory.Place(teamText, new Vector2(0.5f, 0.5f), new Vector2(0f, -150f), new Vector2(800f, 50f));

        var splash = splashCanvas.gameObject.AddComponent<SplashScreenManager>();
        SceneBuilderUtil.Wire(splash, "logoGroup",     logoGroup);
        SceneBuilderUtil.Wire(splash, "teamNameGroup", teamGroup);
        SceneBuilderUtil.Wire(splash, "nextSceneName", "MainMenu");

        SceneBuilderUtil.SaveScene(scene, "Bootstrap");
    }
}
