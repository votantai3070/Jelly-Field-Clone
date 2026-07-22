using UnityEngine;

public static class InputUtility
{
    public static bool TryGetTouchBegan(out Touch touch)
    {
        if (Input.touchCount > 0)
        {
            touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                return true;
            }
        }

        touch = default;
        return false;
    }

    public static bool TryGetTouchMoved(out Touch touch)
    {
        if (Input.touchCount > 0)
        {
            touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
            {
                return true;
            }
        }

        touch = default;
        return false;
    }

    public static bool TryGetTouchEnded(out Touch touch)
    {
        if (Input.touchCount > 0)
        {
            touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                return true;
            }
        }

        touch = default;
        return false;
    }
}