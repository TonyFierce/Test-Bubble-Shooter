using DG.Tweening;
using UnityEngine;

public class ShootBubble : MonoBehaviour
{
    public SpriteRenderer bubbleSprite;
    public Rigidbody2D shootBubbleRigidbody;
    public CircleCollider2D shootBubbleCollider;

    private bool _alreadyHit = false;
    [HideInInspector] public ShootController shootController;
    [HideInInspector] public ShootBubble previousBubble;
    private bool _maxPower = false;
    [HideInInspector] public BubbleColor shootBubbleColor = BubbleColor.Empty;

    private Sequence _reachedCeilingFloorAnimTween;

    private void Start()
    {
        bubbleSprite.color = ColorManager.PickColor(shootBubbleColor);

        BubbleAppearAnim();
    }

    private bool isFirstWallHit = true; // Track if it's the first wall hit

    private void OnCollisionEnter2D(Collision2D collision)
    {
        FieldBubble bubbleHit = collision.gameObject.GetComponent<FieldBubble>();

        // If already hit, do nothing
        if (_alreadyHit) return;

        // Check if the collision is with a bubble
        if (bubbleHit != null)
        {
            _alreadyHit = true;

            bubbleHit.OnBubbleHit(_maxPower, shootBubbleColor, transform.position, this); // Perform the action on the bubble

            FreezeBubblePhysics();
        }
        else
        {
            // Check if the object collided with "Wall"
            if (collision.gameObject.CompareTag("Wall"))
            {
                // Pass -1 for the first wall hit and 1 for subsequent hits
                int ricochetDirectionMulti = isFirstWallHit ? -1 : 1;
                shootController.RicochetBubble(ricochetDirectionMulti);

                isFirstWallHit = !isFirstWallHit; // Toggle the state for next hit
            }
            else if (collision.gameObject.CompareTag("Floor") || collision.gameObject.CompareTag("Ceiling"))
            {
                BubbleReachedCeilingOrFloor();

            }
        }
    }

    void BubbleAppearAnim()
    {
        // Set the initial size to zero
        bubbleSprite.size = Vector2.zero;

        float animDuration = 0.3f;

        DOTween.Sequence()
        .Append(DOTween.To(() => bubbleSprite.size, x => bubbleSprite.size = x, new Vector2(0.6f, 0.6f), animDuration * 0.4f)
        .SetEase(Ease.OutQuad)) // Smoothly grow
        .Append(DOTween.To(() => bubbleSprite.size, x => bubbleSprite.size = x, new Vector2(1, 1), animDuration * 0.6f)
        .SetEase(Ease.OutBack, 3f)) // Final growth with overshoot effect
        .OnComplete(() =>
        {
            shootController.readyToShoot = true;



            if (previousBubble != null)
            { 
                Destroy(previousBubble.gameObject);
            }
        });
    }

    public void SetMaxPower(bool maxPower)
    {
        _maxPower = maxPower;
    }

    public void FreezeBubblePhysics()
    {
        _alreadyHit = true;

        shootBubbleCollider.radius = 0.2f;
        shootBubbleRigidbody.gravityScale = 0;
        shootBubbleRigidbody.velocity = Vector2.zero;
    }

    public void BubbleReachedCeilingOrFloor()
    {
        FreezeBubblePhysics();

        _reachedCeilingFloorAnimTween?.Kill();

        // Set the initial size
        bubbleSprite.size = new Vector2(0.7f, 0.7f);

        float shrinkDuration = 0.25f; // Duration of the shrink effect

        _reachedCeilingFloorAnimTween = DOTween.Sequence();

        // Shrink the bubble smoothly to zero size with an overshoot effect
        _reachedCeilingFloorAnimTween.Append(DOTween.To(() => bubbleSprite.size, x => bubbleSprite.size = x, Vector2.zero, shrinkDuration)
            .SetEase(Ease.InBack, 4f));

        _reachedCeilingFloorAnimTween.OnComplete(() =>
        {

            Destroy(gameObject);

            shootController.SpawnNewBubble();
        });

    }

}
