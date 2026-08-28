namespace SpaceSurvivors.Data
{
    /// <summary>
    /// Every one-shot sound event the game can raise. The <see cref="SfxBank"/> maps each to
    /// one or more clips; the audio director (UI layer) plays them in response to gameplay
    /// events — nothing in the gameplay code references audio directly (AI_Guidelines §1).
    /// </summary>
    public enum SfxId
    {
        None = 0,
        PlayerShootLaser,
        PlayerShootMissile,
        EnemyDeath,
        BossDeath,
        PlayerHurt,
        ShieldAbsorb,
        PickupCollect,
        LevelUp,
        BossWarning,
        UiClick,
        UiHover,
        RunWon,
        RunLost,
    }
}
