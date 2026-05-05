import 'dart:math' as math;
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:get/get.dart';
import '../../controllers/auth_controller.dart';
import '../../models/user_model.dart';
import '../../utils/validators.dart';
import '../widgets/auth/auth_widgets.dart';
import 'home_screen.dart';

// ═══════════════════════════════════════════════════════════════
// Medieval War MMORPG · Color Palette
// ═══════════════════════════════════════════════════════════════
const _woodDark    = Color(0xFF2E1A0E);   // Very dark walnut
const _burntOrange = Color(0xFFBF360C);  // Radiant Burnt Orange — primary action
const _burntGlow   = Color(0xFFE64A19);  // Glow variant
const _silverSteel = Color(0xFFB0BEC5);  // Antique Silver/Steel
const _goldMetal   = Color(0xFFFFB300);  // Metallic Gold accent
const _smokedGlass = Color(0xFF1E1E1E);  // Dark Smoked Glass panel
const _glassBorder = Color(0xFF3D3D3D);  // Glass border
const _candleAmber = Color(0xFFFF8F00);  // Candle warm light
const _warText     = Color(0xFFE8E8E8);  // Main text
const _warMuted    = Color(0xFF888888);  // Muted/secondary text
const _warError    = Color(0xFFFF6B6B);  // Error red

class AuthScreen extends StatefulWidget {
  const AuthScreen({super.key, this.infoMessage});
  static const routeName = '/';
  final String? infoMessage;

  @override
  State<AuthScreen> createState() => _AuthScreenState();
}

class _AuthScreenState extends State<AuthScreen> {
  final _controller = Get.put(AuthController());
  final _loginEmailController    = TextEditingController();
  final _loginPasswordController = TextEditingController();
  final _loginPasswordFocusNode  = FocusNode();
  final _registerUsernameController = TextEditingController();
  final _registerEmailController    = TextEditingController();
  final _registerPasswordController = TextEditingController();

  String? _loginEmailError;
  String? _loginPasswordError;
  String? _registerUsernameError;
  String? _registerEmailError;
  String? _registerPasswordError;
  bool _rememberMe = false;

  @override
  void initState() {
    super.initState();
    SystemChrome.setPreferredOrientations(DeviceOrientation.values);
    if (widget.infoMessage != null && widget.infoMessage!.trim().isNotEmpty) {
      WidgetsBinding.instance.addPostFrameCallback((_) {
        if (!mounted) return;
        Get.snackbar('Sesion', widget.infoMessage!,
            snackPosition: SnackPosition.BOTTOM,
            duration: const Duration(seconds: 2));
      });
    }
  }

  @override
  void dispose() {
    SystemChrome.setPreferredOrientations(DeviceOrientation.values);
    _loginEmailController.dispose();
    _loginPasswordController.dispose();
    _loginPasswordFocusNode.dispose();
    _registerUsernameController.dispose();
    _registerEmailController.dispose();
    _registerPasswordController.dispose();
    super.dispose();
  }

  Future<void> _submitLogin() async {
    final emailError    = Validators.validateEmail(_loginEmailController.text.trim());
    final passwordError = Validators.validatePassword(_loginPasswordController.text.trim());
    setState(() {
      _loginEmailError    = emailError;
      _loginPasswordError = passwordError;
    });
    if (emailError != null || passwordError != null) return;
    final errorMessage = await _controller.submitLogin(
      _loginEmailController.text.trim(),
      _loginPasswordController.text.trim(),
    );
    if (!mounted) return;
    if (errorMessage == null) _showBiometricPrompt();
  }

