namespace War.Core.Equipment;

// ══════════════════════════════════════════════════════════════════
// TIPOS DE OBJETO EN EL INVENTARIO
// ══════════════════════════════════════════════════════════════════

/// <summary>
/// Tipos de objetos que pueden existir en el inventario.
/// Cada tipo tiene reglas de apilamiento distintas.
/// </summary>
public enum InventoryItemType
{
    /// <summary>Equipamiento (arma, armadura, joyería). UNO por casilla, NO apilable.</summary>
    Equipment,

    /// <summary>Gema. UNA por casilla, NO apilable (como equipamiento).</summary>
    Gem,

    /// <summary>Recurso (materiales de crafteo, moneda, etc.). Apilable en una sola casilla.</summary>
    Resource,

    /// <summary>Espíritu, libro, objeto especial. Apilable en una sola casilla por subtipo.</summary>
    Special
}

/// <summary>
/// Reglas de apilamiento derivadas del tipo de objeto.
/// </summary>
public static class InventoryStackingRules
{
    /// <summary>
    /// Determina si un tipo de objeto puede apilar múltiples unidades en una casilla.
    /// Equipment y Gem: NO (1 por casilla).
    /// Resource y Special: SÍ (N por casilla del mismo subtipo).
    /// </summary>
    public static bool IsStackable(InventoryItemType type)
    {
        return type is InventoryItemType.Resource or InventoryItemType.Special;
    }

    /// <summary>Cantidad máxima por stack. No-apilables siempre son 1.</summary>
    public static int MaxStackSize(InventoryItemType type)
    {
        return type switch
        {
            InventoryItemType.Equipment => 1,
            InventoryItemType.Gem => 1,
            InventoryItemType.Resource => 9999,
            InventoryItemType.Special => 999,
            _ => 1
        };
    }
}

// ══════════════════════════════════════════════════════════════════
// ITEM EN INVENTARIO
// ══════════════════════════════════════════════════════════════════

/// <summary>
/// Un ítem concreto en el inventario del jugador. Cada ítem ocupa
/// exactamente una casilla (slot) del inventario.
///
/// Para Equipment/Gem: quantity es siempre 1.
/// Para Resource/Special: quantity puede ser > 1 (stack).
///
/// Un equipo "equipado" tiene IsEquipped = true pero SIGUE en el
/// inventario. No se duplica, no se saca. Solo se marca.
/// </summary>
public sealed class InventoryItem
{
    /// <summary>ID único e irrepetible de este ítem. Generado server-side.</summary>
    public string ItemId { get; init; } = Guid.NewGuid().ToString("N");

    /// <summary>Tipo de objeto (determina reglas de apilamiento).</summary>
    public required InventoryItemType ItemType { get; init; }

    /// <summary>
    /// ID de la definición del catálogo (e.g., "weapon.sorcerer.common.offensive").
    /// Para recursos: el ID del tipo de recurso (e.g., "resource.iron-ore").
    /// </summary>
    public required string DefinitionId { get; init; }

    /// <summary>Cantidad en este stack. Para Equipment/Gem siempre es 1.</summary>
    public int Quantity { get; set; } = 1;

    /// <summary>Índice de la casilla en el inventario (0-based).</summary>
    public int SlotIndex { get; set; }

    // ── Solo para Equipment ──

    /// <summary>Tier actual del equipo (1-4). Solo aplica a Equipment.</summary>
    public int Tier { get; set; } = 1;

    /// <summary>Nivel de desarrollo actual (1-30). Solo aplica a Equipment.</summary>
    public int DevelopmentLevel { get; set; } = 1;

    /// <summary>
    /// ¿Está equipado en el personaje? Si true, ocupa el slot de equipo
    /// correspondiente. Solo puede haber 1 equipo por slot del personaje.
    /// El ítem SIGUE en el inventario (no se saca). Solo se marca.
    /// </summary>
    public bool IsEquipped { get; set; }

    /// <summary>
    /// Slot de equipamiento donde está equipado (solo válido si IsEquipped = true
    /// y ItemType = Equipment). Null si no está equipado.
    /// </summary>
    public EquipmentSlot? EquippedSlot { get; set; }

    /// <summary>Timestamp de cuando el ítem fue creado/obtenido. Auditoría.</summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>Timestamp de la última modificación (desarrollo, crafteo). Auditoría.</summary>
    public DateTime LastModifiedAt { get; set; } = DateTime.UtcNow;
}

// ══════════════════════════════════════════════════════════════════
// INVENTARIO DEL JUGADOR
// ══════════════════════════════════════════════════════════════════

