using DG.Tweening;
using UnityEngine;

public class DroppedBubble : MonoBehaviour
{
    public SpriteRenderer bubbleSprite;
    private Sequence _reachedFloorAnimTween;

    public int bubbleDropScoreValue = 20;

    [HideInInspector] public float scoreMultiplier = 1;
    [HideInInspector] public BubbleColor shootBubbleColor = BubbleColor.Empty;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Check if the collision is with the Floor
        if (collision.gameObject.CompareTag("Floor"))
        {
            BubbleReachedFloorAnim();
        }
    }

    void BubbleReachedFloorAnim()
    {
        if (scoreMultiplier != 0)
        {
            Field.UpdateLevelScore((int)(bubbleDropScoreValue * scoreMultiplier));
        }

        _reachedFloorAnimTween?.Kill();

        // Set the initial size
        bubbleSprite.size = new Vector2(0.7f, 0.7f);

        float shrinkDuration = 0.25f; // Duration of the shrink effect

        _reachedFloorAnimTween = DOTween.Sequence();

        // Shrink the bubble smoothly to zero size with an overshoot effect
        _reachedFloorAnimTween.Append(DOTween.To(() => bubbleSprite.size, x => bubbleSprite.size = x, Vector2.zero, shrinkDuration)
            .SetEase(Ease.InBack, 4f));

        _reachedFloorAnimTween.OnComplete(() =>
        {
            // Remove itself from Field.droppedBubbles list
            if (Field.droppedBubbles.Count > 0) 
            {
                Field.droppedBubbles.Remove(this);
            }

            if (Field.droppedBubbles.Count == 0)
            {
                Field.gameManager.shootController.SpawnNewBubble();
            }

            Destroy(gameObject);
        });

    }

}