  Future<void> _submitRegister() async {
    final usernameError = Validators.validateUsername(_registerUsernameController.text.trim());
    final emailError    = Validators.validateEmail(_registerEmailController.text.trim());
    final passwordError = Validators.validatePassword(_registerPasswordController.text.trim());
    setState(() {
      _registerUsernameError = usernameError;
      _registerEmailError    = emailError;
      _registerPasswordError = passwordError;
    });
    if (usernameError != null || emailError != null || passwordError != null) return;
    final user = UserModel(
      username: _registerUsernameController.text.trim(),
      email:    _registerEmailController.text.trim(),
      password: _registerPasswordController.text.trim(),
    );
    final result = await _controller.submitRegister(user);
    if (!mounted) return;
    if (result.isSuccess) {
      final registeredEmail = _registerEmailController.text.trim();
      _registerUsernameController.clear();
      _registerEmailController.clear();
      _registerPasswordController.clear();
      _loginEmailController.text = registeredEmail;
      _loginPasswordController.clear();
      setState(() {
        _loginEmailError = _loginPasswordError = null;
        _registerUsernameError = _registerEmailError = _registerPasswordError = null;
      });
      _controller.toggleLogin(true);
      WidgetsBinding.instance.addPostFrameCallback((_) {
        if (mounted) _loginPasswordFocusNode.requestFocus();
      });
      Get.snackbar('Registro exitoso', 'Ahora puedes iniciar sesión.',
          snackPosition: SnackPosition.BOTTOM,
          duration: const Duration(seconds: 2));
      if (result.warningMessage != null) {
        Get.snackbar('Aviso', result.warningMessage!,
            snackPosition: SnackPosition.BOTTOM,
            duration: const Duration(seconds: 4),
            backgroundColor: const Color(0xFF1E1E1E),
            colorText: _burntOrange);
      }
    }
  }

  Future<void> _signInWithGoogle() async {
    final errorMessage = await _controller.submitGoogleLogin();
    if (!mounted) return;
    if (errorMessage == null) _showBiometricPrompt(isBiometricGoogle: true);
  }

  Future<void> _submitBiometricLogin() async {
    final errorMessage = await _controller.submitBiometricLogin();
    if (!mounted) return;
    if (errorMessage == null) Get.offAll(() => const HomeScreen());
  }

