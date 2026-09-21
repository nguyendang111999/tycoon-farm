using UnityEngine;
using UnityEngine.EventSystems;

namespace Farm.Core
{
    /// <summary>
    /// Touch-aware primary-pointer queries for the legacy Input Manager, unifying mouse and single-finger touch.
    /// </summary>
    public static class PointerInput
    {
        /// <summary>True when the primary pointer is over a uGUI element.</summary>
        /// <remarks>
        /// The parameterless <c>IsPointerOverGameObject()</c> probes the mouse pointer id (-1), which never exists
        /// on touch devices, so it always returns false on mobile. The touch fingerId overload must be used instead.
        /// </remarks>
        public static bool IsOverUI()
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null) return false;

            if (Input.touchCount > 0)
            {
                return eventSystem.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
            }

            return eventSystem.IsPointerOverGameObject();
        }

        /// <summary>True on the frame the primary pointer is pressed.</summary>
        public static bool TryGetDown(out Vector3 screenPosition)
        {
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began)
                {
                    screenPosition = touch.position;
                    return true;
                }
            }
            else if (Input.GetMouseButtonDown(0))
            {
                screenPosition = Input.mousePosition;
                return true;
            }

            screenPosition = Vector3.zero;
            return false;
        }

        /// <summary>True while the primary pointer is held down.</summary>
        public static bool TryGetPosition(out Vector3 screenPosition)
        {
            if (Input.touchCount > 0)
            {
                screenPosition = Input.GetTouch(0).position;
                return true;
            }

            if (Input.GetMouseButton(0))
            {
                screenPosition = Input.mousePosition;
                return true;
            }

            screenPosition = Vector3.zero;
            return false;
        }

        /// <summary>True on the frame the primary pointer is released.</summary>
        public static bool TryGetUp()
        {
            if (Input.touchCount > 0)
            {
                TouchPhase phase = Input.GetTouch(0).phase;
                return phase == TouchPhase.Ended || phase == TouchPhase.Canceled;
            }

            return Input.GetMouseButtonUp(0);
        }
    }
}
