namespace War.Core.Skills.Books;

/// <summary>
/// Definición inmutable de un libro de habilidad en el catálogo.
/// </summary>
/// <param name="DefinitionId">
/// ID único. Para el común universal: <c>book.common.knowledge</c>.
/// Para específicos: <c>book.{classKey}.{skillKey}.{rarity}</c>,
/// p. ej. <c>book.sorcerer.chispa-ignea.legendary</c>.
/// </param>
/// <param name="DisplayName">
/// Nombre épico mostrado al jugador. El sufijo de rareza se añade al final para los específicos,
/// p. ej. "Llamarada del Fénix Abisal · Legendario".
/// </param>
/// <param name="Rarity">Rareza del libro.</param>
/// <param name="SkillId">
/// ID de la skill a la que pertenece (solo para no-comunes). Null para el común universal.
/// Coincide con el <c>SkillDefinition.Id</c> del catálogo de habilidades.
/// </param>
public sealed record SkillBookDefinition(
    string DefinitionId,
    string DisplayName,
    SkillBookRarity Rarity,
    string? SkillId);
