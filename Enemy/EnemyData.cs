using UnityEngine;

public enum EnemyType
{
    Melee,
    Fast,
    Ranged,
    Boss
}

[CreateAssetMenu(fileName = "New Enemy", menuName = "ScriptableObjects/Enemy")]
public class EnemyData : ScriptableObject
{
    public string enemyName;
    public Sprite enemySprite;
    public RuntimeAnimatorController animatorController; // Added field for Animator Controller
    public EnemyType type;
    public float maxHealth;
    public float damage;
    public float moveSpeed;
    public float attackRange;
    public float attackSpeed;
    public int moneyDropMin;
    public int moneyDropMax;
    public float moneyDropChance;

    [Header("Ranged Enemy Properties")]
    public GameObject projectilePrefab;
    public float projectileSpeed;

    [Header("Boss Properties")]
    public float phase2HealthPercentage = 0.5f; // Khi HP giảm xuống 50%
    public float phase2DamageMultiplier = 1.5f;
    public float phase2SpeedMultiplier = 1.2f;
}
