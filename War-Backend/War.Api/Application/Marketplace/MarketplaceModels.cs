namespace War.Api.Application.Marketplace;

/// <summary>
/// Snapshot inmutable de un ítem dentro del marketplace o de la bolsa de una cuenta.
/// Mantenemos esto separado de InventoryItem (del flujo OnlinePlayer) porque el
/// marketplace vive en términos de cuentas estables (Firebase UID, etc.), no de
/// conexiones SignalR efímeras.
/// </summary>
public sealed record MarketItemSnapshot(
    string ItemId,            // GUID único de esta instancia del ítem
    string DefinitionId,      // ID del catálogo (ej. "hacha-guerra")
    string Name,              // Nombre visible (ej. "Hacha de Guerra")
    string Category,          // "arma" | "defensa" | "objeto"
    string Rarity,            // "Común" | "Raro" | "Épico" | "Legendario"
    string Emoji,             // Emoji visual representativo
    string Description,
    int Tier = 1,
    int DevelopmentLevel = 1);

/// <summary>
/// Estado completo de una cuenta dentro del marketplace: oro + ítems en bolsa.
/// </summary>
public sealed class MarketAccount
{
    public required string AccountId { get; init; }
    public required string DisplayName { get; set; }
    public long Gold { get; set; }
    public List<MarketItemSnapshot> Bag { get; } = new();
    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
    public DateTime LastSeenUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Listado activo en el marketplace. Un ítem listado SALE de la bolsa del
/// vendedor y queda "en escrow" aquí hasta que alguien compre o se cancele.
/// </summary>
public sealed class MarketListing
{
    public required string ListingId { get; init; }
    public required string SellerAccountId { get; init; }
    public required string SellerDisplayName { get; init; }
    public required MarketItemSnapshot Item { get; init; }
    public required long PriceGold { get; init; }
    public DateTime ListedAtUtc { get; init; } = DateTime.UtcNow;
}

// ── DTOs para el wire SignalR/REST ──

public sealed record MarketListingDto(
    string ListingId,
    string SellerAccountId,
    string SellerDisplayName,
    string ItemId,
    string DefinitionId,
    string Name,
    string Category,
    string Rarity,
    string Emoji,
    string Description,
    int Tier,
    int DevelopmentLevel,
    long PriceGold,
    DateTime ListedAtUtc);

public sealed record MarketAccountDto(
    string AccountId,
    string DisplayName,
    long Gold,
    IReadOnlyList<MarketItemSnapshotDto> Bag);

public sealed record MarketItemSnapshotDto(
    string ItemId,
    string DefinitionId,
    string Name,
    string Category,
    string Rarity,
    string Emoji,
    string Description,
    int Tier,
    int DevelopmentLevel);

public sealed record MarketActionResult(
    bool Success,
    string? ErrorMessage = null,
    MarketAccountDto? UpdatedAccount = null,
    MarketListingDto? AffectedListing = null);

public sealed record GlobalChatMessageDto(
    Guid MessageId,
    string SenderAccountId,
    string SenderDisplayName,
    string Content,
    DateTime SentAtUtc);
