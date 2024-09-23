using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class WinLoseScreen : MonoBehaviour
{
    public Color lossColor;
    public Color winColor;

    public RectTransform winLossParentWidget;
    public CanvasGroup winLossParentCanvasGroup;
    public CanvasGroup scoreCanvasGroup;
    public TextMeshProUGUI scoreNumberText;
    public TextMeshProUGUI scoreRecordText;
    public RectTransform winLossTextWidget;
    public TextMeshProUGUI winLossText;
    public CanvasGroup restartCanvasGroup;
    public EventTrigger goToMenuButton;
    public EventTrigger restartButton;
    public Image winLossMenuBorder;

    public UIManager userInterfaceManager;
    public static WinLoseScreen selfInstance;

    private bool _levelWon = false;
    private bool _recordScore = false;

    public static Tweener highScoreAnim;

    private float _scoreRecordTextDefaultAlpha;

    private void Awake()
    {
        selfInstance = this;
        _scoreRecordTextDefaultAlpha = scoreRecordText.alpha;
    }

    public void DisplayWinLoseScreen(bool levelWon, bool highScoreSet)
    {
        SetDefaultWidgetPositions();
        _levelWon = levelWon;
        _recordScore = highScoreSet;
        userInterfaceManager.FadeInFadeOutBlur(0, 1, 0.7f, false);
        Invoke("BeginWinLoseUISequence", 0.7f);
    }

    public void SetDefaultWidgetPositions()
    {
        winLossParentCanvasGroup.alpha = 1;
        scoreRecordText.alpha = _scoreRecordTextDefaultAlpha;
        winLossParentWidget.gameObject.SetActive(false);
        winLossParentWidget.anchoredPosition = new Vector2(userInterfaceManager.screenSize.x / -2, userInterfaceManager.screenSize.y / -2);
        winLossTextWidget.gameObject.SetActive(false);
        winLossMenuBorder.gameObject.SetActive(false);
        scoreCanvasGroup.gameObject.SetActive(false);
        restartCanvasGroup.gameObject.SetActive(false);
    }

    void BeginWinLoseUISequence()
    {
        winLossParentCanvasGroup.blocksRaycasts = true;

        winLossParentWidget.gameObject.SetActive(true);
        userInterfaceManager.gameManagerRef.gameplayElements.SetActive(false);
        userInterfaceManager.newGameMenu.gameObject.SetActive(false);
        GameManager.IsGameOpen = false;

        // Set text widgets info
        winLossText.text = _levelWon ? "You Win" : "Bad Luck";
        winLossText.color = _levelWon ? winColor : lossColor;

        winLossTextWidget.gameObject.SetActive(true);

        if (_levelWon)
        {
            scoreNumberText.text = GameManager.currentScore.ToString();
            scoreRecordText.text = _recordScore ? "New Record" : "Your Score";

            if (_recordScore) PlayRecordScoreAnim();
        }
        else
        {
            scoreNumberText.text = GameManager.currentScore.ToString();
            scoreRecordText.text = "Your Score";
        }

        float animDuration = 0.3f;

        winLossText.transform.localScale = Vector3.zero;

        // Set border defaults
        winLossMenuBorder.color = new Color(1, 1, 1, 0);
        winLossMenuBorder.gameObject.SetActive(true);
        winLossMenuBorder.raycastTarget = true;

        // You win, you lose text pop anim with overshoot
        DOTween.Sequence()
        .Append(DOTween.To(() => winLossText.transform.localScale, x => winLossText.transform.localScale = x, new Vector3(0.6f, 0.6f, 1), animDuration * 0.4f)
        .SetEase(Ease.OutQuad))
        .Append(DOTween.To(() => winLossText.transform.localScale, x => winLossText.transform.localScale = x, new Vector3(1, 1, 1), animDuration * 0.6f)
        .SetEase(Ease.OutBack, 3f))
        .Join(winLossMenuBorder.DOFade(0.02f, animDuration))
        .OnComplete(() =>
        {
            FadeInScore();
        });
    }

    void FadeInScore()
    {
        scoreCanvasGroup.alpha = 0;
        scoreCanvasGroup.gameObject.SetActive(true);
        scoreCanvasGroup.DOFade(1, 0.5f)
            .OnComplete(() =>
            {
                FadeInRestart();
            });
    }

    void FadeInRestart()
    {
        restartCanvasGroup.alpha = 0;
        restartCanvasGroup.gameObject.SetActive(true);
        restartCanvasGroup.DOFade(1, 0.5f)
            .OnComplete(() =>
            {
                winLossMenuBorder.raycastTarget = false;
            });
    }

    void PlayRecordScoreAnim()
    {
        // Reset the alpha before starting the animation
        scoreRecordText.alpha = 0;

        // Create a loop for the fade-in and fade-out effect
        highScoreAnim = scoreRecordText.DOFade(_scoreRecordTextDefaultAlpha, 0.6f)
            .SetLoops(-1, LoopType.Yoyo) // Ping-pong indefinitely
            .SetEase(Ease.InOutSine); // Smooth easing for a nicer effect

        // Reset the alpha after killing the animation
        highScoreAnim.OnKill(() => scoreRecordText.alpha = _scoreRecordTextDefaultAlpha);
    }

}
