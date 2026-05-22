using System.Collections.ObjectModel;

namespace War.Core.Skills.Books;

/// <summary>
/// Catálogo inmutable de los 157 libros de habilidad del juego.
/// </summary>
/// <remarks>
///   · 1 libro común universal (el de niveles 0→5 de cualquier skill).
///   · 52 nombres base × 3 raridades (Special/Epic/Legendary) = 156 libros específicos.
///   · Total: <see cref="TotalBookCount"/> = 157 DefinitionIds únicos.
///
/// Los nombres base son épicos y dramáticos, uno por habilidad. La rareza se agrega al display
/// como sufijo (" · Especial", " · Épico", " · Legendario"). Las ultimates (slot 13 de cada
/// clase) tienen sus propios nombres por coherencia de sabor.
/// </remarks>
public static class SkillBookCatalog
{
    public const string CommonBookDefinitionId = "book.common.knowledge";
    public const string CommonBookDisplayName  = "Libro Común del Conocimiento";

    private static readonly Dictionary<string, SkillBookDefinition> _byId;
    private static readonly ReadOnlyCollection<SkillBookDefinition> _all;

    static SkillBookCatalog()
    {
        var all = new List<SkillBookDefinition>(capacity: 1 + 52 * 3);

        // ── Común universal ──
        all.Add(new SkillBookDefinition(
            DefinitionId: CommonBookDefinitionId,
            DisplayName: CommonBookDisplayName,
            Rarity: SkillBookRarity.Common,
            SkillId: null));

        // ── Específicos: 52 bases × 3 raridades ──
        foreach (var (skillId, baseName) in BaseNamesBySkillId)
        {
            foreach (var rarity in new[] { SkillBookRarity.Special, SkillBookRarity.Epic, SkillBookRarity.Legendary })
            {
                all.Add(new SkillBookDefinition(
                    DefinitionId: BuildDefinitionId(skillId, rarity),
                    DisplayName: $"{baseName} · {RaritySuffix(rarity)}",
                    Rarity: rarity,
                    SkillId: skillId));
            }
        }

        _byId = all.ToDictionary(b => b.DefinitionId);
        _all = new ReadOnlyCollection<SkillBookDefinition>(all);
    }

    public static IReadOnlyCollection<SkillBookDefinition> All => _all;

    public static int TotalBookCount => _all.Count;

    public static SkillBookDefinition Get(string definitionId) =>
        _byId.TryGetValue(definitionId, out var b)
            ? b
            : throw new KeyNotFoundException($"Libro de habilidad desconocido: {definitionId}");

    public static SkillBookDefinition? TryGet(string definitionId) =>
        _byId.TryGetValue(definitionId, out var b) ? b : null;

    /// <summary>
    /// Devuelve el DefinitionId del libro específico de una skill en una rareza dada.
    /// Lanza si la skill no tiene nombre base mapeado (debería tenerlo para las 52 oficiales).
    /// </summary>
    public static string GetSpecificBookId(string skillId, SkillBookRarity rarity)
    {
        if (rarity == SkillBookRarity.Common)
            return CommonBookDefinitionId;
        if (!BaseNamesBySkillId.ContainsKey(skillId))
            throw new ArgumentException($"Skill sin libro específico mapeado: {skillId}.", nameof(skillId));
        return BuildDefinitionId(skillId, rarity);
    }

    // ── Internos ────────────────────────────────────────────────────────────

    private static string BuildDefinitionId(string skillId, SkillBookRarity rarity)
    {
        var rarityKey = rarity switch
        {
            SkillBookRarity.Special   => "special",
            SkillBookRarity.Epic      => "epic",
            SkillBookRarity.Legendary => "legendary",
            _ => throw new ArgumentOutOfRangeException(nameof(rarity))
        };
        return $"book.{skillId}.{rarityKey}";
    }

    private static string RaritySuffix(SkillBookRarity rarity) => rarity switch
    {
        SkillBookRarity.Special   => "Especial",
        SkillBookRarity.Epic      => "Épico",
        SkillBookRarity.Legendary => "Legendario",
        _ => throw new ArgumentOutOfRangeException(nameof(rarity))
    };

