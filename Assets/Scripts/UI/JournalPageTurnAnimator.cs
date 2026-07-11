using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Presentation-only page turn played when switching journal sections.</summary>
public sealed class JournalPageTurnAnimator : MonoBehaviour
{
    [SerializeField] RawImage overlay;
    [SerializeField] Button heritageTab;
    [SerializeField] Button codingTab;
    [SerializeField] Button oracleTab;
    [SerializeField] float frameDuration = 0.045f;
    int _currentTab;
    Coroutine _turn;

    void Awake()
    {
        heritageTab?.onClick.AddListener(() => TurnTo(0));
        codingTab?.onClick.AddListener(() => TurnTo(1));
        oracleTab?.onClick.AddListener(() => TurnTo(2));
    }

    void TurnTo(int tab)
    {
        if (tab == _currentTab || overlay == null || overlay.texture == null) return;
        bool forward = tab > _currentTab;
        _currentTab = tab;
        if (_turn != null) StopCoroutine(_turn);
        _turn = StartCoroutine(Play(forward ? 0 : 1));
    }

    IEnumerator Play(int rowFromTop)
    {
        overlay.gameObject.SetActive(true);
        const int frames = 8;
        for (int i = 0; i < frames; i++)
        {
            int frame = rowFromTop == 0 ? i : frames - 1 - i;
            overlay.uvRect = new Rect(frame / (float)frames, rowFromTop == 0 ? 0.5f : 0f,
                                      1f / frames, 0.5f);
            yield return new WaitForSecondsRealtime(frameDuration);
        }
        overlay.gameObject.SetActive(false);
        _turn = null;
    }
}
