using Microsoft.AspNetCore.Mvc;
using War.Api.Application.GameWorld;
using War.Api.Application.Marketplace;

namespace War.Api.Controllers;

/// <summary>
/// REST endpoints para la aplicación móvil (Flutter). El backend ya expone
/// SignalR hubs para el cliente Unity; este controller proporciona el equivalente
/// HTTP para clientes que prefieren un wire-protocol más simple.
///
/// Endpoints:
///   GET    /api/marketplace/account/{accountId}            → estado actual (oro + bolsa)
///   PUT    /api/marketplace/account/{accountId}/displayName→ actualizar nombre visible
///   GET    /api/marketplace/listings                       → todos los listados activos
///   POST   /api/marketplace/listings                       → vender un ítem de mi bolsa
///   POST   /api/marketplace/listings/{listingId}/buy       → comprar un listado
///   DELETE /api/marketplace/listings/{listingId}           → cancelar mi listado
/// </summary>
[ApiController]
[Route("api/marketplace")]
public sealed class MarketplaceController : ControllerBase
{
    private readonly MarketplaceService _marketplace;
    private readonly HubActionRateLimiter _rateLimiter;

    public MarketplaceController(MarketplaceService marketplace, HubActionRateLimiter rateLimiter)
    {
        _marketplace = marketplace;
        _rateLimiter = rateLimiter;
    }

    public sealed record EnsureAccountQuery(string DisplayName);
    public sealed record ListItemRequest(string AccountId, string ItemId, long PriceGold);
    public sealed record BuyListingRequest(string BuyerAccountId);
    public sealed record CancelListingRequest(string AccountId);

    /// <summary>
    /// Devuelve el estado de la cuenta. La crea si no existe (idempotente).
    /// La primera vez entrega oro de bienvenida + 3 ítems iniciales para que
    /// el jugador tenga algo que listar en la demo.
    /// </summary>
    [HttpGet("account/{accountId}")]
    public ActionResult<MarketAccountDto> GetAccount(
        [FromRoute] string accountId,
        [FromQuery] string? displayName = null)
    {
        if (string.IsNullOrWhiteSpace(accountId))
            return BadRequest(new { error = "accountId requerido." });

        var account = _marketplace.EnsureAccount(accountId, displayName ?? "Guerrero");
        return Ok(MarketplaceService.ToDto(account));
    }

    [HttpPut("account/{accountId}/displayName")]
    public ActionResult<MarketAccountDto> UpdateDisplayName(
        [FromRoute] string accountId,
        [FromBody] EnsureAccountQuery body)
    {
        if (string.IsNullOrWhiteSpace(accountId) || string.IsNullOrWhiteSpace(body?.DisplayName))
            return BadRequest(new { error = "accountId y displayName requeridos." });
        var account = _marketplace.EnsureAccount(accountId, body.DisplayName);
        return Ok(MarketplaceService.ToDto(account));
    }

    [HttpGet("listings")]
    public ActionResult<IReadOnlyList<MarketListingDto>> GetListings()
    {
        var listings = _marketplace.GetActiveListings()
            .Select(MarketplaceService.ToDto)
            .ToList();
        return Ok(listings);
    }

    [HttpPost("listings")]
    public ActionResult<MarketActionResult> ListItem([FromBody] ListItemRequest body)
    {
        if (!_rateLimiter.TryAcquire($"market:list:{body?.AccountId}", out var retry))
            return StatusCode(429, new { error = "Demasiadas solicitudes.", retryAfterMs = retry });
        if (body is null || string.IsNullOrWhiteSpace(body.AccountId) || string.IsNullOrWhiteSpace(body.ItemId))
            return BadRequest(new { error = "AccountId, ItemId y PriceGold son requeridos." });

        var result = _marketplace.ListItemForSale(body.AccountId, body.ItemId, body.PriceGold);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("listings/{listingId}/buy")]
    public ActionResult<MarketActionResult> Buy(
        [FromRoute] string listingId,
        [FromBody] BuyListingRequest body)
    {
        if (!_rateLimiter.TryAcquire($"market:buy:{body?.BuyerAccountId}", out var retry))
            return StatusCode(429, new { error = "Demasiadas solicitudes.", retryAfterMs = retry });
        if (body is null || string.IsNullOrWhiteSpace(body.BuyerAccountId))
            return BadRequest(new { error = "BuyerAccountId requerido." });

        var result = _marketplace.BuyListing(body.BuyerAccountId, listingId);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("listings/{listingId}")]
    public ActionResult<MarketActionResult> Cancel(
        [FromRoute] string listingId,
        [FromBody] CancelListingRequest body)
    {
        if (body is null || string.IsNullOrWhiteSpace(body.AccountId))
            return BadRequest(new { error = "AccountId requerido." });

        var result = _marketplace.CancelListing(body.AccountId, listingId);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
