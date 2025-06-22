using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Player Data")]
    public PlayerData playerData;

    [Header("Default Weapon")]
    public WeaponData defaultMeleeWeapon; // Assign the starting melee weapon SO here

    [Header("Components")]
    public Rigidbody2D rb;
    public Animator animator;
    public SpriteRenderer spriteRenderer;

    private Vector2 moveInput;
    private PlayerStats playerStats;
    private WeaponManager weaponManager; // Reference to WeaponManager
    private float moveSpeed;

    [Header("Runtime Modifiers")]
    public float moveSpeedMultiplier = 10f; // Mặc định là 100% tốc độ

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        playerStats = GetComponent<PlayerStats>();
        weaponManager = GetComponent<WeaponManager>(); // Get WeaponManager component
    }

    private void Start()
    {
        InitializePlayer();
    }

    private void InitializePlayer()
    {
        if (playerData == null)
        {
            int selectedCharacterIndex = PlayerPrefs.GetInt("SelectedCharacter", 0);
            playerData = Resources.Load<PlayerData>($"Players/Farmer{selectedCharacterIndex + 1}");

            if (playerData == null)
            {
                Debug.LogError("Không thể tải PlayerData! Sử dụng giá trị mặc định.");
                return;
            }
        }

        if (spriteRenderer != null && playerData.playerSprite != null)
        {
            spriteRenderer.sprite = playerData.playerSprite;
        }

        if (animator != null && playerData.animatorController != null)
        {
            animator.runtimeAnimatorController = playerData.animatorController;
        }

        moveSpeed = playerData.moveSpeed;

        if (playerStats != null)
        {
            playerStats.InitializeFromPlayerData(playerData);
        }

        if (weaponManager != null && defaultMeleeWeapon != null)
        {
            // weaponManager.ClearWeapons(); // Add this method to WeaponManager if needed
            weaponManager.AddWeapon(defaultMeleeWeapon);
            Debug.Log($"Added default weapon: {defaultMeleeWeapon.name}");
        }
        else
        {
            Debug.LogWarning("WeaponManager or DefaultMeleeWeapon not assigned in PlayerController! Player starts with no weapon.");
        }
    }

    private void Update()
    {
        UpdateAnimation();
    }

    private void FixedUpdate()
    {
        Move();
    }

    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    private void Move()
    {
        if (playerStats != null && !playerStats.isDead)
        {
            rb.linearVelocity = moveInput * moveSpeed * moveSpeedMultiplier;
        }
        else
        {
            rb.linearVelocity = Vector2.zero; 
        }
    }

    private void UpdateAnimation()
    {
        if (animator == null) return;

        if (moveInput != Vector2.zero && Time.timeScale != 0)
        {
            animator.SetBool("IsRunning", true);
            if (moveInput.x > 0.01f)
            {
                spriteRenderer.flipX = false;
            }
            else if (moveInput.x < -0.01f)
            {
                spriteRenderer.flipX = true;
            }
        }
        else
        {
            animator.SetBool("IsRunning", false);
        }
    }
}
