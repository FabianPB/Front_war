namespace War.Core.Skills.Books;

/// <summary>
/// Rareza de un libro de habilidad. El libro común es universal (una sola definición).
/// A partir de Special, cada libro es específico de una habilidad concreta.
/// </summary>
public enum SkillBookRarity
{
    Common = 0,     // Universal: sirve para cualquier skill de cualquier clase, niveles 0→5.
    Special = 1,    // Específico por skill. Usado en niveles 5→7.
    Epic = 2,       // Específico por skill. Usado en niveles 7→9.
    Legendary = 3,  // Específico por skill. Usado en nivel 9→10.
}