  void _showBiometricPrompt({bool isBiometricGoogle = false}) {
    if (!_controller.isBiometricAvailable.value) {
      Get.offAll(() => const HomeScreen());
      return;
    }
    Get.dialog(
      AlertDialog(
        backgroundColor: const Color(0xFF1E1E1E),
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(4),
          side: BorderSide(color: _goldMetal.withValues(alpha: 0.5)),
        ),
        title: const Text('SEGURIDAD BIOMÉTRICA',
            style: TextStyle(
                color: _burntOrange,
                fontFamily: 'serif',
                fontWeight: FontWeight.w900,
                letterSpacing: 2.0)),
        content: const Text(
            '¿Deseas activar acceso por huella dactilar para tu próximo login?',
            style: TextStyle(color: _warText, fontSize: 13)),
        actions: [
          TextButton(
            onPressed: () {
              Get.back();
              Get.offAll(() => const HomeScreen());
            },
            child: const Text('AHORA NO', style: TextStyle(color: _warMuted)),
          ),
          ElevatedButton(
            style: ElevatedButton.styleFrom(
                backgroundColor: _burntOrange,
                shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(4))),
            onPressed: () async {
              Get.back();
              if (isBiometricGoogle) {
                await _controller.saveBiometricGoogleLogin();
              } else {
                await _controller.saveBiometricEmailCredentials(
                  _loginEmailController.text.trim(),
                  _loginPasswordController.text.trim(),
                );
              }
              Get.offAll(() => const HomeScreen());
            },
            child: const Text('ACTIVAR', style: TextStyle(color: Colors.white)),
          ),
        ],
      ),
      barrierDismissible: false,
    );
  }

  // ─────────────────────────────────────────────────────────────
  // Root scaffold
  // ─────────────────────────────────────────────────────────────
  @override
  Widget build(BuildContext context) {
    final isPortrait =
        MediaQuery.of(context).orientation == Orientation.portrait;
    return Scaffold(
      backgroundColor: _woodDark,
      resizeToAvoidBottomInset: true,
      body: Stack(
        children: [
          // Full-screen wood-grain background
          Positioned.fill(
            child: CustomPaint(
              painter: _WoodGrainPainter(),
              child: Container(
                decoration: const BoxDecoration(
                  gradient: LinearGradient(
                    begin: Alignment.topLeft,
                    end: Alignment.bottomRight,
                    colors: [
                      Color(0xFF4E342E),
                      Color(0xFF3E2723),
                      Color(0xFF2A1510),
                      Color(0xFF3E2723),
                    ],
                    stops: [0.0, 0.35, 0.65, 1.0],
                  ),
                ),
              ),
            ),
          ),
          if (isPortrait)
            _buildPortraitAuthLayout()
          else
            Row(
              children: [
                Expanded(flex: 5, child: _buildParchmentPanel()),
                Expanded(flex: 4, child: _buildFormPanel()),
              ],
            ),
        ],
      ),
    );
  }

  // ─────────────────────────────────────────────────────────────
  // Portrait: titulo WAR + formulario sobre el fondo de madera
  // ─────────────────────────────────────────────────────────────
  Widget _buildPortraitAuthLayout() {
    return SafeArea(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          // WAR header transparente — el fondo de madera se ve a través
          Padding(
            padding: const EdgeInsets.fromLTRB(24, 22, 24, 10),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.center,
              children: [
                Text(
                  'WAR',
                  textAlign: TextAlign.center,
                  style: TextStyle(
                    fontFamily: 'serif',
                    fontSize: 52,
                    fontWeight: FontWeight.w900,
                    color: _burntOrange,
                    letterSpacing: 6,
                    shadows: [
                      Shadow(
                          color: _burntGlow.withValues(alpha: 0.85),
                          blurRadius: 20),
                      Shadow(
                          color: _candleAmber.withValues(alpha: 0.45),
                          blurRadius: 40),
                    ],
                  ),
                ),
                const SizedBox(height: 3),
                Text(
                  'MMORPG — PVP',
                  textAlign: TextAlign.center,
                  style: TextStyle(
                    fontSize: 10,
                    letterSpacing: 3.5,
                    color: _silverSteel.withValues(alpha: 0.80),
                    fontWeight: FontWeight.w600,
                  ),
                ),
                const SizedBox(height: 10),
                // Línea decorativa
                Container(
                  height: 0.5,
                  decoration: BoxDecoration(
                    gradient: LinearGradient(
                      colors: [
                        Colors.transparent,
                        _silverSteel.withValues(alpha: 0.35),
                        Colors.transparent,
                      ],
                    ),
                  ),
                ),
              ],
            ),
          ),
          // Formulario con scroll — tarjeta sobre el fondo de madera
          Expanded(
            child: SingleChildScrollView(
              padding:
                  const EdgeInsets.symmetric(horizontal: 20, vertical: 8),
              child: _buildOrnateCard(
                child: Obx(() {
                  final isLogin = _controller.showLogin.value;
                  return AnimatedSwitcher(
                    duration: const Duration(milliseconds: 280),
                    child: isLogin
                        ? KeyedSubtree(
                            key: const ValueKey('login'),
                            child: _buildLoginContent())
                        : KeyedSubtree(
                            key: const ValueKey('register'),
                            child: _buildRegisterContent()),
                  );
                }),
              ),
            ),
          ),
          const SizedBox(height: 10),
        ],
      ),
    );
  }

  // ─────────────────────────────────────────────────────────────
  // LEFT: Aged Parchment Branding Panel
  // ─────────────────────────────────────────────────────────────
  Widget _buildParchmentPanel() {
    return Stack(
      fit: StackFit.expand,
      children: [
        // Aged parchment gradient
        Container(
          decoration: const BoxDecoration(
            gradient: LinearGradient(
              begin: Alignment.topCenter,
              end: Alignment.bottomCenter,
              colors: [
                Color(0xFFEDE0C4),
                Color(0xFFD9C9A8),
                Color(0xFFC4A882),
                Color(0xFF6D4C41),
                Color(0xFF3E2723),
              ],
              stops: [0.0, 0.35, 0.60, 0.82, 1.0],
            ),
          ),
        ),
        // Worn-edge vignette
        Positioned.fill(
          child: DecoratedBox(
            decoration: const BoxDecoration(
              gradient: RadialGradient(
                center: Alignment.center,
                radius: 0.85,
                colors: [Colors.transparent, Color(0x44000000)],
              ),
            ),
          ),
        ),
        // Subtle parchment ruled lines
        Positioned.fill(child: CustomPaint(painter: _ParchmentLinesPainter())),
        // Candle glow — top left
        Positioned(
          top: -40, left: -40,
          child: Container(
            width: 220, height: 220,
            decoration: const BoxDecoration(
              shape: BoxShape.circle,
              gradient: RadialGradient(
                colors: [Color(0x55FF8F00), Color(0x22FF6F00), Colors.transparent],
              ),
            ),
          ),
        ),
        // Candle glow — bottom right
        Positioned(
          bottom: -20, right: 30,
          child: Container(
            width: 170, height: 170,
            decoration: const BoxDecoration(
              shape: BoxShape.circle,
              gradient: RadialGradient(
                colors: [Color(0x33FF8F00), Colors.transparent],
              ),
            ),
          ),
        ),
        // Right-edge fade into wood form panel
        Positioned(
          right: 0, top: 0, bottom: 0, width: 70,
          child: Container(
            decoration: const BoxDecoration(
              gradient: LinearGradient(
                begin: Alignment.centerLeft,
                end: Alignment.centerRight,
                colors: [Colors.transparent, Color(0xFF2E1A0E)],
              ),
            ),
          ),
        ),
        // Branding content
        Positioned(
          top: 0, left: 0, right: 70, bottom: 0,
          child: SafeArea(
            child: Padding(
              padding: const EdgeInsets.fromLTRB(20, 12, 10, 12),
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                crossAxisAlignment: CrossAxisAlignment.center,
                children: [
                  Column(
                    crossAxisAlignment: CrossAxisAlignment.center,
                    children: [
                          // WAR — glowing orange serif
                          Text(
                            'WAR',
                            style: TextStyle(
                              fontFamily: 'serif',
                              fontSize: 50,
                              fontWeight: FontWeight.w900,
                              color: _burntOrange,
                              letterSpacing: 6,
                              shadows: [
                                Shadow(color: _burntGlow.withValues(alpha: 0.85), blurRadius: 18),
                                Shadow(color: _candleAmber.withValues(alpha: 0.45), blurRadius: 36),
                              ],
                            ),
                          ),
                          const SizedBox(height: 2),
                          Text(
                            'MMORPG — PVP',
                            style: TextStyle(
                              fontSize: 10,
                              letterSpacing: 3.5,
                              color: _silverSteel.withValues(alpha: 0.85),
                              fontWeight: FontWeight.w600,
                            ),
                          ),
                          const SizedBox(height: 6),
                          // Decorative rule
                          Container(
                            width: 165,
                            height: 1,
                            decoration: BoxDecoration(
                              gradient: LinearGradient(
                                colors: [
                                  Colors.transparent,
                                  _silverSteel.withValues(alpha: 0.45),
                                  Colors.transparent,
                                ],
                              ),
                            ),
                          ),
                          const SizedBox(height: 6),
                          Text(
                            'LOGIN TO YOUR WAR COUNCIL',
                            style: TextStyle(
                              fontSize: 8,
                              letterSpacing: 2,
                              color: _silverSteel.withValues(alpha: 0.60),
                              fontWeight: FontWeight.w500,
                            ),
                          ),
                    ],
                  ),
                ],
              ),
            ),
          ),
        ),
      ],
    );
  }

  // ─────────────────────────────────────────────────────────────
  // RIGHT: Dark Wood + Smoked-Glass Form Panel
  // ─────────────────────────────────────────────────────────────
  Widget _buildFormPanel() {
    return Container(
      decoration: const BoxDecoration(
        gradient: LinearGradient(
          begin: Alignment.topLeft,
          end: Alignment.bottomRight,
          colors: [Color(0xFF3E2723), Color(0xFF2A1510), Color(0xFF1E0F08)],
        ),
      ),
      child: SafeArea(
        left: false,
        child: Center(
          child: SingleChildScrollView(
            padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
            child: _buildOrnateCard(
              child: Obx(() {
                final isLogin = _controller.showLogin.value;
                return AnimatedSwitcher(
                  duration: const Duration(milliseconds: 280),
                  child: isLogin
                      ? KeyedSubtree(key: const ValueKey('login'),    child: _buildLoginContent())
                      : KeyedSubtree(key: const ValueKey('register'), child: _buildRegisterContent()),
                );
              }),
            ),
          ),
        ),
      ),
    );
  }

  // Smoked glass card with ornate dark-metal filigree border
  Widget _buildOrnateCard({required Widget child}) {
    return Container(
      decoration: BoxDecoration(
        color: _smokedGlass,
        border: Border.all(color: _goldMetal.withValues(alpha: 0.55), width: 1.5),
        borderRadius: BorderRadius.circular(3),
        boxShadow: [
          BoxShadow(color: Colors.black.withValues(alpha: 0.65), blurRadius: 28, spreadRadius: 6),
          BoxShadow(color: _burntOrange.withValues(alpha: 0.07), blurRadius: 35),
        ],
      ),
      child: Stack(
        children: [
          // Inner inset border (double-frame filigree effect)
          Positioned.fill(
            child: Container(
              margin: const EdgeInsets.all(6),
              decoration: BoxDecoration(
                border: Border.all(color: _goldMetal.withValues(alpha: 0.22), width: 0.5),
                borderRadius: BorderRadius.circular(1),
              ),
            ),
          ),
          // Ornate corner flourishes
          const Positioned(top: 3, left: 3,   child: _CornerFlourish()),
          const Positioned(top: 3, right: 3,  child: _CornerFlourish(flipX: true)),
          const Positioned(bottom: 3, left: 3,  child: _CornerFlourish(flipY: true)),
          const Positioned(bottom: 3, right: 3, child: _CornerFlourish(flipX: true, flipY: true)),
          // Form content
          Padding(
            padding: const EdgeInsets.fromLTRB(20, 18, 20, 14),
            child: child,
          ),
        ],
      ),
    );
  }

  // ─────────────────────────────────────────────────────────────
  // Login form content
  // ─────────────────────────────────────────────────────────────
  Widget _buildLoginContent() {
    return Obx(() {
      final errorMessage   = _controller.loginError.value;
      final biometricEnabled = _controller.isBiometricEnabled.value;
      return Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        mainAxisSize: MainAxisSize.min,
        children: [
          // Section header with accent bar
          Row(
            children: [
              Container(
                width: 3, height: 16,
                decoration: BoxDecoration(
                  color: _burntOrange,
                  boxShadow: [BoxShadow(color: _burntGlow, blurRadius: 8)],
                ),
              ),
              const SizedBox(width: 8),
              const Text(
                'INICIAR SESIÓN',
                style: TextStyle(
                  color: _warText,
                  fontFamily: 'serif',
                  fontSize: 14,
                  fontWeight: FontWeight.w900,
                  letterSpacing: 2,
                ),
              ),
            ],
          ),
          const SizedBox(height: 14),
          // Email field — keyhole icon
          _MedievalInput(
            controller: _loginEmailController,
            hint: 'Correo electrónico',
            icon: Icons.vpn_key_outlined,
            type: TextInputType.emailAddress,
            error: _loginEmailError,
          ),
          const SizedBox(height: 8),
          // Password field — shield icon
          _MedievalInput(
            controller: _loginPasswordController,
            hint: 'Contraseña',
            icon: Icons.shield_outlined,
            obscure: true,
            error: _loginPasswordError,
            focusNode: _loginPasswordFocusNode,
          ),
          const SizedBox(height: 10),
          // Recuérdame — orange-glowing custom toggle
          Row(
            mainAxisSize: MainAxisSize.min,
            children: [
              _OrangeToggle(
                value: _rememberMe,
                onChanged: (v) => setState(() => _rememberMe = v),
              ),
              const SizedBox(width: 8),
              Text(
                'Recuérdame',
                style: TextStyle(
                  color: _silverSteel.withValues(alpha: 0.85),
                  fontSize: 11,
                  letterSpacing: 0.5,
                ),
              ),
            ],
          ),
          if (errorMessage != null) ...[
            const SizedBox(height: 8),
            Text(errorMessage,
                style: const TextStyle(color: _warError, fontSize: 11),
                textAlign: TextAlign.center),
          ],
          const SizedBox(height: 12),
          // ENTRAR AL CONSEJO
          AuthPrimaryButton(
            text: 'ENTRAR AL CONSEJO',
            onPressed: _submitLogin,
            isLoading: _controller.isLoading.value,
          ),
          const SizedBox(height: 8),
          // Secondary: biometric / Google
          Row(
            children: [
              if (biometricEnabled) ...[
                Expanded(
                  child: _WarSmallButton(
                    icon: Icons.fingerprint,
                    label: 'HUELLA',
                    isLoading: _controller.isBiometricLoading.value,
                    onPressed: _submitBiometricLogin,
                    borderColor: _burntOrange.withValues(alpha: 0.4),
                  ),
                ),
                const SizedBox(width: 8),
              ],
              Expanded(
                child: _WarSmallButton(
                  icon: Icons.g_mobiledata,
                  label: 'GOOGLE',
                  isLoading: _controller.isGoogleLoading.value,
                  onPressed: _signInWithGoogle,
                  borderColor: _glassBorder,
                ),
              ),
            ],
          ),
          const SizedBox(height: 10),
          // Register link
          Center(
            child: GestureDetector(
              onTap: () => _controller.toggleLogin(false),
              child: RichText(
                text: const TextSpan(
                  style: TextStyle(fontSize: 10, color: _warMuted, letterSpacing: 0.5),
                  children: [
                    TextSpan(text: '¿NUEVO GUERRERO? '),
                    TextSpan(
                      text: 'REGÍSTRATE',
                      style: TextStyle(
                          color: _burntOrange,
                          fontWeight: FontWeight.w700,
                          letterSpacing: 1),
                    ),
                  ],
                ),
              ),
            ),
          ),
        ],
      );
    });
  }

  // ─────────────────────────────────────────────────────────────
  // Register form content
  // ─────────────────────────────────────────────────────────────
  Widget _buildRegisterContent() {
    return Obx(() {
      final errorMessage   = _controller.registerError.value;
      final warningMessage = _controller.registerWarning.value;
      return Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        mainAxisSize: MainAxisSize.min,
        children: [
          Row(
            children: [
              Container(
                width: 3, height: 16,
                decoration: BoxDecoration(
                  color: _burntOrange,
                  boxShadow: [BoxShadow(color: _burntGlow, blurRadius: 8)],
                ),
              ),
              const SizedBox(width: 8),
              const Text(
                'CREAR CUENTA',
                style: TextStyle(
                  color: _warText,
                  fontFamily: 'serif',
                  fontSize: 14,
                  fontWeight: FontWeight.w900,
                  letterSpacing: 2,
                ),
              ),
            ],
          ),
          const SizedBox(height: 14),
          _MedievalInput(
            controller: _registerUsernameController,
            hint: 'Tu alias en batalla',
            icon: Icons.person_outline,
            error: _registerUsernameError,
          ),
          const SizedBox(height: 8),
          _MedievalInput(
            controller: _registerEmailController,
            hint: 'Correo electrónico',
            icon: Icons.vpn_key_outlined,
            type: TextInputType.emailAddress,
            error: _registerEmailError,
          ),
          const SizedBox(height: 8),
          _MedievalInput(
            controller: _registerPasswordController,
            hint: 'Contraseña',
            icon: Icons.shield_outlined,
            obscure: true,
            error: _registerPasswordError,
          ),
          if (errorMessage != null) ...[
            const SizedBox(height: 8),
            Text(errorMessage,
                style: const TextStyle(color: _warError, fontSize: 11),
                textAlign: TextAlign.center),
          ],
          if (warningMessage != null) ...[
            const SizedBox(height: 6),
            Text(warningMessage,
                style: const TextStyle(color: _burntOrange, fontSize: 11),
                textAlign: TextAlign.center),
          ],
          const SizedBox(height: 12),
          AuthPrimaryButton(
            text: 'FORJAR ALIANZA',
            onPressed: _submitRegister,
            isLoading: _controller.isLoading.value,
          ),
          const SizedBox(height: 10),
          Center(
            child: GestureDetector(
              onTap: () => _controller.toggleLogin(true),
              child: RichText(
                text: const TextSpan(
                  style: TextStyle(fontSize: 10, color: _warMuted, letterSpacing: 0.5),
                  children: [
                    TextSpan(text: '¿Ya tienes cuenta? '),
                    TextSpan(
                      text: 'INGRESA',
                      style: TextStyle(
                          color: _burntOrange,
                          fontWeight: FontWeight.w700,
                          letterSpacing: 1),
                    ),
                  ],
                ),
              ),
            ),
          ),
        ],
      );
    });
  }
}

