import 'package:flutter/material.dart';
import '../../../models/store_item_model.dart';

const _wText = Color(0xFFE8E8E8);
const _wAccent = Color(0xFFE6451C);
const _wMuted = Color(0xFF888888);
const _wSurface = Color(0xFF1A1A1E);
const _wBorder = Color(0xFF2A2A2E);

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
          color: isActive
              ? _wAccent.withValues(alpha: 0.18)
              : Colors.transparent,
          border: Border.all(
            color: isActive
                ? _wAccent
                : _wBorder,
          ),
          borderRadius: BorderRadius.circular(4),
        ),
        child: Text(
          label.toUpperCase(),
          style: TextStyle(
            fontSize: 10,
            fontWeight: FontWeight.w700,
            letterSpacing: 1.2,
            color: isActive ? _wText : _wMuted,
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
        color: _wSurface,
        border: Border.all(color: _wBorder),
        borderRadius: BorderRadius.circular(8),
        boxShadow: [
          BoxShadow(
            color: _wAccent.withValues(alpha: 0.06),
            blurRadius: 18,
            offset: const Offset(0, 8),
          ),
        ],
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
                  color: _wText,
                ),
              ),
              const SizedBox(height: 4),
              Expanded(
                child: Text(
                  item.description,
                  textAlign: TextAlign.center,
                  style: TextStyle(
                    fontSize: 10,
                    color: _wMuted,
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
                      color: _wAccent,
                    ),
                  ),
                  Container(
                    padding:
                        const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                    decoration: BoxDecoration(
                      color: _wAccent.withValues(alpha: 0.14),
                      border: Border.all(
                          color: _wAccent.withValues(alpha: 0.4)),
                      borderRadius: BorderRadius.circular(4),
                    ),
                    child: const Text(
                      'COMPRAR',
                      style: TextStyle(
                        fontSize: 9,
                        fontWeight: FontWeight.w700,
                        letterSpacing: 1.0,
                        color: _wAccent,
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
                padding:
                    const EdgeInsets.symmetric(horizontal: 5, vertical: 2),
                decoration: BoxDecoration(
                  color: _wAccent.withValues(alpha: 0.18),
                  border:
                      Border.all(color: _wAccent.withValues(alpha: 0.4)),
                  borderRadius: BorderRadius.circular(3),
                ),
                child: Text(
                  item.badge!,
                  style: const TextStyle(
                    fontSize: 8,
                    fontWeight: FontWeight.w700,
                    letterSpacing: 0.8,
                    color: _wAccent,
                  ),
                ),
              ),
            ),
        ],
      ),
    );
  }
}
