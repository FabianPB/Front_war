using War.Core.Stats;

namespace War.Core.Progression;

/// <summary>
/// Servicio de otorgamiento de experiencia. Gestiona el flujo completo:
///
///   MobKill → XP base fija → reparto grupal → multiplicadores →
///   ExpGain stat → buffs temporales → GrantExperience()
///
/// ═══════════════════════════════════════════════════════════════
/// PIPELINE DE XP (orden estricto)
/// ═══════════════════════════════════════════════════════════════
///
///   1. XP base del mob (fija, del catálogo de mobs)
///   2. Reparto grupal: dividir equitativamente entre miembros vivos
///   3. Multiplicador ExpGain del personaje (stat acumulada de equipo,
///      espíritus, gemas, códice, etc.)
///   4. Multiplicador de buffs temporales (pastillas de vigor, etc.)
///   5. Redondeo final y otorgamiento via GrantExperience()
///
/// Fórmula:
///   xpFinal = floor(xpBase / groupSize × (1 + expGainStat/100) × buffMultiplier)
///
/// ═══════════════════════════════════════════════════════════════
/// SEGURIDAD
/// ═══════════════════════════════════════════════════════════════
///
/// El servidor calcula todo. El cliente NO puede enviar "dame X xp".
/// Solo puede reportar "maté mob ID X" y el servidor verifica y calcula.
/// </summary>
public sealed class ExperienceService
{
    private readonly ICharacterLevelProgressionService _levelService;

    public ExperienceService(ICharacterLevelProgressionService levelService)
    {
        _levelService = levelService;
    }

    /// <summary>
    /// Procesa la muerte de un mob y otorga XP a todos los miembros del grupo
    /// que participaron (o al jugador solo si no hay grupo).
    ///
    /// Retorna los resultados de XP para cada receptor.
    /// </summary>
    public IReadOnlyList<ExperienceGrantResult> ProcessMobKill(
        MobDefinition mob,
        IReadOnlyList<ExperienceRecipient> recipients)
    {
        if (recipients.Count == 0)
            return Array.Empty<ExperienceGrantResult>();

        var baseXp = mob.BaseExperience;
        var groupSize = recipients.Count;

        // Reparto equitativo
        var xpPerMember = baseXp / groupSize;
        if (xpPerMember <= 0) xpPerMember = 1; // mínimo 1 XP

        var results = new List<ExperienceGrantResult>(groupSize);

        foreach (var recipient in recipients)
        {
            // Multiplicador de ExpGain (stat del personaje: espíritus, gemas, códice, etc.)
            var expGainPct = recipient.ExpGainStat; // ya viene como porcentaje (e.g., 15.0 = +15%)
            var expGainMultiplier = 1.0m + expGainPct / 100m;

            // Multiplicador de buffs temporales (pastillas de vigor, eventos, etc.)
            var buffMultiplier = recipient.ActiveXpBuffMultiplier;
            if (buffMultiplier < 1.0m) buffMultiplier = 1.0m; // nunca menor a 1x

            // XP final para este receptor
            var xpFinal = (long)Math.Floor(xpPerMember * expGainMultiplier * buffMultiplier);
            if (xpFinal <= 0) xpFinal = 1; // garantía mínima

            // Otorgar via el sistema de progresión existente
            var grantResult = _levelService.GrantExperience(recipient.Progress, xpFinal);

            results.Add(new ExperienceGrantResult(
                PlayerId: recipient.PlayerId,
                BaseXpFromMob: baseXp,
                GroupSize: groupSize,
                XpAfterSplit: xpPerMember,
                ExpGainPercent: expGainPct,
                BuffMultiplier: buffMultiplier,
                FinalXpGranted: xpFinal,
                LevelsGained: grantResult.LevelsGained,
                NewLevel: grantResult.UpdatedProgress.Level,
                NewCurrentXp: grantResult.UpdatedProgress.CurrentXp,
                NewXpToNext: grantResult.UpdatedProgress.XpToNextLevel,
                LevelUpOccurred: grantResult.LevelsGained > 0));
        }

        return results;
    }

