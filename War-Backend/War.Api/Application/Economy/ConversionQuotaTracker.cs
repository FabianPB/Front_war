using System.Collections.Concurrent;
using War.Core.Economy;

namespace War.Api.Application.Economy;

/// <summary>
/// Tracker de quotas de conversión por jugador, por ventana temporal (día/semana/mes).
/// Thread-safe; vive en memoria por ahora (se migrará a BD cuando haga falta cruzar reinicios).
/// </summary>
/// <remarks>
/// Las ventanas son **de calendario** (no "últimas 24h"):
///   · Día: 00:00:00 local → 23:59:59 local
///   · Semana: desde el lunes 00:00 local
///   · Mes: día 1 del mes a las 00:00 local
///
/// No-acumulables: al rollear a una nueva ventana el contador se reinicia a 0. Si el jugador no
/// consumió su quota anterior, la pierde — no se "guarda".
///
/// Cada conversión exitosa suma al contador de las 3 ventanas simultáneamente. Las 3 deben
/// respetar el tope por separado (regla AND).
/// </remarks>
public sealed class ConversionQuotaTracker
{
    private readonly ConcurrentDictionary<Guid, PlayerQuotaState> _states = new();

    private sealed class PlayerQuotaState
    {
        public long SilverToday, SilverThisWeek, SilverThisMonth;
        public long GoldToday, GoldThisWeek, GoldThisMonth;
        public DateTime DailyAnchor, WeeklyAnchor, MonthlyAnchor;
    }

    /// <summary>
    /// Cuánto ha convertido el jugador en cada ventana. Aplica rollover implícito si las ventanas
    /// previas ya expiraron (retorna 0 en esos casos).
    /// </summary>
    public QuotaSnapshot GetUsage(Guid playerId, DateTime? now = null)
    {
        var nowLocal = (now ?? DateTime.Now).Date;
        var state = _states.GetOrAdd(playerId, _ => NewState(nowLocal));

        lock (state)
        {
            RollIfNeeded(state, nowLocal);
            return new QuotaSnapshot(
                SilverToday: state.SilverToday,
                SilverThisWeek: state.SilverThisWeek,
                SilverThisMonth: state.SilverThisMonth,
                GoldToday: state.GoldToday,
                GoldThisWeek: state.GoldThisWeek,
                GoldThisMonth: state.GoldThisMonth);
        }
    }

    /// <summary>
    /// Pre-chequea si una conversión cabe en las 3 ventanas actuales sin aplicarla.
    /// </summary>
    public QuotaCheckResult CheckAllowance(
        Guid playerId,
        CurrencyType targetCurrency,
        long amountCreated,
        long perDay,
        long perWeek,
        long perMonth,
        DateTime? now = null)
    {
        var snap = GetUsage(playerId, now);
        long usedDay, usedWeek, usedMonth;
        if (targetCurrency == CurrencyType.Silver)
            (usedDay, usedWeek, usedMonth) = (snap.SilverToday, snap.SilverThisWeek, snap.SilverThisMonth);
        else if (targetCurrency == CurrencyType.Gold)
            (usedDay, usedWeek, usedMonth) = (snap.GoldToday, snap.GoldThisWeek, snap.GoldThisMonth);
        else
            return QuotaCheckResult.Deny($"Moneda {targetCurrency} no convertible.");

        if (usedDay + amountCreated > perDay)
            return QuotaCheckResult.Deny($"Excede el cupo diario ({usedDay + amountCreated}/{perDay}).");
        if (usedWeek + amountCreated > perWeek)
            return QuotaCheckResult.Deny($"Excede el cupo semanal ({usedWeek + amountCreated}/{perWeek}).");
        if (usedMonth + amountCreated > perMonth)
            return QuotaCheckResult.Deny($"Excede el cupo mensual ({usedMonth + amountCreated}/{perMonth}).");

        return QuotaCheckResult.Allow();
    }

    /// <summary>
    /// Registra que una conversión ya se aplicó. Debe llamarse DESPUÉS del cobro exitoso.
    /// </summary>
    public void RecordConversion(
        Guid playerId,
        CurrencyType targetCurrency,
        long amountCreated,
        DateTime? now = null)
    {
        var nowLocal = (now ?? DateTime.Now).Date;
        var state = _states.GetOrAdd(playerId, _ => NewState(nowLocal));

        lock (state)
        {
            RollIfNeeded(state, nowLocal);
            if (targetCurrency == CurrencyType.Silver)
            {
                state.SilverToday     += amountCreated;
                state.SilverThisWeek  += amountCreated;
                state.SilverThisMonth += amountCreated;
            }
            else if (targetCurrency == CurrencyType.Gold)
            {
                state.GoldToday     += amountCreated;
                state.GoldThisWeek  += amountCreated;
                state.GoldThisMonth += amountCreated;
            }
        }
    }

    // ── Rollover de ventanas ────────────────────────────────────────────────

    private static PlayerQuotaState NewState(DateTime nowLocal)
    {
        return new PlayerQuotaState
        {
            DailyAnchor = nowLocal,
            WeeklyAnchor = StartOfWeek(nowLocal),
            MonthlyAnchor = new DateTime(nowLocal.Year, nowLocal.Month, 1)
        };
    }

    private static void RollIfNeeded(PlayerQuotaState state, DateTime nowLocal)
    {
        // Diario
        if (nowLocal > state.DailyAnchor)
        {
            state.SilverToday = 0;
            state.GoldToday = 0;
            state.DailyAnchor = nowLocal;
        }
        // Semanal
        var weekStart = StartOfWeek(nowLocal);
        if (weekStart > state.WeeklyAnchor)
        {
            state.SilverThisWeek = 0;
            state.GoldThisWeek = 0;
            state.WeeklyAnchor = weekStart;
        }
        // Mensual
        var monthStart = new DateTime(nowLocal.Year, nowLocal.Month, 1);
        if (monthStart > state.MonthlyAnchor)
        {
            state.SilverThisMonth = 0;
            state.GoldThisMonth = 0;
            state.MonthlyAnchor = monthStart;
        }
    }

    private static DateTime StartOfWeek(DateTime d)
    {
        // Lunes como inicio de semana.
        var diff = ((int)d.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return d.AddDays(-diff).Date;
    }
}

public sealed record QuotaSnapshot(
    long SilverToday,
    long SilverThisWeek,
    long SilverThisMonth,
    long GoldToday,
    long GoldThisWeek,
    long GoldThisMonth);

public sealed record QuotaCheckResult(bool Allowed, string? Reason)
{
    public static QuotaCheckResult Allow() => new(true, null);
    public static QuotaCheckResult Deny(string reason) => new(false, reason);
}
