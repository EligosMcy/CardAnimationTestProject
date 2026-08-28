using UnityEngine;
using UnityEngine.InputSystem;

namespace Test
{
    public class CardOnDragTest : MonoBehaviour
    { 
        [SerializeField] private InputActionProperty _inputActionProperty;

        void Start()
        {
            _inputActionProperty.action.Enable();
            _inputActionProperty.action.performed += Action_performed;
        }

        private void Action_performed(InputAction.CallbackContext obj)
        {
            Debug.Log("Test Input");
        }

        private void OnMouseDrag()
        {
            Debug.Log("Card OnDrag Test : Process");
        }
    }
}
