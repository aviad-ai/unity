using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.LowLevel;


namespace Aviad.Samples
{
    public class FirstPersonInputModule : StandaloneInputModule
    {
        protected override MouseState GetMousePointerEventData(int id)
        {
            var lockState = Cursor.lockState;
            Cursor.lockState = CursorLockMode.None;
            var mouseState = base.GetMousePointerEventData(id);
            Cursor.lockState = lockState;
            return mouseState;
        }

        protected override void ProcessMove(PointerEventData pointerEvent)
        {
            var lockState = Cursor.lockState;
            Cursor.lockState = CursorLockMode.None;
            base.ProcessMove(pointerEvent);
            Cursor.lockState = lockState;
        }

        protected override void ProcessDrag(PointerEventData pointerEvent)
        {
            var lockState = Cursor.lockState;
            Cursor.lockState = CursorLockMode.None;
            base.ProcessDrag(pointerEvent);
            Cursor.lockState = lockState;
        }
    }
}