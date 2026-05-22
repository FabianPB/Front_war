using Microsoft.AspNetCore.Mvc;
using War.Api.Application.Social;
using War.Api.Localization;

namespace War.Api.Controllers;

// Decision: Single controller for all social endpoints because they share the same auth pattern
// (X-Character-Id header) and proximity/relationship validation pipeline. Splitting into
// FriendsController, BlockController, etc. would scatter related logic without real benefit.
[ApiController]
[Route("api/social")]
public sealed class SocialController : ControllerBase
{
    private readonly ISocialRelationshipService _relationships;
    private readonly PublicProfileService _profiles;

    public SocialController(
        ISocialRelationshipService relationships,
        PublicProfileService profiles)
    {
        _relationships = relationships;
        _profiles = profiles;
    }

    // ────────────────────────────────────────────────────────────────────
    // Nearby Players & Profiles
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns a list of nearby players for the social discovery panel.
    /// </summary>
    [HttpGet("nearby")]
    public async Task<IActionResult> GetNearbyPlayers()
    {
        try
        {
            if (!TryGetCharacterId(out var characterId))
                return BadRequest(new { message = UiStrings.ErrorInvalidCharacterId });

            var nearby = await _profiles.GetNearbyPlayersAsync(characterId);
            return Ok(nearby);
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = UiStrings.ErrorInternal });
        }
    }

    /// <summary>
    /// Returns the public profile of a target character. Requires proximity.
    /// </summary>
    [HttpGet("profile/{characterId:guid}")]
    public async Task<IActionResult> GetPublicProfile(Guid characterId)
    {
        try
        {
            if (!TryGetCharacterId(out var viewerId))
                return BadRequest(new { message = UiStrings.ErrorInvalidCharacterId });

            var profile = await _profiles.GetPublicProfileAsync(viewerId, characterId);
            if (profile is null)
                return NotFound(new { message = UiStrings.ProfileNotAvailable });

            return Ok(profile);
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = UiStrings.ErrorInternal });
        }
    }

    // ────────────────────────────────────────────────────────────────────
    // Friend Requests
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Sends a friend request to a target character. Requires proximity.
    /// </summary>
    [HttpPost("friends/request")]
    public async Task<IActionResult> SendFriendRequest([FromBody] SendFriendRequestDto dto)
    {
        try
        {
            if (!TryGetCharacterId(out var senderId))
                return BadRequest(new { message = UiStrings.ErrorInvalidCharacterId });

            var result = await _relationships.SendFriendRequestAsync(senderId, dto.TargetCharacterId);
            return result.Success
                ? Ok(new { message = UiStrings.FriendRequestSent })
                : BadRequest(new { message = result.ErrorMessage });
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = UiStrings.ErrorInternal });
        }
    }

    /// <summary>
    /// Accepts or rejects a pending friend request.
    /// </summary>
    [HttpPost("friends/respond")]
    public async Task<IActionResult> RespondToFriendRequest([FromBody] RespondFriendRequestDto dto)
    {
        try
        {
            if (!TryGetCharacterId(out var receiverId))
                return BadRequest(new { message = UiStrings.ErrorInvalidCharacterId });

            var result = await _relationships.RespondToFriendRequestAsync(receiverId, dto.RequestId, dto.Accept);
            return result.Success
                ? Ok(new { message = dto.Accept ? UiStrings.FriendRequestAccepted : UiStrings.FriendRequestRejected })
                : BadRequest(new { message = result.ErrorMessage });
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = UiStrings.ErrorInternal });
        }
    }

    /// <summary>
    /// Returns the list of pending inbound friend requests.
    /// </summary>
    [HttpGet("friends/requests/pending")]
    public async Task<IActionResult> GetPendingFriendRequests()
    {
        try
        {
            if (!TryGetCharacterId(out var characterId))
                return BadRequest(new { message = UiStrings.ErrorInvalidCharacterId });

            var requests = await _relationships.GetPendingInboundRequestsAsync(characterId);
            return Ok(requests);
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = UiStrings.ErrorInternal });
        }
    }

    // ────────────────────────────────────────────────────────────────────
    // Friend List & Removal
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the caller's friend list.
    /// </summary>
    [HttpGet("friends")]
    public async Task<IActionResult> GetFriendList()
    {
        try
        {
            if (!TryGetCharacterId(out var characterId))
                return BadRequest(new { message = UiStrings.ErrorInvalidCharacterId });

            var friends = await _relationships.GetFriendListAsync(characterId);
            return Ok(friends);
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = UiStrings.ErrorInternal });
        }
    }

    /// <summary>
    /// Removes a friend from the caller's friend list. Mutual removal.
    /// </summary>
    [HttpDelete("friends/{characterId:guid}")]
    public async Task<IActionResult> RemoveFriend(Guid characterId)
    {
        try
        {
            if (!TryGetCharacterId(out var callerId))
                return BadRequest(new { message = UiStrings.ErrorInvalidCharacterId });

            var result = await _relationships.RemoveFriendAsync(callerId, characterId);
            return result.Success
                ? Ok(new { message = UiStrings.FriendRemoved })
                : BadRequest(new { message = result.ErrorMessage });
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = UiStrings.ErrorInternal });
        }
    }

    // ────────────────────────────────────────────────────────────────────
    // Block / Unblock
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Blocks a target character. Requires proximity for initial block.
    /// Removes any existing friendship.
    /// </summary>
    [HttpPost("block")]
    public async Task<IActionResult> BlockPlayer([FromBody] BlockPlayerDto dto)
    {
        try
        {
            if (!TryGetCharacterId(out var characterId))
                return BadRequest(new { message = UiStrings.ErrorInvalidCharacterId });

            var result = await _relationships.BlockPlayerAsync(characterId, dto.TargetCharacterId);
            return result.Success
                ? Ok(new { message = UiStrings.PlayerBlocked })
                : BadRequest(new { message = result.ErrorMessage });
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = UiStrings.ErrorInternal });
        }
    }

    /// <summary>
    /// Unblocks a previously blocked character.
    /// </summary>
    [HttpDelete("block/{characterId:guid}")]
    public async Task<IActionResult> UnblockPlayer(Guid characterId)
    {
        try
        {
            if (!TryGetCharacterId(out var callerId))
                return BadRequest(new { message = UiStrings.ErrorInvalidCharacterId });

            var result = await _relationships.UnblockPlayerAsync(callerId, characterId);
            return result.Success
                ? Ok(new { message = UiStrings.PlayerUnblocked })
                : BadRequest(new { message = result.ErrorMessage });
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = UiStrings.ErrorInternal });
        }
    }

    /// <summary>
    /// Returns the caller's block list.
    /// </summary>
    [HttpGet("block")]
    public async Task<IActionResult> GetBlockList()
    {
        try
        {
            if (!TryGetCharacterId(out var characterId))
                return BadRequest(new { message = UiStrings.ErrorInvalidCharacterId });

            var blocked = await _relationships.GetBlockListAsync(characterId);
            return Ok(blocked);
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = UiStrings.ErrorInternal });
        }
    }

    // ────────────────────────────────────────────────────────────────────
    // Helper: Extract character ID from header
    // ────────────────────────────────────────────────────────────────────

    // TODO [Unity Integration]: Replace X-Character-Id header with authenticated identity from the game session.
    // Decision: Temporary header-based identification for development. In production, the character ID
    // will come from the authenticated session token after the auth system is implemented.
    private bool TryGetCharacterId(out Guid characterId)
    {
        characterId = Guid.Empty;
        var headerValue = Request.Headers["X-Character-Id"].FirstOrDefault();
        return !string.IsNullOrEmpty(headerValue) && Guid.TryParse(headerValue, out characterId);
    }
}
