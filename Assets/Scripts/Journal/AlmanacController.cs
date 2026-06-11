using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drives the two-page Almanac UI: tab switching, sidebar lock states,
/// heritage/coding content, and the close button.
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

    enum Tab { Heritage, Coding }

    // Local palette copy — runtime scripts must not reference editor UIFactory.
    static readonly Color Accent     = new Color(0.95f, 0.65f, 0.15f, 1f);
    static readonly Color PanelDark  = new Color(0.10f, 0.12f, 0.16f, 0.96f);
    static readonly Color TextBright = new Color(0.93f, 0.93f, 0.88f, 1f);
    static readonly Color TextDim    = new Color(0.62f, 0.64f, 0.66f, 1f);

    Tab _currentTab = Tab.Heritage;
    int _selectedPageId;
    bool _bound;
    bool _entriesBuilt;
    readonly List<Button> _sidebarEntries = new List<Button>();

    // -------------------------------------------------------------------------

    public bool IsOpen => bookRoot != null && bookRoot.activeSelf;

    // -------------------------------------------------------------------------

    void Start()
    {
        Bind();
        BuildSidebarEntries();
        if (bookRoot != null) bookRoot.SetActive(false);
    }

    // -------------------------------------------------------------------------
    // Public API

    public void Open()
    {
        Bind();
        BuildSidebarEntries();
        RefreshSidebarLockStates();
        ShowPage(_selectedPageId);
        if (bookRoot != null) bookRoot.SetActive(true);
    }

    public void Close()
    {
        if (bookRoot != null) bookRoot.SetActive(false);
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

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);
    }

    // -------------------------------------------------------------------------

    void SetTab(Tab tab)
    {
        _currentTab = tab;
        RefreshTabVisuals();
        ShowPage(_selectedPageId);
    }

    void RefreshTabVisuals()
    {
        SetTabColor(heritageTabButton, _currentTab == Tab.Heritage);
        SetTabColor(codingTabButton,    _currentTab == Tab.Coding);
    }

    void SetTabColor(Button button, bool active)
    {
        if (button == null) return;
        var label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
            label.color = active ? Accent : TextDim;
    }

    // -------------------------------------------------------------------------

    void BuildSidebarEntries()
    {
        if (_entriesBuilt) return;
        if (sidebarEntryTemplate == null || sidebarContent == null) return;

        _entriesBuilt = true;

        for (int i = 0; i < JournalPageLibrary.Pages.Count; i++)
        {
            int pageId = i;
            Button entry = Instantiate(sidebarEntryTemplate, sidebarContent);
            entry.gameObject.SetActive(true);
            entry.name = $"SidebarEntry_{pageId}";

            var label = entry.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
                label.text = LevelLibrary.Names[pageId];

            entry.onClick.AddListener(() => SelectPage(pageId));
            _sidebarEntries.Add(entry);
        }
    }

    void RefreshSidebarLockStates()
    {
        for (int i = 0; i < _sidebarEntries.Count; i++)
        {
            Button entry = _sidebarEntries[i];
            if (entry == null) continue;

            bool unlocked = ProgressionRules.IsUnlocked(SaveSystem.Current, i);
            bool selected = i == _selectedPageId;

            entry.interactable = true;

            var label = entry.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                if (selected)      label.color = Accent;
                else if (unlocked) label.color = TextBright;
                else               label.color = TextDim;
            }

            var image = entry.image;
            if (image != null)
                image.color = selected ? new Color(Accent.r, Accent.g, Accent.b, 0.25f) : PanelDark;
        }
    }

    void SelectPage(int pageId)
    {
        _selectedPageId = pageId;
        RefreshSidebarLockStates();
        ShowPage(pageId);
    }

    // -------------------------------------------------------------------------

    void ShowPage(int pageId)
    {
        if (pageId < 0 || pageId >= JournalPageLibrary.Pages.Count) return;

        bool unlocked = ProgressionRules.IsUnlocked(SaveSystem.Current, pageId);

        if (!unlocked)
        {
            ShowLockedContent();
            return;
        }

        JournalPageDefinition page = JournalPageLibrary.Pages[pageId];

        if (_currentTab == Tab.Heritage)
        {
            if (contentTitle != null)
                contentTitle.text = page.heritageTitle;

            if (contentBody != null)
            {
                contentBody.text =
                    $"<b>{LevelLibrary.Names[pageId]}</b>\n\n" +
                    $"{page.heritageBody}\n\n" +
                    $"<color=#{ColorUtility.ToHtmlStringRGB(TextDim)}><i>{page.artifactCardDescription}</i></color>";
            }
        }
        else
        {
            if (contentTitle != null)
                contentTitle.text = page.codingConceptName;

            if (contentBody != null)
            {
                contentBody.text =
                    $"<b>{page.codingConceptName}</b>\n\n" +
                    $"{page.codingReferenceBody}\n\n" +
                    $"<color=#{ColorUtility.ToHtmlStringRGB(Accent)}>{page.codeExample}</color>";
            }
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
                "The journal entry and coding reference will appear here once the town is unlocked.";
        }

        RefreshBodyHeight();
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
