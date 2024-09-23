using TMPro;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;
using System.IO;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public PlayerPrefsReset playerPrefsResetScript;

    public LevelWidget levelWidgetPrefab;

    public static Dictionary<string, int> levelHighScores = new Dictionary<string, int>();
    public static string currentLevel = "Level 1";

    public NextBubble nextBubble;

    private Tweener _scoreUpdateTween;

    public static TextMeshProUGUI scoreText;
    public static int currentScore = 0;

    public Button pauseButton;
    public TextMeshProUGUI pauseButtonText;
    public EventTrigger pauseEventTrigger;

    private Tweener _pauseTween;

    public UIManager userInterfaceManager;
    public GameObject gameplayElements;

    public Field gameField;
    public GameObject walls;
    public ShootController shootController;

    public static bool IsGameOpen;
    private bool _isGameStarted;

    public float perRemainingBubbleScoreMulti = 0.25f;
    public int remainingBubbleScoreValue = 20;

    private void Awake()
    {
        playerPrefsResetScript.ResetSavedData();

        LoadHighScores();   // Load PlayerPrefs
        SaveLoadUnlockedLevels();

        var refreshRateRatio = Screen.currentResolution.refreshRateRatio;

        // Extract the numerator and denominator to calculate the actual refresh rate
        float refreshRate = refreshRateRatio.numerator / refreshRateRatio.denominator;

        Application.targetFrameRate = (int)refreshRate;
    }

    void SaveLoadUnlockedLevels()
    {
        // Clear previously spawned LevelWidget prefabs
        foreach (Transform child in userInterfaceManager.levelGrid)
        {
            if (child.TryGetComponent<LevelWidget>(out LevelWidget levelWidget))
            {
                Destroy(child.gameObject);
            }
        }

        // Load all JSON files from the Resources/Levels folder
        TextAsset[] levelFiles = Resources.LoadAll<TextAsset>("Levels");

        foreach (TextAsset levelFile in levelFiles)
        {
            // Extract the level number from the file name ("Level 1" -> 1)
            string levelName = Path.GetFileNameWithoutExtension(levelFile.name);
            string levelNumber = levelName.Replace("Level ", "");

            // Create the button from the prefab
            LevelWidget levelWidget = Instantiate(levelWidgetPrefab, userInterfaceManager.levelGrid);

            levelWidget.levelNameText.text = levelNumber;

            // Attempt to retrieve the high score for the current level
            int highScore = levelHighScores.TryGetValue(levelName, out highScore) ? highScore : 0;

            // Display the high score
            levelWidget.levelScoreText.text = highScore.ToString();

            // Check if the level is not "Level 1" and if the previous level has no high score
            if (levelNumber != "1")
            {
                // Get the previous level number
                string previousLevelName = "Level " + (int.Parse(levelNumber) - 1).ToString();
                int previousHighScore = levelHighScores.TryGetValue(previousLevelName, out previousHighScore) ? previousHighScore : 0;

                // Lock the level if the previous level has no high score
                if (previousHighScore == 0)
                {
                    levelWidget.levelStartTrigger.enabled = false;
                    levelWidget.levelNameText.alpha = 0.3f;
                    levelWidget.levelScoreText.alpha = 0.3f;
                    levelWidget.levelButton.interactable = false;
                    continue; // Skip to the next level
                }
            }

            // Create a new EventTrigger.Entry to launch the level on button click
            EventTrigger.Entry entry = new EventTrigger.Entry
            {
                eventID = EventTriggerType.PointerDown
            };

            // Add a callback method
            entry.callback.AddListener((eventData) => { levelWidget.SetCurrentLevel(); });
            entry.callback.AddListener((eventData) => { userInterfaceManager.LevelSelectToNewGame(); });

            // Add the entry to the event trigger
            levelWidget.levelStartTrigger.triggers.Add(entry);
        }
    }

    public void StartNewGame(float animDuration, float animOvershoot, UITransitionType transitionEffect)
    {
        
        if (NextBubble.bubbleSprite != null) 
        {
            NextBubble.bubbleSprite.enabled = false;
        }
        nextBubble.numBubblesText.text = "";

        if (!_isGameStarted)
        {
            shootController.TouchAssistAnim(false);
            DisablePauseDuringSpawnAnim();
        }

        switch (transitionEffect)
        {
            case UITransitionType.Slide:

                gameplayElements.SetActive(true);

                gameplayElements.transform.position = new Vector3(_isGameStarted ? 10 : -10, -2.8f, 0);
                gameplayElements.transform.DOMoveX(0, animDuration).SetEase(Ease.OutBack, animOvershoot)
                    .OnComplete(() =>
                    {
                        OnGameStartTransitionComplete();
                    });
                break;

            case UITransitionType.Fade:

                userInterfaceManager.FadeInFadeOutBlur(0, 1, animDuration * 0.5f, true);
                Invoke("ActivateGameplayWithDelay", animDuration * 0.5f);
                Invoke("OnGameStartTransitionComplete", animDuration);
                break;

            case UITransitionType.Instant:

                gameplayElements.SetActive(true);

                OnGameStartTransitionComplete();
                break;

        }
        
    }

    void ActivateGameplayWithDelay()
    {
        gameplayElements.SetActive(true);
    }

    void DeactivateGameplayWithDelay()
    {
        gameplayElements.SetActive(false);
    }

    void OnGameStartTransitionComplete()
    {
        if (!_isGameStarted)
        {
            gameField.StartCreateBubbleGrid(this, currentLevel);
        }
        else
        {
            shootController.SpawnNewBubble();
        }
        _isGameStarted = true;
    }

    public void EndGame()
    {
        shootController.TouchAssistAnim(false);
        shootController.touchAssistShown = false;

        _isGameStarted = false;
        foreach (var bubble in Field.bubbleDataGrid)
        {
            Destroy(bubble.Value.bubbleReference.gameObject);
        }
        Field.bubbleDataGrid.Clear();

        if (shootController.currentShootBubble != null)
        {
            Destroy(shootController.currentShootBubble.gameObject);
            shootController.readyToShoot = false;
        }

        Destroy(gameField.belowLastRowLineRef.gameObject);
    }

    public void HideGameField(float animDuration, UITransitionType transitionEffect)
    {

        switch (transitionEffect)
        {
            case UITransitionType.Slide:
                gameplayElements.transform.DOMoveX(10, animDuration)
                .OnComplete(() =>
                {
                    gameplayElements.SetActive(false);
                });
                break;

            case UITransitionType.Fade:
                userInterfaceManager.FadeInFadeOutBlur(0, 1, animDuration * 0.5f, false);
                Invoke("DeactivateGameplayWithDelay", animDuration * 0.5f);
                break;

            case UITransitionType.Instant:
                gameplayElements.SetActive(false);
                break;

        }  
    }

    public void SwitchPauseButton(bool enable)
    {
        // Get the current ColorBlock from the Button
        ColorBlock colorBlock = pauseButton.colors;

        // Set pause border alpha
        pauseButton.interactable = enable;
        colorBlock.fadeDuration = 0.3f;

        // Reapply the ColorBlock back to the Button
        pauseButton.colors = colorBlock;

        _pauseTween?.Kill();

        if (!enable)
        {
            pauseEventTrigger.enabled = false;
        }

        _pauseTween = pauseButtonText.DOFade(enable ? 1 : 0.3f, 0.3f)
            .OnComplete(() =>
            {
                if (enable)
                {
                    pauseEventTrigger.enabled = true;

                }

                colorBlock.fadeDuration = 0.1f;

                pauseButton.colors = colorBlock;
            });

    }

    public void ScoreUpdateAnim()
    {
        _scoreUpdateTween?.Kill();

        scoreText.transform.localScale = Vector2.one;

        // Parameters for the punch effect
        int vibrato = 1;
        float elasticity = 1f;

        // Calculate the punch scale to double the size
        Vector3 punch_scale = scoreText.transform.localScale * 0.5f;

        // Apply the punch effect
        _scoreUpdateTween = scoreText.transform.DOPunchScale(punch_scale, 0.4f, vibrato, elasticity);
    }

    void DisablePauseDuringSpawnAnim()
    {
        pauseEventTrigger.enabled = false;
        pauseButtonText.alpha = 0.3f;
        pauseButton.interactable = false;
    }

    public void CheckWinCondition(out bool levelWon)
    {
        // Count the number of colored bubbles in the bubbleDataGrid
        int coloredBubbleCount = 0;
        levelWon = false;

        foreach (var bubbleEntry in Field.bubbleDataGrid.Values)
        {
            if (bubbleEntry.bubbleColor != BubbleColor.Empty)
            {
                coloredBubbleCount++;
            }

            // If more than 3 colored bubbles, no need to continue checking, did not win yet
            if (coloredBubbleCount > 3)
            {
                break;
            }
        }

        bool highScoreSet = false; // Track if a high score was set

        if (coloredBubbleCount <= 3)
        {
            levelWon = true;

            if (NextBubble.shootBubbleQueue.Count == 0)
            {
                Debug.Log("Won");

                highScoreSet = SaveHighScore(currentLevel, currentScore); // Save high score and get the status
                SaveLoadUnlockedLevels();

                // Display win screen with high score status
                WinLoseScreen.selfInstance.DisplayWinLoseScreen(levelWon, highScoreSet);
                return;
            }
            else
            {
                // Convert remaining bubbles into score
                StartCoroutine(ConvertRemainingBubblesOnWin());
                return;
            }
            
        }

        // Check if there are more bubbles to shoot
        if (NextBubble.shootBubbleQueue.Count == 0 && !levelWon)
        {
            Debug.Log("Lost");

            // Display lose screen
            WinLoseScreen.selfInstance.DisplayWinLoseScreen(levelWon, false);
        }
    }

    private bool SaveHighScore(string levelName, int score)
    {
        bool highScoreSet = false; // Track if a high score was set

        // Check if the current level's high score exists
        if (levelHighScores.TryGetValue(levelName, out int existingScore))
        {
            // Update the high score if the current score is higher
            if (score > existingScore)
            {
                levelHighScores[levelName] = score;
                PlayerPrefs.SetInt(levelName, score); // Save to PlayerPrefs
                PlayerPrefs.Save(); // Ensure PlayerPrefs is saved
                highScoreSet = true; // A new high score was set
                Debug.Log($"New high score for {levelName}: {score}");
            }
        }
        else
        {
            // If no existing high score, set it
            levelHighScores[levelName] = score;
            PlayerPrefs.SetInt(levelName, score); // Save to PlayerPrefs
            PlayerPrefs.Save(); // Ensure PlayerPrefs is saved
            highScoreSet = true; // A high score was set
            Debug.Log($"High score set for {levelName}: {score}");
        }

        return highScoreSet; // Return whether a high score was set
    }

    private void LoadHighScores()
    {
        // Assuming you have a consistent naming convention for your levels
        // Iterate over the levels you have (e.g., "Level 1", "Level 2", etc.)
        for (int i = 1; i <= 10; i++) // Adjust the range based on how many levels you have
        {
            string levelName = $"Level {i}";
            int score = PlayerPrefs.GetInt(levelName, 0); // Get the score, default to 0 if not found

            if (score > 0) // Only add if there's a score
            {
                levelHighScores[levelName] = score;
                Debug.Log($"Loaded high score for {levelName}: {score}");
            }
        }
    }

    private IEnumerator ConvertRemainingBubblesOnWin()
    {
        float remainingBubbleScoreMulti = 1 + (NextBubble.shootBubbleQueue.Count - 1) * perRemainingBubbleScoreMulti;

        bool highScoreSet = false;

        // Convert each bubble color in the queue into score
        while (NextBubble.shootBubbleQueue.Count > 0)
        {
            // Remove current bubble from queue and play anim for the next bubble
            NextBubble.shootBubbleQueue.Dequeue();
            NextBubble.SetNextBubbleColor();

            Field.UpdateLevelScore((int)(remainingBubbleScoreValue * remainingBubbleScoreMulti));

            // Delay between each bubble
            yield return new WaitForSeconds(0.2f);

        }

        Debug.Log("Won");

        highScoreSet = SaveHighScore(currentLevel, currentScore); // Save high score and get the status
        SaveLoadUnlockedLevels();

        // Display win screen with high score status
        WinLoseScreen.selfInstance.DisplayWinLoseScreen(true, highScoreSet);
    }

}
