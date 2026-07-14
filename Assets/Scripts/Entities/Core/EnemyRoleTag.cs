using System;

/// <summary>
/// Describes the combat roles an enemy can have.
/// </summary>
[Flags]
public enum EnemyRoleTag
{
    None = 0,
    Bruiser = 1,
    Ranged = 2,
    Beast = 4
}
