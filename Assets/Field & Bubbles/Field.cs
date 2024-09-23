using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class LevelData
{
    public int activeRows;
    public int columns;
    public int numberOfShootBubbles;
    public List<string> bubbleColors;
    public List<string> shootBubblesQueue;
}

public class Field : MonoBehaviour
{
    public BelowLastRowLine belowLastRowLinePrefab;
    [HideInInspector] public BelowLastRowLine belowLastRowLineRef;
    private Tweener _belowLastRowAnim;

    public float perDroppedBubbleScoreMulti = 0.25f;
    private float _bubbleDropScoreMulti = 1;

    [HideInInspector] public static int rows;
    [HideInInspector] public static int activeRows;
    [HideInInspector] public static int columns;

    public FieldBubble bubblePrefab;
    public static GameManager gameManager;
    private static Field _selfInstance;     // Singleton

    public float bubbleRadius = 0.25f;      // Default radius for prefab with 0.5 scale
    public float gapSize = 0.05f;        // For 0.5 scale

    [HideInInspector] public static Dictionary<Vector2Int, BubbleData> bubbleDataGrid = new Dictionary<Vector2Int, BubbleData>();
    [HideInInspector] public static List<DroppedBubble> droppedBubbles = new List<DroppedBubble>();

    public float rowDelay = 0.5f;       // Time delay between each row

    private void Awake()
    {
        _selfInstance = this;
    }

    // Load JSON Data
    private LevelData LoadLevelData(string levelFileName)
    {
        // Levels are stored in "Levels" folder in Resources
        TextAsset fileData = Resources.Load<TextAsset>("Levels/" + levelFileName);
        LevelData levelData = JsonUtility.FromJson<LevelData>(fileData.text);
        return levelData;
    }

    public void StartCreateBubbleGrid(GameManager gameManagerRef, string levelFileName)
    {
        gameManager = gameManagerRef;
        StartCoroutine(CreateBubbleGridCoroutine(levelFileName));
    }

    private IEnumerator CreateBubbleGridCoroutine(string levelFileName)
    {
        // Load level data from JSON
        LevelData levelData = LoadLevelData(levelFileName);

        // Use rows and columns from the level data
        activeRows = levelData.activeRows;
        rows = activeRows + 3;
        columns = levelData.columns;
        NextBubble.InitializeShootBubbleQueue(levelData.shootBubblesQueue, levelData.numberOfShootBubbles);

        if (levelData.bubbleColors == null || levelData.bubbleColors.Count != activeRows * columns)
        {
            Debug.LogError("Invalid JSON bubble colors data");
            yield break;
        }

        // Convert flat list into 2D array
        string[][] bubbleColorsArray = new string[activeRows][];
        for (int i = 0; i < activeRows; i++)
        {
            bubbleColorsArray[i] = new string[columns];
            for (int j = 0; j < columns; j++)
            {
                int index = i * columns + j;
                bubbleColorsArray[i][j] = levelData.bubbleColors[index];
            }
        }

        // Loop through all rows and columns to create the grid
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                // Calculate position based on row and column
                float xPos = col * bubbleRadius * (1 + gapSize);

                // Offset every other row by half of the radius to create a staggered effect
                if (row % 2 == 1)
                {
                    xPos += bubbleRadius * (1 + gapSize) / 2;
                }

                // Calculate the y position
                float yPos = row * bubbleRadius * (-1 - gapSize);

                // Create the bubble at the calculated position
                Vector2 bubblePosition = new Vector2(xPos, yPos);

                // Instantiate the bubble and parent it to the Field GameObject
                FieldBubble bubble = Instantiate(bubblePrefab, bubblePosition, Quaternion.identity, transform);

                bubble.transform.localPosition = bubblePosition;

                // Determine the bubble color: If within activeRows, use data from JSON; otherwise, set to "Empty"
                BubbleColor newBubbleColor;
                if (row < activeRows)
                {
                    string colorString = bubbleColorsArray[row][col];
                    if (string.IsNullOrEmpty(colorString))
                    {
                        newBubbleColor = BubbleColor.Empty; // Default to Empty
                    }
                    else if (Enum.TryParse(colorString, out BubbleColor parsedColor))
                    {
                        newBubbleColor = parsedColor;
                    }
                    else
                    {
                        Debug.LogWarning($"Invalid color value: {colorString}");
                        newBubbleColor = BubbleColor.Empty; // Default to Empty
                    }
                }
                else
                {
                    // Set the color for inactive rows to Empty
                    newBubbleColor = BubbleColor.Empty;
                }

                // Create bubble data
                BubbleData newBubbleData = new BubbleData { bubbleReference = bubble, bubbleColor = newBubbleColor };

                Vector2Int bubbleXY = new Vector2Int(row, col);

                // Add the bubble data to the grid
                bubbleDataGrid.Add(bubbleXY, newBubbleData);

                // Initialize the bubble
                bubble.InitializeBubble(bubbleXY, newBubbleData);

            }

