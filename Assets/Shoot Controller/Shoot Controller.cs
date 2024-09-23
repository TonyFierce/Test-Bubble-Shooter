using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ShootController : MonoBehaviour
{
    [HideInInspector] public bool touchAssistShown = false;

    public SpriteRenderer touchAssistCircle;
    private Tween _touchAssistAnim;
    private Vector2 _touchAssistDefaultSize;

    public ShootBubble shootBubblePrefab;

    public SwipeArea shootControlSwipeArea;

    [HideInInspector] public ShootBubble currentShootBubble;
    private Rigidbody2D _currentRigidbody;

    private ShootBubble _previousShootBubble;

    [HideInInspector] public bool readyToShoot = false;
    [HideInInspector] public bool aiming = false;

    public GameObject powerBar;
    public Image powerBarFill;

    private Vector2 _startDragPosition;
    private Vector2 _currentDragPosition;

    private Vector2 _clampedLaunchDirection;

    public float maxLaunchForce = 700f;
    public float minLaunchForceRatio = 0.75f;
    public float shootClampAngle = 60f;

    public LineRenderer trajectoryLineRenderer;
    public float trajectorySimulationDuration = 0.02f;
    public float timeStep = 0.005f;

    public float shootBubbleGravityScale = 2.0f;

    public void SpawnNewBubble()
    {
        if (readyToShoot) return;

        BubbleColor nextShootBubbleColor = BubbleColor.Empty;

        // Get the next bubble color from the queue
        if (NextBubble.shootBubbleQueue.Count > 0)
        {
            Field.gameManager.CheckWinCondition(out bool levelWon);

            if (levelWon) return;

            nextShootBubbleColor = NextBubble.shootBubbleQueue.Dequeue();
            NextBubble.SetNextBubbleColor();
        }
        else
        {
            Destroy(currentShootBubble.gameObject);

            Field.gameManager.CheckWinCondition(out bool levelWon);
            return;
        }

        TouchAssistAnim(true);

        _wallCollisionCount = 0;
        currentShootBubble = Instantiate(shootBubblePrefab, transform.position, Quaternion.identity, transform);
        currentShootBubble.shootBubbleColor = nextShootBubbleColor;
        currentShootBubble.previousBubble = _previousShootBubble;
        currentShootBubble.shootController = this;
        _currentRigidbody = currentShootBubble.GetComponent<Rigidbody2D>();
        _currentLaunchForce = 0;

        Field.gameManager.SwitchPauseButton(true);
        shootControlSwipeArea.enabled = true;
    }

    private float _currentLaunchForce = 0;

    public void LaunchBubble()
    {
        if (readyToShoot && aiming)
        {
            // If launch power is 0, cancel aiming
            if (powerBarFill.fillAmount == 0)
            {
                SwitchAimMode(false);
                Field.gameManager.SwitchPauseButton(true);
                shootControlSwipeArea.enabled = true;
                return;
            }

            // Apply the clamped launch direction as the force
            Vector2 launchDirection = _clampedLaunchDirection;

            // If at max power, apply a random angle offset within ±15 degrees
            if (powerBarFill.fillAmount == 1)
            {
                float randomAngleOffset = Random.Range(-15f, 15f); // Generate random angle between -15 and 15 degrees
                Quaternion rotation = Quaternion.Euler(0, 0, randomAngleOffset); // Create rotation quaternion
                launchDirection = rotation * launchDirection; // Apply the rotation to the launch direction
            }

            currentShootBubble.SetMaxPower(powerBarFill.fillAmount == 1);

            // Apply the adjusted or clamped launch direction as the force
            _currentRigidbody.AddForce(launchDirection * _currentLaunchForce);

            _currentRigidbody.gravityScale = shootBubbleGravityScale;

            readyToShoot = false;

            SwitchAimMode(false);

            // Disable pause button while the bubble is flying
            Field.gameManager.SwitchPauseButton(false);

            _previousShootBubble = currentShootBubble;

            shootControlSwipeArea.enabled = false;
        }
    }

    void SwitchAimMode(bool enable)
    {
        aiming = enable;
        powerBar.SetActive(enable);
        trajectoryLineRenderer.enabled = enable;  // Show or hide the trajectory

        if (!enable)
        {
            leftSpreadTrajectory.enabled = false;
            rightSpreadTrajectory.enabled = false;

            if (!readyToShoot) return;

            TouchAssistAnim(true);
        }
    }

    // Captures cursor position
    public void StartAiming(PointerEventData eventData, out bool startedAiming)
    {
        if (readyToShoot && currentShootBubble != null)
        {
            powerBarFill.fillAmount = 0;

            SwitchAimMode(true);
            _startDragPosition = eventData.position;

            startedAiming = true;

            TouchAssistAnim(false);
        }
        else
        {
            startedAiming = false;
        }
    }

    public float maxDragNegativeDistance = -85f;

    // Event triggered each frame during dragging
    public void OnDrag(Vector2 screenPosition)
    {
        // Calculate the distance from the start drag position
        float distance = screenPosition.y - _startDragPosition.y;

        // Calculate the fill amount based on the distance
        float fillAmount = Mathf.InverseLerp(0, maxDragNegativeDistance, distance);

        // Fill amount is between 0 and 1
        fillAmount = Mathf.Clamp01(fillAmount);

        // Set the power bar fill amount
        powerBarFill.fillAmount = fillAmount;

        // Update the current drag position
        _currentDragPosition = screenPosition;

        // Calculate the launch direction
        CalculateLaunchDirection();

        // Calculate the launch force based on power bar fill amount
        _currentLaunchForce = Mathf.Lerp(maxLaunchForce * minLaunchForceRatio, maxLaunchForce, powerBarFill.fillAmount);

        // Only predict trajectory if power bar fill is greater than 0
        if (powerBarFill.fillAmount > 0)
        {
            SimulateArc();

        }
        else
        {
            // Disable the trajectory line if launch power is 0
            trajectoryLineRenderer.positionCount = 0;

        }
    }

    // Calculate the clamped launch direction
    private void CalculateLaunchDirection()
    {
        // Convert the screen position to world position
        Vector2 dragWorldPosition = Camera.main.ScreenToWorldPoint(_currentDragPosition);

        // Apply an offset to move the calculated mouse position slightly downwards
        Vector2 offset = new Vector2(0, -0.5f); // Adjust the offset value as needed
        dragWorldPosition += offset;

        Vector2 shootControllerPosition = _currentRigidbody.transform.position;

        // Calculate the direction from the drag position to Shoot Controller
        Vector2 direction = (shootControllerPosition - dragWorldPosition).normalized;

        // Calculate the angle between the direction and the upward vector
        float angle = Vector2.Angle(Vector2.up, direction);

        // Clamp the angle to a maximum of X degrees left or right
        if (angle > shootClampAngle)
        {
            // Get the cross product to determine the sign of the angle
            float sign = Mathf.Sign(Vector3.Cross(Vector2.up, direction).z);

            float clampedAngle = shootClampAngle * sign;

            Quaternion rotation = Quaternion.AngleAxis(clampedAngle, Vector3.forward);
            _clampedLaunchDirection = rotation * Vector2.up;
        }
        else
        {
            // If the angle is within the allowed range, use the original direction
            _clampedLaunchDirection = direction;
        }
    }

    public float trajectoryStartGap = 0.4f;

    // Artificially multiply the gravity for accurate trajectory
    public float gravityAccuracyMulti = 1.2f;
    public float gravityMultiRicochetDecrement = 0.025f;

    public LineRenderer leftSpreadTrajectory;
    public LineRenderer rightSpreadTrajectory;

    // Method for 3 arcs (main, left, right spread)
    private void SimulateArc()
    {
        int steps = (int)(trajectorySimulationDuration / timeStep);
        List<Vector3> lineRendererPoints = new List<Vector3>();

        Vector2 directionVector = _clampedLaunchDirection;
        Vector2 launchPosition = _currentRigidbody.transform.position;

        float launchSpeed = _currentLaunchForce / _currentRigidbody.mass * Time.fixedDeltaTime;
        Vector2 offsetLaunchPosition = launchPosition + (directionVector.normalized * trajectoryStartGap);

        int currentRicochets = 0;

        // Simulate main trajectory
        SimulateSingleArc(trajectoryLineRenderer, directionVector, offsetLaunchPosition, launchSpeed, currentRicochets, steps);

        // Check if power bar is full (max power)
        if (powerBarFill.fillAmount == 1)
        {
            leftSpreadTrajectory.enabled = true;
            rightSpreadTrajectory.enabled = true;

            // Calculate spread directions
            Vector2 leftSpreadDirection = Quaternion.Euler(0, 0, 15f) * directionVector;
            Vector2 rightSpreadDirection = Quaternion.Euler(0, 0, -15f) * directionVector;

            // Simulate spread trajectories
            SimulateSingleArc(leftSpreadTrajectory, leftSpreadDirection, offsetLaunchPosition, launchSpeed, currentRicochets, steps);
            SimulateSingleArc(rightSpreadTrajectory, rightSpreadDirection, offsetLaunchPosition, launchSpeed, currentRicochets, steps);
        }
        else
        {
            leftSpreadTrajectory.positionCount = 0;
            rightSpreadTrajectory.positionCount = 0;
            leftSpreadTrajectory.enabled = false;
            rightSpreadTrajectory.enabled = false;
        }
    }

    // Simulates a single arc trajectory
    private void SimulateSingleArc(LineRenderer lineRenderer, Vector2 directionVector, Vector2 offsetLaunchPosition,
                                   float launchSpeed, int currentRicochets, int steps)
    {
        List<Vector3> lineRendererPoints = new List<Vector3>();

        for (int i = 0; i < steps; ++i)
        {
            float time = i * timeStep;

            // Calculate position based on time
            Vector2 calculatedPosition = offsetLaunchPosition + (directionVector * (launchSpeed * time));
            calculatedPosition.y += 0.5f * Physics2D.gravity.y * shootBubbleGravityScale *
                (gravityAccuracyMulti - gravityMultiRicochetDecrement * currentRicochets) * time * time;

            if (i > 1)
            {
                // Check for collision
                if (CheckCollision(calculatedPosition, out bool isBubble, out bool isWall, out bool isFloor, out bool isCeiling))
                {
                    // Stop trajectory if a bubble, floor, or ceiling is hit
                    if (isBubble || isFloor || isCeiling)
                    {
                        lineRendererPoints.Add(new Vector3(calculatedPosition.x, calculatedPosition.y, 0f));
                        break;
                    }
                    // Reflect direction if a wall is hit and it's the first bounce
                    else if (isWall && currentRicochets < maxRicochets)
                    {
                        currentRicochets++;

                        launchSpeed = launchSpeed * ricochetSpeedReduction;

                        // Add collision point and reflect direction
                        lineRendererPoints.Add(new Vector3(calculatedPosition.x, calculatedPosition.y, 0f));
                        directionVector = new Vector2(-directionVector.x, directionVector.y);

                        offsetLaunchPosition = calculatedPosition;
                        i = 0;

                        continue;
                    }

                    break;
                }
            }

            lineRendererPoints.Add(new Vector3(calculatedPosition.x, calculatedPosition.y, 0f));
        }

        lineRenderer.positionCount = lineRendererPoints.Count;
        lineRenderer.SetPositions(lineRendererPoints.ToArray());
    }

    private bool CheckCollision(Vector2 position, out bool isBubble, out bool isWall, out bool isFloor, out bool isCeiling)
    {
        // Adjust the radius of the collider for the trajectory check
        float colliderRadius = 0.5f * shootBubblePrefab.transform.localScale.x;

        Collider2D hit = Physics2D.OverlapCircle(position, colliderRadius);

        isBubble = false;
        isWall = false;
        isFloor = false;
        isCeiling = false;

        if (hit != null)
        {
            if (hit.CompareTag("Bubble"))
            {
                isBubble = true; // Mark that a bubble was hit
                return true;
            }
            else if (hit.CompareTag("Wall"))
            {
                isWall = true; // Mark that a wall was hit
                return true;
            }
            else if (hit.CompareTag("Floor"))
            {
                isFloor = true;
                return true;
            }
            else if (hit.CompareTag("Ceiling"))
            {
                isCeiling = true;
                return true;
            }

        }

        return false;
    }

    public float ricochetSpeedReduction = 0.75f;
    public int maxRicochets = 1;
    private int _wallCollisionCount = 0;

    public void RicochetBubble(int ricochetDirectionMulti)
    {
        _wallCollisionCount++;

        if (_wallCollisionCount >= maxRicochets + 1)
        {
            // Reset velocity to zero on the second wall collision
            _currentRigidbody.velocity = Vector2.zero;
        }
        else
        {
            // Reflect direction and apply force
            Vector2 reflectedDirection = new Vector2(_clampedLaunchDirection.x * ricochetDirectionMulti, _clampedLaunchDirection.y);
            float ricochetForce = _currentLaunchForce * Mathf.Pow(ricochetSpeedReduction, _wallCollisionCount) / _currentRigidbody.mass;

            _currentRigidbody.velocity = Vector2.zero; // Reset current velocity
            _currentRigidbody.AddForce(reflectedDirection * ricochetForce);
        }
    }

    public void TouchAssistAnim(bool show)
    {
        _touchAssistDefaultSize = new Vector2(2.56f, 2.56f);

        _touchAssistAnim?.Kill();

        // Add extra bool check to show touch assist only once
        if (show)  // && !touchAssistShown
        {
            touchAssistShown = true;

            touchAssistCircle.size = _touchAssistDefaultSize * 0.7f;

            touchAssistCircle.enabled = true;

            // Create a loop for the circle ping-pong shrink effect
            _touchAssistAnim = DOTween.To(() => touchAssistCircle.size, x => touchAssistCircle.size = x, _touchAssistDefaultSize * 1, 0.8f)
                .SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
        }
        else
        {
            touchAssistCircle.enabled = false;
        }
    }

}
