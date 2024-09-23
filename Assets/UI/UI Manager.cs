using UnityEngine;
using DG.Tweening;
using TMPro;

public class UIManager : MonoBehaviour
{
    public UITransitionType menuTransitionType = UITransitionType.Fade;
    public float menuSwitchAnimDuration;
    public float menuSwitchOvershoot;

    public RectTransform levelGrid;
    public CanvasGroup blurCanvas;
    public TextMeshProUGUI scoreText;
    public RectTransform levelSelectMenu;
    public RectTransform mainMenu;
    public RectTransform newGameMenu;
    public RectTransform giveUpMenu;
    public RectTransform aboutMenu;
    public RectTransform exitConfirmMenu;
    public RectTransform howToPlayMenu;

    public GameManager gameManagerRef;

    public Vector2 screenSize;

    public string creditsURL;

    private Tweener _blurTween;
    private Tweener _hideWidgetTween;
    private Tweener _showWidgetTween;

    private void Awake()
    {
        mainMenu.gameObject.SetActive(false);
        GameManager.scoreText = scoreText;
    }

    private void Start()
    {
        InitializeMainMenu();
    }

    public void LevelSelectToMainMenu()
    {
        UISwitchAnim(levelSelectMenu, mainMenu, true, menuTransitionType, false, menuSwitchAnimDuration);
    }

    public void MainMenuToLevelSelect()
    {
        UISwitchAnim(mainMenu, levelSelectMenu, false, menuTransitionType, false, menuSwitchAnimDuration);
    }

    public void WinLoseToLevelSelect()
    {
        WinLoseScreen.highScoreAnim?.Kill();
        gameManagerRef.EndGame();
        blurCanvas.gameObject.SetActive(false);
        UISwitchAnim(WinLoseScreen.selfInstance.winLossParentWidget, levelSelectMenu, true, menuTransitionType, false, menuSwitchAnimDuration);
    }

    public void WinLoseToNewGame()
    {
        WinLoseScreen.highScoreAnim?.Kill();
        gameManagerRef.EndGame();
        blurCanvas.gameObject.SetActive(false);
        GameManager.IsGameOpen = true;
        GameManager.currentScore = 0;
        scoreText.text = GameManager.currentScore.ToString();
        UISwitchAnim(WinLoseScreen.selfInstance.winLossParentWidget, newGameMenu, false, menuTransitionType, true, menuSwitchAnimDuration);
    }

    public void InitializeMainMenu()
    {
        UISwitchAnim(null, mainMenu, false, UITransitionType.Fade, false, 0.7f);
    }

    public void LevelSelectToNewGame()
    {
        GameManager.IsGameOpen = true;
        GameManager.currentScore = 0;
        scoreText.text = GameManager.currentScore.ToString();
        UISwitchAnim(levelSelectMenu, newGameMenu, false, menuTransitionType, true, menuSwitchAnimDuration);
    }

    public void NewGameToGiveUp()
    {
        GameManager.IsGameOpen = false;
        UISwitchAnim(newGameMenu, giveUpMenu, false, menuTransitionType, true, menuSwitchAnimDuration);
    }

    public void GiveUpNewGame()
    {
        UISwitchAnim(giveUpMenu, levelSelectMenu, false, menuTransitionType, false, menuSwitchAnimDuration);
        gameManagerRef.EndGame();
    }

    public void CancelGiveUp()
    {
        GameManager.IsGameOpen = true;
        UISwitchAnim(giveUpMenu, newGameMenu, true, menuTransitionType, true, menuSwitchAnimDuration);
    }

    public void MainToExitGame()
    {
        UISwitchAnim(mainMenu, exitConfirmMenu, true, menuTransitionType, false, menuSwitchAnimDuration);
    }

    public void ExitGameToMain()
    {
        UISwitchAnim(exitConfirmMenu, mainMenu, false, menuTransitionType, false, menuSwitchAnimDuration);
    }

    public void MainToAbout()
    {
        UISwitchAnim(mainMenu, aboutMenu, true, menuTransitionType, false, menuSwitchAnimDuration);
    }

    public void AboutToMain()
    {
        UISwitchAnim(aboutMenu, mainMenu, false, menuTransitionType, false, menuSwitchAnimDuration);
    }

    public void AboutToHTP()
    {
        UISwitchAnim(aboutMenu, howToPlayMenu, true, menuTransitionType, false, menuSwitchAnimDuration);
    }

    public void HTPToMain()
    {
        UISwitchAnim(howToPlayMenu, mainMenu, false, menuTransitionType, false, menuSwitchAnimDuration);
    }

    public void ExitGame()
    {
        Application.Quit();
    }

    public void OpenAuthorURL()
    {
        Application.OpenURL(creditsURL);
    }

