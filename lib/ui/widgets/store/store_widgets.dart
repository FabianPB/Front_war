import 'package:flutter/material.dart';
import '../../../models/store_item_model.dart';

class StoreFilterChip extends StatelessWidget {
  const StoreFilterChip({
    super.key,
    required this.label,
    required this.isActive,
    required this.onTap,
  });

  final String label;
  final bool isActive;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return InkWell(
      onTap: onTap,
      child: AnimatedContainer(
        duration: const Duration(milliseconds: 200),
        padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
        decoration: BoxDecoration(
          color: isActive ? const Color.fromRGBO(192, 0, 26, 0.2) : Colors.transparent,
          border: Border.all(
            color: isActive ? const Color(0xFFC0001A) : const Color.fromRGBO(192, 0, 26, 0.25),
          ),
          borderRadius: BorderRadius.circular(2),
        ),
        child: Text(
          label.toUpperCase(),
          style: TextStyle(
            fontSize: 10,
            fontWeight: FontWeight.w700,
            letterSpacing: 1.2,
            color: isActive ? const Color(0xFFF5E8E8) : const Color.fromRGBO(200, 170, 170, 0.5),
          ),
        ),
      ),
    );
  }
}

class StoreItemCard extends StatelessWidget {
  const StoreItemCard({super.key, required this.item});

  final StoreItemModel item;

  @override
  Widget build(BuildContext context) {
    return Container(
      decoration: BoxDecoration(
        color: const Color.fromRGBO(14, 3, 5, 0.85),
        border: Border.all(color: const Color.fromRGBO(192, 0, 26, 0.18)),
        borderRadius: BorderRadius.circular(3),
      ),
      padding: const EdgeInsets.fromLTRB(8, 16, 8, 12),
      child: Stack(
        clipBehavior: Clip.none,
        children: [
          Column(
            crossAxisAlignment: CrossAxisAlignment.center,
            children: [
              Text(item.emoji, style: const TextStyle(fontSize: 38)),
              const SizedBox(height: 8),
              Text(
                item.name.toUpperCase(),
                textAlign: TextAlign.center,
                style: const TextStyle(
                  fontFamily: 'serif',
                  fontSize: 11,
                  fontWeight: FontWeight.w700,
                  letterSpacing: 1.0,
                  color: Color(0xFFF5E8E8),
                ),
              ),
              const SizedBox(height: 4),
              Expanded(
                child: Text(
                  item.description,
                  textAlign: TextAlign.center,
                  style: const TextStyle(
                    fontSize: 10,
                    color: Color.fromRGBO(200, 170, 170, 0.45),
                    height: 1.35,
                  ),
                ),
              ),
              const SizedBox(height: 4),
              Text(
                item.rarity.toUpperCase(),
                textAlign: TextAlign.center,
                style: TextStyle(
                  fontSize: 9,
                  fontWeight: FontWeight.w700,
                  letterSpacing: 1.0,
                  color: item.rarityColor,
                ),
              ),
              const SizedBox(height: 10),
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Text(
                    '💰 ${item.price}',
                    style: const TextStyle(
                      fontSize: 12,
                      fontWeight: FontWeight.w700,
                      color: Color(0xFFD4A03A),
                    ),
                  ),
                  Container(
                    padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                    decoration: BoxDecoration(
                      color: const Color.fromRGBO(192, 0, 26, 0.25),
                      border: Border.all(color: const Color.fromRGBO(192, 0, 26, 0.45)),
                      borderRadius: BorderRadius.circular(2),
                    ),
                    child: const Text(
                      'COMPRAR',
                      style: TextStyle(
                        fontSize: 9,
                        fontWeight: FontWeight.w700,
                        letterSpacing: 1.0,
                        color: Color(0xFFFF1A2E),
                      ),
                    ),
                  ),
                ],
              ),
            ],
          ),
          if (item.badge != null)
            Positioned(
              top: -8,
              right: 0,
              child: Container(
                padding: const EdgeInsets.symmetric(horizontal: 5, vertical: 2),
                decoration: BoxDecoration(
                  color: const Color(0xFF7A0010),
                  border: Border.all(color: const Color.fromRGBO(255, 30, 50, 0.3)),
                  borderRadius: BorderRadius.circular(2),
                ),
                child: Text(
                  item.badge!,
                  style: const TextStyle(
                    fontSize: 8,
                    fontWeight: FontWeight.w700,
                    letterSpacing: 0.8,
                    color: Color(0xFFFF1A2E),
                  ),
                ),
              ),
            ),
        ],
      ),
    );
  }
}
