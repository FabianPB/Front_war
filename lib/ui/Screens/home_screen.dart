import 'dart:io';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:get/get.dart';
import '../../controllers/auth_controller.dart';
import '../../services/local_storage_service.dart';
import '../widgets/home/home_fighter.dart';
import 'profile_screen.dart';
import 'chat_screen.dart';
import 'form_screen.dart';
import 'game_screen.dart';
import 'store_screen.dart';

const _warAccent = Color(0xFFE6451C);
const _warText = Color(0xFFE8E8E8);
const _warMuted = Color(0xFF888888);
const _warBorder = Color(0xFF2A2A2E);
const _warInputBg = Color(0xFF1A1A1E);

class HomeScreen extends StatefulWidget {
  const HomeScreen({super.key});
  static const routeName = '/home';

  @override
  State<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends State<HomeScreen> {
  String? _avatarPath;

  @override
  void initState() {
    super.initState();
    SystemChrome.setPreferredOrientations([
      DeviceOrientation.landscapeLeft,
      DeviceOrientation.landscapeRight,
    ]);
    _refreshAvatar();
  }

  void _refreshAvatar() {
    setState(() => _avatarPath = LocalStorageService.getPhotoPath());
  }

  Future<void> _openProfile() async {
    await Get.toNamed(ProfileScreen.routeName);
    if (mounted) _refreshAvatar();
  }

  void _navigate(Widget screen) {
    Navigator.of(context).push(MaterialPageRoute(builder: (_) => screen));
  }

  @override
  Widget build(BuildContext context) {
    final hasAvatar = _avatarPath != null && File(_avatarPath!).existsSync();

    return Scaffold(
      backgroundColor: const Color(0xFF0A0707),
      body: Row(
        children: [
          Expanded(flex: 5, child: _buildLeftPanel(hasAvatar)),
          Expanded(flex: 4, child: _buildRightPanel()),
        ],
      ),
    );
  }

  Widget _buildLeftPanel(bool hasAvatar) {
    return Stack(
      fit: StackFit.expand,
      children: [
        // Dark warm base
        Container(
          decoration: const BoxDecoration(
            gradient: LinearGradient(
              begin: Alignment.topLeft,
              end: Alignment.bottomRight,
              colors: [Color(0xFF0D0808), Color(0xFF120A06)],
            ),
          ),
        ),
        // Fire glow from bottom
        Positioned.fill(
          child: Container(
            decoration: const BoxDecoration(
              gradient: RadialGradient(
                center: Alignment.bottomCenter,
                radius: 1.2,
                colors: [
                  Color(0xAAE6451C),
                  Color(0x55B83318),
                  Color(0x22802010),
                  Colors.transparent,
                ],
                stops: [0.0, 0.35, 0.65, 1.0],
              ),
            ),
          ),
        ),
        // Top-left ember
        Positioned(
          top: -30,
          left: -20,
          child: Container(
            width: 160,
            height: 160,
            decoration: const BoxDecoration(
              shape: BoxShape.circle,
              gradient: RadialGradient(
                  colors: [Color(0x33E6451C), Colors.transparent]),
            ),
          ),
        ),
        // Faded WAR texture
        Center(
          child: Opacity(
            opacity: 0.04,
            child: Text(
              'WAR',
              style: TextStyle(
                fontFamily: 'serif',
                fontSize: 180,
                fontWeight: FontWeight.w900,
                color: Colors.white,
                letterSpacing: 20,
              ),
            ),
          ),
        ),
        // Content
        SafeArea(
          right: false,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              // Top bar: avatar + branding
              Padding(
                padding: const EdgeInsets.fromLTRB(16, 10, 16, 0),
                child: Row(
                  children: [
                    GestureDetector(
                      onTap: _openProfile,
                      child: Container(
                        width: 42,
                        height: 42,
                        decoration: BoxDecoration(
                          shape: BoxShape.circle,
                          border: Border.all(color: _warAccent, width: 1.5),
                          color: const Color(0xFF1A1A1E),
                        ),
                        child: ClipOval(
                          child: hasAvatar
                              ? Image.file(File(_avatarPath!), fit: BoxFit.cover)
                              : const Icon(Icons.person, color: _warMuted, size: 22),
                        ),
                      ),
                    ),
                    const SizedBox(width: 12),
                    Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: const [
                        Text(
                          'W.A.R.',
                          style: TextStyle(
                            color: _warAccent,
                            fontFamily: 'serif',
                            fontSize: 20,
                            fontWeight: FontWeight.w900,
                            letterSpacing: 4,
                            shadows: [
                              Shadow(color: Color(0x88E6451C), blurRadius: 14)
                            ],
                          ),
                        ),
                        Text(
                          'MMORPG · PVP',
                          style: TextStyle(
                            color: _warMuted,
                            fontSize: 8,
                            letterSpacing: 3,
                          ),
                        ),
                      ],
                    ),
                  ],
                ),
              ),
              // Fighter centered in remaining space
              Expanded(
                child: Center(
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Stack(
                        alignment: Alignment.center,
                        children: [
                          Container(
                            width: 130,
                            height: 130,
                            decoration: const BoxDecoration(
                              shape: BoxShape.circle,
                              boxShadow: [
                                BoxShadow(
                                  color: Color(0x55E6451C),
                                  blurRadius: 40,
                                  spreadRadius: 8,
                                ),
                              ],
                            ),
                          ),
                          SizedBox(
                            width: 130,
                            height: 130,
                            child: FittedBox(
                              fit: BoxFit.contain,
                              child: const HomeFighter(),
                            ),
                          ),
                        ],
                      ),
                      const SizedBox(height: 10),
                      const Text(
                        'EL HONOR SE GANA CON SANGRE',
                        style: TextStyle(
                          color: _warMuted,
                          fontSize: 8,
                          letterSpacing: 3,
                        ),
                      ),
                    ],
                  ),
                ),
              ),
            ],
          ),
        ),
        // Right-edge fade into panel
        Positioned(
          right: 0,
          top: 0,
          bottom: 0,
          child: Container(
            width: 50,
            decoration: const BoxDecoration(
              gradient: LinearGradient(
                begin: Alignment.centerLeft,
                end: Alignment.centerRight,
                colors: [Colors.transparent, Color(0xFF141418)],
              ),
            ),
          ),
        ),
      ],
    );
  }

  Widget _buildRightPanel() {
    return Container(
      color: const Color(0xFF141418),
      child: SafeArea(
        left: false,
        child: Column(
          children: [
            // Logout row
            Align(
              alignment: Alignment.topRight,
              child: Padding(
                padding: const EdgeInsets.only(right: 6, top: 2),
                child: IconButton(
                  icon: const Icon(Icons.logout, color: _warMuted, size: 20),
                  tooltip: 'Cerrar sesión',
                  onPressed: () => Get.put(AuthController()).logout(),
                ),
              ),
            ),
            // Menu
            Expanded(
              child: Padding(
                padding: const EdgeInsets.fromLTRB(18, 0, 18, 12),
                child: Column(
                  mainAxisAlignment: MainAxisAlignment.center,
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    const Text(
                      'MENÚ PRINCIPAL',
                      style: TextStyle(
                        color: _warText,
                        fontFamily: 'serif',
                        fontSize: 14,
                        fontWeight: FontWeight.w900,
                        letterSpacing: 3,
                      ),
                    ),
                    const SizedBox(height: 12),
                    _WarMenuItem(
                      icon: Icons.storefront,
                      label: 'TIENDA',
                      badge: 'NUEVO',
                      onTap: () => _navigate(const StoreScreen()),
                    ),
                    const SizedBox(height: 5),
                    _WarMenuItem(
                      icon: Icons.chat_bubble_outline,
                      label: 'CHAT',
                      badge: '12',
                      onTap: () => _navigate(const ChatScreen()),
                    ),
                    const SizedBox(height: 5),
                    _WarMenuItem(
                      icon: Icons.flash_on,
                      label: 'EVENTOS',
                      badge: 'LIVE',
                      onTap: () => _navigate(const GameScreen()),
                    ),
                    const SizedBox(height: 5),
                    _WarMenuItem(
                      icon: Icons.security,
                      label: 'SOPORTE',
                      onTap: () => _navigate(const FormScreen()),
                    ),
                    const SizedBox(height: 14),
                    // Divider
                    Container(
                      height: 1,
                      decoration: BoxDecoration(
                        gradient: LinearGradient(colors: [
                          Colors.transparent,
                          _warAccent.withValues(alpha: 0.3),
                          Colors.transparent,
                        ]),
                      ),
                    ),
                    const SizedBox(height: 14),
                    // Battle button
                    _BattleButton(
                      onTap: () => _navigate(const GameScreen()),
                    ),
                  ],
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _WarMenuItem extends StatefulWidget {
  const _WarMenuItem({
    required this.icon,
    required this.label,
    this.badge,
    required this.onTap,
  });

  final IconData icon;
  final String label;
  final String? badge;
  final VoidCallback onTap;

  @override
  State<_WarMenuItem> createState() => _WarMenuItemState();
}

class _WarMenuItemState extends State<_WarMenuItem> {
  bool _pressed = false;

  @override
  Widget build(BuildContext context) {
    return InkWell(
      onTap: widget.onTap,
      onHighlightChanged: (h) => setState(() => _pressed = h),
      borderRadius: BorderRadius.circular(6),
      child: AnimatedContainer(
        duration: const Duration(milliseconds: 180),
        padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 11),
        decoration: BoxDecoration(
          color: _pressed
              ? const Color(0x22E6451C)
              : const Color(0xFF1A1A1E),
          border: Border.all(
            color: _pressed
                ? _warAccent.withValues(alpha: 0.6)
                : _warBorder,
          ),
          borderRadius: BorderRadius.circular(6),
        ),
        child: Row(
          children: [
            Icon(
              widget.icon,
              color: _pressed ? _warAccent : _warMuted,
              size: 18,
            ),
            const SizedBox(width: 12),
            Expanded(
              child: Text(
                widget.label,
                style: TextStyle(
                  color: _pressed ? _warText : _warMuted,
                  fontSize: 12,
                  fontWeight: FontWeight.w700,
                  letterSpacing: _pressed ? 2 : 1,
                ),
              ),
            ),
            if (widget.badge != null)
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
                decoration: BoxDecoration(
                  color: _warAccent.withValues(alpha: 0.18),
                  border: Border.all(
                      color: _warAccent.withValues(alpha: 0.4)),
                  borderRadius: BorderRadius.circular(3),
                ),
                child: Text(
                  widget.badge!,
                  style: const TextStyle(
                    color: _warAccent,
                    fontSize: 9,
                    fontWeight: FontWeight.bold,
                  ),
                ),
              ),
            const SizedBox(width: 4),
            Icon(
              Icons.chevron_right,
              color: _warAccent.withValues(alpha: _pressed ? 1.0 : 0.0),
              size: 14,
            ),
          ],
        ),
      ),
    );
  }
}

class _BattleButton extends StatelessWidget {
  const _BattleButton({required this.onTap});

  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: onTap,
      child: Container(
        padding: const EdgeInsets.symmetric(vertical: 14),
        decoration: BoxDecoration(
          gradient: const LinearGradient(
            colors: [Color(0xFFE6451C), Color(0xFFB83318)],
          ),
          borderRadius: BorderRadius.circular(6),
          boxShadow: [
            BoxShadow(
              color: const Color(0xFFE6451C).withValues(alpha: 0.35),
              blurRadius: 18,
              offset: const Offset(0, 4),
            ),
          ],
        ),
        child: FittedBox(
          fit: BoxFit.scaleDown,
          child: Row(
            mainAxisSize: MainAxisSize.min,
            children: const [
              Icon(Icons.sports_kabaddi, color: Colors.white, size: 18),
              SizedBox(width: 8),
              Text(
                '¡COMIENZA LA BATALLA!',
                style: TextStyle(
                  fontFamily: 'serif',
                  fontWeight: FontWeight.w900,
                  letterSpacing: 1.5,
                  color: Colors.white,
                  fontSize: 12,
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
