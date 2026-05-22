namespace War.Api.Localization;

// Decision: Centralized static class for all user-facing Spanish strings in the social system.
// This avoids scattering hardcoded strings across services and controllers, making future
// localization (if needed) a single-file change. Constants are compile-time resolved so there
// is zero runtime overhead compared to inline strings.
public static class UiStrings
{
    // ────────────────────────────────────────────────────────────────────
    // Chat
    // ────────────────────────────────────────────────────────────────────

    public const string ChatSenderNotIdentified =
        "No se pudo identificar al remitente. Reconecta al chat.";

    public const string ChatInternalError =
        "Error interno al enviar el mensaje. Intenta de nuevo.";

    public const string ChatMessageEmpty =
        "El mensaje esta vacio o contiene solo caracteres no permitidos.";

    public const string ChatSelfMessage =
        "No puedes enviarte un mensaje a ti mismo.";

    public const string ChatBlocked =
        "No puedes enviar mensajes a este jugador.";

    public const string ChatSenderNotFound =
        "Tu personaje no existe.";

    // ────────────────────────────────────────────────────────────────────
    // Friends
    // ────────────────────────────────────────────────────────────────────

    public const string FriendRequestSent =
        "Solicitud de amistad enviada correctamente.";

    public const string FriendRequestAccepted =
        "Solicitud de amistad aceptada.";

    public const string FriendRequestRejected =
        "Solicitud de amistad rechazada.";

    public const string FriendRemoved =
        "Amigo eliminado de tu lista.";

    public const string FriendRequestSelfTarget =
        "No puedes enviarte una solicitud de amistad a ti mismo.";

    public const string FriendRequestTargetNotFound =
        "El personaje objetivo no existe.";

    public const string FriendRequestBlockedRelationship =
        "No se puede enviar una solicitud de amistad a este jugador.";

    public const string FriendRequestAlreadyFriends =
        "Ya eres amigo de este jugador.";

    public const string FriendRequestAlreadyPending =
        "Ya existe una solicitud de amistad pendiente con este jugador.";

    public const string FriendRequestNotFound =
        "La solicitud de amistad no existe.";

    public const string FriendRequestNotAuthorized =
        "No tienes permiso para responder a esta solicitud.";

    public const string FriendRequestAlreadyResolved =
        "Esta solicitud ya ha sido resuelta.";

    public const string FriendRequestExpired =
        "Esta solicitud de amistad ha expirado.";

    public const string FriendSelfRemoval =
        "No puedes eliminarte a ti mismo de tu lista de amigos.";

    public const string FriendNotInList =
        "Este jugador no se encuentra en tu lista de amigos.";

    public const string FriendSenderLimitReached =
        "El remitente ha alcanzado su limite maximo de amigos.";

    // ────────────────────────────────────────────────────────────────────
    // Block
    // ────────────────────────────────────────────────────────────────────

    public const string PlayerBlocked =
        "Jugador bloqueado correctamente.";

    public const string PlayerUnblocked =
        "Jugador desbloqueado correctamente.";

    public const string BlockSelfTarget =
        "No puedes bloquearte a ti mismo.";

    public const string BlockTargetNotFound =
        "El personaje objetivo no existe.";

    public const string BlockAlreadyBlocked =
        "Este jugador ya esta bloqueado.";

    public const string UnblockSelfTarget =
        "No puedes desbloquearte a ti mismo.";

    public const string UnblockNotBlocked =
        "Este jugador no esta bloqueado.";

    // ────────────────────────────────────────────────────────────────────
    // Profile
    // ────────────────────────────────────────────────────────────────────

    public const string ProfileNotAvailable =
        "El perfil no esta disponible. Verifica que el jugador exista y se encuentre dentro del rango.";

    // ────────────────────────────────────────────────────────────────────
    // Proximity
    // ────────────────────────────────────────────────────────────────────

    public const string ProximityOutOfRange =
        "El jugador objetivo no se encuentra dentro del rango de interaccion.";

    public const string ProximityDefaultDenial =
        "Fuera de rango de interaccion.";

    // ────────────────────────────────────────────────────────────────────
    // General Errors
    // ────────────────────────────────────────────────────────────────────

    public const string ErrorInvalidCharacterId =
        "El identificador de personaje es invalido o no fue proporcionado. Envia el header X-Character-Id con un GUID valido.";

    public const string ErrorInternal =
        "Error interno del servidor. Intenta de nuevo mas tarde.";
}