    void UISwitchAnim(RectTransform hideWidget, RectTransform showWidget, bool moveLeft, UITransitionType transitionEffect, bool affectGameplay, float animDuration)
    {
        _showWidgetTween?.Kill();
        _hideWidgetTween?.Kill();

        showWidget.gameObject.SetActive(true);

        CanvasGroup showCanvasGroup = showWidget.GetComponent<CanvasGroup>();

        if (showCanvasGroup == null) 
        {
            Debug.LogError(showWidget + " does not have a Canvas Group component.");
            return;
        }

        CanvasGroup hideCanvasGroup = null;
        if (hideWidget != null)
        {
            hideWidget.gameObject.SetActive(true);

            hideCanvasGroup = hideWidget.GetComponent<CanvasGroup>();

            if (hideCanvasGroup == null)
            {
                Debug.LogError(hideWidget + " does not have a Canvas Group component.");
                return;
            }

            hideCanvasGroup.blocksRaycasts = false;
        }

        showCanvasGroup.blocksRaycasts = false;

        // Hide gameplay objects
        if (!GameManager.IsGameOpen && affectGameplay)
        {
            gameManagerRef.HideGameField(animDuration, transitionEffect);
        }
        // Show gameplay objects
        else if (affectGameplay)
        {
            gameManagerRef.StartNewGame(animDuration, menuSwitchOvershoot, transitionEffect);
        }

        switch (transitionEffect)
        {
            case UITransitionType.Slide:

                if (moveLeft)
                {
                    if (hideWidget != null)
                    {
                        _hideWidgetTween = hideWidget.DOAnchorPosX(screenSize.x * -2, animDuration);
                    }
                    showWidget.anchoredPosition = new Vector2(screenSize.x * 1.5f, screenSize.y / -2);
                }
                else
                {
                    if (hideWidget != null)
                    {
                        _hideWidgetTween = hideWidget.DOAnchorPosX(screenSize.x * 1.5f, animDuration);
                    }
                    showWidget.anchoredPosition = new Vector2(screenSize.x * -2, screenSize.y / -2);
                }

                _showWidgetTween = showWidget.DOAnchorPosX(screenSize.x / -2, animDuration).SetEase(Ease.OutBack, menuSwitchOvershoot)
                    .OnComplete(() =>
                    {
                        OnUITransitionComplete(hideWidget, showCanvasGroup);
                    });

                break;

            case UITransitionType.Fade:

                showWidget.anchoredPosition = new Vector2(screenSize.x / -2, screenSize.y / -2);

                showCanvasGroup.alpha = 0;

                if (hideWidget != null)
                {
                    _hideWidgetTween = hideCanvasGroup.DOFade(0, animDuration * 0.5f).SetEase(Ease.InQuad)
                        .OnComplete(() => 
                    {
                        _showWidgetTween = showCanvasGroup.DOFade(1, animDuration * 0.5f).SetEase(Ease.InQuad)
                        .OnComplete(() =>
                        {
                        OnUITransitionComplete(hideWidget, showCanvasGroup);
                        });
                    });
                }
                else
                {
                    _showWidgetTween = showCanvasGroup.DOFade(1, animDuration * 0.5f).SetEase(Ease.InQuad)
                        .OnComplete(() =>
                        {
                            OnUITransitionComplete(hideWidget, showCanvasGroup);
                        });
                }

                break;

            case UITransitionType.Instant:

                _showWidgetTween?.Kill();
                _hideWidgetTween?.Kill();

                showCanvasGroup.alpha = 1;
                showWidget.anchoredPosition = new Vector2(screenSize.x / -2, screenSize.y / -2);

                if (hideWidget != null) 
                {
                    hideCanvasGroup.blocksRaycasts = false;
                    hideCanvasGroup.alpha = 0;
                }

                OnUITransitionComplete(hideWidget, showCanvasGroup);

                break;

        }
        
    }

    void OnUITransitionComplete(RectTransform hideWidget, CanvasGroup showCanvasGroup)
    {
        if (hideWidget != null)
        {
            hideWidget.gameObject.SetActive(false);
        }
        showCanvasGroup.blocksRaycasts = true;
    }

    public void FadeInFadeOutBlur(float startAlpha, float endAlpha, float animDuration, bool pingPongBlur)
    {
        _blurTween?.Kill();

        blurCanvas.alpha = startAlpha;
        blurCanvas.gameObject.SetActive(true);

        _blurTween = blurCanvas.DOFade(endAlpha, animDuration).SetEase(Ease.InQuad)
            .OnComplete(() =>
            {
                if (pingPongBlur && endAlpha == 1)
                {
                    _blurTween.Kill();

                    _blurTween = blurCanvas.DOFade(startAlpha, animDuration).SetEase(Ease.InQuad)
                    .OnComplete(() =>
                    {
                        blurCanvas.blocksRaycasts = false;
                        blurCanvas.gameObject.SetActive(false);
                    });
                }
                else
                {
                    blurCanvas.blocksRaycasts = endAlpha == 0 ? false : true;
                    blurCanvas.gameObject.SetActive(endAlpha == 0 ? false : true);
                }
                
            });
    }

}

public enum UITransitionType
{
    Slide,
    Fade,
    Instant
}
