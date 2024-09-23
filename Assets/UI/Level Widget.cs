using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class LevelWidget : MonoBehaviour
{
    public TextMeshProUGUI levelNameText;
    public TextMeshProUGUI levelScoreText;
    public EventTrigger levelStartTrigger;
    public Button levelButton;

    public void SetCurrentLevel()
    {
        GameManager.currentLevel = "Level " + levelNameText.text;
    }
}