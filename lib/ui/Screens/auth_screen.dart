import 'dart:ui';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:get/get.dart';
import '../../controllers/auth_controller.dart';
import '../../models/user_model.dart';
import '../../utils/validators.dart';
import '../widgets/auth/auth_widgets.dart';
import 'home_screen.dart';

const _warAccent = Color(0xFFE6451C);
const _warText = Color(0xFFE8E8E8);
const _warMuted = Color(0xFF888888);
const _warInputBg = Color(0xFF1A1A1E);
const _warBorder = Color(0xFF2A2A2E);

class AuthScreen extends StatefulWidget {
  const AuthScreen({super.key, this.infoMessage});
  static const routeName = '/';

  final String? infoMessage;

  @override
  State<AuthScreen> createState() => _AuthScreenState();
}

class _AuthScreenState extends State<AuthScreen> {
  final _controller = Get.put(AuthController());
  final _loginEmailController = TextEditingController();
  final _loginPasswordController = TextEditingController();
  final _loginPasswordFocusNode = FocusNode();
  final _registerUsernameController = TextEditingController();
  final _registerEmailController = TextEditingController();
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
    // Allow automatic orientation rotation on login screen
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
    final emailError = Validators.validateEmail(_loginEmailController.text.trim());
    final passwordError = Validators.validatePassword(_loginPasswordController.text.trim());
    setState(() {
      _loginEmailError = emailError;
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
    final emailError = Validators.validateEmail(_registerEmailController.text.trim());
    final passwordError = Validators.validatePassword(_registerPasswordController.text.trim());
    setState(() {
      _registerUsernameError = usernameError;
      _registerEmailError = emailError;
      _registerPasswordError = passwordError;
    });
    if (usernameError != null || emailError != null || passwordError != null) return;

    final user = UserModel(
      username: _registerUsernameController.text.trim(),
      email: _registerEmailController.text.trim(),
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
        _loginEmailError = null;
        _loginPasswordError = null;
        _registerUsernameError = null;
        _registerEmailError = null;
        _registerPasswordError = null;
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
            backgroundColor: const Color(0xFF1A1A1E),
            colorText: _warAccent);
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
        backgroundColor: const Color(0xFF141418),
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(12),
          side: const BorderSide(color: _warBorder),
        ),
        title: const Text('SEGURIDAD BIOMÉTRICA',
            style: TextStyle(
                color: _warAccent,
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
                backgroundColor: _warAccent,
                shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(6))),
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

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: const Color(0xFF0A0707),
      resizeToAvoidBottomInset: true,
      body: Row(
        children: [
          Expanded(flex: 5, child: _buildArtPanel()),
          Expanded(flex: 4, child: _buildFormPanel()),
        ],
      ),
    );
  }

  Widget _buildArtPanel() {
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
        // Fire glow rising from bottom
        Positioned(
          bottom: 0,
          left: 0,
          right: 0,
          height: double.infinity,
          child: Container(
            decoration: const BoxDecoration(
              gradient: RadialGradient(
                center: Alignment.bottomCenter,
                radius: 1.2,
                colors: [Color(0xBBE6451C), Color(0x66B83318), Color(0x22802010), Colors.transparent],
                stops: [0.0, 0.35, 0.65, 1.0],
              ),
            ),
          ),
        ),
        // Top-left ember glow
        Positioned(
          top: -40,
          left: -20,
          child: Container(
            width: 180,
            height: 180,
            decoration: const BoxDecoration(
              shape: BoxShape.circle,
              gradient: RadialGradient(
                colors: [Color(0x44E6451C), Colors.transparent],
              ),
            ),
          ),
        ),
        // Massive faded WAR text as texture
        Center(
          child: Opacity(
            opacity: 0.04,
            child: Text(
              'WAR',
              style: TextStyle(
                fontFamily: 'serif',
                fontSize: 220,
                fontWeight: FontWeight.w900,
                color: Colors.white,
                letterSpacing: 24,
              ),
            ),
          ),
        ),
        // Center branding
        Center(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Container(
                width: 72,
                height: 72,
                decoration: BoxDecoration(
                  shape: BoxShape.circle,
                  border: Border.all(color: const Color(0x55E6451C), width: 1.5),
                  color: const Color(0x22E6451C),
                ),
                child: const Icon(Icons.security, size: 38, color: Color(0xAAE6451C)),
              ),
              const SizedBox(height: 14),
              const Text(
                'WAR',
                style: TextStyle(
                  color: _warAccent,
                  fontFamily: 'serif',
                  fontSize: 40,
                  fontWeight: FontWeight.w900,
                  letterSpacing: 10,
                  shadows: [Shadow(color: Color(0xBBE6451C), blurRadius: 24)],
                ),
              ),
              const SizedBox(height: 4),
              const Text(
                'MMORPG · PVP',
                style: TextStyle(color: _warMuted, fontSize: 10, letterSpacing: 4),
              ),
            ],
          ),
        ),
        // Right-edge fade to blend with form panel
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

  Widget _buildFormPanel() {
    return ClipRect(
      child: BackdropFilter(
        filter: ImageFilter.blur(sigmaX: 0, sigmaY: 0),
        child: Container(
          color: const Color(0xFF141418),
          child: SafeArea(
            left: false,
            child: Padding(
              padding: const EdgeInsets.symmetric(horizontal: 22, vertical: 10),
              child: Obx(() {
                final isLogin = _controller.showLogin.value;
                return AnimatedSwitcher(
                  duration: const Duration(milliseconds: 280),
                  child: isLogin
                      ? KeyedSubtree(
                          key: const ValueKey('login'),
                          child: _buildLoginContent(),
                        )
                      : KeyedSubtree(
                          key: const ValueKey('register'),
                          child: _buildRegisterContent(),
                        ),
                );
              }),
            ),
          ),
        ),
      ),
    );
  }

  Widget _buildLoginContent() {
    return Obx(() {
      final errorMessage = _controller.loginError.value;
      final biometricEnabled = _controller.isBiometricEnabled.value;
      return SingleChildScrollView(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          mainAxisSize: MainAxisSize.min,
          children: [
            const Text(
              'INICIAR SESIÓN',
              style: TextStyle(
                color: _warText,
                fontFamily: 'serif',
                fontSize: 17,
                fontWeight: FontWeight.w900,
                letterSpacing: 2,
              ),
            ),
            const SizedBox(height: 12),
            _WarInput(
              controller: _loginEmailController,
              hint: 'Correo electrónico',
              icon: Icons.email_outlined,
              type: TextInputType.emailAddress,
              error: _loginEmailError,
            ),
            const SizedBox(height: 8),
            _WarInput(
              controller: _loginPasswordController,
              hint: 'Contraseña',
              icon: Icons.lock_outline,
              obscure: true,
              error: _loginPasswordError,
              focusNode: _loginPasswordFocusNode,
            ),
            const SizedBox(height: 8),
            Row(
              children: [
                SizedBox(
                  width: 20,
                  height: 20,
                  child: Checkbox(
                    value: _rememberMe,
                    onChanged: (v) => setState(() => _rememberMe = v ?? false),
                    fillColor: WidgetStateProperty.resolveWith((s) =>
                        s.contains(WidgetState.selected)
                            ? _warAccent
                            : Colors.transparent),
                    side: const BorderSide(color: _warMuted),
                    shape: RoundedRectangleBorder(
                        borderRadius: BorderRadius.circular(3)),
                  ),
                ),
                const SizedBox(width: 8),
                const Text('Recuérdame',
                    style: TextStyle(color: _warMuted, fontSize: 11)),
              ],
            ),
            if (errorMessage != null) ...[
              const SizedBox(height: 8),
              Text(errorMessage,
                  style: const TextStyle(
                      color: Color(0xFFFF6B6B), fontSize: 11),
                  textAlign: TextAlign.center),
            ],
            const SizedBox(height: 12),
            AuthPrimaryButton(
              text: 'ENTRAR A LA ARENA',
              onPressed: _submitLogin,
              isLoading: _controller.isLoading.value,
            ),
            const SizedBox(height: 8),
            Row(
              children: [
                if (biometricEnabled) ...[
                  Expanded(
                    child: _WarSmallButton(
                      icon: Icons.fingerprint,
                      label: 'HUELLA',
                      isLoading: _controller.isBiometricLoading.value,
                      onPressed: _submitBiometricLogin,
                      borderColor: _warAccent.withValues(alpha: 0.4),
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
                    borderColor: _warBorder,
                  ),
                ),
              ],
            ),
            const SizedBox(height: 10),
            Center(
              child: GestureDetector(
                onTap: () => _controller.toggleLogin(false),
                child: RichText(
                  text: const TextSpan(
                    style: TextStyle(fontSize: 11, color: _warMuted),
                    children: [
                      TextSpan(text: '¿Nuevo guerrero? '),
                      TextSpan(
                        text: 'REGÍSTRATE',
                        style: TextStyle(
                            color: _warAccent, fontWeight: FontWeight.w700),
                      ),
                    ],
                  ),
                ),
              ),
            ),
          ],
        ),
      );
    });
  }

  Widget _buildRegisterContent() {
    return Obx(() {
      final errorMessage = _controller.registerError.value;
      final warningMessage = _controller.registerWarning.value;
      return SingleChildScrollView(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          mainAxisSize: MainAxisSize.min,
          children: [
            const Text(
              'CREAR CUENTA',
              style: TextStyle(
                color: _warText,
                fontFamily: 'serif',
                fontSize: 17,
                fontWeight: FontWeight.w900,
                letterSpacing: 2,
              ),
            ),
            const SizedBox(height: 12),
            _WarInput(
              controller: _registerUsernameController,
              hint: 'Tu alias en batalla',
              icon: Icons.person_outline,
              error: _registerUsernameError,
            ),
            const SizedBox(height: 8),
            _WarInput(
              controller: _registerEmailController,
              hint: 'Correo electrónico',
              icon: Icons.email_outlined,
              type: TextInputType.emailAddress,
              error: _registerEmailError,
            ),
            const SizedBox(height: 8),
            _WarInput(
              controller: _registerPasswordController,
              hint: 'Contraseña',
              icon: Icons.lock_outline,
              obscure: true,
              error: _registerPasswordError,
            ),
            if (errorMessage != null) ...[
              const SizedBox(height: 8),
              Text(errorMessage,
                  style: const TextStyle(
                      color: Color(0xFFFF6B6B), fontSize: 11),
                  textAlign: TextAlign.center),
            ],
            if (warningMessage != null) ...[
              const SizedBox(height: 6),
              Text(warningMessage,
                  style: const TextStyle(color: _warAccent, fontSize: 11),
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
                    style: TextStyle(fontSize: 11, color: _warMuted),
                    children: [
                      TextSpan(text: '¿Ya tienes cuenta? '),
                      TextSpan(
                        text: 'INGRESA',
                        style: TextStyle(
                            color: _warAccent, fontWeight: FontWeight.w700),
                      ),
                    ],
                  ),
                ),
              ),
            ),
          ],
        ),
      );
    });
  }
}