    /// <summary>
    /// Otorga XP directa a un solo jugador (para quests, logros, etc.)
    /// Aplica ExpGain y buffs igualmente.
    /// </summary>
    public ExperienceGrantResult GrantDirectExperience(
        ExperienceRecipient recipient,
        long rawXp,
        string source)
    {
        var expGainMultiplier = 1.0m + recipient.ExpGainStat / 100m;
        var buffMultiplier = Math.Max(1.0m, recipient.ActiveXpBuffMultiplier);
        var xpFinal = (long)Math.Floor(rawXp * expGainMultiplier * buffMultiplier);
        if (xpFinal <= 0) xpFinal = 1;

        var grantResult = _levelService.GrantExperience(recipient.Progress, xpFinal);

        return new ExperienceGrantResult(
            PlayerId: recipient.PlayerId,
            BaseXpFromMob: rawXp,
            GroupSize: 1,
            XpAfterSplit: rawXp,
            ExpGainPercent: recipient.ExpGainStat,
            BuffMultiplier: buffMultiplier,
            FinalXpGranted: xpFinal,
            LevelsGained: grantResult.LevelsGained,
            NewLevel: grantResult.UpdatedProgress.Level,
            NewCurrentXp: grantResult.UpdatedProgress.CurrentXp,
            NewXpToNext: grantResult.UpdatedProgress.XpToNextLevel,
            LevelUpOccurred: grantResult.LevelsGained > 0);
    }
}

// ══════════════════════════════════════════════════════════════════
// MODELOS
// ══════════════════════════════════════════════════════════════════

/// <summary>
/// Información de un receptor de XP. Se construye server-side
/// a partir del OnlinePlayer y sus stats/buffs activos.
/// </summary>
public sealed record ExperienceRecipient(
    string PlayerId,
    CharacterLevelProgress Progress,
    /// <summary>Stat ExpGain acumulada (equipos, espíritus, gemas, códice). Porcentaje.</summary>
    decimal ExpGainStat,
    /// <summary>Multiplicador de buffs temporales activos (pastillas de vigor, etc.). 1.0 = sin buff.</summary>
    decimal ActiveXpBuffMultiplier);

/// <summary>
/// Resultado detallado de un otorgamiento de XP. Incluye todo el
/// desglose del pipeline para auditoría y feedback al cliente.
/// </summary>
public sealed record ExperienceGrantResult(
    string PlayerId,
    long BaseXpFromMob,
    int GroupSize,
    long XpAfterSplit,
    decimal ExpGainPercent,
    decimal BuffMultiplier,
    long FinalXpGranted,
    int LevelsGained,
    int NewLevel,
    long NewCurrentXp,
    long NewXpToNext,
    bool LevelUpOccurred);

// ══════════════════════════════════════════════════════════════════
// MOBS (definición base, XP fija)
// ══════════════════════════════════════════════════════════════════

/// <summary>
/// Definición de un mob/monstruo. La XP base es FIJA — nunca cambia.
/// Los multiplicadores del jugador (ExpGain, buffs) se aplican después.
/// </summary>
public sealed record MobDefinition(
    string MobId,
    string Name,
    int Level,
    long BaseExperience,
    /// <summary>Tiempo de respawn en segundos. 0 = no respawnea.</summary>
    int RespawnSeconds = 30,
    string? ZoneId = null);

/// <summary>
/// Catálogo de mobs del juego. Los mobs se organizan por zona y nivel.
/// La XP base escala con el nivel del mob.
/// </summary>
public static class MobCatalog
{
    /// <summary>
    /// Calcula la XP base de un mob a partir de su nivel.
    /// Fórmula: baseXp = 50 + (level × 12) + (level² × 0.8)
    ///
    /// Esto da una curva suave:
    ///   Nivel 1:  ~63 XP
    ///   Nivel 10: ~250 XP
    ///   Nivel 20: ~610 XP
    ///   Nivel 30: ~1090 XP
    ///   Nivel 40: ~1690 XP
    ///   Nivel 50: ~2410 XP
    ///   Nivel 80: ~5570 XP
    /// </summary>
    public static long CalculateBaseXp(int mobLevel)
    {
        return (long)(50 + mobLevel * 12 + mobLevel * mobLevel * 0.8);
    }

    /// <summary>
    /// Genera un mob genérico para un nivel dado. Útil para spots de farmeo.
    /// </summary>
    public static MobDefinition CreateGenericMob(int level, string? zoneId = null)
    {
        return new MobDefinition(
            MobId: $"mob.generic.level{level}",
            Name: $"Criatura Nivel {level}",
            Level: level,
            BaseExperience: CalculateBaseXp(level),
            RespawnSeconds: 30,
            ZoneId: zoneId);
    }

    /// <summary>
    /// Genera un mob élite (jefe de zona) que da más XP.
    /// </summary>
    public static MobDefinition CreateEliteMob(int level, string name, string? zoneId = null)
    {
        return new MobDefinition(
            MobId: $"mob.elite.{name.ToLowerInvariant().Replace(' ', '-')}",
            Name: name,
            Level: level,
            BaseExperience: CalculateBaseXp(level) * 5, // 5x XP de un mob normal
            RespawnSeconds: 300, // 5 minutos
            ZoneId: zoneId);
    }
}

