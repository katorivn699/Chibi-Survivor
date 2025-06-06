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
        // Khởi tạo nhân vật từ PlayerData
        InitializePlayer();
    }

    private void InitializePlayer()
    {
        // Nếu không có playerData, tải từ lựa chọn đã lưu
        if (playerData == null)
        {
            int selectedCharacterIndex = PlayerPrefs.GetInt("SelectedCharacter", 0);
            // Tải PlayerData từ Resources
            // Giả định rằng các PlayerData được lưu trong thư mục Resources/Players với tên Player0, Player1, ...
            playerData = Resources.Load<PlayerData>($"Players/Farmer{selectedCharacterIndex + 1}");

            if (playerData == null)
            {
                Debug.LogError("Không thể tải PlayerData! Sử dụng giá trị mặc định.");
                // Optionally load a fallback PlayerData here
                return;
            }
        }

        // Áp dụng sprite
        if (spriteRenderer != null && playerData.playerSprite != null)
        {
            spriteRenderer.sprite = playerData.playerSprite;
        }

        // Áp dụng animator controller
        if (animator != null && playerData.animatorController != null)
        {
            animator.runtimeAnimatorController = playerData.animatorController;
        }

        // Áp dụng stats
        moveSpeed = playerData.moveSpeed;

        // Cập nhật PlayerStats
        if (playerStats != null)
        {
            playerStats.InitializeFromPlayerData(playerData);
        }

        // Add default melee weapon
        if (weaponManager != null && defaultMeleeWeapon != null)
        {
            // Clear any existing weapons first (in case of restart/reinitialization)
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
        // Cập nhật animation
        UpdateAnimation();
    }

    private void FixedUpdate()
    {
        // Di chuyển nhân vật
        Move();
    }

    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    private void Move()
    {
        // Chỉ di chuyển khi nhân vật còn sống
        if (playerStats != null && !playerStats.isDead)
        {
            rb.linearVelocity = moveInput * moveSpeed * moveSpeedMultiplier;
        }
        else
        {
            rb.linearVelocity = Vector2.zero; // Stop movement if dead
        }
    }

    private void UpdateAnimation()
    {
        if (animator == null) return;

        if (moveInput != Vector2.zero && Time.timeScale != 0)
        {
            animator.SetBool("IsRunning", true);
            // Flip sprite based on horizontal movement
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