    // ── Los 52 nombres épicos ───────────────────────────────────────────────
    private static readonly Dictionary<string, string> BaseNamesBySkillId = new()
    {
        // ── Sorcerer (13) ───────────────────────────────────────────────────
        { "sorcerer.skill.chispa-ignea",              "Llamarada del Fénix Abisal" },
        { "sorcerer.skill.anillo-incandescente",      "Corona Ardiente de Helios" },
        { "sorcerer.skill.colapso-termal",            "Epílogo del Sol Moribundo" },
        { "sorcerer.skill.meteorito-escarlata",       "Caída del Cielo Sangrante" },
        { "sorcerer.skill.lanza-glacial",             "Lanza del Dios Congelado" },
        { "sorcerer.skill.nucleo-criogenico",         "Corazón del Invierno Eterno" },
        { "sorcerer.skill.prision-de-escarcha",       "Mausoleo de Hielo Azul" },
        { "sorcerer.skill.pulso-glacial",             "Respiración del Titán Blanco" },
        { "sorcerer.skill.cadena-de-trueno",          "Grilletes de Zeus Furibundo" },
        { "sorcerer.skill.descarga-de-arco",          "Aullido del Trueno Primigenio" },
        { "sorcerer.skill.orbe-voltaico",             "Ojo de la Tormenta Divina" },
        { "sorcerer.skill.tormenta-fractal",          "Sinfonía del Apocalipsis Azul" },
        { "sorcerer.ultimate.tornado-dragon",         "Despertar del Dragón de Mil Tempestades" },

        // ── Juramentada (13) ────────────────────────────────────────────────
        { "juramentada.skill.golpe-sagrado",          "Juramento del Alba Dorada" },
        { "juramentada.skill.resplandor-sanador",     "Lágrima de la Diosa Errante" },
        { "juramentada.skill.bendicion-de-batalla",   "Estandarte del Voto Imperecedero" },
        { "juramentada.skill.cadenas-de-luz",         "Grilletes del Sol Vengador" },
        { "juramentada.skill.juicio-radiante",        "Veredicto del Ocaso Sagrado" },
        { "juramentada.skill.onda-de-juicio",         "Clamor del Trono Celeste" },
        { "juramentada.skill.sentencia-divina",       "Última Voluntad del Empíreo" },
        { "juramentada.skill.laceracion-impia",       "Caricia del Ángel Caído" },
        { "juramentada.skill.marca-de-corrupcion",    "Sello del Pecado Original" },
        { "juramentada.skill.colapso-espiritual",     "Susurro del Alma Rota" },
        { "juramentada.skill.flagelo-purificador",    "Látigo de la Redención Amarga" },
        { "juramentada.skill.plaga-purificadora",     "Aliento del Dios Leproso" },
        { "juramentada.ultimate.avatar-del-juramento","Encarnación del Pacto Eterno" },

        // ── Lancero (13) ────────────────────────────────────────────────────
        { "lancero.skill.estocada-veloz",             "Arte del Viento Afilado" },
        { "lancero.skill.descarga-de-asta",           "Ráfaga del Cazador Silencioso" },
        { "lancero.skill.remolino-de-asta",           "Danza del Huracán Plateado" },
        { "lancero.skill.empalamiento",               "Ley del Hierro Vertical" },
        { "lancero.skill.perforacion-vital",          "Senda del Corazón Roto" },
        { "lancero.skill.lanza-celestial",            "Descenso de la Estrella Lancera" },
        { "lancero.skill.lluvia-de-espinas",          "Manto de Agujas del Infierno" },
        { "lancero.skill.tormenta-de-lanzas",         "Diluvio del Guerrero Errante" },
        { "lancero.skill.relampago-de-lanza",         "Asta del Rayo Imparable" },
        { "lancero.skill.cadena-de-rayos",            "Cadena del Dios Eléctrico" },
        { "lancero.skill.punta-envenenada",           "Beso del Áspid Carmesí" },
        { "lancero.skill.erupcion-toxica",            "Vómito de la Serpiente Negra" },
        { "lancero.ultimate.dragon-de-mil-lanzas",    "Advenimiento del Wyvern de Astas Infinitas" },

        // ── Bruiser (13) ────────────────────────────────────────────────────
        { "bruiser.skill.puno-de-hierro",             "Doctrina del Puño Inquebrantable" },
        { "bruiser.skill.golpe-demoledor",            "Martillo del Coloso Rabioso" },
        { "bruiser.skill.impacto-devastador",         "Ira del Meteoro de Puño" },
        { "bruiser.skill.martillo-de-guerra",         "Cántico del Yunque Ensangrentado" },
        { "bruiser.skill.fractura-de-armadura",       "Técnica del Desquebraje Divino" },
        { "bruiser.skill.rafaga-de-golpes",           "Tambor del Guerrero sin Piedad" },
        { "bruiser.skill.embestida-acorazada",        "Carrera del Toro Milenario" },
        { "bruiser.skill.rugido-de-furia",            "Bramido del León Inmortal" },
        { "bruiser.skill.onda-sismica",               "Temblor del Gigante Dormido" },
        { "bruiser.skill.erupcion-de-ira",            "Estallido del Volcán Viviente" },
        { "bruiser.skill.tempestad-de-furia",         "Huracán del Campeón Rabioso" },
        { "bruiser.skill.cataclismo",                 "Fin del Mundo de los Mortales" },
        { "bruiser.ultimate.titan-de-guerra",         "Resurrección del Titán Primordial" },
    };
}
