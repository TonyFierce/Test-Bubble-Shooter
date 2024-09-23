using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FieldBubble : MonoBehaviour
{
    public int bubblePopScoreValue = 10;
    public float perPoppedBubbleScoreMulti = 0.5f;

    public DroppedBubble droppedBubblePrefab;

    private bool _isInitialized;

    private int _row;
    private int _column;

    public SpriteRenderer bubbleSprite;
    public Collider2D bubbleCollider;

    private BubbleData _bubbleData;

    private Sequence _appearAnimTween;
    private Sequence _burstAnimTween;
    private Sequence _popAnimTween;

    public float bubblePopDelay = 0.08f;

    public void InitializeBubble(Vector2Int bubbleXY, BubbleData bubbleData)
    {
        _row = bubbleXY.x;
        _column = bubbleXY.y;
        _bubbleData = bubbleData;

        HandleBubbleState();

        _isInitialized = true;
    }

    void HandleBubbleState()
    {
        // At game start
        if (!_isInitialized)
        {
            if (_bubbleData.bubbleColor == BubbleColor.Empty)
            {
                bubbleSprite.enabled = false;
                bubbleCollider.enabled = false;
            }
            else
            {
                bubbleCollider.enabled = true;
                bubbleSprite.enabled = true;
                bubbleSprite.color = ColorManager.PickColor(_bubbleData.bubbleColor);

                BubbleAppearAnim();
            }
        }

        // During the game
        else
        {
            if (_bubbleData.bubbleColor == BubbleColor.Empty)
            {
                bubbleSprite.enabled = false;
                bubbleCollider.enabled = false;
            }
            else
            {
                bubbleSprite.color = ColorManager.PickColor(_bubbleData.bubbleColor);
                bubbleSprite.enabled = true;
                bubbleCollider.enabled = true;
            }
        }
    }

    void BubbleAppearAnim()
    {
        _appearAnimTween?.Kill();

        // Set the initial size to zero
        bubbleSprite.size = Vector2.zero;

        float animDuration = 0.3f;

        _appearAnimTween = DOTween.Sequence()
        .Append(DOTween.To(() => bubbleSprite.size, x => bubbleSprite.size = x, new Vector2(0.6f, 0.6f), animDuration * 0.4f)
        .SetEase(Ease.OutQuad)) // Smoothly grow
        .Append(DOTween.To(() => bubbleSprite.size, x => bubbleSprite.size = x, Vector2.one, animDuration * 0.6f)
        .SetEase(Ease.OutBack, 3f)); // Final growth with overshoot effect

    }

    void BubbleMaxPowerAnim(BubbleColor shootBubbleColor, Vector2Int updatedBubbleXY)
    {
        _burstAnimTween?.Kill();

        // Set the initial size to half
        bubbleSprite.size = new Vector2(0.5f, 0.5f);

        float animDuration = 0.3f;

        _burstAnimTween = DOTween.Sequence()
        .Append(DOTween.To(() => bubbleSprite.size, x => bubbleSprite.size = x, Vector2.one, animDuration)
        .SetEase(Ease.OutBack, 4f)) // Grow with overshoot effect
        .OnComplete(() =>
        {
            PopBubbleGroups(shootBubbleColor, updatedBubbleXY);
        });
    }

    public void OnBubbleHit(bool maxPower, BubbleColor shootBubbleColor, Vector2 shotBubblePosition, ShootBubble shotBubbleRef)
    {
        if (maxPower)
        {
            _bubbleData.bubbleColor = shootBubbleColor;
            Field.bubbleDataGrid[new Vector2Int(_row, _column)] = _bubbleData;

            BubbleMaxPowerAnim(shootBubbleColor, new Vector2Int(_row, _column));
            HandleBubbleState();

            shotBubbleRef.bubbleSprite.enabled = false; // Hide the shot bubble sprite
        }
        else
        {
            // Get the neighbors of the hit bubble
            List<Vector2Int> neighbors = GetNeighbors(_row, _column);

            // Try to place the bubble in the closest empty neighbor
            Vector2Int closestEmptyNeighbor = FindClosestNeighbor(neighbors, shotBubblePosition, color => color == BubbleColor.Empty);

            if (closestEmptyNeighbor != Vector2Int.zero)
            {
                // Check if the closest empty neighbor is in the last row
                if (closestEmptyNeighbor.x == Field.rows - 1)
                {
                    Debug.Log("Closest empty space is below the last allowed row. Destroying the bubble.");

                    // Drop the bubble and get 0 score
                    shotBubbleRef.bubbleSprite.enabled = false;
                    shotBubbleRef.FreezeBubblePhysics();
                    DropShotBubbleFromLastRow(shotBubbleRef);

                    return; // Stop the method if the closest empty neighbor is in the last row
                }

                // Place the bubble in the closest empty neighbor
                if (TryPlaceBubbleInClosestNeighbor(neighbors, shootBubbleColor, shotBubblePosition, BubbleColor.Empty))
                {
                    // Disable the shot bubble sprite if an empty neighbor was found
                    shotBubbleRef.bubbleSprite.enabled = false;

                    return; // Exit if the bubble was successfully placed
                }
            }

            // If no suitable empty neighbor found, check all neighbors
            Debug.Log("Closest empty space not found. Searching for the closest non-empty neighbor...");

            // Find the closest non-empty neighbor
            Vector2Int closestNonEmptyXY = FindClosestNeighbor(neighbors, shotBubblePosition, color => color != BubbleColor.Empty);

            if (closestNonEmptyXY != Vector2Int.zero)
            {
                // Get the neighbors of the closest non-empty bubble and try to place in an empty neighbor
                List<Vector2Int> closestNonEmptyNeighbors = GetNeighbors(closestNonEmptyXY.x, closestNonEmptyXY.y);

                if (!TryPlaceBubbleInClosestNeighbor(closestNonEmptyNeighbors, shootBubbleColor, shotBubblePosition, BubbleColor.Empty))
                {
                    Debug.Log("No empty space found near the closest non-empty bubble.");

                    shotBubbleRef.BubbleReachedCeilingOrFloor();
                }
                else
                {
                    shotBubbleRef.bubbleSprite.enabled = false;
                }
            }
        }
    }

    // General method to place a bubble in the closest matching neighbor based on a color condition
    private bool TryPlaceBubbleInClosestNeighbor(List<Vector2Int> neighbors, BubbleColor shootBubbleColor, Vector2 shotBubblePosition, BubbleColor targetColor)
    {
        Vector2Int closestNeighborPosition = FindClosestNeighbor(neighbors, shotBubblePosition, color => color == targetColor);

        if (closestNeighborPosition != Vector2Int.zero)
        {
            // Update the closest empty neighbor's color and animate
            BubbleData closestBubbleData = Field.bubbleDataGrid[closestNeighborPosition];
            closestBubbleData.bubbleColor = shootBubbleColor;

            Field.bubbleDataGrid[closestNeighborPosition] = closestBubbleData;
            closestBubbleData.bubbleReference.InitializeBubble(closestNeighborPosition, closestBubbleData);
            closestBubbleData.bubbleReference.BubbleMaxPowerAnim(shootBubbleColor, closestNeighborPosition);

            return true;
        }

        return false;
    }

    // General method to find the closest neighbor matching a color condition
    private Vector2Int FindClosestNeighbor(List<Vector2Int> neighbors, Vector2 shotBubblePosition, Func<BubbleColor, bool> colorCondition)
    {
        BubbleData closestBubbleData = default;
        float closestDistance = float.MaxValue;
        Vector2Int closestNeighborPosition = Vector2Int.zero;

        foreach (var neighborPosition in neighbors)
        {
            // Check if the neighbor position exists in the grid
            if (Field.bubbleDataGrid.TryGetValue(neighborPosition, out BubbleData neighborBubbleData))
            {
                // Apply the color condition (empty or non-empty)
                if (colorCondition(neighborBubbleData.bubbleColor))
                {
                    float distance = Vector2.Distance(shotBubblePosition, neighborBubbleData.bubbleReference.transform.position);

                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestBubbleData = neighborBubbleData;
                        closestNeighborPosition = neighborPosition;
                    }
                }
            }
        }

        return closestBubbleData.bubbleReference != null ? closestNeighborPosition : Vector2Int.zero;
    }

    // Helper method to get neighbors based on row and column
    private List<Vector2Int> GetNeighbors(int row, int column)
    {
        GetNeighborOffsets(row, out int[] rowOffsets, out int[] columnOffsets);
        List<Vector2Int> neighbors = new List<Vector2Int>();

        for (int i = 0; i < rowOffsets.Length; i++)
        {
            neighbors.Add(new Vector2Int(row + rowOffsets[i], column + columnOffsets[i]));
        }

        return neighbors;
    }

    void PopBubbleGroups(BubbleColor shootBubbleColor, Vector2Int updatedBubbleXY)
    {
        // List to store the bubbles that are part of the same color group
        List<FieldBubble> connectedBubbles = new List<FieldBubble>();

        // Start the flood fill process to find all connected bubbles of the same color
        FloodFill(connectedBubbles, updatedBubbleXY.x, updatedBubbleXY.y, shootBubbleColor);

        // If there are at least 3 connected bubbles, start popping them with delay
        if (connectedBubbles.Count >= 3)
        {
            StartCoroutine(PopBubblesWithDelay(connectedBubbles));
        }
        else
        {
            // Spawn new bubble
            Field.gameManager.shootController.SpawnNewBubble();
        }
    }

    IEnumerator PopBubblesWithDelay(List<FieldBubble> bubblesToPop)
    {
        float delay = bubblePopDelay;

        // Pop the first bubble immediately without delay
        bubblesToPop[0].BubblePopAnim(0, bubblesToPop.Count);

        // Loop through the remaining bubbles, applying the delay before popping each one
        for (int i = 1; i < bubblesToPop.Count; i++)
        {
            // Wait for delay before popping the next bubble
            yield return new WaitForSeconds(delay);

            // Play bubble pop animation and pass the current index and total bubble count
            bubblesToPop[i].BubblePopAnim(i, bubblesToPop.Count);
        }
    }

    void FloodFill(List<FieldBubble> connectedBubbles, int row, int column, BubbleColor targetColor)
    {
        // Check if the current bubble position is valid in the grid
        if (!Field.bubbleDataGrid.TryGetValue(new Vector2Int(row, column), out BubbleData currentBubbleData))
        {
            return;
        }

        // Skip if the bubble is already added to the connected list or if it is not of the target color
        if (currentBubbleData.bubbleColor != targetColor || connectedBubbles.Contains(currentBubbleData.bubbleReference))
        {
            return;
        }

        // Add this bubble to the connected list
        connectedBubbles.Add(currentBubbleData.bubbleReference);

        // Determine the neighbors based on the row's parity
        GetNeighborOffsets(row, out int[] rowOffsets, out int[] columnOffsets);

        // Check all 6 neighbors (considering staggered rows)
        for (int i = 0; i < rowOffsets.Length; i++)
        {
            int newRow = row + rowOffsets[i];
            int newColumn = column + columnOffsets[i];
            FloodFill(connectedBubbles, newRow, newColumn, targetColor);
        }
    }

    void BubblePopAnim(int bubbleIndex, int totalBubbles)
    {
        float bubbleDropScoreMulti = 1 + (totalBubbles - 1) * perPoppedBubbleScoreMulti;

        Field.UpdateLevelScore((int)(bubblePopScoreValue * bubbleDropScoreMulti));

        _bubbleData.bubbleColor = BubbleColor.Empty;
        Field.bubbleDataGrid[new Vector2Int(_row, _column)] = _bubbleData;

        _popAnimTween?.Kill();

        // Set the initial size to normal
        bubbleSprite.size = Vector2.one;

        float shrinkDuration = 0.25f; // Duration of the shrink effect

        _popAnimTween = DOTween.Sequence();

        // Shrink the bubble smoothly to zero size with an overshoot effect
        _popAnimTween.Append(DOTween.To(() => bubbleSprite.size, x => bubbleSprite.size = x, Vector2.zero, shrinkDuration)
            .SetEase(Ease.InBack, 4f));

        _popAnimTween.OnComplete(() =>
        {
            // On anim complete, disable collision and sprite
            HandleBubbleState();

            // Check if this was the last bubble in the list
            if (bubbleIndex == totalBubbles - 1)
            {
                // This is the last bubble
                Field.CheckFreeFloatingBubbles();
            }
        });
    }

    public void GetNeighborOffsets(int row, out int[] rowOffsets, out int[] columnOffsets)
    {
        if (row % 2 != 0) // Odd row
        {
            rowOffsets = new[] { -1, -1, 0, 0, 1, 1 };
            columnOffsets = new[] { 0, 1, -1, 1, 0, 1 };
        }
        else // Even row
        {
            rowOffsets = new[] { -1, -1, 0, 0, 1, 1 };
            columnOffsets = new[] { -1, 0, -1, 1, -1, 0 };
        }
    }

    private void DropShotBubbleFromLastRow(ShootBubble shotBubbleRef)
    {
        // Instantiate a DroppedBubble prefab
        DroppedBubble droppedBubble = Instantiate(droppedBubblePrefab, shotBubbleRef.transform.position, Quaternion.identity);

        droppedBubble.scoreMultiplier = 0;

        droppedBubble.bubbleSprite.color = ColorManager.PickColor(shotBubbleRef.shootBubbleColor);
    }

}
