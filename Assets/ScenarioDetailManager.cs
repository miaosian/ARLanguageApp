using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;      // for UnityWebRequestTexture
using TMPro;

public class ScenarioDetailManager : MonoBehaviour
{
    [Header("UI Panels")]
    public CanvasGroup contextSelectionPanel;
    public CanvasGroup detailCanvasGroup;
    public Button      backButton;
    public RawImage    bgImage;             // fills panel
    public TMP_Text    titleText;           // scenario title
    public TMP_Text    descText;            // scenario description
    public Button      startButton;         // “Start Conversation”

    [Header("Flow")]
    public ConversationManager conversationManager; // assign in Inspector

    [Header("Fade Settings")]
    public float fadeDuration = 0.5f;


    private ScenarioData currentScenario;

    void Awake()
    {
        // hide at startup
        detailCanvasGroup.alpha = 0;
        detailCanvasGroup.blocksRaycasts = false;

        startButton.onClick.AddListener(OnStartConversation);
        backButton.onClick.AddListener(OnBackPressed);

    }

    /// <summary>
    /// Called by ContextSelectionManager after a Context is tapped.
    /// </summary>
    public void ShowScenario(ScenarioData s)
    {
        currentScenario = s;

        // update UI
        titleText.text = s.title;
        descText.text  = s.description;

        // load background image
        StartCoroutine(LoadBg(s.backgroundUrl));

        // fade in this panel
        StartCoroutine(Fade(detailCanvasGroup, 0, 1));
    }

    IEnumerator LoadBg(string url)
    {
        using (var uw = UnityWebRequestTexture.GetTexture(url))
        {
            yield return uw.SendWebRequest();
            if (uw.result == UnityWebRequest.Result.Success)
            {
                bgImage.texture = DownloadHandlerTexture.GetContent(uw);
            }
            else
            {
                Debug.LogError($"[ScenarioDetail] Failed to load bg: {uw.error}");
            }
        }
    }

    void OnStartConversation()
    {
        // fade this panel out, then begin conversation
        StartCoroutine(Fade(detailCanvasGroup, 1, 0, () =>
        {
            conversationManager.StartConversation(currentScenario);
        }));
    }

    IEnumerator Fade(CanvasGroup cg, float from, float to, Action onComplete = null)
    {
        float t = 0f;
        cg.alpha = from;
        cg.blocksRaycasts = to > 0.5f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            cg.alpha = Mathf.Lerp(from, to, t / fadeDuration);
            yield return null;
        }
        cg.alpha = to;
        onComplete?.Invoke();
    }

    void OnBackPressed() {
      // fade detail out
      StartCoroutine(Fade(detailCanvasGroup, 1, 0, () => {
        // once hidden, fade the context selection back in:
        StartCoroutine(Fade(contextSelectionPanel, 0, 1));
      }));
    }
}
