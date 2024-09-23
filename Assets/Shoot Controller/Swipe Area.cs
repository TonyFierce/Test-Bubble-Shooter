using UnityEngine;
using UnityEngine.EventSystems;

public class SwipeArea : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public ShootController shootController;
    private bool _isTrackingPointer = false;

    // Called when the pointer is pressed down
    public void OnPointerDown(PointerEventData eventData)
    {
        // Handle pointer down logic
        shootController.StartAiming(eventData, out bool startedAiming);

        if (startedAiming) 
        {
            _isTrackingPointer = true;
        }
    }

    // Called when the pointer is released
    public void OnPointerUp(PointerEventData eventData)
    {
        // Handle pointer up logic
        shootController.LaunchBubble();
        _isTrackingPointer = false;
    }

    void Update()
    {
        if (_isTrackingPointer && shootController.readyToShoot)
        {
            // Handle touch input
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0); // Get the first touch

                // Get the screen position of the touch
                Vector2 screenPosition = touch.position;

                // Notify the ShootController about the pointer position update
                shootController.OnDrag(screenPosition);
            }
            else
            {
                // Handle mouse input as a fallback (optional)
                Vector2 screenPosition = Input.mousePosition;

                // Notify the ShootController about the pointer position update
                shootController.OnDrag(screenPosition);
            }
        }
    }
}
