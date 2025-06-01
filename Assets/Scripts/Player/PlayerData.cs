using UnityEngine;

[CreateAssetMenu(fileName = "New Player", menuName = "ScriptableObjects/Player")]
public class PlayerData : ScriptableObject
{
    [Header("Basic Info")]
    public string playerName;
    public Sprite playerSprite;
    
    [Header("Animation")]
    public RuntimeAnimatorController animatorController;
    
    [Header("Stats")]
    public float maxHealth = 100f;
    public float moveSpeed = 5f;
    public float damageMultiplier = 1f;
    public float attackSpeedMultiplier = 1f;
    
    [Header("Description")]
    [TextArea(3, 5)]
    public string description;
}
