using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Runtime UI controller for the branching passenger dialogue system.
/// Drives a DialogueRuntime against a built overlay, fires gameplay events,
/// and handles both the boarding/hub flow and the completion reveal.
/// </summary>
public class DialogueController : MonoBehaviour
{
    [Header("Dialogue Bar")]
    [SerializeField] private GameObject     root;
    [SerializeField] private DialogBox      dialogBox;
    [SerializeField] private GameObject     continueIndicator;
    [SerializeField] private Button         nextButton;
    [SerializeField] private Button         skipButton;
    [SerializeField] private RectTransform  choiceContainer;
    [SerializeField] private Button         choiceButtonTemplate;

    [Header("Speaker portrait (HoYo placeholder)")]
    [SerializeField] private Image          speakerPortrait;
    [SerializeField] private TMP_Text       speakerInitials;

    [Header("Reveal / Cutscene")]
    [SerializeField] private GameObject     revealRoot;
    [SerializeField] private TMP_Text       revealBody;
    [SerializeField] private TMP_Text       journalCard;

    public event Action<DialogueEventKind, string> OnEvent;

    // Hard wall-clock cap on how long a line waits for the AI rephrase before falling
    // back to the authored text. The transport may walk several keys/models, so this
    // bounds the *total* wait the player sees, not any single attempt.
    const float DialogueAiBudgetSeconds = 4f;

    DialogueRuntime _runtime;
    DialogueConversation _conversation;
    Action          _onFinished;
    Action          _onRevealDone;
    bool            _waitingForRevealAdvance;
    bool            _revealingJournalCard;
    bool            _subscribedToSettings;
    bool            _choiceClickedThisFrame;
    bool            _awaitingRephrase;
    AiCancellation  _dialogueCancellation;

    // Reusable choice button pool.
    readonly List<Button> _activeChoices = new List<Button>();

    void OnEnable()
    {
        if (SettingsManager.Instance != null && !_subscribedToSettings)
        {
            SettingsManager.Instance.OnSettingsChanged += ApplySettings;
            _subscribedToSettings = true;
        }
        ApplySettings();

        if (nextButton != null)
            nextButton.onClick.AddListener(OnAdvancePressed);
        if (skipButton != null)
            skipButton.onClick.AddListener(SkipConversation);
    }

    void OnDisable()
    {
        _dialogueCancellation?.Cancel();
        if (SettingsManager.Instance != null && _subscribedToSettings)
        {
            SettingsManager.Instance.OnSettingsChanged -= ApplySettings;
            _subscribedToSettings = false;
        }

        if (nextButton != null)
            nextButton.onClick.RemoveListener(OnAdvancePressed);
        if (skipButton != null)
            skipButton.onClick.RemoveListener(SkipConversation);
    }

    void Update()
    {
        if (_runtime == null || _runtime.IsFinished) return;
        if (_awaitingRephrase) return;

        if (_choiceClickedThisFrame)
        {
            _choiceClickedThisFrame = false;
            return;
        }

        bool pointerPressedAwayFromUi = Input.GetMouseButtonDown(0) &&
                                        (EventSystem.current == null ||
                                         !EventSystem.current.IsPointerOverGameObject());
        bool advancePressed = Input.GetKeyDown(KeyCode.Space) || pointerPressedAwayFromUi;
        if (!advancePressed) return;

        OnAdvancePressed();
    }

    /// <summary>
    /// Advances the dialogue by one step (completes typewriter, advances reveal,
    /// or steps the runtime). Shared by keyboard/click and the Next button.
    /// </summary>
    void OnAdvancePressed()
    {
        if (_runtime == null || _runtime.IsFinished) return;
        if (_awaitingRephrase) return;

        // Reveal flow uses the same advance input.
        if (_waitingForRevealAdvance)
        {
            if (dialogBox != null && dialogBox.IsRevealing)
            {
                dialogBox.Advance();
            }
            else if (_revealingJournalCard)
            {
                FinishReveal();
            }
            else
            {
                AdvanceRevealLine();
            }
            return;
        }

        if (dialogBox != null && dialogBox.IsRevealing)
        {
            dialogBox.Advance();
            return;
        }

        if (_runtime.AvailableChoices().Count > 0)
            return; // player must pick a choice button

        if (_runtime.IsAwaitingEventClear)
            return; // waiting for the drive controller to clear the event

        StepRuntime();
    }