/// <summary>
/// Inventario completo de un jugador. Contiene todos sus ítems
/// organizados en casillas indexadas. El inventario es la FUENTE
/// DE VERDAD — los equipos "equipados" son marcas sobre ítems
/// que ya están aquí dentro.
///
/// Capacidad: inicial 80, expandible en lotes de 50 hasta 280 con recursos del juego.
/// La capacidad efectiva vive en <see cref="Capacity"/>; la cuenta de expansiones
/// ya compradas vive en <see cref="ExpansionsPurchased"/>. Ambas se mantienen
/// sincronizadas por <see cref="Expand"/>.
/// </summary>
public sealed class PlayerInventory
{
    /// <summary>Capacidad inicial del inventario al crear un personaje.</summary>
    public const int DefaultCapacity = InventoryExpansionCostCalculator.InitialCapacity; // 80

    private readonly Dictionary<string, InventoryItem> _itemsById = new();
    private readonly Dictionary<int, InventoryItem> _itemsBySlot = new();

    /// <summary>
    /// Capacidad actual (slots totales disponibles). Arranca en <see cref="DefaultCapacity"/> y
    /// crece en lotes de 50 al ejecutar <see cref="Expand"/>.
    /// </summary>
    public int Capacity { get; private set; } = DefaultCapacity;

    /// <summary>Cuántas expansiones se han comprado (0..<see cref="InventoryExpansionCostCalculator.MaxExpansions"/>).</summary>
    public int ExpansionsPurchased { get; private set; }

    /// <summary>
    /// Expande la capacidad en +<see cref="InventoryExpansionCostCalculator.ExpansionBatchSize"/> slots.
    /// NO cobra recursos — la validación y el cobro los hace la capa de aplicación
    /// (PlayerWalletService) antes de llamar aquí. Este método es el "último escalón"
    /// que solo mueve números, una vez aceptada la transacción del wallet.
    /// </summary>
    /// <returns>true si se expandió; false si ya estaba al tope.</returns>
    public bool Expand()
    {
        if (!InventoryExpansionCostCalculator.CanExpandFurther(ExpansionsPurchased))
            return false;

        ExpansionsPurchased++;
        Capacity = InventoryExpansionCostCalculator.GetCapacityAfter(ExpansionsPurchased);
        return true;
    }

    /// <summary>Todos los ítems en el inventario.</summary>
    public IReadOnlyCollection<InventoryItem> AllItems => _itemsById.Values;

    /// <summary>Cantidad de casillas ocupadas.</summary>
    public int UsedSlots => _itemsBySlot.Count;

    /// <summary>Casillas libres.</summary>
    public int FreeSlots => Capacity - UsedSlots;

    // ── QUERIES ──

    public InventoryItem? GetById(string itemId)
        => _itemsById.TryGetValue(itemId, out var item) ? item : null;

    public InventoryItem? GetBySlot(int slotIndex)
        => _itemsBySlot.TryGetValue(slotIndex, out var item) ? item : null;

    /// <summary>
    /// Busca el ítem equipado en un slot de equipamiento específico.
    /// Solo puede haber 0 o 1 por slot.
    /// </summary>
    public InventoryItem? GetEquippedInSlot(EquipmentSlot equipSlot)
    {
        return _itemsById.Values.FirstOrDefault(i =>
            i.IsEquipped && i.EquippedSlot == equipSlot);
    }

    /// <summary>Todos los ítems actualmente equipados.</summary>
    public IReadOnlyList<InventoryItem> GetAllEquipped()
    {
        return _itemsById.Values.Where(i => i.IsEquipped).ToArray();
    }

    /// <summary>Busca un stack existente de un recurso/especial apilable.</summary>
    public InventoryItem? FindExistingStack(string definitionId, InventoryItemType type)
    {
        if (!InventoryStackingRules.IsStackable(type)) return null;
        return _itemsById.Values.FirstOrDefault(i =>
            i.DefinitionId == definitionId && i.ItemType == type);
    }

    // ── MUTATIONS (todas con validación) ──

    /// <summary>
    /// Añade un ítem al inventario. Si es apilable y ya existe un stack
    /// del mismo tipo, incrementa la cantidad. Si no, ocupa una casilla nueva.
    ///
    /// Devuelve el ítem añadido/actualizado, o null si no hay espacio.
    /// </summary>
    public InventoryItem? AddItem(InventoryItem item)
    {
        // Apilable: buscar stack existente
        if (InventoryStackingRules.IsStackable(item.ItemType))
        {
            var existing = FindExistingStack(item.DefinitionId, item.ItemType);
            if (existing is not null)
            {
                var maxStack = InventoryStackingRules.MaxStackSize(item.ItemType);
                var canAdd = Math.Min(item.Quantity, maxStack - existing.Quantity);
                if (canAdd <= 0) return null; // stack lleno
                existing.Quantity += canAdd;
                existing.LastModifiedAt = DateTime.UtcNow;
                return existing;
            }
        }

        // No apilable o no hay stack existente: buscar casilla libre
        var freeSlot = FindFreeSlot();
        if (freeSlot < 0) return null; // inventario lleno

        // Validación: Equipment/Gem siempre cantidad 1
        if (!InventoryStackingRules.IsStackable(item.ItemType))
        {
            item.Quantity = 1;
        }

        item.SlotIndex = freeSlot;
        _itemsById[item.ItemId] = item;
        _itemsBySlot[freeSlot] = item;
        return item;
    }

