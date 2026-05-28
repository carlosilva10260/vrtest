using UnityEngine;
using UnityEngine.InputSystem;

public class PauseMenuToggle : MonoBehaviour
{
    [Header("References")]
    public GameObject menuCanvas;
    public Transform head;

    [Header("Input")]
    public InputActionReference pauseInput;

    [Header("Placement")]
    public float distanceFromUser = 2.0f;
    public float heightOffset = 0.0f;

    private bool isOpen = false;

    private void Start()
    {
        if (menuCanvas != null)
            menuCanvas.SetActive(false);
    }

    private void OnEnable()
    {
        if (pauseInput != null)
        {
            pauseInput.action.Enable();
            pauseInput.action.performed += OnPausePressed;
        }
    }

    private void OnDisable()
    {
        if (pauseInput != null)
        {
            pauseInput.action.performed -= OnPausePressed;
        }
    }

    private void OnPausePressed(InputAction.CallbackContext context)
    {
        ToggleMenu();
    }

    private void ToggleMenu()
    {
        isOpen = !isOpen;

        if (menuCanvas == null || head == null)
            return;

        menuCanvas.SetActive(isOpen);

        if (isOpen)
            PlaceMenuInFrontOfUser();
    }

    private void PlaceMenuInFrontOfUser()
    {
        Vector3 forward = head.forward;
        forward.y = 0f;
        forward.Normalize();

        menuCanvas.transform.position =
            head.position + forward * distanceFromUser + Vector3.up * heightOffset;

        menuCanvas.transform.rotation =
            Quaternion.LookRotation(forward, Vector3.up);
    }
}