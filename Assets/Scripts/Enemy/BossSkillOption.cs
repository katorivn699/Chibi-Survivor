using System;
using UnityEngine;

public enum skillType
{
    RangedAttack,
    SpreadProjectile,
    ProjectileCircle,
    ChargeAttack,
    Spiral,
    MeleeAttack
}

[Serializable]
public class BossSkillOption
{
    public skillType skillName;
    [Range(0f, 1f)] public float weightPhase1 = 0.25f;
    [Range(0f, 1f)] public float weightPhase2 = 0.2f;

    [HideInInspector] public Action skillAction;
}
