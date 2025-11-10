using System;
using UnityEngine;

namespace ZombieGame.Input
{
    public class InputManager : MonoBehaviour
    {
        public static InputManager Instance { get; private set; }

        public event Action OnJumpPressed;
        public event Action OnSprintPressed;
        public event Action OnSprintReleased;
        public event Action OnCrouchPressed;
        public event Action OnInteractPressed;
        public event Action OnReloadPressed;
        public event Action OnFirePressed;
        public event Action OnFireReleased;
        public event Action OnAimPressed;
        public event Action OnAimReleased;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Update()
        {
            // Movement
            if (UnityEngine.Input.GetButtonDown("Jump")) OnJumpPressed?.Invoke();
            
            if (UnityEngine.Input.GetKeyDown(KeyCode.LeftShift)) OnSprintPressed?.Invoke();
            if (UnityEngine.Input.GetKeyUp(KeyCode.LeftShift)) OnSprintReleased?.Invoke();
            
            if (UnityEngine.Input.GetKeyDown(KeyCode.LeftControl)) OnCrouchPressed?.Invoke();
            
            // Interaction
            if (UnityEngine.Input.GetKeyDown(KeyCode.E)) OnInteractPressed?.Invoke();
            if (UnityEngine.Input.GetKeyDown(KeyCode.R)) OnReloadPressed?.Invoke();
            
            // Combat
            if (UnityEngine.Input.GetMouseButtonDown(0)) OnFirePressed?.Invoke();
            if (UnityEngine.Input.GetMouseButtonUp(0)) OnFireReleased?.Invoke();
            if (UnityEngine.Input.GetMouseButtonDown(1)) OnAimPressed?.Invoke();
            if (UnityEngine.Input.GetMouseButtonUp(1)) OnAimReleased?.Invoke();
        }

        public Vector2 GetMovementInput()
        {
            return new Vector2(
                UnityEngine.Input.GetAxisRaw("Horizontal"),
                UnityEngine.Input.GetAxisRaw("Vertical")
            );
        }

        public Vector2 GetMouseInput()
        {
            return new Vector2(
                UnityEngine.Input.GetAxis("Mouse X"),
                UnityEngine.Input.GetAxis("Mouse Y")
            );
        }
    }
}
