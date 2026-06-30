using System.Collections.Generic;

/// <summary>
/// The UI string table: every player-facing interface key with its English and
/// Filipino text. Static + data-only so it is available at scene-build time (the
/// builders seed each label's English text from here) and at runtime (the
/// <see cref="LocalizationManager"/> looks up the active language).
///
/// Scope: menus, buttons, HUD chrome, and settings — per the localization plan.
/// Story/heritage content (NPC dialogue, journal pages, passenger reveals) is a
/// separate later content pass and is NOT keyed here.
///
/// Filipino is a first-pass translation meant for review.
/// </summary>
public static class LocalizationTable
{
    // key -> { English, Filipino }. Index matches (int)GameLanguage.
    static readonly Dictionary<string, string[]> Entries = new Dictionary<string, string[]>
    {
        // --- Common -----------------------------------------------------------
        ["common.continue"] = new[] { "Continue", "Magpatuloy" },
        ["common.close"]    = new[] { "Close",    "Isara" },
        ["common.back"]     = new[] { "Back",     "Bumalik" },

        // --- Main menu --------------------------------------------------------
        ["menu.newgame"]   = new[] { "JEEP JOURNEY", "BIYAHENG DYIP" },
        ["menu.continue"]  = new[] { "CONTINUE",     "IPAGPATULOY" },
        ["menu.journal"]   = new[] { "JOURNAL",      "TALAARAWAN" },
        ["menu.settings"]  = new[] { "SETTINGS",     "MGA SETTING" },
        ["menu.quit"]      = new[] { "EXIT",         "LUMABAS" },

        // --- Settings: title + sections --------------------------------------
        ["settings.title"]               = new[] { "SETTINGS",         "MGA SETTING" },
        ["settings.section.gameplay"]    = new[] { "GAMEPLAY",         "PAGLALARO" },
        ["settings.section.controls"]    = new[] { "CONTROLS",         "MGA KONTROL" },
        ["settings.section.audio"]       = new[] { "AUDIO",            "TUNOG" },
        ["settings.section.languagetext"]= new[] { "LANGUAGE & TEXT",  "WIKA AT TEKSTO" },
        ["settings.section.appearance"]  = new[] { "APPEARANCE",       "ITSURA" },

        // --- Settings: row labels --------------------------------------------
        ["settings.drivemode"]      = new[] { "Drive Mode",        "Paraan ng Maneho" },
        ["settings.codinginterface"]= new[] { "Coding Interface",  "Interface ng Code" },
        ["settings.spacebrake"]     = new[] { "Space Brake",       "Preno (Space)" },
        ["settings.musicvolume"]    = new[] { "Music Volume",      "Lakas ng Musika" },
        ["settings.sfxvolume"]      = new[] { "SFX Volume",        "Lakas ng SFX" },
        ["settings.language"]       = new[] { "Language",          "Wika" },
        ["settings.subtitles"]      = new[] { "Subtitles",         "Subtitle" },
        ["settings.dialoguespeed"]  = new[] { "Dialogue Speed",    "Bilis ng Usapan" },
        ["settings.codetheme"]      = new[] { "Code Theme",        "Tema ng Code" },

        // --- Settings: option pills ------------------------------------------
        ["opt.manual"]     = new[] { "Manual",     "Manu-mano" },
        ["opt.automation"] = new[] { "Automation", "Awtomatiko" },
        ["opt.blocks"]     = new[] { "Blocks",     "Bloke" },
        ["opt.code"]       = new[] { "Code",       "Code" },
        ["opt.hold"]       = new[] { "Hold",       "Hawak" },
        ["opt.toggle"]     = new[] { "Toggle",     "Pindot" },
        ["opt.on"]         = new[] { "On",         "Naka-on" },
        ["opt.off"]        = new[] { "Off",        "Naka-off" },
        ["opt.english"]    = new[] { "English",    "English" },
        ["opt.filipino"]   = new[] { "Filipino",   "Filipino" },
        ["opt.speed.slow"]   = new[] { "Slow",     "Mabagal" },
        ["opt.speed.normal"] = new[] { "Normal",   "Karaniwan" },
        ["opt.speed.fast"]   = new[] { "Fast",     "Mabilis" },
        ["opt.speed.instant"]= new[] { "Instant",  "Agad" },
        ["settings.theme.cycle"] = new[] { "Cycle", "Palit" },

        // --- Level select -----------------------------------------------------
        ["levelselect.title"]    = new[] { "SELECT A LEG", "PUMILI NG BIYAHE" },
        ["levelselect.subtitle"] = new[] { "ILOILO CITY TO SAN JOAQUIN  -  RECOVER THE JOURNAL PAGES",
                                           "ILOILO CITY HANGGANG SAN JOAQUIN  -  BAWIIN ANG MGA PAHINA" },

        // --- Badge ------------------------------------------------------------
        ["badge.earned"] = new[] { "BADGE EARNED", "NAKAMIT NA BADGE" },

        // --- Results ----------------------------------------------------------
        ["results.replay"]       = new[] { "Replay Leg",    "Ulitin ang Biyahe" },
        ["results.replaypuzzle"] = new[] { "Replay Puzzle", "Ulitin ang Puzzle" },

        // --- HUD / drive chrome ----------------------------------------------
        ["hud.exit"]      = new[] { "Exit",      "Lumabas" },
        ["hud.journal"]   = new[] { "Journal",   "Talaarawan" },
        ["hud.fuel"]      = new[] { "FUEL",      "GASOLINA" },
        ["hud.speed"]     = new[] { "SPEED",     "BILIS" },
        ["hud.manualhint"]= new[] { "A / D change lanes · Space brakes",
                                    "A / D para lumipat ng linya · Space para mag-preno" },

        // --- Automation control bar ------------------------------------------
        ["auto.run"]       = new[] { "RUN",       "PATAKBO" },
        ["auto.reset"]     = new[] { "Reset",     "I-reset" },
        ["auto.autopilot"] = new[] { "Auto",      "Auto" },
        ["auto.pause"]     = new[] { "Pause",     "I-pause" },
        ["auto.step"]      = new[] { "Step",      "Hakbang" },
    };

    /// <summary>Localized text for a key. Falls back to English, then to the key
    /// itself (so a missing translation is visible during development, never blank).</summary>
    public static string Get(string key, GameLanguage language)
    {
        if (string.IsNullOrEmpty(key)) return "";
        if (Entries.TryGetValue(key, out string[] values))
        {
            int i = (int)language;
            if (i >= 0 && i < values.Length && !string.IsNullOrEmpty(values[i])) return values[i];
            return values.Length > 0 ? values[0] : key;
        }
        return key;
    }

    public static bool Has(string key) => key != null && Entries.ContainsKey(key);
}