// ═══════════════════════════════════════════════════════════════
// Medieval input field — dark metal beveled frame
// ═══════════════════════════════════════════════════════════════
class _MedievalInput extends StatefulWidget {
  const _MedievalInput({
    required this.controller,
    required this.hint,
    required this.icon,
    this.obscure  = false,
    this.type,
    this.error,
    this.focusNode,
  });

  final TextEditingController controller;
  final String hint;
  final IconData icon;
  final bool obscure;
  final TextInputType? type;
  final String? error;
  final FocusNode? focusNode;

  @override
  State<_MedievalInput> createState() => _MedievalInputState();
}

class _MedievalInputState extends State<_MedievalInput> {
  late bool _isObscured;
  late final FocusNode _effectiveNode;
  late final bool _ownsNode;

  @override
  void initState() {
    super.initState();
    _isObscured = widget.obscure;
    _ownsNode   = widget.focusNode == null;
    _effectiveNode = widget.focusNode ?? FocusNode();
  }

  @override
  void dispose() {
    if (_ownsNode) _effectiveNode.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return TextField(
      controller:  widget.controller,
      focusNode:   _effectiveNode,
      obscureText: _isObscured,
      keyboardType: widget.type,
      style: const TextStyle(color: _warText, fontSize: 13),
      decoration: InputDecoration(
        hintText: widget.hint,
        errorText: widget.error,
        hintStyle: TextStyle(color: _silverSteel.withValues(alpha: 0.40), fontSize: 12),
        prefixIcon: Icon(
          widget.icon,
          size: 17,
          color: _silverSteel.withValues(alpha: 0.55),
        ),
        filled: true,
        fillColor: const Color(0xFF161616),
        isDense: true,
        contentPadding: const EdgeInsets.symmetric(horizontal: 12, vertical: 13),
        suffixIcon: widget.obscure
            ? IconButton(
                splashRadius: 16,
                icon: Icon(
                  _isObscured
                      ? Icons.visibility_off_outlined
                      : Icons.visibility_outlined,
                  size: 17,
                  color: _silverSteel.withValues(alpha: 0.5),
                ),
                onPressed: () => setState(() => _isObscured = !_isObscured),
              )
            : null,
        enabledBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(4),
          borderSide: BorderSide(color: _glassBorder.withValues(alpha: 0.8)),
        ),
        focusedBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(4),
          borderSide: const BorderSide(color: _burntOrange, width: 1.5),
        ),
        errorBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(4),
          borderSide: const BorderSide(color: _warError),
        ),
        focusedErrorBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(4),
          borderSide: const BorderSide(color: _warError, width: 1.5),
        ),
        errorStyle: const TextStyle(color: _warError, fontSize: 10),
      ),
    );
  }
}