// Compact icon-prefix input for landscape layout
class _WarInput extends StatefulWidget {
  const _WarInput({
    required this.controller,
    required this.hint,
    required this.icon,
    this.obscure = false,
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
  State<_WarInput> createState() => _WarInputState();
}

class _WarInputState extends State<_WarInput> {
  late bool _isObscured;

  @override
  void initState() {
    super.initState();
    _isObscured = widget.obscure;
  }

  @override
  Widget build(BuildContext context) {
    return TextField(
      controller: widget.controller,
      focusNode: widget.focusNode,
      obscureText: _isObscured,
      keyboardType: widget.type,
      style: const TextStyle(color: _warText, fontSize: 13),
      decoration: InputDecoration(
        hintText: widget.hint,
        errorText: widget.error,
        hintStyle: const TextStyle(color: Color(0xFF555555), fontSize: 12),
        prefixIcon: Icon(widget.icon, size: 18, color: _warMuted),
        filled: true,
        fillColor: _warInputBg,
        isDense: true,
        contentPadding:
            const EdgeInsets.symmetric(horizontal: 12, vertical: 13),
        suffixIcon: widget.obscure
            ? IconButton(
                splashRadius: 16,
                icon: Icon(
                  _isObscured
                      ? Icons.visibility_off_outlined
                      : Icons.visibility_outlined,
                  size: 18,
                  color: _warMuted,
                ),
                onPressed: () =>
                    setState(() => _isObscured = !_isObscured),
              )
            : null,
        enabledBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(6),
          borderSide: const BorderSide(color: _warBorder),
        ),
        focusedBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(6),
          borderSide: const BorderSide(color: _warAccent),
        ),
        errorBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(6),
          borderSide: const BorderSide(color: Color(0xFFFF6B6B)),
        ),
        focusedErrorBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(6),
          borderSide: const BorderSide(color: Color(0xFFFF6B6B)),
        ),
      ),
    );
  }
}

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
              width: 14,
              height: 14,
              child: CircularProgressIndicator(
                  strokeWidth: 1.5, color: _warMuted),
            )
          : Icon(icon, size: 16, color: _warText),
      label: Text(
        label,
        style: const TextStyle(
            fontSize: 9, color: _warText, letterSpacing: 1),
      ),
      style: OutlinedButton.styleFrom(
        padding: const EdgeInsets.symmetric(vertical: 10),
        side: BorderSide(color: borderColor),
        shape:
            RoundedRectangleBorder(borderRadius: BorderRadius.circular(6)),
        backgroundColor: _warInputBg,
      ),
    );
  }
}
