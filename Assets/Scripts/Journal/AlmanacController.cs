using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drives the three-tab Almanac UI: tab switching, the sidebar (towns for Heritage,
/// programming concepts for Coding Reference), the detail content, and the close button.
/// </summary>
public class AlmanacController : MonoBehaviour
{
    [Header("Toggled Root")]
    [SerializeField] private GameObject bookRoot;

    [Header("Tabs")]
    [SerializeField] private Button heritageTabButton;
    [SerializeField] private Button codingTabButton;

    [Header("Sidebar")]
    [SerializeField] private RectTransform sidebarContent;
    [SerializeField] private Button      sidebarEntryTemplate;

    [Header("Content")]
    [SerializeField] private TMP_Text contentTitle;
    [SerializeField] private TMP_Text contentBody;

    [Header("Oracle")]
    [SerializeField] private ChatController chatController;

    [Header("Navigation")]
    [SerializeField] private Button closeButton;
    [SerializeField] private Button previousButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private TMP_Text pageIndicator;

    [Header("PvZ layout")]
    [SerializeField] private GameObject detailPane;
    [SerializeField] private GameObject oraclePane;
    [SerializeField] private Button     oracleTabButton;
    [SerializeField] private Image      entryArt;
    [SerializeField] private TMP_Text   entryArtLabel;
    [SerializeField] private Sprite heritageCardSprite;
    [SerializeField] private Sprite heritageSelectedSprite;
    [SerializeField] private Sprite heritageLockedSprite;
    [SerializeField] private Sprite codingRowSprite;
    [SerializeField] private Sprite codingSelectedSprite;
    [SerializeField] private Sprite[] landmarkSprites;
    [SerializeField] private Sprite[] codingIconSprites;

    enum Tab { Heritage, Coding, Oracle }

    // Local palette copy — runtime scripts must not reference editor UIFactory.
    static readonly Color Accent     = new Color(0.95f, 0.65f, 0.15f, 1f);
    static readonly Color PanelDark  = new Color(0.10f, 0.12f, 0.16f, 0.96f);
    static readonly Color TextBright = new Color(0.93f, 0.93f, 0.88f, 1f);
    static readonly Color TextDim    = new Color(0.62f, 0.64f, 0.66f, 1f);

    Tab _currentTab = Tab.Heritage;
    int _selectedPageId;      // Heritage selection (town / level)
    int _selectedConceptId;   // Coding selection (programming concept)
    bool _bound;
    readonly List<Button> _sidebarEntries = new List<Button>();

    // -------------------------------------------------------------------------

    public bool IsOpen => bookRoot != null && bookRoot.activeSelf;

    // -------------------------------------------------------------------------

    void Start()
    {
        Bind();
        RebuildSidebar();
        if (bookRoot != null) bookRoot.SetActive(false);
    }

    // -------------------------------------------------------------------------
    // Public API

    public void Open()
    {
        Bind();
        RebuildSidebar();
        if (bookRoot != null) bookRoot.SetActive(true);
    }

    public void Close()
    {
        if (bookRoot != null) bookRoot.SetActive(false);
        // Each visit to the Oracle starts fresh — wipe the transcript on close.
        if (chatController != null) chatController.ClearChat();
    }

    public void Toggle()
    {
        if (IsOpen) Close();
        else        Open();
    }

    // -------------------------------------------------------------------------

    void Bind()
    {
        if (_bound) return;
        _bound = true;

        if (heritageTabButton != null)
            heritageTabButton.onClick.AddListener(() => SetTab(Tab.Heritage));

        if (codingTabButton != null)
            codingTabButton.onClick.AddListener(() => SetTab(Tab.Coding));

        if (oracleTabButton != null)
            oracleTabButton.onClick.AddListener(() => SetTab(Tab.Oracle));

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);
        if (previousButton != null)
            previousButton.onClick.AddListener(() => StepSelection(-1));
        if (nextButton != null)
            nextButton.onClick.AddListener(() => StepSelection(1));
    }

    // -------------------------------------------------------------------------

    void SetTab(Tab tab)
    {
        _currentTab = tab;
        RefreshTabVisuals();

        bool oracle = tab == Tab.Oracle;
        if (oraclePane != null) oraclePane.SetActive(oracle);
        if (detailPane != null) detailPane.SetActive(!oracle);

        if (!oracle) RebuildSidebar();
        RefreshNavigation();
    }

    void RefreshTabVisuals()
    {
        SetTabColor(heritageTabButton, _currentTab == Tab.Heritage);
        SetTabColor(codingTabButton,   _currentTab == Tab.Coding);
        SetTabColor(oracleTabButton,   _currentTab == Tab.Oracle);
    }

    void SetTabColor(Button button, bool active)
    {
        if (button == null) return;
        var label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
            label.color = active ? Accent : TextDim;
    }

    // -------------------------------------------------------------------------
    // Sidebar — rebuilt per tab: towns (Heritage) vs concepts (Coding).

    void RebuildSidebar()
    {
        if (sidebarEntryTemplate == null || sidebarContent == null) return;
        if (_currentTab == Tab.Oracle) return;   // Oracle has no sidebar list

        foreach (Button b in _sidebarEntries)
            if (b != null) Destroy(b.gameObject);
        _sidebarEntries.Clear();

        if (_currentTab == Tab.Coding) BuildConceptEntries();
        else                           BuildPlaceEntries();

        RefreshSidebarLockStates();

        if (_currentTab == Tab.Coding) ShowConcept(_selectedConceptId);
        else                           ShowPage(_selectedPageId);
        RefreshNavigation();
    }

    void BuildPlaceEntries()
    {
        for (int i = 0; i < JournalPageLibrary.Pages.Count; i++)
        {
            int pageId = i;
            Button entry = NewEntry($"SidebarEntry_{pageId}", LevelLibrary.Names[pageId]);
            entry.onClick.AddListener(() => SelectPage(pageId));
            _sidebarEntries.Add(entry);
        }
    }

    void BuildConceptEntries()
    {
        for (int i = 0; i < CodingConceptLibrary.Concepts.Count; i++)
        {
            int conceptId = i;
            Button entry = NewEntry($"ConceptEntry_{conceptId}", CodingConceptLibrary.Concepts[conceptId].title);
            entry.onClick.AddListener(() => SelectConcept(conceptId));
            _sidebarEntries.Add(entry);
        }
    }

    Button NewEntry(string name, string label)
    {
        Button entry = Instantiate(sidebarEntryTemplate, sidebarContent);
        entry.gameObject.SetActive(true);
        entry.name = name;
        var text = entry.GetComponentInChildren<TMP_Text>(true);
        if (text != null) text.text = label;
        var icon = entry.transform.Find("EntryIcon");
        if (icon != null)
        {
            int index = _currentTab == Tab.Coding ? _sidebarEntries.Count : _sidebarEntries.Count;
            var iconImage = icon.GetComponent<Image>();
            if (iconImage != null)
            {
                iconImage.sprite = _currentTab == Tab.Coding && index < codingIconSprites.Length
                    ? codingIconSprites[index]
                    : index < landmarkSprites.Length ? landmarkSprites[index] : null;
                iconImage.color = Color.white;
            }
        }
        return entry;
    }

    void RefreshSidebarLockStates()
    {
        bool coding = _currentTab == Tab.Coding;
        var grid = sidebarContent != null ? sidebarContent.GetComponent<GridLayoutGroup>() : null;
        if (grid != null) grid.cellSize = coding ? new Vector2(275f, 82f) : new Vector2(270f, 130f);
        int  selected = coding ? _selectedConceptId : _selectedPageId;

        for (int i = 0; i < _sidebarEntries.Count; i++)
        {
            Button entry = _sidebarEntries[i];
            if (entry == null) continue;

            // Concepts are reference material — always available. Towns gate on progress.
            bool unlocked = coding || ProgressionRules.IsUnlocked(SaveSystem.Current, i);
            bool isSelected = i == selected;

            entry.interactable = true;

            var label = entry.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                if (isSelected)    label.color = Accent;
                else if (unlocked) label.color = TextBright;
                else               label.color = TextDim;
            }

            var image = entry.image;
            if (image != null)
            {
                image.color = Color.white;
                image.sprite = coding
                    ? (isSelected ? codingSelectedSprite : codingRowSprite)
                    : (!unlocked ? heritageLockedSprite : isSelected ? heritageSelectedSprite : heritageCardSprite);
                image.type = Image.Type.Sliced;
            }
        }
    }

    void SelectPage(int pageId)
    {
        _selectedPageId = pageId;
        RefreshSidebarLockStates();
        ShowPage(pageId);
        RefreshNavigation();
    }

    void SelectConcept(int conceptId)
    {
        _selectedConceptId = conceptId;
        RefreshSidebarLockStates();
        ShowConcept(conceptId);
        RefreshNavigation();
    }

    void StepSelection(int delta)
    {
        if (_currentTab == Tab.Oracle) return;
        int count = _currentTab == Tab.Coding ? CodingConceptLibrary.Concepts.Count : JournalPageLibrary.Pages.Count;
        int current = _currentTab == Tab.Coding ? _selectedConceptId : _selectedPageId;
        int next = Mathf.Clamp(current + delta, 0, Mathf.Max(0, count - 1));
        if (_currentTab == Tab.Coding) SelectConcept(next); else SelectPage(next);
    }

    void RefreshNavigation()
    {
        bool oracle = _currentTab == Tab.Oracle;
        int count = _currentTab == Tab.Coding ? CodingConceptLibrary.Concepts.Count : JournalPageLibrary.Pages.Count;
        int current = _currentTab == Tab.Coding ? _selectedConceptId : _selectedPageId;
        if (previousButton != null) previousButton.interactable = !oracle && current > 0;
        if (nextButton != null) nextButton.interactable = !oracle && current + 1 < count;
        if (pageIndicator != null) pageIndicator.text = oracle ? "" : $"{current + 1} / {count}";
    }

    // -------------------------------------------------------------------------
    // Heritage content

    void ShowPage(int pageId)
    {
        if (pageId < 0 || pageId >= JournalPageLibrary.Pages.Count) return;

        bool unlocked = ProgressionRules.IsUnlocked(SaveSystem.Current, pageId);
        SetEntryArt(pageId, unlocked);

        if (!unlocked)
        {
            ShowLockedContent();
            return;
        }

        JournalPageDefinition page = JournalPageLibrary.Pages[pageId];

        if (contentTitle != null)
            contentTitle.text = page.heritageTitle;

        if (contentBody != null)
        {
            contentBody.text =
                $"<b>{LevelLibrary.Names[pageId]}</b>\n\n" +
                $"{page.heritageBody}\n\n" +
                $"<color=#{ColorUtility.ToHtmlStringRGB(TextDim)}><i>{page.artifactCardDescription}</i></color>" +
                BuildDiscoveredFacts(pageId);
        }

        RefreshBodyHeight();
    }

    void ShowLockedContent()
    {
        if (contentTitle != null)
            contentTitle.text = "Locked";

        if (contentBody != null)
        {
            contentBody.text =
                "Complete this leg of the journey to recover this page.\n\n" +
                "The journal entry will appear here once the town is unlocked." +
                BuildDiscoveredFacts(_selectedPageId);
        }

        RefreshBodyHeight();
    }

    // -------------------------------------------------------------------------
    // Coding Reference content

    void ShowConcept(int conceptId)
    {
        if (conceptId < 0 || conceptId >= CodingConceptLibrary.Concepts.Count) return;

        CodingConceptEntry concept = CodingConceptLibrary.Concepts[conceptId];
        SetConceptArt();

        if (contentTitle != null)
            contentTitle.text = concept.title;

        if (contentBody != null)
        {
            contentBody.text =
                $"<b>{concept.title}</b>\n\n" +
                $"{concept.body}\n\n" +
                $"<color=#{ColorUtility.ToHtmlStringRGB(Accent)}>{concept.codeExample}</color>";
        }

        RefreshBodyHeight();
    }

    // PvZ-style entry art placeholder: a colored banner + initials per town,
    // greyed with "?" while the entry is still locked.
    void SetEntryArt(int pageId, bool unlocked)
    {
        if (entryArt != null)
        {
            entryArt.sprite = pageId >= 0 && pageId < landmarkSprites.Length ? landmarkSprites[pageId] : null;
            entryArt.preserveAspect = true;
            entryArt.color = unlocked ? Color.white : new Color(0.35f, 0.30f, 0.26f, 0.55f);
        }
        if (entryArtLabel != null)
            entryArtLabel.text = unlocked ? "" : "🔒";
    }

    void SetConceptArt()
    {
        if (entryArt != null)
        {
            entryArt.sprite = _selectedConceptId >= 0 && _selectedConceptId < codingIconSprites.Length
                ? codingIconSprites[_selectedConceptId] : null;
            entryArt.preserveAspect = true;
            entryArt.color = Color.white;
        }
        if (entryArtLabel != null) entryArtLabel.text = "";
    }

    static Color ArtColor(string s)
    {
        int h = 17;
        foreach (char c in s) h = h * 31 + c;
        return Color.HSVToRGB(Mathf.Abs(h % 360) / 360f, 0.45f, 0.62f);
    }

    static string Initials(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        string[] parts = s.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
        string a = parts.Length > 0 ? parts[0].Substring(0, 1) : "";
        string b = parts.Length > 1 ? parts[1].Substring(0, 1) : "";
        return (a + b).ToUpperInvariant();
    }

    // Heritage fun-facts the player has discovered through dialogue for this town,
    // appended to the page so they surface in the Almanac as they're uncovered.
    string BuildDiscoveredFacts(int pageId)
    {
        HeritageEntry town = HeritageLibrary.ForLevel(pageId);
        if (town == null || town.keyFacts == null) return "";

        string accent = ColorUtility.ToHtmlStringRGB(Accent);
        string dim    = ColorUtility.ToHtmlStringRGB(TextDim);
        const string rule = "────────────────────";

        // Each discovered fact becomes its own framed "field note" — a rule, a bold
        // accent headline at a larger size, then the in-depth body — so the page reads
        // like an almanac of entries the player has collected, not a flat blob.
        var entries = new System.Text.StringBuilder();
        int found = 0;
        for (int i = 0; i < town.keyFacts.Length; i++)
        {
            if (!SaveSystem.Current.HasFact(town.townKey + ":" + i)) continue;
            HeritageFact f = town.keyFacts[i];
            found++;

            entries.Append("\n\n<color=#").Append(dim).Append(">").Append(rule).Append("</color>\n")
                   .Append("<size=120%><color=#").Append(accent).Append("><b>")
                   .Append(f.headline).Append("</b></color></size>\n")
                   .Append(f.detail);
        }

        if (found == 0) return "";

        var sb = new System.Text.StringBuilder();
        sb.Append("\n\n<size=130%><color=#").Append(accent).Append("><b>— Field Notes —</b></color></size>")
          .Append("  <color=#").Append(dim).Append("><i>(").Append(found).Append(" gathered)</i></color>");
        sb.Append(entries);
        sb.Append("\n\n<color=#").Append(dim).Append(">").Append(rule).Append("</color>");
        return sb.ToString();
    }

    void RefreshBodyHeight()
    {
        if (contentBody == null) return;

        Canvas.ForceUpdateCanvases();

        var le = contentBody.GetComponent<LayoutElement>();
        if (le != null)
            le.preferredHeight = Mathf.Max(contentBody.preferredHeight + 32f, 120f);
    }
}