    /// <summary>
    /// Skips the remainder of the current conversation and invokes the finished
    /// callback. During the reveal/cutscene flow it jumps straight to the end.
    /// </summary>
    public void SkipConversation()
    {
        if (_runtime == null) return;
        _dialogueCancellation?.Cancel();
        _awaitingRephrase = false;

        if (_waitingForRevealAdvance)
        {
            FinishReveal();
            return;
        }

        while (!_runtime.IsFinished)
        {
            if (_runtime.AvailableChoices().Count > 0)
                break; // cannot auto-resolve a choice; finish now

            if (_runtime.IsAwaitingEventClear)
            {
                _runtime.ClearEvent(); // skip the gameplay beat
                continue;
            }

            _runtime.AdvanceLine();
        }

        HideAll();
        _onFinished?.Invoke();
        _onFinished = null;
    }

    // -------------------------------------------------------------------------
    // Public API

    /// <summary>
    /// Plays the boarding beat and topic hub for a level.
    /// </summary>
    public void Play(DialogueConversation convo, Action onFinished)
    {
        _conversation = convo;
        _onFinished = onFinished;
        _runtime = new DialogueRuntime(convo);
        _runtime.Begin();
        _waitingForRevealAdvance = false;
        _revealingJournalCard = false;

        if (revealRoot != null) revealRoot.SetActive(false);
        ShowDialogueRoot(true);
        RefreshView();
    }

    /// <summary>
    /// Plays the completion reveal + journal page card, then invokes onDone.
    /// </summary>
    public void PlayReveal(DialogueConversation convo, JournalPageDefinition page, Action onDone)
    {
        _conversation = convo;
        _onRevealDone = onDone;
        _runtime = new DialogueRuntime(convo);
        _runtime.Begin();
        _waitingForRevealAdvance = true;
        _revealingJournalCard = false;

        if (revealRoot != null)
        {
            revealRoot.SetActive(true);
            if (journalCard != null && page != null)
            {
                journalCard.text =
                    $"<b>{page.heritageTitle}</b>\n\n" +
                    $"{page.heritageBody}\n\n" +
                    $"<color=#9EA0A2><i>{page.artifactCardDescription}</i></color>";
            }
            if (revealBody != null) revealBody.text = "";
        }

        ShowDialogueRoot(true);
        ClearChoices();
        ShowFirstRevealLine();
    }

    /// <summary>
    /// Resumes after a gameplay event has been handled. Call from the drive controller.
    /// </summary>
    public void ResumeAfterEvent()
    {
        if (_runtime == null) return;
        _runtime.ClearEvent();
        RefreshView();
    }

    /// <summary>
    /// Skips the current typewriter reveal if any.
    /// </summary>
    public void SkipReveal()
    {
        if (dialogBox != null) dialogBox.Advance();
    }

    // -------------------------------------------------------------------------

    void ShowDialogueRoot(bool show)
    {
        // The bar is always shown while dialogue is active so the Next/Skip
        // buttons remain reachable regardless of the Subtitles setting.
        if (root != null)
            root.SetActive(show);
    }

    void ApplySettings()
    {
        if (dialogBox == null) return;

        float cps = 45f;
        if (SettingsManager.Instance != null)
        {
            switch (SettingsManager.Instance.DialogueSpeed)
            {
                case DialogueSpeed.Slow:    cps = 22f;  break;
                case DialogueSpeed.Normal:  cps = 45f;  break;
                case DialogueSpeed.Fast:    cps = 95f;  break;
                case DialogueSpeed.Instant: cps = 0f;   break;
            }
        }

        dialogBox.CharsPerSecond = cps;
        dialogBox.UseTypewriter = cps > 0f;

        // Bar visibility is driven by ShowDialogueRoot, not by the Subtitles
        // setting, so the Next/Skip buttons are always available.
    }

    void RefreshView()
    {
        if (_runtime == null) return;

        if (_runtime.IsFinished)
        {
            HideAll();
            _onFinished?.Invoke();
            _onFinished = null;
            return;
        }

        if (_runtime.IsAwaitingEventClear)
        {
            FirePendingEvent();
            return;
        }

        IReadOnlyList<DialogueChoice> choices = _runtime.AvailableChoices();
        if (choices.Count > 0)
        {
            ShowChoices(choices);
            return;
        }

        if (_runtime.Current != null)
        {
            ClearChoices();
            ShowLine(_runtime.Current, _runtime.CurrentNodeId);
        }
    }

