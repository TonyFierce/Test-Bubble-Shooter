using DG.Tweening;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class NextBubble : MonoBehaviour
{
    private Tweener _numBubblesUpdateAnim;
    public TextMeshPro numBubblesText;

    private static NextBubble _selfInstance;

    public static SpriteRenderer bubbleSprite;

    public static Queue<BubbleColor> shootBubbleQueue = new Queue<BubbleColor>();

    private Sequence _bubbleAppearTween;

    private void Awake()
    {
        _selfInstance = this;
        bubbleSprite = GetComponent<SpriteRenderer>();
        bubbleSprite.enabled = false;
    }

    public static void InitializeShootBubbleQueue(List<string> bubbleColors, int expectedBubbleCount)
    {
        shootBubbleQueue.Clear();

        // Ensure we don't add more bubbles than expectedBubbleCount
        int countToAdd = Mathf.Min(bubbleColors.Count, expectedBubbleCount);

        // Warn if bubbleColors count doesn't match the expected count
        if (bubbleColors.Count != expectedBubbleCount)
        {
            Debug.LogWarning($"Bubble count mismatch: Expected {expectedBubbleCount}, but got {bubbleColors.Count}. Purging the rest.");
        }

        for (int i = 0; i < countToAdd; i++)
        {
            string colorString = bubbleColors[i];
            if (Enum.TryParse(colorString, out BubbleColor bubbleColor))
            {
                shootBubbleQueue.Enqueue(bubbleColor);
            }
            else
            {
                Debug.LogWarning($"Invalid bubble color: {colorString}");
            }
        }
    }

    public static void SetNextBubbleColor()
    {
        if (shootBubbleQueue.Count > 0)
        {
            BubbleColor nextColor = shootBubbleQueue.Peek();
            Color nextRGBA = ColorManager.PickColor(nextColor);
            bubbleSprite.color = new Color(nextRGBA.r, nextRGBA.g, nextRGBA.b, bubbleSprite.color.a);
            bubbleSprite.enabled = true;
            _selfInstance.BubbleAppearAnim();
        }
        else
        {
            bubbleSprite.enabled = false;
            Debug.Log("No more bubbles in the queue.");
        }
        _selfInstance.NumBubblesLeftUpdateAnim();
    }

    void BubbleAppearAnim()
    {
        _bubbleAppearTween?.Kill();

        // Set the initial size to zero
        bubbleSprite.size = Vector2.zero;

        float animDuration = 0.3f;

        _bubbleAppearTween = DOTween.Sequence()
        .Append(DOTween.To(() => bubbleSprite.size, x => bubbleSprite.size = x, new Vector2(0.6f, 0.6f), animDuration * 0.4f)
        .SetEase(Ease.OutQuad)) // Smoothly grow
        .Append(DOTween.To(() => bubbleSprite.size, x => bubbleSprite.size = x, new Vector2(1, 1), animDuration * 0.6f)
        .SetEase(Ease.OutBack, 3f)); // Final growth with overshoot effect
    }

    void NumBubblesLeftUpdateAnim()
    {
        UpdateNumBubblesLeft();

        _numBubblesUpdateAnim?.Kill();

        numBubblesText.transform.localScale = Vector2.one * 0.9f;

        // Parameters for the punch effect
        int vibrato = 1;
        float elasticity = 1f;

        // Calculate the punch scale to double the size
        Vector3 punch_scale = numBubblesText.transform.localScale * 0.3f;

        // Apply the punch effect
        _numBubblesUpdateAnim = numBubblesText.transform.DOPunchScale(punch_scale, 0.4f, vibrato, elasticity);
    }

    void UpdateNumBubblesLeft()
    {
        numBubblesText.text = shootBubbleQueue.Count.ToString();
    }

}