// ═══════════════════════════════════════════════════════════════
// Small secondary action buttons (Google / Biometric)
// ═══════════════════════════════════════════════════════════════
class _WarSmallButton extends StatelessWidget {
  const _WarSmallButton({
    required this.icon,
    required this.label,
    required this.isLoading,
    required this.onPressed,
    required this.borderColor,
  });

  final IconData icon;
  final String label;
  final bool isLoading;
  final VoidCallback onPressed;
  final Color borderColor;

  @override
  Widget build(BuildContext context) {
    return OutlinedButton.icon(
      onPressed: isLoading ? null : onPressed,
      icon: isLoading
          ? const SizedBox(
              width: 14, height: 14,
              child: CircularProgressIndicator(strokeWidth: 1.5, color: _warMuted),
            )
          : Icon(icon, size: 16, color: _warText),
      label: Text(label,
          style: const TextStyle(fontSize: 9, color: _warText, letterSpacing: 1)),
      style: OutlinedButton.styleFrom(
        padding: const EdgeInsets.symmetric(vertical: 10),
        side: BorderSide(color: borderColor),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(4)),
        backgroundColor: const Color(0xFF161616),
      ),
    );
  }
}

// ═══════════════════════════════════════════════════════════════
// Orange-glowing custom toggle for "Recuérdame"
// ═══════════════════════════════════════════════════════════════
class _OrangeToggle extends StatelessWidget {
  const _OrangeToggle({required this.value, required this.onChanged});

