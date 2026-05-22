namespace War.Core.Economy;

/// <summary>
/// Razón de cada movimiento de moneda. Toda transacción debe declarar su origen.
/// </summary>
/// <remarks>
/// Decision: enum cerrado con etiquetas concretas. Si aparece una nueva fuente de moneda (evento,
/// raid, sistema de guerra entre gremios, etc.) se añade aquí, nunca se pasa un string suelto.
/// El auditor puede agrupar ingresos/egresos por fuente con absoluta precisión.
///
/// Convención: valores &lt; 100 son ingresos (Credit). Valores ≥ 100 son egresos (Debit).
/// El PlayerWallet usa esta convención para validar que la operación coincide con la fuente declarada.
/// </remarks>
public enum TransactionSource
{
    // ── Entradas (Credit · [0-99]) ───────────────────────────────────────────
    MobDrop = 0,
    QuestReward = 1,
    EventReward = 2,
    AchievementReward = 3,
    ItemSale = 4,
    MailAttachment = 5,
    RefundCancelled = 6,
    AdminGrant = 8,                 // soporte / corrección manual
    CurrencyConversionIn = 10,      // plata/oro obtenido por conversión
    Meditation = 11,                // energía (y plata rara) por meditar en fuentes
    GatheringPlants = 12,           // plata rara al recolectar plantas
    MiningMinerals = 13,            // plata rara al excavar minerales

    // ── Salidas (Debit · [100-199]) ──────────────────────────────────────────
    CraftingTierUp = 100,
    CraftingDevelopment = 101,
    InventoryExpansion = 102,
    SkillAscension = 103,
    ShopPurchase = 104,
    RepairFee = 105,
    MailCost = 106,
    EnergyConsumed = 107,
    AdminDeduct = 108,
    CurrencyConversionOut = 110,    // cobre/plata consumido al convertir
    ChapelUpgrade = 111,            // coste de subir la Capilla de Economía
    BodyTraining = 120,             // futuro: entrenamiento de cuerpo
    SoulTraining = 121,             // futuro: entrenamiento de alma
    SpiritTraining = 122,           // futuro: entrenamiento de espíritu
}
