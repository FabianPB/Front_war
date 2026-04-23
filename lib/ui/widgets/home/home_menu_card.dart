import 'package:flutter/material.dart';

import '../../../ui/screens/chat_screen.dart';
import '../../../ui/screens/form_screen.dart';
import '../../../ui/screens/game_screen.dart';
import '../../../ui/screens/store_screen.dart';
import 'home_styles.dart';

class HomeMenuCard extends StatelessWidget {
  const HomeMenuCard({super.key, required this.context});

  final BuildContext context;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: 400,
      decoration: BoxDecoration(
        gradient: const LinearGradient(
          colors: [Color.fromRGBO(20, 3, 6, 0.95), Color.fromRGBO(12, 2, 4, 0.98)],
          begin: Alignment.topLeft,
          end: Alignment.bottomRight,
        ),
        border: Border.all(color: const Color.fromRGBO(192, 0, 26, 0.3)),
        borderRadius: BorderRadius.circular(4),
        boxShadow: const [
          BoxShadow(color: Color.fromRGBO(255, 0, 30, 0.08), spreadRadius: 1),
          BoxShadow(color: Colors.black87, blurRadius: 40, offset: Offset(0, 20)),
        ],
      ),
      child: Stack(
        children: [
          Positioned(top: 8, left: 8, child: _Corner(isTop: true, isLeft: true)),
          Positioned(top: 8, right: 8, child: _Corner(isTop: true, isLeft: false)),
          Positioned(bottom: 8, left: 8, child: _Corner(isTop: false, isLeft: true)),
          Positioned(bottom: 8, right: 8, child: _Corner(isTop: false, isLeft: false)),
          Padding(
            padding: const EdgeInsets.all(32),
            child: Column(
              children: [
                _NavListItem(
                  icon: Icons.storefront,
                  label: 'Tienda',
                  badge: 'NUEVO',
                  onTap: () => Navigator.of(context).push(MaterialPageRoute(builder: (_) => const StoreScreen())),
                ),
                const SizedBox(height: 6),
                _NavListItem(
                  icon: Icons.chat_bubble_outline,
                  label: 'Chat',
                  badge: '12',
                  onTap: () => Navigator.of(context).push(MaterialPageRoute(builder: (_) => const ChatScreen())),
                ),
                const SizedBox(height: 6),
                _NavListItem(
                  icon: Icons.flash_on,
                  label: 'Eventos',
                  badge: 'LIVE',
                  onTap: () => Navigator.of(context).push(MaterialPageRoute(builder: (_) => const GameScreen())),
                ),
                const SizedBox(height: 6),
                _NavListItem(
                  icon: Icons.security,
                  label: 'Soporte',
                  onTap: () => Navigator.of(context).push(MaterialPageRoute(builder: (_) => const FormScreen())),
                ),
                Padding(
                  padding: const EdgeInsets.symmetric(vertical: 20),
                  child: Container(
                    height: 1,
                    decoration: const BoxDecoration(
                      gradient: LinearGradient(
                        colors: [Colors.transparent, Color.fromRGBO(192, 0, 26, 0.3), Colors.transparent],
                      ),
                    ),
                  ),
                ),
                SizedBox(
                  width: double.infinity,
                  child: ElevatedButton.icon(
                    onPressed: () => Navigator.of(context).push(MaterialPageRoute(builder: (_) => const GameScreen())),
                    style: ElevatedButton.styleFrom(
                      padding: const EdgeInsets.symmetric(vertical: 18),
                      backgroundColor: Colors.transparent,
                      shadowColor: Colors.transparent,
                      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(3)),
                    ).copyWith(
                      elevation: WidgetStateProperty.all(0),
                    ),
                    icon: const Icon(Icons.sports_kabaddi, color: Colors.white),
                    label: const Text(
                      '¡COMIENZA LA BATALLA!',
                      style: TextStyle(
                        fontFamily: 'serif',
                        fontWeight: FontWeight.w900,
                        letterSpacing: 2,
                        color: Colors.white,
                      ),
                    ),
                  ).wrapWithBattleGradient(),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _Corner extends StatelessWidget {
  const _Corner({required this.isTop, required this.isLeft});

  final bool isTop;
  final bool isLeft;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: 16,
      height: 16,
      decoration: BoxDecoration(
        border: Border(
          top: isTop ? const BorderSide(color: homeRed) : BorderSide.none,
          bottom: !isTop ? const BorderSide(color: homeRed) : BorderSide.none,
          left: isLeft ? const BorderSide(color: homeRed) : BorderSide.none,
          right: !isLeft ? const BorderSide(color: homeRed) : BorderSide.none,
        ),
      ),
    );
  }
}

extension _BattleBtnExt on Widget {
  Widget wrapWithBattleGradient() {
    return Container(
      decoration: BoxDecoration(
        gradient: const LinearGradient(
          colors: [Color(0xFF8A0010), Color(0xFFC0001A), Color(0xFFFF1A2E)],
          begin: Alignment.topLeft,
          end: Alignment.bottomRight,
        ),
        borderRadius: BorderRadius.circular(3),
        boxShadow: const [
          BoxShadow(color: Color.fromRGBO(255, 0, 30, 0.4), blurRadius: 20),
          BoxShadow(color: Colors.black54, blurRadius: 10, offset: Offset(0, 4)),
        ],
      ),
      child: this,
    );
  }
}

class _NavListItem extends StatefulWidget {
  final IconData icon;
  final String label;
  final String? badge;
  final VoidCallback onTap;

  const _NavListItem({required this.icon, required this.label, this.badge, required this.onTap});

  @override
  State<_NavListItem> createState() => _NavListItemState();
}

class _NavListItemState extends State<_NavListItem> {
  bool _isHover = false;

  @override
  Widget build(BuildContext context) {
    return InkWell(
      onTap: widget.onTap,
      onHighlightChanged: (h) => setState(() => _isHover = h),
      child: AnimatedContainer(
        duration: const Duration(milliseconds: 200),
        padding: const EdgeInsets.symmetric(horizontal: 18, vertical: 12),
        decoration: BoxDecoration(
          color: _isHover ? const Color.fromRGBO(192, 0, 26, 0.12) : Colors.transparent,
          border: Border.all(color: _isHover ? const Color.fromRGBO(255, 30, 50, 0.55) : const Color.fromRGBO(192, 0, 26, 0.15)),
          borderRadius: BorderRadius.circular(3),
        ),
        child: Row(
          children: [
            Icon(widget.icon, color: _isHover ? homeRedBright : homeRed, size: 20),
            const SizedBox(width: 14),
            Expanded(
              child: Text(
                widget.label.toUpperCase(),
                style: TextStyle(
                  color: _isHover ? Colors.white : homeSilver,
                  fontSize: 14,
                  fontWeight: FontWeight.bold,
                  letterSpacing: _isHover ? 2 : 1,
                ),
              ),
            ),
            if (widget.badge != null)
              Container(
                margin: const EdgeInsets.only(right: 8),
                padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
                decoration: BoxDecoration(
                  color: homeBlood,
                  border: Border.all(color: const Color.fromRGBO(255, 30, 50, 0.3)),
                  borderRadius: BorderRadius.circular(2),
                ),
                child: Text(
                  widget.badge!,
                  style: const TextStyle(color: homeRedBright, fontSize: 10, fontWeight: FontWeight.bold),
                ),
              ),
            Icon(Icons.play_arrow, color: homeRed.withValues(alpha: _isHover ? 1.0 : 0.0), size: 14),
          ],
        ),
      ),
    );
  }
}