    /// <summary>
    /// Remueve un ítem por su ID. Si es apilable, reduce la cantidad.
    /// Si la cantidad llega a 0, libera la casilla.
    ///
    /// Devuelve true si se removió correctamente.
    /// FALLA si el ítem está equipado (hay que desequiparlo primero).
    /// </summary>
    public bool RemoveItem(string itemId, int quantity = 1)
    {
        if (!_itemsById.TryGetValue(itemId, out var item)) return false;

        // No se puede remover un ítem equipado (anti-exploit)
        if (item.IsEquipped) return false;

        if (InventoryStackingRules.IsStackable(item.ItemType) && item.Quantity > quantity)
        {
            item.Quantity -= quantity;
            item.LastModifiedAt = DateTime.UtcNow;
            return true;
        }

        // Remover completamente
        _itemsById.Remove(itemId);
        _itemsBySlot.Remove(item.SlotIndex);
        return true;
    }

    /// <summary>
    /// Remueve un ítem forzosamente (sin verificar IsEquipped).
    /// DEBE usarse SÓLO desde la capa de aplicación en operaciones atómicas ya validadas:
    /// · Crafteo tier-up (consume la segunda pieza).
    /// · Ventas a NPC / reciclado.
    /// · Correcciones administrativas.
    /// La capa que llama es responsable de haber desequipado/validado antes.
    /// </summary>
    public bool ForceRemoveItem(string itemId)
    {
        if (!_itemsById.TryGetValue(itemId, out var item)) return false;
        _itemsById.Remove(itemId);
        _itemsBySlot.Remove(item.SlotIndex);
        return true;
    }

    /// <summary>
    /// Marca un ítem de equipamiento como "equipado" en un slot del personaje.
    ///
    /// Validaciones:
    ///   1. El ítem debe existir en el inventario
    ///   2. Debe ser de tipo Equipment
    ///   3. El slot objetivo debe coincidir con el slot del equipo
    ///   4. No puede haber otro equipo ya en ese slot
    ///
    /// Devuelve true si se equipó, false si falló alguna validación.
    /// </summary>
    public bool EquipItem(string itemId, EquipmentSlot targetSlot)
    {
        if (!_itemsById.TryGetValue(itemId, out var item)) return false;
        if (item.ItemType != InventoryItemType.Equipment) return false;
        if (item.IsEquipped) return false; // ya está equipado

        // Verificar que no haya otro equipo en ese slot
        var currentlyEquipped = GetEquippedInSlot(targetSlot);
        if (currentlyEquipped is not null) return false; // hay que desequipar primero

        // Verificar que la definición del equipo corresponde al slot
        var definition = EquipmentCatalog.Get(item.DefinitionId);
        if (definition.Slot != targetSlot) return false;

        item.IsEquipped = true;
        item.EquippedSlot = targetSlot;
        item.LastModifiedAt = DateTime.UtcNow;
        return true;
    }

    /// <summary>
    /// Desequipa un ítem. El ítem permanece en el inventario pero
    /// pierde la marca de equipado.
    /// </summary>
    public bool UnequipItem(string itemId)
    {
        if (!_itemsById.TryGetValue(itemId, out var item)) return false;
        if (!item.IsEquipped) return false;

        item.IsEquipped = false;
        item.EquippedSlot = null;
        item.LastModifiedAt = DateTime.UtcNow;
        return true;
    }

    /// <summary>
    /// Intercambia el equipo de un slot: desequipa el actual y equipa el nuevo
    /// en una operación atómica. Ambos permanecen en el inventario.
    /// </summary>
    public bool SwapEquipment(string newItemId, EquipmentSlot targetSlot)
    {
        if (!_itemsById.TryGetValue(newItemId, out var newItem)) return false;
        if (newItem.ItemType != InventoryItemType.Equipment) return false;

        var definition = EquipmentCatalog.Get(newItem.DefinitionId);
        if (definition.Slot != targetSlot) return false;

        // Desequipar actual si hay uno
        var current = GetEquippedInSlot(targetSlot);
        if (current is not null)
        {
            current.IsEquipped = false;
            current.EquippedSlot = null;
            current.LastModifiedAt = DateTime.UtcNow;
        }

        // Equipar nuevo
        if (newItem.IsEquipped)
        {
            // Ya estaba equipado en otro slot — desequipar de ahí primero
            newItem.IsEquipped = false;
            newItem.EquippedSlot = null;
        }

        newItem.IsEquipped = true;
        newItem.EquippedSlot = targetSlot;
        newItem.LastModifiedAt = DateTime.UtcNow;
        return true;
    }

    // ── HELPERS INTERNOS ──

    private int FindFreeSlot()
    {
        for (var i = 0; i < Capacity; i++)
        {
            if (!_itemsBySlot.ContainsKey(i)) return i;
        }
        return -1; // inventario lleno
    }
}