// ══════════════════════════════════════════════════════════════════
// BUFFS DE XP (pastillas de vigor, eventos, etc.)
// ══════════════════════════════════════════════════════════════════

/// <summary>
/// Sistema de buffs temporales de XP. Las pastillas de vigor y otros
/// consumibles otorgan un multiplicador fijo durante un tiempo limitado.
///
/// Estos buffs son ADITIVOS entre sí: si tienes pastilla ×1.5 y evento ×1.2,
/// el multiplicador total es 1.0 + 0.5 + 0.2 = ×1.7 (no multiplicativo).
/// </summary>
public sealed class XpBuffManager
{
    private readonly List<ActiveXpBuff> _activeBuffs = new();
    private readonly object _lock = new();

    /// <summary>
    /// Aplica un buff de XP temporal al jugador.
    /// </summary>
    public void ApplyBuff(XpBuffDefinition buff)
    {
        lock (_lock)
        {
            // Verificar si ya tiene un buff del mismo tipo (no se apilan, se refresca)
            var existing = _activeBuffs.FindIndex(b => b.BuffId == buff.BuffId);
            if (existing >= 0)
            {
                // Refrescar duración
                _activeBuffs[existing] = new ActiveXpBuff(
                    buff.BuffId, buff.BonusPercent, DateTime.UtcNow.Add(buff.Duration));
            }
            else
            {
                _activeBuffs.Add(new ActiveXpBuff(
                    buff.BuffId, buff.BonusPercent, DateTime.UtcNow.Add(buff.Duration)));
            }
        }
    }

    /// <summary>
    /// Calcula el multiplicador total activo. Limpia buffs expirados.
    ///
    /// Retorna >= 1.0 siempre (1.0 = sin buffs activos).
    /// Los buffs son ADITIVOS: pastilla 50% + evento 20% = ×1.7 total.
    /// </summary>
    public decimal GetCurrentMultiplier()
    {
        lock (_lock)
        {
            var now = DateTime.UtcNow;
            _activeBuffs.RemoveAll(b => b.ExpiresAt <= now);

            if (_activeBuffs.Count == 0) return 1.0m;

            var totalBonus = _activeBuffs.Sum(b => b.BonusPercent);
            return 1.0m + totalBonus / 100m;
        }
    }

    /// <summary>Buffs activos para mostrar en el HUD.</summary>
    public IReadOnlyList<ActiveXpBuff> GetActiveBuffs()
    {
        lock (_lock)
        {
            var now = DateTime.UtcNow;
            _activeBuffs.RemoveAll(b => b.ExpiresAt <= now);
            return _activeBuffs.ToArray();
        }
    }
}

/// <summary>Definición de un buff de XP (e.g., pastilla de vigor).</summary>
public sealed record XpBuffDefinition(
    string BuffId,
    string Name,
    /// <summary>Bonus como porcentaje (50 = +50% XP).</summary>
    decimal BonusPercent,
    /// <summary>Duración del efecto.</summary>
    TimeSpan Duration);

/// <summary>Buff de XP activo en un jugador.</summary>
public sealed record ActiveXpBuff(
    string BuffId,
    decimal BonusPercent,
    DateTime ExpiresAt)
{
    public TimeSpan RemainingTime => ExpiresAt > DateTime.UtcNow
        ? ExpiresAt - DateTime.UtcNow
        : TimeSpan.Zero;
}

/// <summary>
/// Catálogo de consumibles de XP predefinidos.
/// </summary>
public static class XpBuffCatalog
{
    /// <summary>Pastilla de Vigor Menor: +25% XP durante 30 minutos.</summary>
    public static readonly XpBuffDefinition VigorPillMinor = new(
        "vigor-pill-minor", "Pastilla de Vigor Menor",
        25m, TimeSpan.FromMinutes(30));

    /// <summary>Pastilla de Vigor Mayor: +50% XP durante 30 minutos.</summary>
    public static readonly XpBuffDefinition VigorPillMajor = new(
        "vigor-pill-major", "Pastilla de Vigor Mayor",
        50m, TimeSpan.FromMinutes(30));

    /// <summary>Pastilla de Vigor Suprema: +100% XP durante 15 minutos.</summary>
    public static readonly XpBuffDefinition VigorPillSupreme = new(
        "vigor-pill-supreme", "Pastilla de Vigor Suprema",
        100m, TimeSpan.FromMinutes(15));

    /// <summary>Evento de experiencia del servidor: +30% XP durante 2 horas.</summary>
    public static readonly XpBuffDefinition ServerXpEvent = new(
        "server-xp-event", "Evento de Experiencia",
        30m, TimeSpan.FromHours(2));
}