    void StepRuntime()
    {
        if (_runtime == null) return;

        bool hasMore = _runtime.AdvanceLine();
        if (hasMore)
            RefreshView();
        else
            RefreshView(); // will finish
    }

    void ShowLine(DialogueLine line, string nodeId)
    {
        if (dialogBox == null) return;

        SetSpeakerPortrait(line.speaker);

        var pax = _conversation != null ? PassengerLibrary.Get(_conversation.passengerId) : null;
        if (pax == null) { dialogBox.Show(line.speaker, line.text); return; }
        StartCoroutine(RephraseLine(line, pax));
    }

    // -------------------------------------------------------------------------
    // Speaker portrait (HoYo-style placeholder: a tinted plate + speaker initials,
    // deterministically coloured per speaker so each character is recognisable).

    void SetSpeakerPortrait(string speaker)
    {
        if (speakerPortrait == null) return;

        bool has = !string.IsNullOrEmpty(speaker);
        speakerPortrait.gameObject.SetActive(has);
        if (speakerInitials != null) speakerInitials.gameObject.SetActive(has);
        if (!has) return;

        speakerPortrait.color = PortraitColor(speaker);
        if (speakerInitials != null) speakerInitials.text = Initials(speaker);
    }

    static Color PortraitColor(string s)
    {
        int h = 17;
        foreach (char c in s) h = h * 31 + c;
        float hue = Mathf.Abs(h % 360) / 360f;
        return Color.HSVToRGB(hue, 0.45f, 0.70f);
    }

    static string Initials(string s)
    {
        string[] parts = s.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        string a = parts.Length > 0 ? parts[0].Substring(0, 1) : "";
        string b = parts.Length > 1 ? parts[1].Substring(0, 1) : "";
        return (a + b).ToUpperInvariant();
    }

    IEnumerator RephraseLine(DialogueLine line, PassengerDefinition pax)
    {
        _awaitingRephrase = true;

        // Token guard: short, trivial lines are delivered as authored — no API call.
        if (!LivingStoryService.ShouldRephrase(line.text))
        {
            if (dialogBox != null)
            {
                dialogBox.BeginStreaming(line.speaker);
                dialogBox.CompleteStreaming(line.text);
            }
            if (_waitingForRevealAdvance && revealBody != null)
                revealBody.text = $"<color=#EAEADC>{line.text}</color>";
            _awaitingRephrase = false;
            yield break;
        }

        _dialogueCancellation?.Cancel();
        AiCancellation cancellation = _dialogueCancellation = new AiCancellation();
        if (dialogBox != null) dialogBox.BeginStreaming(line.speaker);

        AiRequest request = LivingStoryService.BuildRequest(line.text, pax,
            SaveSystem.Current.currentLevelIndex,
            _runtime != null ? _runtime.Tone : DialogueTone.Neutral,
            _runtime != null ? _runtime.Affinity : 0);
        request.Cancellation = cancellation;
        AiResult result = null;
        bool streamDone = false;
        string streamed = "";
        string validationContext = LivingStoryService.ContextForValidation(line.text);

        // Run the stream concurrently so we can enforce the wall-clock budget below.
        StartCoroutine(GeminiClient.Stream(request, delta =>
        {
            if (cancellation.IsCancellationRequested) return; // drop late deltas after fallback
            streamed += delta;
            string trimmed = streamed.TrimEnd();
            bool completeSentence = trimmed.EndsWith(".") || trimmed.EndsWith("!") || trimmed.EndsWith("?");
            if (completeSentence && dialogBox != null &&
                AiGroundingValidator.ValidatePartial(line.text, streamed, validationContext))
                dialogBox.UpdateStreaming(streamed.Trim());
        }, completed => { result = completed; streamDone = true; }));

        float deadline = Time.realtimeSinceStartup + DialogueAiBudgetSeconds;
        while (!streamDone && Time.realtimeSinceStartup < deadline)
        {
            if (cancellation.IsCancellationRequested) yield break; // superseded / disabled / skipped
            yield return null;
        }

        if (!streamDone)
            cancellation.Cancel();        // budget elapsed — abort in-flight, fall back to authored
        else if (cancellation.IsCancellationRequested)
            yield break;                  // cancelled during the final stream callback

        string final = result != null && result.Success ? result.Text.Trim() : null;
        if (!AiGroundingValidator.ValidateParaphrase(line.text, final, validationContext, out string reason))
        {
            if (result != null && result.Success)
                Debug.LogWarning($"[LivingStory] Rejected paraphrase ({reason}); using authored line.");
            final = line.text;
        }
        if (dialogBox != null) dialogBox.CompleteStreaming(final);
        if (_waitingForRevealAdvance && revealBody != null)
            revealBody.text = $"<color=#EAEADC>{final}</color>";
        _awaitingRephrase = false;
    }

