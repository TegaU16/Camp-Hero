using System.Collections.Generic;
using DG.Tweening;
using Game.Inventory;
using Game.Level;
using Game.Saving;
using Game.Upgrades;
using UnityEngine;
using Worlds;

namespace Game.Players
{
    [RequireComponent(typeof(Animator), typeof(PlayerCombat), typeof(CharacterController))]
    [RequireComponent(typeof(PlayerAttributes))]
    public class Player : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float speed = 6f;
        [SerializeField] private float sprintSpeed = 10f;
        [SerializeField] private float gravity = -9.8f;
        [SerializeField] private float jumpHeight = 3f;

        private Vector3 velocity;
        private Vector3 lastPosition;
        private float currentSpeed;

        public float CurrentSpeed => currentSpeed;

        [Header("Status")]
        [HideInInspector] public HealthBar healthBar;
        [HideInInspector] public StaminaBar staminaBar;
        public Health health;
        
        [SerializeField] private float bufferCooldown = 5f;
        [SerializeField] private float jumpDecrease = 5f;
        private float buffer = 0f;

        [HideInInspector] public PlayerAttributes playerAttributes;
        private PlayerCombat playerCombat;

        public float CurrentStamina => staminaBar.GetStamina();

        [Header("Body Settings")]
        public Transform itemHolder;
        public Transform torsoBone;

        private CharacterController controller;
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

        [HideInInspector] public readonly MultiplierStat damageMultiplier = new();
        [HideInInspector] public readonly MultiplierStat attackSpeedMultiplier = new();
        [HideInInspector] public readonly MultiplierStat knockbackForceMultiplier = new();
        [HideInInspector] public readonly MultiplierStat poiseDamageMultiplier = new();
        [HideInInspector] public readonly MultiplierStat critChanceMultiplier = new();
        [HideInInspector] public readonly MultiplierStat critFactorMultiplier = new();
        [HideInInspector] public readonly MultiplierStat resourceDropMultiplier = new();

        public float TotalDamageMultiplier => damageMultiplier.Total;
        public float TotalAttackSpeedMultiplier => attackSpeedMultiplier.Total;
        public float TotalKnockbackForceMultiplier => knockbackForceMultiplier.Total;
        public float TotalPoiseDamageMultiplier => poiseDamageMultiplier.Total;
        public float TotalCritChanceMultiplier => critChanceMultiplier.Total;
        public float TotalCritFactorMultiplier => critFactorMultiplier.Total;
        public float TotalResourceDropMultiplier => resourceDropMultiplier.Total;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            playerCombat = GetComponent<PlayerCombat>();
            controller = GetComponent<CharacterController>();
            playerAttributes = GetComponent<PlayerAttributes>();
        }

        private void Start()
        {
            // Dynamically find and assign the main camera
            cam = Camera.main != null ? Camera.main.transform : null;

            if (cam == null)
                Debug.LogError("Main Camera not found! Ensure there is a Camera tagged as 'MainCamera' in the scene.");
        }

        // Update is called once per frame
        private void Update()
        {
            if (!GameManager.Instance.IsGameActive) return;
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
            if (!GameManager.Instance.IsGameActive) return;
            if (!uiBound) return;
            if (controller == null || !controller.enabled || cam == null) return;

            bool isSprinting = false;
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
                isSprinting = Input.GetKey(sprintKey) && CurrentStamina > 0;

                float moveSpeed = isSprinting ? sprintSpeed : speed;
                float finalSpeed = moveSpeed * playerAttributes.MovementSpeedMultiplier;

                controller.Move(finalSpeed * Time.fixedDeltaTime * moveDir.normalized);

                // Stamina handling
                if (isSprinting)
                {
                    staminaBar.DecreaseStamina();
                    buffer = 0f;
                }
                else if (buffer >= bufferCooldown)
                {
                    staminaBar.IncreaseStamina(playerAttributes.StaminaRegenRate);
                }
                else
                {
                    buffer += Time.fixedDeltaTime;
                }
            }
            else
            {
                if (buffer >= bufferCooldown)
                    staminaBar.IncreaseStamina(playerAttributes.StaminaRegenRate);
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

            Vector3 flatDelta = transform.position - lastPosition;
            flatDelta.y = 0f;

            currentSpeed = flatDelta.magnitude / Time.fixedDeltaTime;

            lastPosition = transform.position;

            float speedFactor = 0f;
            if (currentSpeed > 0f)
                speedFactor = isSprinting ? 1f : 0.5f;

            animator.SetFloat("Speed", speedFactor);
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

        public void RegisterActiveUpgrade(ActiveUpgradeEffect upgrade)
        {
            if (activeUpgrades.Contains(upgrade)) return;

            activeUpgrades.Add(upgrade);
            PlayerStatsManager.Instance.AddSkill(upgrade.skillImagePrefab);
        }

        public void UnregisterActiveUpgrade(ActiveUpgradeEffect upgrade)
        {
            if (activeUpgrades.Contains(upgrade))
                activeUpgrades.Remove(upgrade);
        }

        // Called by animation event for the ground slam upgrade
        private void OnSlamImpact()
        {
            GroundSlamUpgrade upgrade = GetGroundSlam();
            if (upgrade == null) return;

            upgrade.OnSlamImpact();
        }

        private GroundSlamUpgrade GetGroundSlam()
        {
            foreach (ActiveUpgradeEffect activeUpgrade in activeUpgrades)
            {
                if (activeUpgrade is GroundSlamUpgrade groundSlam) return groundSlam;
            }

            return null;
        }

        // Called by animation event
        private void OnEndActiveUpgrade()
        {
            if (playerCombat != null)
                playerCombat.canAttack = true;

            if (health != null)
                health.isImmune = false;
        }

        public void UpdateVitals()
        {
            int oldMaxHealth = health.maxHealth;
            int oldMaxStamina = (int)staminaBar.maxStamina;

            health.maxHealth = playerAttributes.MaxHealth;
            staminaBar.maxStamina = playerAttributes.MaxStamina;

            int currentHealth = health.GetHealth();
            currentHealth += health.maxHealth - oldMaxHealth;
            currentHealth = Mathf.Clamp(currentHealth, 0, health.maxHealth);

            health.SetHealth(currentHealth);
            health.healthBar.Initialize(health.maxHealth, currentHealth);

            float currentStamina = CurrentStamina;
            currentStamina += staminaBar.maxStamina - oldMaxStamina;
            currentStamina = Mathf.Clamp(currentStamina, 0, staminaBar.maxStamina);

            staminaBar.SetNewStamina(currentStamina);
            staminaBar.Initialize(staminaBar.maxStamina, currentStamina);

            if (playerCombat == null) return;

            float meleeMult = playerAttributes.MeleeDamageMultiplier;
            float critMult = playerAttributes.CritChanceMultiplier;

            Utility.SetMultiplierSource(this, meleeMult, damageMultiplier);
            Utility.SetMultiplierSource(this, critMult, critChanceMultiplier);
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
                currentStamina = CurrentStamina,
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
                inventory = InventoryManager.Instance.GetSavedInventoryData(),
                levelData = levelData
            };

            SaveSystem.SavePlayer(WorldSession.CurrentWorldName, playerData);
        }
    }
}
