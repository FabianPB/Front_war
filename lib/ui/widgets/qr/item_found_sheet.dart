import 'package:flutter/material.dart';
import '../../../models/store_item_model.dart';
import '../home/home_styles.dart';

class ItemFoundSheet extends StatelessWidget {
  const ItemFoundSheet({super.key, required this.item});

  final StoreItemModel item;

  static Future<void> show(BuildContext context, StoreItemModel item) {
    return showModalBottomSheet<void>(
      context: context,
      backgroundColor: homeSurfaceColor,
      isScrollControlled: true,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(24)),
      ),
      builder: (ctx) => ItemFoundSheet(item: item),
    );
  }

  @override
  Widget build(BuildContext context) {
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
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 6),
              decoration: BoxDecoration(
                color: homePrimary.withValues(alpha: 0.12),
                borderRadius: BorderRadius.circular(20),
                border: Border.all(color: homePrimary.withValues(alpha: 0.3)),
              ),
              child: const Text(
                '✓ ÍTEM IDENTIFICADO',
                style: TextStyle(
                  color: homePrimary,
                  fontSize: 10,
                  fontWeight: FontWeight.w900,
                  letterSpacing: 2,
                ),
              ),
            ),
            const SizedBox(height: 24),
            Text(item.emoji, style: const TextStyle(fontSize: 64)),
            const SizedBox(height: 12),
            Text(
              item.name.toUpperCase(),
              textAlign: TextAlign.center,
              style: const TextStyle(
                color: homeText,
                fontSize: 22,
                fontWeight: FontWeight.w900,
                letterSpacing: 2,
                fontFamily: 'serif',
              ),
            ),
            const SizedBox(height: 8),
            Text(
              item.rarity,
              style: TextStyle(
                color: item.rarityColor,
                fontSize: 12,
                fontWeight: FontWeight.w700,
                letterSpacing: 1.5,
              ),
            ),
            const SizedBox(height: 16),
            Text(
              item.description,
              textAlign: TextAlign.center,
              style: const TextStyle(color: homeMutedText, fontSize: 13, height: 1.5),
            ),
            const SizedBox(height: 20),
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 18, vertical: 10),
              decoration: BoxDecoration(
                color: homeAccent.withValues(alpha: 0.1),
                borderRadius: BorderRadius.circular(8),
                border: Border.all(color: homeAccent.withValues(alpha: 0.35)),
              ),
              child: Row(
                mainAxisSize: MainAxisSize.min,
                children: [
                  const Icon(Icons.monetization_on_outlined, color: homeAccent, size: 18),
                  const SizedBox(width: 6),
                  Text(
                    '${item.price} monedas',
                    style: const TextStyle(
                      color: homeAccent,
                      fontWeight: FontWeight.w800,
                      fontSize: 14,
                    ),
                  ),
                ],
              ),
            ),
            const SizedBox(height: 24),
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