    void FirePendingEvent()
    {
        if (_runtime.PendingEvent == DialogueEventKind.None) return;

        ClearChoices();
        if (dialogBox != null) dialogBox.Hide();

        OnEvent?.Invoke(_runtime.PendingEvent, _runtime.Conversation.nodes[_runtime.CurrentNodeId].eventPayload);
    }

    void ShowChoices(IReadOnlyList<DialogueChoice> choices)
    {
        if (dialogBox != null) dialogBox.Hide();
        ClearChoices();

        if (choiceContainer == null || choiceButtonTemplate == null) return;
        choiceContainer.gameObject.SetActive(true);

        foreach (DialogueChoice choice in choices)
        {
            Button btn = Instantiate(choiceButtonTemplate, choiceContainer);
            btn.gameObject.SetActive(true);
            var label = btn.GetComponentInChildren<TMP_Text>(true);
            if (label != null) label.text = choice.label;

            DialogueChoice captured = choice;
            btn.onClick.AddListener(() => OnChoiceClicked(captured));
            _activeChoices.Add(btn);
        }
    }

    void ClearChoices()
    {
        foreach (Button btn in _activeChoices)
        {
            if (btn != null) Destroy(btn.gameObject);
        }
        _activeChoices.Clear();
        if (choiceContainer != null) choiceContainer.gameObject.SetActive(false);
    }

    void OnChoiceClicked(DialogueChoice choice)
    {
        if (_runtime == null) return;
        _choiceClickedThisFrame = true;

        // Discussing a hub topic surfaces the next heritage fun-fact into the Almanac.
        if (_conversation != null && _runtime.CurrentNodeId == _conversation.hubNode)
            DiscoverNextFact();

        _runtime.Choose(choice.target);
        RefreshView();
    }

    void DiscoverNextFact()
    {
        HeritageEntry town = HeritageLibrary.ForLevel(_conversation.levelIndex);
        if (town == null || town.keyFacts == null) return;

        // Unlock the next not-yet-discovered fact for this town, in order.
        for (int i = 0; i < town.keyFacts.Length; i++)
        {
            string key = town.townKey + ":" + i;
            if (!SaveSystem.Current.HasFact(key))
            {
                SaveSystem.Current.UnlockFact(key);
                SaveSystem.AutoSave();
                return;
            }
        }
    }

    void HideAll()
    {
        _dialogueCancellation?.Cancel();
        _awaitingRephrase = false;
        ClearChoices();
        if (dialogBox != null) dialogBox.Hide();
        if (root != null) root.SetActive(false);
    }

    // -------------------------------------------------------------------------
    // Reveal flow

    int _revealLineIndex;

    void ShowFirstRevealLine()
    {
        _revealLineIndex = 0;
        ShowCurrentRevealLine();
    }

    void ShowCurrentRevealLine()
    {
        if (_runtime == null || _runtime.Conversation.revealLines == null) return;

        if (_revealLineIndex < _runtime.Conversation.revealLines.Length)
        {
            DialogueLine line = _runtime.Conversation.revealLines[_revealLineIndex];
            ShowLine(line, "__REVEAL__");
            if (revealBody != null)
                revealBody.text = $"<color=#EAEADC>{line.text}</color>";
        }
        else
        {
            ShowJournalCard();
        }
    }

    void AdvanceRevealLine()
    {
        _revealLineIndex++;
        ShowCurrentRevealLine();
    }

    void ShowJournalCard()
    {
        _revealingJournalCard = true;
        SetSpeakerPortrait("");
        if (dialogBox != null)
        {
            string badgeName = BadgeLibrary.Get(_runtime.Conversation.levelIndex)?.badgeName ?? "Badge";
            dialogBox.Show("", $"Earned: {badgeName}");
        }
    }

    void FinishReveal()
    {
        HideAll();
        if (revealRoot != null) revealRoot.SetActive(false);
        _onRevealDone?.Invoke();
        _onRevealDone = null;
        _waitingForRevealAdvance = false;
        _revealingJournalCard = false;
    }
}
