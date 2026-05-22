using System.Collections.Concurrent;

namespace War.Api.Application.Marketplace;

/// <summary>
/// Servicio in-memory que coordina:
///   · Cuentas del marketplace (oro + bolsa por accountId estable).
///   · Listados activos (escrow del ítem).
///   · Compras atómicas (transferencia oro ↔ ítem).
///
/// Decisión: usa accountId (Firebase UID o equivalente) como identificador
/// porque se persiste entre reconexiones SignalR, a diferencia de
/// ConnectionId/OnlinePlayer.Id (que se regeneran al reconectarse).
///
/// Decisión: in-memory (ConcurrentDictionary) para alinearse con el resto del
/// demo (GameWorldService, ChatHub) que tampoco usan DB. Vida = vida del
/// proceso del servidor. Suficiente para presentación.
/// </summary>
public sealed class MarketplaceService
{
    public const long StarterGold = 500;
    public const int StarterItemsCount = 3;

    private readonly ConcurrentDictionary<string, MarketAccount> _accounts = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, MarketListing> _listings = new();
    private readonly object _txLock = new(); // serialize buy/list transactions

    private readonly Random _rng = new();

    /// <summary>
    /// Catálogo "duro" de ítems que se sortean al crear la cuenta starter.
    /// Espejan el demo del Flutter store, así el flujo se ve coherente.
    /// </summary>
    private static readonly MarketItemSnapshot[] StarterCatalog =
    {
        new("", "hacha-guerra",       "Hacha de Guerra",       "arma",    "Legendario", "🪓", "Forjada en volcanes del norte. +45 daño físico."),
        new("", "machete-sangriento", "Machete Sangriento",    "arma",    "Épico",      "🗡️", "Hoja curva impregnada con veneno antiguo."),
        new("", "tridente-maldito",   "Tridente Maldito",      "arma",    "Legendario", "🔱", "Tres puntas, tres almas atrapadas en acero."),
        new("", "escudo-runico",      "Escudo Rúnico",         "defensa", "Raro",       "🛡️", "Absorbe 30% del daño en cada impacto recibido."),
        new("", "espada-infierno",    "Espada del Infierno",   "arma",    "Legendario", "⚔️", "Arde en llamas eternas. Quema a cada impacto."),
        new("", "bumeran-hueso",      "Bumerán de Hueso",      "arma",    "Raro",       "🪃", "Regresa al lanzador tras cada golpe certero."),
        new("", "amuleto-mortal",     "Amuleto Mortal",        "objeto",  "Épico",      "💀", "+20% golpe crítico. Maldice con poder antiguo."),
        new("", "cota-oscura",        "Cota de Malla Oscura",  "defensa", "Épico",      "⛓️", "Armadura forjada en hierro maldito. +60 defensa."),
        new("", "pocion-sangre",      "Poción de Sangre",      "objeto",  "Común",      "🧪", "Restaura 200 HP al instante. Sabor amargo."),
        new("", "mapa-destino",       "Mapa del Destino",      "objeto",  "Común",      "🗺️", "Revela enemigos en un radio de 50 metros."),
    };

    // ════════════════════════════════════════════════════════════════
    // CUENTAS
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// Idempotente: si la cuenta no existe, la crea con oro starter y 3 ítems
    /// random del catálogo. Si existe, sólo actualiza el displayName y LastSeen.
    /// </summary>
    public MarketAccount EnsureAccount(string accountId, string displayName)
    {
        if (string.IsNullOrWhiteSpace(accountId))
            throw new ArgumentException("accountId vacío.", nameof(accountId));
        if (string.IsNullOrWhiteSpace(displayName))
            displayName = "Guerrero";

        return _accounts.AddOrUpdate(
            accountId,
            // factory: nueva cuenta
            _ =>
            {
                var account = new MarketAccount
                {
                    AccountId = accountId,
                    DisplayName = displayName,
                    Gold = StarterGold
                };
                GrantStarterItems(account);
                return account;
            },
            // update existente
            (_, existing) =>
            {
                existing.DisplayName = displayName;
                existing.LastSeenUtc = DateTime.UtcNow;
                return existing;
            });
    }

    public MarketAccount? GetAccount(string accountId)
        => _accounts.TryGetValue(accountId, out var acc) ? acc : null;

    private void GrantStarterItems(MarketAccount account)
    {
        // Elegimos N piezas distintas (sin repetir definition).
        var pool = StarterCatalog.OrderBy(_ => _rng.Next()).Take(StarterItemsCount);
        foreach (var template in pool)
        {
            account.Bag.Add(template with { ItemId = Guid.NewGuid().ToString("N") });
        }
    }

    // ════════════════════════════════════════════════════════════════
    // LISTADOS
    // ════════════════════════════════════════════════════════════════