  final bool value;
  final ValueChanged<bool> onChanged;

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: () => onChanged(!value),
      child: AnimatedContainer(
        duration: const Duration(milliseconds: 200),
        width: 32, height: 17,
        decoration: BoxDecoration(
          borderRadius: BorderRadius.circular(9),
          color: value ? _burntOrange : const Color(0xFF3A3A3A),
          border: Border.all(
            color: value
                ? _burntGlow.withValues(alpha: 0.7)
                : const Color(0xFF555555),
            width: 0.5,
          ),
          boxShadow: value
              ? [BoxShadow(color: _burntOrange.withValues(alpha: 0.55), blurRadius: 8)]
              : null,
        ),
        child: Align(
          alignment: value ? Alignment.centerRight : Alignment.centerLeft,
          child: Container(
            width: 13, height: 13,
            margin: const EdgeInsets.all(2),
            decoration: const BoxDecoration(
              shape: BoxShape.circle,
              color: Colors.white,
            ),
          ),
        ),
      ),
    );
  }
}

// ═══════════════════════════════════════════════════════════════
// Ornate filigree corner decoration
// ═══════════════════════════════════════════════════════════════
class _CornerFlourish extends StatelessWidget {
  const _CornerFlourish({this.flipX = false, this.flipY = false});

  final bool flipX;
  final bool flipY;

