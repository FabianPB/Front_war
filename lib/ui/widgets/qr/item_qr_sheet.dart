import 'package:flutter/material.dart';
import 'package:qr_flutter/qr_flutter.dart';
import '../../../models/store_item_model.dart';
import '../../../services/qr_service.dart';
import '../home/home_styles.dart';

class ItemQrSheet extends StatelessWidget {
  const ItemQrSheet({super.key, required this.item});

  final StoreItemModel item;

  static Future<void> show(BuildContext context, StoreItemModel item) {
    return showModalBottomSheet<void>(
      context: context,
      backgroundColor: homeSurfaceColor,
      isScrollControlled: true,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(24)),
      ),
      builder: (ctx) => ItemQrSheet(item: item),
    );
  }

  @override
  Widget build(BuildContext context) {
    final payload = const QrService().encodeItemId(item.id);

    return SafeArea(
      child: Padding(
        padding: const EdgeInsets.fromLTRB(24, 16, 24, 28),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Container(
              width: 40,
              height: 4,
              decoration: BoxDecoration(
                color: homeMutedText.withValues(alpha: 0.25),
                borderRadius: BorderRadius.circular(2),
              ),
            ),
            const SizedBox(height: 20),
            Row(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                Text(item.emoji, style: const TextStyle(fontSize: 28)),
                const SizedBox(width: 10),
                Flexible(
                  child: Text(
                    item.name.toUpperCase(),
                    style: const TextStyle(
                      color: homeText,
                      fontSize: 18,
                      fontWeight: FontWeight.w900,
                      letterSpacing: 1.5,
                      fontFamily: 'serif',
                    ),
                  ),
                ),
              ],
            ),
            const SizedBox(height: 4),
            Text(
              item.rarity,
              style: TextStyle(
                color: item.rarityColor,
                fontSize: 11,
                fontWeight: FontWeight.w700,
                letterSpacing: 1.5,
              ),
            ),
            const SizedBox(height: 20),
            Container(
              padding: const EdgeInsets.all(16),
              decoration: BoxDecoration(
                color: Colors.white,
                borderRadius: BorderRadius.circular(16),
                border: Border.all(color: homePrimary.withValues(alpha: 0.25)),
                boxShadow: [
                  BoxShadow(
                    color: homePrimary.withValues(alpha: 0.18),
                    blurRadius: 22,
                    spreadRadius: 1,
                  ),
                ],
              ),
              child: QrImageView(
                data: payload,
                version: QrVersions.auto,
                size: 240,
                backgroundColor: Colors.white,
                eyeStyle: const QrEyeStyle(
                  eyeShape: QrEyeShape.square,
                  color: homeText,
                ),
                dataModuleStyle: const QrDataModuleStyle(
                  dataModuleShape: QrDataModuleShape.square,
                  color: homeText,
                ),
              ),
            ),
            const SizedBox(height: 16),
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 8),
              decoration: BoxDecoration(
                color: homeMutedText.withValues(alpha: 0.08),
                borderRadius: BorderRadius.circular(6),
              ),
              child: Text(
                payload,
                style: const TextStyle(
                  color: homeMutedText,
                  fontSize: 11,
                  fontFamily: 'monospace',
                  letterSpacing: 0.5,
                ),
              ),
            ),
            const SizedBox(height: 14),
            const Text(
              'Muestra este código a otro jugador o escanéalo\ndesde la Armería para identificar el ítem',
              textAlign: TextAlign.center,
              style: TextStyle(color: homeMutedText, fontSize: 12, height: 1.5),
            ),
            const SizedBox(height: 20),
            SizedBox(
              width: double.infinity,
              child: ElevatedButton(
                onPressed: () => Navigator.of(context).pop(),
                style: ElevatedButton.styleFrom(
                  backgroundColor: homePrimary,
                  foregroundColor: Colors.white,
                  padding: const EdgeInsets.symmetric(vertical: 14),
                  shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
                ),
                child: const Text(
                  'CERRAR',
                  style: TextStyle(letterSpacing: 3, fontWeight: FontWeight.w700),
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
