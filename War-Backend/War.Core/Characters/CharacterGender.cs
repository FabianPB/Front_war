namespace War.Core.Characters;

/// <summary>
/// Género del personaje, elegido al crearlo y bloqueado para el resto de su vida útil
/// (coincide con la regla "el jugador elige clase al entrar y con ella juega hasta el final").
/// Determina la variante visual (♂/♀) del modelo base por clase. No afecta stats ni mecánicas.
/// </summary>
public enum CharacterGender
{
    Male = 0,
    Female = 1,
}

/// <summary>
/// Helpers para parsear el género que llega por SignalR (viene como string libre del cliente).
/// Nunca lanza excepciones; valores nulos, vacíos o desconocidos caen al default <see cref="CharacterGender.Male"/>.
/// </summary>
public static class CharacterGenderParser
{
    public const CharacterGender Default = CharacterGender.Male;

    /// <summary>
    /// Parsea un string case-insensitive aceptando alias en inglés y español, o un índice numérico.
    /// Ejemplos aceptados: "Male", "M", "Hombre", "Masculino", "0", "Female", "F", "Mujer", "Femenino", "1".
    /// </summary>
    public static CharacterGender ParseOrDefault(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return Default;
        return raw.Trim().ToLowerInvariant() switch
        {
            "male" or "m" or "hombre" or "masculino" or "0" => CharacterGender.Male,
            "female" or "f" or "mujer" or "femenino" or "1" => CharacterGender.Female,
            _ => Default,
        };
    }
}
