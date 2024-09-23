using UnityEngine;

public class ColorManager : MonoBehaviour
{
    public static ColorManager Instance { get; private set; }

    public Color colorGreen;
    public Color colorBlue;
    public Color colorYellow;
    public Color colorPurple;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Optional, to keep it across scenes
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public static Color PickColor(BubbleColor inputColor)
    {
        switch (inputColor)
        {
            case BubbleColor.Green:
                return Instance.colorGreen;
            case BubbleColor.Blue:
                return Instance.colorBlue;
            case BubbleColor.Yellow:
                return Instance.colorYellow;
            case BubbleColor.Purple:
                return Instance.colorPurple;
            default:
                return Color.white;
        }
    }
}