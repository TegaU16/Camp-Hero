using DG.Tweening;
using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 6f;
    public float sprintSpeed = 10f;
    public float gravity = -9.8f;
    public float jumpHeight = 3f;
    private Vector3 velocity;

    private float cachedSpeed;
    private float cachedSprintSpeed;

    [Header("Status")]
    public Health health;
    [HideInInspector] public HealthBar healthBar;
    [HideInInspector] public StaminaBar staminaBar;
    private float buffer = 0f;
    public float bufferCooldown = 5f;
    public float jumpDecrease = 5f;
    public PlayerAttributes playerAttributes;
    public PlayerCombat playerCombat;

    [Header("Body Settings")]
    public CharacterController controller;
    public Animator animator;
    public SimpleRagdollController ragdollController;
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

    private float horizontalInput;
    private float verticalInput;
    private bool jumpInput;

    private bool uiBound = false;

    private void Start()
    {
        // Dynamically find and assign the main camera
        cam = Camera.main != null ? Camera.main.transform : null;

        if (cam == null)
        {
            Debug.LogError("Main Camera not found! Ensure there is a Camera tagged as 'MainCamera' in the scene.");
        }

        cachedSpeed = speed;
        cachedSprintSpeed = sprintSpeed;
    }

    // Update is called once per frame
    private void Update()
    {
        if (!uiBound) return;
        if (GameManager.Instance.isPaused) return;

        // Capture input every frame for responsiveness
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");
        jumpInput = Input.GetButtonDown("Jump");

        // Cursor lock for inventory checks
        if (!InventoryManager.Instance.IsExtensionOpen())
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void FixedUpdate()
    {
        if (!uiBound) return;
        if (GameManager.Instance.isPaused) return;
        if (controller == null || !controller.enabled || cam == null || animator == null || !animator.enabled) return;

        float currentStamina = staminaBar.GetStamina();
        bool isGrounded = controller.isGrounded;
        Vector3 direction = new Vector3(horizontalInput, 0f, verticalInput).normalized;

        // Gravity
        if (isGrounded && velocity.y < 0)
            velocity.y = -1f;
        else
            velocity.y += gravity * Time.fixedDeltaTime;

        // Movement
        if (direction.magnitude >= 0.1f)
        {
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + cam.eulerAngles.y;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);

            Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            bool isSprinting = Input.GetKey(sprintKey) && currentStamina > 0;

            float moveSpeed = isSprinting ? sprintSpeed : speed;
            controller.Move(moveSpeed * Time.fixedDeltaTime * moveDir.normalized);

            // Stamina handling
            if (isSprinting)
            {
                staminaBar.DecreaseStamina();
                buffer = 0f;
            }
            else if (buffer >= bufferCooldown)
            {
                staminaBar.IncreaseStamina();
            }
            else
            {
                buffer += Time.fixedDeltaTime;
            }

            animator.SetFloat("Speed", direction.magnitude * moveSpeed);
        }
        else
        {
            animator.SetFloat("Speed", 0f);
            if (buffer >= bufferCooldown)
                staminaBar.IncreaseStamina();
            else
                buffer += Time.fixedDeltaTime;
        }

        // Jumping
        if (isGrounded && jumpInput && currentStamina >= jumpDecrease)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            float targetStamina = currentStamina - jumpDecrease;
            staminaBar.slider.DOValue(targetStamina, 0.5f);
            staminaBar.SetCurrentStamina((int)targetStamina);
            buffer = 0f;
        }

        // Apply vertical velocity
        controller.Move(velocity * Time.fixedDeltaTime);
    }

    void LateUpdate()
    {
        Vector3 targetPos = transform.position + offset;

        Vector3 smoothed = new(
            targetPos.x,
            Mathf.Lerp(cameraTarget.position.y, targetPos.y, Time.deltaTime * ySmoothSpeed),
            targetPos.z
        );

        cameraTarget.position = smoothed;
    }

    public void BindUI(HealthBar hb, StaminaBar sb)
    {
        healthBar = hb;
        staminaBar = sb;

        if (healthBar != null)
        {
            healthBar.SetMaxHealth(playerAttributes.MaxHealth);
            health.healthBar = healthBar;
            health.ResetHealth(playerAttributes.MaxHealth);
        }

        if (staminaBar != null)
        {
            staminaBar.SetMaxStamina(playerAttributes.MaxStamina);
            staminaBar.SetNewStamina(playerAttributes.MaxStamina);
        }

        uiBound = true;
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
        SavePlayer();
        GameManager.Instance.RespawnPlayer(this);
    }

    public void UpdateVitals()
    {
        int oldMaxHealth = health.maxHealth;
        int oldMaxStamina = (int)staminaBar.maxStamina;

        health.maxHealth = playerAttributes.MaxHealth;
        staminaBar.maxStamina = playerAttributes.MaxStamina;

        int currentHealth = health.GetHealth();
        currentHealth += health.maxHealth - oldMaxHealth;

        int currentStamina = (int)staminaBar.GetStamina();
        currentStamina += (int)staminaBar.maxStamina - oldMaxStamina;

        currentHealth = Mathf.Clamp(currentHealth, 0, health.maxHealth);
        currentStamina = Mathf.Clamp(currentStamina, 0, (int)staminaBar.maxStamina);

        health.SetHealth(currentHealth);
        staminaBar.SetNewStamina(currentStamina);

        staminaBar.incrementRate = playerAttributes.StaminaRegenRate;

        if (playerCombat != null)
        {
            float meleeMult = playerAttributes.MeleeDamageMultiplier;
            float critMult = playerAttributes.CritChanceMultiplier;

            playerCombat.damageMultiplier = meleeMult;
            playerCombat.critMultiplier = critMult;
        }
    }

    public void FreezeMovement()
    {
        speed = 0;
        sprintSpeed = 0;
    }

    public void ResetMovement()
    {
        speed = cachedSpeed;
        sprintSpeed = cachedSprintSpeed;
    }

    public void SavePlayer()
    {
        PlayerStats stats = PlayerStatsManager.Instance.stats;
        LevelData levelData = LevelManager.Instance.GetLevelData();

        PlayerSaveData playerData = new()
        {
            position = transform.position,
            maxHealth = health.maxHealth,
            currentHealth = health.GetHealth(),
            maxStamina = staminaBar.maxStamina,
            currentStamina = staminaBar.GetStamina(),
            attributes = new PlayerAttributesData
            {
                strength = stats.strength.Value,
                vitality = stats.vitality.Value,
                endurance = stats.endurance.Value,
                stamina = stats.stamina.Value,
                luck = stats.luck.Value
            },
            inventory = InventoryManager.Instance.GetSavedPlayerItems(),
            availablePoints = PlayerStatsManager.Instance.stats.availablePoints,
            levelData = levelData
        };

        SaveSystem.SavePlayer(GameManager.Instance.currentWorldName, playerData);
    }
}
