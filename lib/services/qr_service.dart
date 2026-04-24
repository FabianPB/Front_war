class QrService {
  const QrService();

  static const String itemPrefix = 'WAR-ITEM-';

  String encodeItemId(String itemId) => '$itemPrefix$itemId';

  /// Extracts the item id from a raw QR payload.
  /// Returns null if the payload doesn't match the `WAR-ITEM-<id>` format.
  String? parseItemId(String? raw) {
    if (raw == null) return null;
    final value = raw.trim();
    if (!value.startsWith(itemPrefix)) return null;
    final id = value.substring(itemPrefix.length).trim();
    if (id.isEmpty) return null;
    return id;
  }
}