  @override
  Widget build(BuildContext context) {
    Widget child = const SizedBox(
      width: 12, height: 12,
      child: CustomPaint(painter: _FiligreePainter()),
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

class _FiligreePainter extends CustomPainter {
  const _FiligreePainter();

  @override
  void paint(Canvas canvas, Size size) {
    final paint = Paint()
      ..color = const Color(0xFFFFB300).withValues(alpha: 0.62)
      ..strokeWidth = 1.0
      ..style = PaintingStyle.stroke
      ..strokeCap = StrokeCap.round;

    // L-shaped corner lines
    canvas.drawLine(Offset(size.width, 0), Offset.zero, paint);
    canvas.drawLine(Offset.zero, Offset(0, size.height), paint);

    // Mid-arm tick marks
    canvas.drawLine(Offset(size.width * 0.55, 0), Offset(size.width * 0.55, 3.5), paint);
    canvas.drawLine(Offset(0, size.height * 0.55), Offset(3.5, size.height * 0.55), paint);

    // Corner dot
    paint.style = PaintingStyle.fill;
    canvas.drawCircle(Offset.zero, 1.8, paint);
  }

  @override
  bool shouldRepaint(covariant CustomPainter oldDelegate) => false;
}

// ═══════════════════════════════════════════════════════════════
// Wood grain texture painter
// ═══════════════════════════════════════════════════════════════
class _WoodGrainPainter extends CustomPainter {
  @override
  void paint(Canvas canvas, Size size) {
    final rng   = math.Random(17);
    final paint = Paint()
      ..style      = PaintingStyle.stroke
      ..strokeWidth = 0.8;

    for (int i = 0; i < 28; i++) {
      final startY = size.height * rng.nextDouble();
      final alpha  = 0.02 + rng.nextDouble() * 0.06;
      paint.color = Color.fromRGBO(0, 0, 0, alpha);

      final path = Path()..moveTo(0, startY);
      double cx = 0, cy = startY;
      while (cx < size.width) {
        final step  = 30.0 + rng.nextDouble() * 80.0;
        final drift = (rng.nextDouble() - 0.5) * 10.0;
        path.lineTo(cx + step, cy + drift);
        cx += step;
        cy += drift;
      }
      canvas.drawPath(path, paint);
    }
  }

  @override
  bool shouldRepaint(covariant CustomPainter oldDelegate) => false;
}

// ═══════════════════════════════════════════════════════════════
// Parchment ruled-line painter (faint horizontal lines)
// ═══════════════════════════════════════════════════════════════
class _ParchmentLinesPainter extends CustomPainter {
  @override
  void paint(Canvas canvas, Size size) {
    final paint = Paint()
      ..color       = const Color(0x0E000000)
      ..strokeWidth = 0.5;

    double y = 22;
    while (y < size.height * 0.78) {
      canvas.drawLine(Offset(12, y), Offset(size.width - 12, y), paint);
      y += 18;
    }
  }

  @override
  bool shouldRepaint(covariant CustomPainter oldDelegate) => false;
}
