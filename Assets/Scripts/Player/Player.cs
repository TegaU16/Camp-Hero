using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    private float currentSpeed;
    public float CurrentSpeed => currentSpeed;

    [Header("Status")]
    public Health health;
    [HideInInspector] public HealthBar healthBar;
    [HideInInspector] public StaminaBar staminaBar;
    private float buffer = 0f;
    public float bufferCooldown = 5f;
    public float jumpDecrease = 5f;
    public PlayerAttributes playerAttributes;
    public PlayerCombat playerCombat;
    public float CurrentStamina => staminaBar.GetStamina();

    [Header("Body Settings")]
    public CharacterController controller;
    public SimpleRagdollController ragdollController;
    public Transform itemHolder;
    private ProceduralAnimator proceduralAnimator;
    private Animator animator;

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

    private readonly List<ActiveUpgradeEffect> activeUpgrades = new();
    private readonly List<UpgradeEffect> unlockedActiveAbilities = new();

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void Start()
    {
        // Dynamically find and assign the main camera
        cam = Camera.main != null ? Camera.main.transform : null;

        if (cam == null)
            Debug.LogError("Main Camera not found! Ensure there is a Camera tagged as 'MainCamera' in the scene.");

        proceduralAnimator = GetComponent<ProceduralAnimator>();
        if (proceduralAnimator == null)
            proceduralAnimator = gameObject.AddComponent<ProceduralAnimator>();
    }

    // Update is called once per frame
    private void Update()
    {
        if (!GameManager.Instance.IsGameManagerReady()) return;
        if (!uiBound) return;
        
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

        foreach (ActiveUpgradeEffect upgrade in activeUpgrades)
        {
            // Check if key is pressed and the upgrade is ready
            if (Input.GetKeyDown(upgrade.activationKey) && !upgrade.isOnCooldown)
                upgrade.Activate(this);
        }
    }

    private void FixedUpdate()
    {
        if (!GameManager.Instance.IsGameManagerReady()) return;
        if (!uiBound) return;
        if (controller == null || !controller.enabled || cam == null) return;

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
            bool isSprinting = Input.GetKey(sprintKey) && CurrentStamina > 0;

            float moveSpeed = isSprinting ? sprintSpeed : speed;
            float finalSpeed = moveSpeed * playerAttributes.MovementSpeedMultiplier;

            controller.Move(finalSpeed * Time.fixedDeltaTime * moveDir.normalized);

            if (proceduralAnimator != null)
                proceduralAnimator.SetMovementSpeed(finalSpeed);

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
        }

        else
        {
            if (proceduralAnimator != null)
                proceduralAnimator.SetMovementSpeed(0f);

            if (buffer >= bufferCooldown)
                staminaBar.IncreaseStamina();
            else
                buffer += Time.fixedDeltaTime;
        }

        // Jumping
        if (isGrounded && jumpInput && CurrentStamina >= jumpDecrease)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            UseStamina(jumpDecrease);
        }

        // Apply vertical velocity
        controller.Move(velocity * Time.fixedDeltaTime);

        Vector3 horizontalVelocity = controller.velocity;
        horizontalVelocity.y = 0f;
        currentSpeed = horizontalVelocity.magnitude;
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

    private void OnAnimatorMove() 
    { 
        if (animator == null || controller == null || !controller.enabled) return;

        Vector3 rootDelta = animator.deltaPosition;
        rootDelta.y = velocity.y * Time.deltaTime;

        controller.Move(rootDelta);
        transform.rotation *= animator.deltaRotation;
    }

    public void UseStamina(float stamina)
    {
        float targetStamina = CurrentStamina - stamina;
        staminaBar.slider.DOValue(targetStamina, 0.5f);
        staminaBar.SetNewStamina((int)targetStamina);
        buffer = 0f;
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
        if (controller != null)
            controller.enabled = false;

        if (ragdollController != null)
            ragdollController.EnableRagdoll();
        else
            Debug.LogWarning("No SimpleRagdollController found!");

        InventoryManager.Instance.ResetExtensions();
        InventoryManager.Instance.DropAllItems();

        InteractableItemManager.Instance.ForceMergeAll();

        foreach (TrialAltar trialAltar in KeyStructureSpawner.Instance.activeTrialAltars.ToList())
        {
            if (trialAltar != null && trialAltar.IsWaveInProgress())
                trialAltar.FailTrial();
        }

        Invoke(nameof(Despawn), 5f);
    }

    private void Despawn()
    {
        ragdollController.DisableRagdoll();
        SavePlayer();
        GameManager.Instance.RespawnPlayer(this);
    }

    // Called when the player unlocks an active upgrade permanently
    public void UnlockActiveAbility(UpgradeEffect upgrade)
    {
        if (!unlockedActiveAbilities.Contains(upgrade))
            unlockedActiveAbilities.Add(upgrade);

        upgrade.OnUnlocked(this);
    }

    // Called when an ability is currently in use, active, or equipped
    public void RegisterActiveUpgrade(ActiveUpgradeEffect upgrade)
    {
        if (!activeUpgrades.Contains(upgrade))
            activeUpgrades.Add(upgrade);
    }

    // Called when you want to remove or deactivate it (e.g., ability ends or unequipped)
    public void UnregisterActiveUpgrade(ActiveUpgradeEffect upgrade)
    {
        if (activeUpgrades.Contains(upgrade))
            activeUpgrades.Remove(upgrade);
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
        health.healthBar.Initialize(health.maxHealth, currentHealth);

        staminaBar.SetNewStamina(currentStamina);
        staminaBar.Initialize(staminaBar.maxStamina, currentStamina);

        staminaBar.incrementRate = playerAttributes.StaminaRegenRate;

        if (playerCombat != null)
        {
            float meleeMult = playerAttributes.MeleeDamageMultiplier;
            float critMult = playerAttributes.CritChanceMultiplier;

            playerCombat.SetDamageMultiplierSource(this, meleeMult);
            playerCombat.critMultiplier = critMult;
        }
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
                luck = stats.luck.Value,
                availablePoints = stats.availablePoints,
                goldenPoints = stats.goldenPoints,
                unlockedUpgrades = PlayerStatsManager.Instance.GetPlayerUpgrades()
            },
            inventory = InventoryManager.Instance.GetSavedPlayerItems(),
            levelData = levelData
        };

        SaveSystem.SavePlayer(GameManager.Instance.currentWorldName, playerData);
    }
}
