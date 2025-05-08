using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float speed = 6f;
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float jumpHeight = 1.5f;

    [Header("Mouse Look")]
    [SerializeField] private float mouseSensitivity = 50;

    Transform cameraTransform;
    private Animator hammerAnimator;
    private CharacterController controller;
    private Vector3 velocity;
    private float xRotation = 0f;

    private bool hasInitialized = false;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        cameraTransform = transform.GetChild(0);
        hammerAnimator = cameraTransform.GetChild(0).GetComponent<Animator>();

        // Lock the cursor and hide it
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Initialize player rotation to face forward direction
        transform.rotation = Quaternion.Euler(0f, 0f, 0f); 
    }

    void Update()
    {
        
        // Only allow mouse input after initialization
        if (hasInitialized) HandleMouseLook();
        else if (!hasInitialized && Input.GetMouseButtonDown(0)) hasInitialized = true;

        HandleJump();
        HandleMouseClick();
        HandleMovement();
        HandleExit();
    }

    void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    void HandleMovement()
    {
        if (controller.isGrounded && velocity.y < 0) velocity.y = -2f;

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 move = transform.right * x + transform.forward * z;
        controller.Move(move * speed * Time.deltaTime);
    }

    void HandleJump()
    {
        if (Input.GetButtonDown("Jump")) velocity.y = Mathf.Sqrt(jumpHeight * -gravity);

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    /// <summary>
    /// Play the swing animation when clicking
    /// </summary>
    void HandleMouseClick()
    {
        if (Input.GetMouseButton(0)) hammerAnimator.SetBool("bSwingHammer", true);
        else hammerAnimator.SetBool("bSwingHammer", false);
    }
    
    void HandleExit()
    {
        if(Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit(); // Quit if using executable build

            // Allow user to exit window if WebGL build
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }
}