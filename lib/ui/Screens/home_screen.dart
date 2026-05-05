import 'dart:io';
import 'dart:math' as math;
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:get/get.dart';
import '../../controllers/auth_controller.dart';
import '../../services/local_storage_service.dart';
import 'profile_screen.dart';
import 'chat_screen.dart';
import 'form_screen.dart';
import 'game_screen.dart';
import 'store_screen.dart';

// ═══════════════════════════════════════════════════════════════
// Medieval War Home — paleta idéntica al login
// ═══════════════════════════════════════════════════════════════
const _dungeonBase = Color(0xFF0D0808);
const _woodDark    = Color(0xFF2E1A0E);
const _burntOrange = Color(0xFFBF360C);
const _burntGlow   = Color(0xFFE64A19);
const _goldMetal   = Color(0xFFFFB300);
const _candleAmber = Color(0xFFFF8F00);
const _silverSteel = Color(0xFFB0BEC5);
const _smokedGlass = Color(0xFF1E1E1E);
const _warText     = Color(0xFFE8E8E8);
const _warMuted    = Color(0xFF888888);

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
    SystemChrome.setPreferredOrientations(DeviceOrientation.values);
    _refreshAvatar();
  }

  @override
  void dispose() {
    SystemChrome.setPreferredOrientations(DeviceOrientation.values);
    super.dispose();
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
    final isPortrait =
        MediaQuery.of(context).orientation == Orientation.portrait;
    return isPortrait
        ? _buildPortraitLayout(hasAvatar)
        : _buildLandscapeLayout(hasAvatar);
  }

  // ─────────────────────────────────────────────────────────────
  // Portrait: fondo de mazmorra + cuadrícula de opciones
  // ─────────────────────────────────────────────────────────────
  Widget _buildPortraitLayout(bool hasAvatar) {
    return Scaffold(
      backgroundColor: _dungeonBase,
      body: Stack(
        fit: StackFit.expand,
        children: [
          // Textura de piedra
          Positioned.fill(child: CustomPaint(painter: _StonePainter())),
          // Overlay oscuro con gradiente atmosférico
          Positioned.fill(
            child: Container(
              decoration: const BoxDecoration(
                gradient: LinearGradient(
                  begin: Alignment.topCenter,
                  end: Alignment.bottomCenter,
                  colors: [
                    Color(0xCC0D0808),
                    Color(0x550D0808),
                    Color(0xEE0D0808),
                  ],
                  stops: [0.0, 0.38, 1.0],
                ),
              ),
            ),
          ),
          // Brillo de antorcha superior izquierda
          const Positioned(
            top: -30, left: -30,
            child: _TorchGlow(size: 220, alpha: 0.26),
          ),
          // Brillo de antorcha superior derecha
          const Positioned(
            top: -30, right: -30,
            child: _TorchGlow(size: 220, alpha: 0.26),
          ),
          // Contenido principal
          SafeArea(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                // Barra superior: avatar · W.A.R. · cerrar sesión
                _buildTopBar(hasAvatar),
                // Escena de mazmorra (arco gótico)
                const Expanded(
                  flex: 4,
                  child: _DungeonScene(),
                ),
                // Separador con lema
                Padding(
                  padding: const EdgeInsets.symmetric(vertical: 6),
                  child: Row(
                    children: [
                      const SizedBox(width: 16),
                      Expanded(
                        child: Container(
                          height: 0.5,
                          color: _goldMetal.withValues(alpha: 0.30),
                        ),
                      ),
                      Padding(
                        padding: const EdgeInsets.symmetric(horizontal: 10),
                        child: Text(
                          'ELIGE TU DESTINO',
                          style: TextStyle(
                            fontSize: 8,
                            letterSpacing: 3,
                            color: _goldMetal.withValues(alpha: 0.6),
                            fontWeight: FontWeight.w700,
                          ),
                        ),
                      ),
                      Expanded(
                        child: Container(
                          height: 0.5,
                          color: _goldMetal.withValues(alpha: 0.30),
                        ),
                      ),
                      const SizedBox(width: 16),
                    ],
                  ),
                ),
                // Cuadrícula 2×2 de opciones
                Expanded(
                  flex: 5,
                  child: _buildMenuGrid(),
                ),
                const SizedBox(height: 10),
              ],
            ),
          ),
        ],
      ),
    );
  }

  // ─────────────────────────────────────────────────────────────
  // Landscape: panel izquierdo (atmósfera) | panel derecho (menú)
  // ─────────────────────────────────────────────────────────────
  Widget _buildLandscapeLayout(bool hasAvatar) {
    return Scaffold(
      backgroundColor: _dungeonBase,
      body: Row(
        children: [
          Expanded(flex: 5, child: _buildLandscapeLeft()),
          Expanded(flex: 4, child: _buildLandscapeRight(hasAvatar)),
        ],
      ),
    );
  }

  Widget _buildLandscapeLeft() {
    return Stack(
      fit: StackFit.expand,
      children: [
        Positioned.fill(child: CustomPaint(painter: _StonePainter())),
        Positioned.fill(
          child: Container(
            decoration: const BoxDecoration(
              gradient: LinearGradient(
                begin: Alignment.topCenter,
                end: Alignment.bottomCenter,
                colors: [
                  Color(0xBB0D0808),
                  Color(0x440D0808),
                  Color(0xAA0D0808),
                ],
                stops: [0.0, 0.4, 1.0],
              ),
            ),
          ),
        ),
        const Positioned(
          top: -30, left: -30,
          child: _TorchGlow(size: 200, alpha: 0.30),
        ),
        const Positioned(
          bottom: -30, left: 10,
          child: _TorchGlow(size: 150, alpha: 0.18),
        ),
        // Fundido derecho hacia el panel de menú
        Positioned(
          right: 0, top: 0, bottom: 0,
          child: Container(
            width: 55,
            decoration: const BoxDecoration(
              gradient: LinearGradient(
                begin: Alignment.centerLeft,
                end: Alignment.centerRight,
                colors: [Colors.transparent, _woodDark],
              ),
            ),
          ),
        ),
        // Marca W.A.R. + escena de mazmorra
        SafeArea(
          right: false,
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Text(
                'W.A.R.',
                style: TextStyle(
                  fontFamily: 'serif',
                  fontSize: 30,
                  fontWeight: FontWeight.w900,
                  color: _burntOrange,
                  letterSpacing: 5,
                  shadows: [
                    Shadow(
                        color: _burntGlow.withValues(alpha: 0.85),
                        blurRadius: 16),
                    Shadow(
                        color: _candleAmber.withValues(alpha: 0.45),
                        blurRadius: 28),
                  ],
                ),
              ),
              const SizedBox(height: 2),
              Text(
                'MMORPG · PVP',
                style: TextStyle(
                  fontSize: 8,
                  letterSpacing: 3,
                  color: _silverSteel.withValues(alpha: 0.65),
                ),
              ),
              const SizedBox(height: 8),
              const Expanded(child: _DungeonScene()),
            ],
          ),
        ),
      ],
    );
  }

  Widget _buildLandscapeRight(bool hasAvatar) {
    return Container(
      decoration: BoxDecoration(
        color: _woodDark,
        border: Border(
          left: BorderSide(
              color: _goldMetal.withValues(alpha: 0.22), width: 0.8),
        ),
      ),
      child: SafeArea(
        left: false,
        child: LayoutBuilder(
          builder: (context, constraints) {
            final compact = constraints.maxWidth < 220;
            return Column(
              children: [
                // Avatar + título + logout
                Padding(
                  padding: EdgeInsets.fromLTRB(compact ? 10 : 14, 10, 4, 0),
                  child: Row(
                    children: [
                      GestureDetector(
                        onTap: _openProfile,
                        child: Container(
                          width: 36,
                          height: 36,
                          decoration: BoxDecoration(
                            shape: BoxShape.circle,
                            border: Border.all(color: _burntOrange, width: 1.5),
                            color: _smokedGlass,
                            boxShadow: [
                              BoxShadow(
                                color: _burntOrange.withValues(alpha: 0.3),
                                blurRadius: 6,
                              ),
                            ],
                          ),
                          child: ClipOval(
                            child: hasAvatar
                                ? Image.file(File(_avatarPath!),
                                    fit: BoxFit.cover)
                                : const Icon(Icons.person,
                                    color: _warMuted, size: 18),
                          ),
                        ),
                      ),
                      const SizedBox(width: 8),
                      Expanded(
                        child: Text(
                          'MENÚ PRINCIPAL',
                          style: TextStyle(
                            color: _goldMetal.withValues(alpha: 0.85),
                            fontFamily: 'serif',
                            fontSize: compact ? 10 : 12,
                            fontWeight: FontWeight.w900,
                            letterSpacing: 1.5,
                            overflow: TextOverflow.ellipsis,
                          ),
                        ),
                      ),
                      IconButton(
                        icon: const Icon(Icons.logout,
                            color: _warMuted, size: 17),
                        tooltip: 'Cerrar sesión',
                        onPressed: () => Get.put(AuthController()).logout(),
                      ),
                    ],
                  ),
                ),
                Padding(
                  padding: const EdgeInsets.fromLTRB(14, 6, 14, 6),
                  child: Container(
                    height: 0.5,
                    color: _goldMetal.withValues(alpha: 0.28),
                  ),
                ),
                // Lista de opciones
                Expanded(
                  child: SingleChildScrollView(
                    padding: EdgeInsets.fromLTRB(
                        compact ? 10 : 14, 4, compact ? 10 : 14, 12),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.stretch,
                      children: [
                        _MedievalListItem(
                          icon: Icons.storefront,
                          label: 'TIENDA',
                          subtitle: 'Armería del guerrero',
                          badge: 'NUEVO',
                          onTap: () => _navigate(const StoreScreen()),
                        ),
                        const SizedBox(height: 6),
                        _MedievalListItem(
                          icon: Icons.chat_bubble_outline,
                          label: 'CHAT',
                          subtitle: 'Consejo de guerra',
                          badge: '12',
                          onTap: () => _navigate(const ChatScreen()),
                        ),
                        const SizedBox(height: 6),
                        _MedievalListItem(
                          icon: Icons.sports_kabaddi,
                          label: 'JUGAR',
                          subtitle: 'Campo de batalla',
                          badge: 'LIVE',
                          onTap: () => _navigate(const GameScreen()),
                        ),
                        const SizedBox(height: 6),
                        _MedievalListItem(
                          icon: Icons.security,
                          label: 'SOPORTE',
                          subtitle: 'Guardia real',
                          onTap: () => _navigate(const FormScreen()),
                        ),
                      ],
                    ),
                  ),
                ),
              ],
            );
          },
        ),
      ),
    );
  }

  // ─────────────────────────────────────────────────────────────
  // Barra superior (portrait)
  // ─────────────────────────────────────────────────────────────
  Widget _buildTopBar(bool hasAvatar) {
    return Padding(
      padding: const EdgeInsets.fromLTRB(16, 10, 8, 4),
      child: Row(
        children: [
          GestureDetector(
            onTap: _openProfile,
            child: Container(
              width: 38,
              height: 38,
              decoration: BoxDecoration(
                shape: BoxShape.circle,
                border: Border.all(color: _burntOrange, width: 1.5),
                color: _smokedGlass,
                boxShadow: [
                  BoxShadow(
                    color: _burntOrange.withValues(alpha: 0.35),
                    blurRadius: 8,
                  ),
                ],
              ),
              child: ClipOval(
                child: hasAvatar
                    ? Image.file(File(_avatarPath!), fit: BoxFit.cover)
                    : const Icon(Icons.person, color: _warMuted, size: 20),
              ),
            ),
          ),
          const SizedBox(width: 12),
          Expanded(
            child: Text(
              'W.A.R.',
              textAlign: TextAlign.center,
              style: TextStyle(
                fontFamily: 'serif',
                fontSize: 22,
                fontWeight: FontWeight.w900,
                color: _burntOrange,
                letterSpacing: 5,
                shadows: [
                  Shadow(
                      color: _burntGlow.withValues(alpha: 0.80),
                      blurRadius: 14),
                  Shadow(
                      color: _candleAmber.withValues(alpha: 0.40),
                      blurRadius: 28),
                ],
              ),
            ),
          ),
          const SizedBox(width: 12),
          IconButton(
            icon: const Icon(Icons.logout, color: _warMuted, size: 18),
            tooltip: 'Cerrar sesión',
            onPressed: () => Get.put(AuthController()).logout(),
          ),
        ],
      ),
    );
  }

  // ─────────────────────────────────────────────────────────────
  // Cuadrícula 2×2 de opciones (portrait)
  // ─────────────────────────────────────────────────────────────
  Widget _buildMenuGrid() {
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 14),
      child: Column(
        children: [
          Expanded(
            child: Row(
              children: [
                Expanded(
                  child: _MedievalCard(
                    icon: Icons.storefront,
                    label: 'TIENDA',
                    subtitle: 'Armería',
                    badge: 'NUEVO',
                    onTap: () => _navigate(const StoreScreen()),
                  ),
                ),
                const SizedBox(width: 10),
                Expanded(
                  child: _MedievalCard(
                    icon: Icons.chat_bubble_outline,
                    label: 'CHAT',
                    subtitle: 'Consejo',
                    badge: '12',
                    onTap: () => _navigate(const ChatScreen()),
                  ),
                ),
              ],
            ),
          ),
          const SizedBox(height: 10),
          Expanded(
            child: Row(
              children: [
                Expanded(
                  child: _MedievalCard(
                    icon: Icons.sports_kabaddi,
                    label: 'JUGAR',
                    subtitle: 'Batalla',
                    badge: 'LIVE',
                    isPrimary: true,
                    onTap: () => _navigate(const GameScreen()),
                  ),
                ),
                const SizedBox(width: 10),
                Expanded(
                  child: _MedievalCard(
                    icon: Icons.security,
                    label: 'SOPORTE',
                    subtitle: 'Guardia real',
                    onTap: () => _navigate(const FormScreen()),
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

// ═══════════════════════════════════════════════════════════════
// Escena de mazmorra — arco gótico + antorchas + ambiente
// ═══════════════════════════════════════════════════════════════
class _DungeonScene extends StatelessWidget {
  const _DungeonScene();

  @override
  Widget build(BuildContext context) {
    return Stack(
      children: [
        // Arco gótico pintado
        Positioned.fill(child: CustomPaint(painter: _ArchPainter())),
        // Bruma inferior
        Positioned(
          bottom: 0, left: 0, right: 0,
          child: Container(
            height: 55,
            decoration: const BoxDecoration(
              gradient: LinearGradient(
                begin: Alignment.topCenter,
                end: Alignment.bottomCenter,
                colors: [
                  Colors.transparent,
                  Color(0x33BF360C),
                  Color(0x99050202),
                ],
              ),
            ),
          ),
        ),
        // Antorcha izquierda
        const Positioned(left: 22, top: 12, child: _TorchWidget()),
        // Antorcha derecha
        const Positioned(right: 22, top: 12, child: _TorchWidget()),
        // Escudo central + texto bajo el arco
        Center(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Icon(
                Icons.shield,
                size: 44,
                color: _goldMetal.withValues(alpha: 0.50),
                shadows: const [
                  Shadow(
                    color: Color(0x55BF360C),
                    blurRadius: 20,
                  ),
                ],
              ),
              const SizedBox(height: 6),
              Text(
                'EL HONOR SE GANA\nCON SANGRE',
                textAlign: TextAlign.center,
                style: TextStyle(
                  fontSize: 8,
                  letterSpacing: 2.5,
                  color: _silverSteel.withValues(alpha: 0.45),
                  fontWeight: FontWeight.w600,
                  height: 1.7,
                ),
              ),
            ],
          ),
        ),
      ],
    );
  }
}

// ═══════════════════════════════════════════════════════════════
// Antorcha — icono + resplandor
// ═══════════════════════════════════════════════════════════════
class _TorchWidget extends StatelessWidget {
  const _TorchWidget();

  @override
  Widget build(BuildContext context) {
    return Stack(
      alignment: Alignment.topCenter,
      children: [
        Container(
          width: 52, height: 52,
          decoration: BoxDecoration(
            shape: BoxShape.circle,
            gradient: RadialGradient(
              colors: [
                _candleAmber.withValues(alpha: 0.50),
                _burntOrange.withValues(alpha: 0.20),
                Colors.transparent,
              ],
            ),
          ),
        ),
        const Icon(
          Icons.local_fire_department,
          size: 24,
          color: _candleAmber,
        ),
      ],
    );
  }
}

// ═══════════════════════════════════════════════════════════════
// Resplandor de antorcha (fondo)
// ═══════════════════════════════════════════════════════════════
class _TorchGlow extends StatelessWidget {
  const _TorchGlow({required this.size, required this.alpha});
  final double size;
  final double alpha;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: size, height: size,
      decoration: BoxDecoration(
        shape: BoxShape.circle,
        gradient: RadialGradient(
          colors: [
            _candleAmber.withValues(alpha: alpha),
            _burntOrange.withValues(alpha: alpha * 0.50),
            Colors.transparent,
          ],
        ),
      ),
    );
  }
}

// ═══════════════════════════════════════════════════════════════
// Tarjeta medieval (cuadrícula portrait)
// ═══════════════════════════════════════════════════════════════
class _MedievalCard extends StatefulWidget {
  const _MedievalCard({
    required this.icon,
    required this.label,
    required this.subtitle,
    this.badge,
    required this.onTap,
    this.isPrimary = false,
  });

  final IconData icon;
  final String label;
  final String subtitle;
  final String? badge;
  final VoidCallback onTap;
  final bool isPrimary;

  @override
  State<_MedievalCard> createState() => _MedievalCardState();
}

class _MedievalCardState extends State<_MedievalCard> {
  bool _pressed = false;

  @override
  Widget build(BuildContext context) {
    final borderColor = _pressed
        ? _burntOrange.withValues(alpha: 0.85)
        : widget.isPrimary
            ? _burntOrange.withValues(alpha: 0.55)
            : _goldMetal.withValues(alpha: 0.38);

    return GestureDetector(
      onTapDown: (_) => setState(() => _pressed = true),
      onTapUp: (_) {
        setState(() => _pressed = false);
        widget.onTap();
      },
      onTapCancel: () => setState(() => _pressed = false),
      child: AnimatedContainer(
        duration: const Duration(milliseconds: 150),
        decoration: BoxDecoration(
          color: _pressed ? const Color(0xFF1E1E1E) : const Color(0xFF141414),
          border: Border.all(color: borderColor, width: 1.2),
          borderRadius: BorderRadius.circular(4),
          boxShadow: [
            BoxShadow(
                color: Colors.black.withValues(alpha: 0.55), blurRadius: 10),
            if (widget.isPrimary)
              BoxShadow(
                color: _burntOrange.withValues(alpha: _pressed ? 0.28 : 0.12),
                blurRadius: 18,
              ),
          ],
        ),
        child: Stack(
          children: [
            // Esquinas ornamentales
            const Positioned(top: 3, left: 3, child: _CardCorner()),
            const Positioned(
                top: 3, right: 3, child: _CardCorner(flipX: true)),
            const Positioned(
                bottom: 3, left: 3, child: _CardCorner(flipY: true)),
            const Positioned(
                bottom: 3,
                right: 3,
                child: _CardCorner(flipX: true, flipY: true)),
            // Contenido
            Padding(
              padding:
                  const EdgeInsets.symmetric(horizontal: 10, vertical: 14),
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  // Icono con aureola
                  Container(
                    width: 46, height: 46,
                    decoration: BoxDecoration(
                      shape: BoxShape.circle,
                      color: widget.isPrimary
                          ? _burntOrange.withValues(alpha: 0.14)
                          : _goldMetal.withValues(alpha: 0.08),
                      border: Border.all(
                        color: widget.isPrimary
                            ? _burntOrange.withValues(alpha: 0.42)
                            : _goldMetal.withValues(alpha: 0.28),
                      ),
                    ),
                    child: Icon(
                      widget.icon,
                      size: 22,
                      color: widget.isPrimary ? _burntOrange : _goldMetal,
                      shadows: [
                        Shadow(
                          color: widget.isPrimary
                              ? _burntGlow.withValues(alpha: 0.60)
                              : _candleAmber.withValues(alpha: 0.38),
                          blurRadius: 12,
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(height: 8),
                  Text(
                    widget.label,
                    style: TextStyle(
                      color: widget.isPrimary ? _warText : _goldMetal,
                      fontFamily: 'serif',
                      fontSize: 12,
                      fontWeight: FontWeight.w900,
                      letterSpacing: 1.5,
                    ),
                  ),
                  const SizedBox(height: 2),
                  Text(
                    widget.subtitle,
                    style: TextStyle(
                      color: _silverSteel.withValues(alpha: 0.50),
                      fontSize: 9,
                      letterSpacing: 0.4,
                    ),
                  ),
                  if (widget.badge != null) ...[
                    const SizedBox(height: 6),
                    Container(
                      padding: const EdgeInsets.symmetric(
                          horizontal: 6, vertical: 2),
                      decoration: BoxDecoration(
                        color: _burntOrange.withValues(alpha: 0.18),
                        border: Border.all(
                            color: _burntOrange.withValues(alpha: 0.50)),
                        borderRadius: BorderRadius.circular(3),
                      ),
                      child: Text(
                        widget.badge!,
                        style: const TextStyle(
                          color: _burntOrange,
                          fontSize: 8,
                          fontWeight: FontWeight.w800,
                          letterSpacing: 1,
                        ),
                      ),
                    ),
                  ],
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}

// ═══════════════════════════════════════════════════════════════
// Elemento de lista medieval (panel landscape)
// ═══════════════════════════════════════════════════════════════
class _MedievalListItem extends StatefulWidget {
  const _MedievalListItem({
    required this.icon,
    required this.label,
    required this.subtitle,
    this.badge,
    required this.onTap,
  });

  final IconData icon;
  final String label;
  final String subtitle;
  final String? badge;
  final VoidCallback onTap;

  @override
  State<_MedievalListItem> createState() => _MedievalListItemState();
}

class _MedievalListItemState extends State<_MedievalListItem> {
  bool _pressed = false;

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTapDown: (_) => setState(() => _pressed = true),
      onTapUp: (_) {
        setState(() => _pressed = false);
        widget.onTap();
      },
      onTapCancel: () => setState(() => _pressed = false),
      child: AnimatedContainer(
        duration: const Duration(milliseconds: 140),
        padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
        decoration: BoxDecoration(
          color: _pressed
              ? _burntOrange.withValues(alpha: 0.10)
              : const Color(0xFF1A1010),
          border: Border.all(
            color: _pressed
                ? _burntOrange.withValues(alpha: 0.65)
                : _goldMetal.withValues(alpha: 0.22),
          ),
          borderRadius: BorderRadius.circular(4),
        ),
        child: Row(
          children: [
            Icon(
              widget.icon,
              size: 18,
              color: _pressed ? _burntOrange : _goldMetal,
            ),
            const SizedBox(width: 10),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                mainAxisSize: MainAxisSize.min,
                children: [
                  Text(
                    widget.label,
                    style: TextStyle(
                      color: _pressed ? _warText : _goldMetal,
                      fontFamily: 'serif',
                      fontSize: 11,
                      fontWeight: FontWeight.w900,
                      letterSpacing: 1.5,
                      overflow: TextOverflow.ellipsis,
                    ),
                  ),
                  Text(
                    widget.subtitle,
                    style: TextStyle(
                      color: _silverSteel.withValues(alpha: 0.48),
                      fontSize: 9,
                      overflow: TextOverflow.ellipsis,
                    ),
                  ),
                ],
              ),
            ),
            if (widget.badge != null) ...[
              const SizedBox(width: 6),
              Container(
                padding:
                    const EdgeInsets.symmetric(horizontal: 5, vertical: 2),
                decoration: BoxDecoration(
                  color: _burntOrange.withValues(alpha: 0.18),
                  border: Border.all(
                      color: _burntOrange.withValues(alpha: 0.42)),
                  borderRadius: BorderRadius.circular(3),
                ),
                child: Text(
                  widget.badge!,
                  style: const TextStyle(
                    color: _burntOrange,
                    fontSize: 8,
                    fontWeight: FontWeight.w800,
                  ),
                ),
              ),
            ],
            const SizedBox(width: 4),
            Icon(
              Icons.chevron_right,
              color: _burntOrange.withValues(alpha: _pressed ? 1.0 : 0.28),
              size: 14,
            ),
          ],
        ),
      ),
    );
  }
}

// ═══════════════════════════════════════════════════════════════
// Esquina ornamental de tarjeta
// ═══════════════════════════════════════════════════════════════
class _CardCorner extends StatelessWidget {
  const _CardCorner({this.flipX = false, this.flipY = false});
  final bool flipX;
  final bool flipY;

  @override
  Widget build(BuildContext context) {
    Widget child = const SizedBox(
      width: 10, height: 10,
      child: CustomPaint(painter: _CornerPainter()),
    );
    if (flipX || flipY) {
      child = Transform(
        alignment: Alignment.center,
        transform: Matrix4.diagonal3Values(
          flipX ? -1.0 : 1.0,
          flipY ? -1.0 : 1.0,
          1.0,
        ),
        child: child,
      );
    }
    return child;
  }
}

class _CornerPainter extends CustomPainter {
  const _CornerPainter();

  @override
  void paint(Canvas canvas, Size size) {
    final paint = Paint()
      ..color = const Color(0xFFFFB300).withValues(alpha: 0.52)
      ..strokeWidth = 0.9
      ..style = PaintingStyle.stroke
      ..strokeCap = StrokeCap.round;
    canvas.drawLine(Offset(size.width, 0), Offset.zero, paint);
    canvas.drawLine(Offset.zero, Offset(0, size.height), paint);
    canvas.drawLine(Offset(size.width * 0.55, 0),
        Offset(size.width * 0.55, 3.0), paint);
    canvas.drawLine(Offset(0, size.height * 0.55),
        Offset(3.0, size.height * 0.55), paint);
    paint.style = PaintingStyle.fill;
    canvas.drawCircle(Offset.zero, 1.5, paint);
  }

  @override
  bool shouldRepaint(covariant CustomPainter oldDelegate) => false;
}

// ═══════════════════════════════════════════════════════════════
// Pintor del arco gótico de mazmorra
// ═══════════════════════════════════════════════════════════════
class _ArchPainter extends CustomPainter {
  @override
  void paint(Canvas canvas, Size size) {
    final cx = size.width / 2;
    final archW = math.min(size.width * 0.62, 260.0);
    final archLeft  = cx - archW / 2;
    final archRight = cx + archW / 2;
    final archTop    = size.height * 0.04;
    final archBottom = size.height * 0.86;
    final archMid    = archTop + (archBottom - archTop) * 0.44;

    // ── Construcción del arco gótico apuntado ─────────────────
    final archPath = Path();
    archPath.moveTo(archLeft, archBottom);
    archPath.lineTo(archLeft, archMid);
    archPath.cubicTo(
      archLeft, archTop + (archMid - archTop) * 0.22,
      cx - archW * 0.07, archTop,
      cx, archTop,
    );
    archPath.cubicTo(
      cx + archW * 0.07, archTop,
      archRight, archTop + (archMid - archTop) * 0.22,
      archRight, archMid,
    );
    archPath.lineTo(archRight, archBottom);

    // Interior oscuro del arco
    canvas.drawPath(archPath,
        Paint()..color = const Color(0xFF060303));

    // Borde dorado exterior
    canvas.drawPath(
      archPath,
      Paint()
        ..color = _goldMetal.withValues(alpha: 0.72)
        ..style = PaintingStyle.stroke
        ..strokeWidth = 1.8
        ..strokeJoin = StrokeJoin.round,
    );

    // Arco interior decorativo
    final m = archW * 0.075;
    final iPath = Path();
    final iL = archLeft + m;
    final iR = archRight - m;
    final iTop = archTop + m * 0.65;
    iPath.moveTo(iL, archBottom);
    iPath.lineTo(iL, archMid);
    iPath.cubicTo(
      iL, iTop + (archMid - iTop) * 0.22,
      cx - (archW - m * 2) * 0.07, iTop,
      cx, iTop,
    );
    iPath.cubicTo(
      cx + (archW - m * 2) * 0.07, iTop,
      iR, iTop + (archMid - iTop) * 0.22,
      iR, archMid,
    );
    iPath.lineTo(iR, archBottom);
    canvas.drawPath(
      iPath,
      Paint()
        ..color = _goldMetal.withValues(alpha: 0.22)
        ..style = PaintingStyle.stroke
        ..strokeWidth = 0.7,
    );

    // ── Perspectiva del suelo ─────────────────────────────────
    _paintFloor(canvas, size, archBottom);

    // ── Clave del arco (diamante) ─────────────────────────────
    _paintKeystone(canvas, cx, archTop);

    // ── Capiteles de las columnas ─────────────────────────────
    _paintPillarCaps(canvas, archLeft, archRight, archBottom);
  }

  void _paintFloor(Canvas canvas, Size size, double floorY) {
    final paint = Paint()
      ..color = _goldMetal.withValues(alpha: 0.13)
      ..strokeWidth = 0.55;
    final cx = size.width / 2;
    final vY = floorY * 0.60;
    // Líneas de perspectiva diagonales
    for (var i = 0; i <= 6; i++) {
      final sx = size.width * i / 6;
      canvas.drawLine(
        Offset(sx, size.height),
        Offset(cx + (sx - cx) * 0.22, vY),
        paint,
      );
    }
    // Líneas horizontales del suelo
    for (var i = 1; i <= 3; i++) {
      final y = floorY + (size.height - floorY) * i / 4;
      canvas.drawLine(Offset(0, y), Offset(size.width, y), paint);
    }
  }

  void _paintKeystone(Canvas canvas, double cx, double archTop) {
    final paint = Paint()
      ..color = _goldMetal.withValues(alpha: 0.88);
    final path = Path();
    const ks = 7.0;
    path.moveTo(cx, archTop - ks);
    path.lineTo(cx + ks * 0.55, archTop);
    path.lineTo(cx, archTop + ks);
    path.lineTo(cx - ks * 0.55, archTop);
    path.close();
    canvas.drawPath(path, paint);
  }

  void _paintPillarCaps(
      Canvas canvas, double left, double right, double bottom) {
    final paint = Paint()
      ..color = _goldMetal.withValues(alpha: 0.42)
      ..strokeWidth = 1.4
      ..strokeCap = StrokeCap.round;
    canvas.drawLine(Offset(left - 7, bottom), Offset(left + 7, bottom), paint);
    canvas.drawLine(
        Offset(right - 7, bottom), Offset(right + 7, bottom), paint);
  }

  @override
  bool shouldRepaint(covariant CustomPainter oldDelegate) => false;
}

// ═══════════════════════════════════════════════════════════════
// Textura de muro de piedra medieval
// ═══════════════════════════════════════════════════════════════
class _StonePainter extends CustomPainter {
  @override
  void paint(Canvas canvas, Size size) {
    final rng = math.Random(33);
    const blockH = 26.0;
    const blockW = 52.0;
    final paint = Paint()
      ..style = PaintingStyle.stroke
      ..strokeWidth = 0.55;

    for (var row = 0; row * blockH < size.height + blockH; row++) {
      final offset = (row % 2 == 0) ? 0.0 : blockW * 0.5;
      for (var col = -1; col * blockW < size.width + blockW; col++) {
        final x = col * blockW + offset;
        final y = row * blockH;
        final shade = 0.022 + rng.nextDouble() * 0.042;
        paint.color = Color.fromRGBO(180, 115, 55, shade);
        canvas.drawRect(
          Rect.fromLTWH(x + 0.5, y + 0.5, blockW - 1, blockH - 1),
          paint,
        );
      }
    }
  }

  @override
  bool shouldRepaint(covariant CustomPainter oldDelegate) => false;
}
