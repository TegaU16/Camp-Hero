using DG.Tweening;
using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 6f;
    public float sprintSpeed = 10f;
    public float gravity = -9.8f; // Gravity force
    public float jumpHeight = 3f; // Jump height
    private Vector3 velocity; // To store current velocity

    [Header("Status")]
    public Health health;
    [HideInInspector] public HealthBar healthBar;
    private StaminaBar staminaBar;
    private float buffer = 0f;
    public float bufferCooldown = 5f;
    public float jumpDecrease = 10f;
    public PlayerAttributes playerAttributes;
    private PlayerCombat playerCombat;

    [Header("Body Settings")]
    public CharacterController controller;
    private Animator animator;
    private SimpleRagdollController ragdollController;
    public Transform itemHolder;

    [Header("Camera Settings")]
    private Transform cam;
    public float turnSmoothTime = 0.1f;
    float turnSmoothVelocity;
    public Transform cameraTarget;
    public float ySmoothSpeed = 5f;
    public Vector3 offset = new(0, 1.5f, 0);

    [Header("Keys")]
    public KeyCode sprintKey = KeyCode.LeftShift;

    private void Start()
    {
        Debug.Log("Player Start() ran!");

        // Dynamically find and assign the main camera
        cam = Camera.main != null ? Camera.main.transform : null;

        if (cam == null)
        {
            Debug.LogError("Main Camera not found! Ensure there is a Camera tagged as 'MainCamera' in the scene.");
        }

        playerCombat = GetComponent<PlayerCombat>();

        healthBar = FindAnyObjectByType<HealthBar>();
        staminaBar = FindAnyObjectByType<StaminaBar>();

        if (controller == null) controller = GetComponent<CharacterController>();
        if (animator == null) animator = GetComponent<Animator>();
        if (ragdollController == null) ragdollController = GetComponent<SimpleRagdollController>();
        if (playerAttributes == null) playerAttributes = GetComponent<PlayerAttributes>();

        health.ResetHealth(playerAttributes.MaxHealth);
        healthBar.SetMaxHealth(playerAttributes.MaxHealth);
        staminaBar.SetMaxStamina(playerAttributes.MaxStamina);

        UpdateVitals();
    }

    // Update is called once per frame
    void Update()
    {
        if (GameManager.Instance.isPaused) return;

        if (controller == null || controller.enabled == false) return;

        if (cam == null) return;
        
        if (animator == null ||  animator.enabled == false) return;

        float currentStamina = staminaBar.GetStamina();

        // Check if the player is grounded using CharacterController's built-in isGrounded property
        bool isGrounded = controller.isGrounded;

        // Movement input
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;

        // Reset downward velocity if grounded
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Small value to keep the player on the ground
        }

        if (!InventoryManager.Instance.IsExtensionOpen())
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            // Move the player
            if (direction.magnitude >= 0.1f)
            {
                float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + cam.eulerAngles.y;
                float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
                transform.rotation = Quaternion.Euler(0f, angle, 0f);

                Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;

                if (Input.GetKey(sprintKey) && currentStamina > 0)
                {
                    controller.Move(sprintSpeed * Time.deltaTime * moveDir.normalized);

                    staminaBar.DecreaseStamina();
                    buffer = 0f; // Reset the timer when sprinting
                } 
                else
                {
                    controller.Move(speed * Time.deltaTime * moveDir.normalized);

                    if (buffer >= bufferCooldown && !Input.GetKey(sprintKey))
                    {
                        staminaBar.IncreaseStamina();
                    }
                    else
                    {
                        buffer += Time.deltaTime;
                    }
                }
            }

            if (isGrounded)
            {
                bool isSprinting = Input.GetKey(sprintKey) && currentStamina > 0;
                float moveSpeed = direction.magnitude * (isSprinting ? sprintSpeed : speed);
                animator.SetFloat("Speed", moveSpeed);

                if (Input.GetButtonDown("Jump") && currentStamina > 0)
                {
                    velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

                    // Tween the stamina bar decrease
                    float targetStamina = currentStamina - jumpDecrease;
                    staminaBar.slider.DOValue(targetStamina, 0.5f); // 0.5f is the duration of the tween
                    staminaBar.SetCurrentStamina((int)targetStamina);

                    buffer = 0f; // Reset the timer after jumping
                }
                else
                {
                    if (buffer >= bufferCooldown)
                    {
                        staminaBar.IncreaseStamina();
                    }
                    else
                    {
                        buffer += Time.deltaTime;
                    }
                }
            }
            else
            {
                animator.SetFloat("Speed", 0f);
            }
        } 
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // Apply gravity (continuously when not grounded)
        if (!isGrounded)
        {
            velocity.y += gravity * Time.deltaTime; // Apply gravity while in air
        }

        // Apply final movement with gravity
        controller.Move(velocity * Time.deltaTime);
    }

    void LateUpdate()
    {
        Vector3 targetPos = transform.position + offset;

        // Smooth only Y axis if you want to avoid height jitter/zoom
        Vector3 smoothed = new(
            targetPos.x,
            Mathf.Lerp(cameraTarget.position.y, targetPos.y, Time.deltaTime * ySmoothSpeed),
            targetPos.z
        );

        cameraTarget.position = smoothed;
    }

    public void Die()
    {
        animator.enabled = false;

        if (controller != null)
            controller.enabled = false;

        if (ragdollController != null)
        {
            ragdollController.EnableRagdoll();
        }
        else
        {
            Debug.LogWarning("No SimpleRagdollController found!");
        }

        InventoryManager.Instance.DropAllItems();
        Invoke(nameof(Despawn), 5f);
    }

    private void Despawn()
    {
        ragdollController.DisableRagdoll();
        animator.enabled = true;
        GameManager.Instance.RespawnPlayer(this);
    }

    public void UpdateVitals()
    {
        int oldMaxHealth = health.maxHealth;
        int oldMaxStamina = (int)staminaBar.maxStamina;

        health.maxHealth = playerAttributes.MaxHealth;
        staminaBar.maxStamina = playerAttributes.MaxStamina;

        // Optionally heal or refill stamina proportionally
        int currentHealth = health.GetHealth();
        currentHealth += health.maxHealth - oldMaxHealth;

        int currentStamina = (int)staminaBar.GetStamina();
        currentStamina += (int)staminaBar.maxStamina - oldMaxStamina;

        // Clamp so it never exceeds new max
        currentHealth = Mathf.Clamp(currentHealth, 0, health.maxHealth);
        currentStamina = Mathf.Clamp(currentStamina, 0, (int)staminaBar.maxStamina);

        health.SetHealth(currentHealth, healthBar);
        staminaBar.SetNewStamina(currentStamina);

        staminaBar.incrementRate = playerAttributes.StaminaRegenRate;

        if (playerCombat != null)
        {
            playerCombat.damageMultiplier = playerAttributes.MeleeDamageMultiplier;
            playerCombat.critMultiplier = playerAttributes.CritChanceMultiplier;
        }
    }
}
