using System.Collections.Concurrent;
using War.Core.Economy;

namespace War.Api.Application.Economy;

/// <summary>
/// Log centralizado de todas las transacciones monetarias del servidor.
/// Persistir esto a BD es trivial — por ahora vive en memoria con TTL y tope.
/// </summary>
/// <remarks>
/// Decision: el log es APPEND-ONLY. Nunca se mutan ni se eliminan entradas. La limpieza por edad
/// (>30 días) y por tope de capacidad se hace en rotación — las entradas caducan, no se borran
/// por petición.
///
/// Para el demo sin BD, todo vive aquí. Cuando se conecte PostgreSQL, este servicio se moverá a
/// un repositorio que respete la misma API (append + query readonly).
/// </remarks>
public sealed class WalletAuditLog
{
    private const int MaxInMemoryEntries = 50_000;
    private static readonly TimeSpan EntryLifetime = TimeSpan.FromDays(30);

    // LinkedList para conservar orden cronológico con inserción/expulsión O(1).
    private readonly LinkedList<WalletTransaction> _entries = new();
    private readonly object _lock = new();

    public void Record(WalletTransaction transaction)
    {
        lock (_lock)
        {
            _entries.AddLast(transaction);
            EvictIfNeeded();
        }
    }

    public void RecordMany(IEnumerable<WalletTransaction> transactions)
    {
        lock (_lock)
        {
            foreach (var tx in transactions)
                _entries.AddLast(tx);
            EvictIfNeeded();
        }
    }

    /// <summary>
    /// Consulta el historial de un jugador (más reciente primero).
    /// </summary>
    public IReadOnlyList<WalletTransaction> GetHistoryForPlayer(Guid playerId, int limit = 100)
    {
        lock (_lock)
        {
            var result = new List<WalletTransaction>(Math.Min(limit, 100));
            // Iterar del final al principio (más reciente primero).
            for (var node = _entries.Last; node is not null && result.Count < limit; node = node.Previous)
            {
                if (node.Value.PlayerId == playerId)
                    result.Add(node.Value);
            }
            return result;
        }
    }

    /// <summary>Total acumulado de transacciones en memoria (diagnóstico).</summary>
    public int Count
    {
        get { lock (_lock) return _entries.Count; }
    }

    private void EvictIfNeeded()
    {
        // 1. Expulsar por edad
        var cutoff = DateTime.UtcNow - EntryLifetime;
        while (_entries.First is { } first && first.Value.Timestamp < cutoff)
            _entries.RemoveFirst();

        // 2. Expulsar por tope
        while (_entries.Count > MaxInMemoryEntries)
            _entries.RemoveFirst();
    }
}