            // Only delay for active rows
            if (row < activeRows)
            {
                yield return new WaitForSeconds(rowDelay);
            }
        }

        // Calculate the Y position for the last row
        float lastRowYpos = (rows - 1) * bubbleRadius * (-1 - gapSize);

        // Unmask last row line after all the field bubbles are spawned
        UnmaskLastRowLine(lastRowYpos);

    }

    public static void CheckFreeFloatingBubbles()
    {
        _selfInstance.GridConnectionsCheck();
    }

    void GridConnectionsCheck()
    {
        // Step 1: Create a set to track visited bubbles and reachable bubbles
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        // Step 2: Create a queue for BFS
        Queue<Vector2Int> queue = new Queue<Vector2Int>();

        // Start BFS with non-empty bubbles in row 0
        foreach (var key in bubbleDataGrid.Keys)
        {
            if (key.x == 0 && bubbleDataGrid[key].bubbleColor != BubbleColor.Empty) // Row 0 bubbles are always connected
            {
                queue.Enqueue(key);
                visited.Add(key);
            }
        }

        // Perform BFS
        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            FieldBubble currentBubble = bubbleDataGrid[current].bubbleReference;

            // Get neighbor offsets
            currentBubble.GetNeighborOffsets(current.x, out int[] rowOffsets, out int[] columnOffsets);

            for (int i = 0; i < rowOffsets.Length; i++)
            {
                Vector2Int neighborPos = new Vector2Int(current.x + rowOffsets[i], current.y + columnOffsets[i]);

                // Check if the neighbor position is within the grid boundaries and is not empty
                if (IsPositionWithinGrid(neighborPos) && bubbleDataGrid.ContainsKey(neighborPos))
                {
                    BubbleColor neighborColor = bubbleDataGrid[neighborPos].bubbleColor;

                    // Add the neighbor to the queue if it has a color and has not been visited
                    if (neighborColor != BubbleColor.Empty && !visited.Contains(neighborPos))
                    {
                        visited.Add(neighborPos);
                        queue.Enqueue(neighborPos);
                    }
                }
            }
        }

        // Step 3: Identify and handle free-floating bubbles
        List<Vector2Int> freeFloatingBubbles = new List<Vector2Int>();
        foreach (var key in bubbleDataGrid.Keys)
        {
            if (!visited.Contains(key) && bubbleDataGrid[key].bubbleColor != BubbleColor.Empty)
            {
                freeFloatingBubbles.Add(key);
            }
        }

        _bubbleDropScoreMulti = 1 + (freeFloatingBubbles.Count - 1) * perDroppedBubbleScoreMulti;

        // Handle free-floating bubbles
        foreach (var pos in freeFloatingBubbles)
        {
            DropFreeFloatingBubble(pos);
        }

        // Step 4: Check if there are no free-floating bubbles and call SpawnNewBubble
        if (freeFloatingBubbles.Count == 0)
        {
            gameManager.shootController.SpawnNewBubble();
        }
    }

    // Method to check if a position is within grid boundaries
    private bool IsPositionWithinGrid(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < rows && pos.y >= 0 && pos.y < columns;
    }

    public DroppedBubble droppedBubblePrefab;

    // Drop bubbles which are not connected to any other non-empty bubble (except for row 0)
    private void DropFreeFloatingBubble(Vector2Int pos)
    {
        if (bubbleDataGrid.TryGetValue(pos, out BubbleData bubbleData))
        {

            // Instantiate a DroppedBubble prefab
            DroppedBubble droppedBubble = Instantiate(droppedBubblePrefab, bubbleData.bubbleReference.transform.position, Quaternion.identity, transform);

            droppedBubble.scoreMultiplier = _bubbleDropScoreMulti;

            droppedBubble.bubbleSprite.color = ColorManager.PickColor(bubbleData.bubbleColor);

            // Add to the list of dropped bubbles
            droppedBubbles.Add(droppedBubble);

            // Set the field bubble's color to Empty
            bubbleData.bubbleColor = BubbleColor.Empty;
            bubbleDataGrid[pos] = bubbleData;

            // Update the bubble reference
            bubbleData.bubbleReference.InitializeBubble(pos, bubbleData);

        }
    }

    public static void UpdateLevelScore(int value)
    {
        int updatedValue = GameManager.currentScore + value;

        GameManager.currentScore = updatedValue;

        GameManager.scoreText.text = updatedValue.ToString();

        gameManager.ScoreUpdateAnim();
    }

    void UnmaskLastRowLine(float ySpawnLocation)
    {
        // Instantiate the line and parent it to the Field GameObject
        belowLastRowLineRef = Instantiate(belowLastRowLinePrefab, Vector2.zero, Quaternion.identity, transform);

        // Set the Y position to the spawn location
        belowLastRowLineRef.transform.localPosition = new Vector2(belowLastRowLinePrefab.transform.localPosition.x, ySpawnLocation);

        // Set the mask starting position offset (to the left of its original position)
        float originalXPos = belowLastRowLinePrefab.showMask.transform.localPosition.x;

        belowLastRowLineRef.showMask.transform.localPosition = new Vector2(originalXPos - 9.75f, 0);

        _belowLastRowAnim?.Kill();
        
        _belowLastRowAnim = belowLastRowLineRef.showMask.transform.DOLocalMoveX(originalXPos, 0.4f)
            .OnComplete(() =>
        {
            gameManager.shootController.SpawnNewBubble();
        });
    }

}

public enum BubbleColor
{
    Empty,
    Green,
    Blue,
    Yellow,
    Purple
}

public struct BubbleData
{
    public FieldBubble bubbleReference;
    public BubbleColor bubbleColor;
}