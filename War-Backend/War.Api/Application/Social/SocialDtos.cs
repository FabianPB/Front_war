using System.ComponentModel.DataAnnotations;

namespace War.Api.Application.Social;

// Decision: DTOs are separate from Core domain records to keep API concerns (validation annotations,
// serialization shape) out of the domain layer. Mapping happens in the service layer.

// --- Request DTOs ---

public sealed class SendFriendRequestDto
{
    [Required(ErrorMessage = "El identificador del personaje objetivo es obligatorio.")]
    public Guid TargetCharacterId { get; set; }
}

public sealed class RespondFriendRequestDto
{
    [Required(ErrorMessage = "El identificador de la solicitud es obligatorio.")]
    public Guid RequestId { get; set; }

    [Required(ErrorMessage = "Debe indicar si acepta o rechaza la solicitud.")]
    public bool Accept { get; set; }
}

public sealed class BlockPlayerDto
{
    [Required(ErrorMessage = "El identificador del personaje a bloquear es obligatorio.")]
    public Guid TargetCharacterId { get; set; }
}

public sealed class UnblockPlayerDto
{
    [Required(ErrorMessage = "El identificador del personaje a desbloquear es obligatorio.")]
    public Guid TargetCharacterId { get; set; }
}

public sealed class RemoveFriendDto
{
    [Required(ErrorMessage = "El identificador del amigo a eliminar es obligatorio.")]
    public Guid FriendCharacterId { get; set; }
}

public sealed class SendChatMessageDto
{
    [Required(ErrorMessage = "El identificador del destinatario es obligatorio.")]
    public Guid RecipientCharacterId { get; set; }

    [Required(ErrorMessage = "El contenido del mensaje es obligatorio.")]
    [StringLength(500, ErrorMessage = "El mensaje no puede superar los 500 caracteres.")]
    public string Content { get; set; } = string.Empty;
}

// --- Response DTOs ---

public sealed class NearbyPlayerDto
{
    public Guid CharacterId { get; set; }
    public string CharacterName { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public int Level { get; set; }
}

public sealed class FriendListEntryDto
{
    public Guid CharacterId { get; set; }
    public string CharacterName { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public int Level { get; set; }
    public bool IsOnline { get; set; }
}

public sealed class BlockListEntryDto
{
    public Guid CharacterId { get; set; }
    public string CharacterName { get; set; } = string.Empty;
}

public sealed class PendingFriendRequestDto
{
    public Guid RequestId { get; set; }
    public Guid SenderCharacterId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public string SenderClassName { get; set; } = string.Empty;
    public int SenderLevel { get; set; }
    public DateTime SentAtUtc { get; set; }
}

public sealed class PublicProfileDto
{
    public Guid CharacterId { get; set; }
    public string CharacterName { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public int Level { get; set; }
    public int PowerScore { get; set; }
    public IReadOnlyList<PublicSkillSummaryDto> EquippedSkills { get; set; } = [];
    public IReadOnlyList<PublicEquipmentSummaryDto> EquippedItems { get; set; } = [];
    public IReadOnlyList<PublicSpiritSummaryDto> BoundSpirits { get; set; } = [];
}

public sealed class PublicSkillSummaryDto
{
    public string SkillId { get; set; } = string.Empty;
    public string SkillName { get; set; } = string.Empty;
    public int AscensionLevel { get; set; }
}

public sealed class PublicEquipmentSummaryDto
{
    public string SlotName { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public int EnhancementLevel { get; set; }
}

public sealed class PublicSpiritSummaryDto
{
    public string SpiritId { get; set; } = string.Empty;
    public string SpiritName { get; set; } = string.Empty;
    public int Level { get; set; }
}

// --- Generic Result Wrapper ---

// Decision: Simple result wrapper instead of exceptions for expected business-rule failures.
// Exceptions are reserved for truly unexpected errors. This keeps controller code clean with pattern matching.
public sealed class SocialOperationResult
{
    public bool Success { get; }
    public string? ErrorMessage { get; }

    private SocialOperationResult(bool success, string? errorMessage)
    {
        Success = success;
        ErrorMessage = errorMessage;
    }

    public static SocialOperationResult Ok() => new(true, null);
    public static SocialOperationResult Fail(string errorMessage) => new(false, errorMessage);
}