    public IReadOnlyList<MarketListing> GetActiveListings()
        => _listings.Values
            .OrderByDescending(l => l.ListedAtUtc)
            .ToList();

    public MarketActionResult ListItemForSale(string accountId, string itemId, long priceGold)
    {
        if (priceGold <= 0)
            return new MarketActionResult(false, "El precio debe ser mayor que 0.");
        if (priceGold > 1_000_000)
            return new MarketActionResult(false, "Precio absurdamente alto. Máximo 1.000.000 de oro.");

        lock (_txLock)
        {
            if (!_accounts.TryGetValue(accountId, out var account))
                return new MarketActionResult(false, "Cuenta no encontrada. Inicializa el marketplace primero.");

            var item = account.Bag.FirstOrDefault(i => i.ItemId == itemId);
            if (item is null)
                return new MarketActionResult(false, "El ítem no está en tu bolsa.");

            account.Bag.Remove(item);

            var listing = new MarketListing
            {
                ListingId = Guid.NewGuid().ToString("N"),
                SellerAccountId = accountId,
                SellerDisplayName = account.DisplayName,
                Item = item,
                PriceGold = priceGold
            };
            _listings[listing.ListingId] = listing;

            return new MarketActionResult(true, null, ToDto(account), ToDto(listing));
        }
    }

    public MarketActionResult CancelListing(string accountId, string listingId)
    {
        lock (_txLock)
        {
            if (!_listings.TryGetValue(listingId, out var listing))
                return new MarketActionResult(false, "Listado no encontrado.");
            if (!string.Equals(listing.SellerAccountId, accountId, StringComparison.OrdinalIgnoreCase))
                return new MarketActionResult(false, "No puedes cancelar listados de otros.");

            if (!_accounts.TryGetValue(accountId, out var account))
                return new MarketActionResult(false, "Cuenta no encontrada.");

            _listings.TryRemove(listingId, out _);
            account.Bag.Add(listing.Item);

            return new MarketActionResult(true, null, ToDto(account));
        }
    }

    // ════════════════════════════════════════════════════════════════
    // COMPRAS
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// Compra atómica: descuenta oro al comprador, acredita al vendedor,
    /// mueve el ítem a la bolsa del comprador, y retira el listado.
    /// </summary>
    public MarketActionResult BuyListing(string buyerAccountId, string listingId)
    {
        lock (_txLock)
        {
            if (!_listings.TryGetValue(listingId, out var listing))
                return new MarketActionResult(false, "Listado no encontrado o ya vendido.");

            if (string.Equals(listing.SellerAccountId, buyerAccountId, StringComparison.OrdinalIgnoreCase))
                return new MarketActionResult(false, "No puedes comprar tus propios listados.");

            if (!_accounts.TryGetValue(buyerAccountId, out var buyer))
                return new MarketActionResult(false, "Cuenta compradora no encontrada.");

            if (buyer.Gold < listing.PriceGold)
                return new MarketActionResult(false, $"Oro insuficiente. Necesitas {listing.PriceGold}, tienes {buyer.Gold}.");

            // El vendedor puede no estar online o conectado actualmente,
            // pero su cuenta debe existir para recibir el oro.
            if (!_accounts.TryGetValue(listing.SellerAccountId, out var seller))
                return new MarketActionResult(false, "Cuenta vendedora ya no existe.");

            // Transacción atómica
            buyer.Gold -= listing.PriceGold;
            seller.Gold += listing.PriceGold;
            buyer.Bag.Add(listing.Item);
            _listings.TryRemove(listingId, out _);

            return new MarketActionResult(true, null, ToDto(buyer), ToDto(listing));
        }
    }

    // ════════════════════════════════════════════════════════════════
    // MAPPERS DTO
    // ════════════════════════════════════════════════════════════════

    public static MarketAccountDto ToDto(MarketAccount account)
        => new(
            account.AccountId,
            account.DisplayName,
            account.Gold,
            account.Bag.Select(ToItemDto).ToList());

    public static MarketItemSnapshotDto ToItemDto(MarketItemSnapshot item)
        => new(
            item.ItemId,
            item.DefinitionId,
            item.Name,
            item.Category,
            item.Rarity,
            item.Emoji,
            item.Description,
            item.Tier,
            item.DevelopmentLevel);

    public static MarketListingDto ToDto(MarketListing listing)
        => new(
            listing.ListingId,
            listing.SellerAccountId,
            listing.SellerDisplayName,
            listing.Item.ItemId,
            listing.Item.DefinitionId,
            listing.Item.Name,
            listing.Item.Category,
            listing.Item.Rarity,
            listing.Item.Emoji,
            listing.Item.Description,
            listing.Item.Tier,
            listing.Item.DevelopmentLevel,
            listing.PriceGold,
            listing.ListedAtUtc);
}
