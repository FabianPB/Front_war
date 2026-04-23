import 'package:flutter/material.dart';
import 'package:get/get.dart';
import '../../controllers/auth_controller.dart';
import '../../models/user_model.dart';
import '../../utils/validators.dart';
import '../widgets/auth/auth_widgets.dart';
import '../widgets/app_scaffold.dart';
import 'home_screen.dart';

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

  @override
  void initState() {
    super.initState();
    if (widget.infoMessage != null && widget.infoMessage!.trim().isNotEmpty) {
      WidgetsBinding.instance.addPostFrameCallback((_) {
        if (!mounted) return;
        Get.snackbar(
          'Sesion',
          widget.infoMessage!,
          snackPosition: SnackPosition.BOTTOM,
          duration: const Duration(seconds: 2),
        );
      });
    }
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
    if (errorMessage == null) Get.offAll(() => const HomeScreen());
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
        if (mounted) {
          _loginPasswordFocusNode.requestFocus();
        }
      });

      Get.snackbar(
        'Registro exitoso',
        'Ahora puedes iniciar sesión con tu cuenta.',
        snackPosition: SnackPosition.BOTTOM,
        duration: const Duration(seconds: 2),
      );

      if (result.warningMessage != null) {
        Get.snackbar(
          'Aviso de sincronización',
          result.warningMessage!,
          snackPosition: SnackPosition.BOTTOM,
          duration: const Duration(seconds: 4),
          backgroundColor: const Color(0xFF3A2F12),
          colorText: const Color(0xFFFFD479),
        );
      }
    }
  }

  Future<void> _signInWithGoogle() async {
    final errorMessage = await _controller.submitGoogleLogin();
    if (!mounted) return;
    if (errorMessage == null) Get.offAll(() => const HomeScreen());
  }

  @override
  void dispose() {
    _loginEmailController.dispose();
    _loginPasswordController.dispose();
    _loginPasswordFocusNode.dispose();
    _registerUsernameController.dispose();
    _registerEmailController.dispose();
    _registerPasswordController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return AppScaffold(
      title: 'W.A.R.',
      resizeToAvoidBottomInset: true,
      body: SafeArea(
        child: SingleChildScrollView(
          padding: EdgeInsets.fromLTRB(20, 24, 20, MediaQuery.of(context).viewInsets.bottom + 20),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              const SizedBox(height: 10),
              Center(
                child: Column(
                  children: [
                    const Text('⚔   COMBATE SIN PIEDAD   ⚔', style: TextStyle(color: Color(0xFFC0001A), fontSize: 10, fontWeight: FontWeight.w600, letterSpacing: 4.0)),
                    const SizedBox(height: 10),
                    RichText(
                      textAlign: TextAlign.center,
                      text: const TextSpan(
                        children: [
                          TextSpan(
                            text: 'W.A.R.',
                            style: TextStyle(
                              color: Color(0xFFFF1A2E),
                              fontFamily: 'serif',
                              fontSize: 54,
                              fontWeight: FontWeight.w900,
                              letterSpacing: 4.0,
                              shadows: [Shadow(color: Color.fromRGBO(255, 0, 30, 0.6), blurRadius: 20)],
                            ),
                          ),
                        ],
                      ),
                    ),
                  ],
                ),
              ),
              const SizedBox(height: 30),
              Obx(() => Row(
                    children: [
                      Expanded(
                        child: AuthTabButton(
                          label: 'INGRESAR',
                          isActive: _controller.showLogin.value,
                          onTap: () => _controller.toggleLogin(true),
                        ),
                      ),
                      const SizedBox(width: 8),
                      Expanded(
                        child: AuthTabButton(
                          label: 'REGISTRO',
                          isActive: !_controller.showLogin.value,
                          onTap: () => _controller.toggleLogin(false),
                        ),
                      ),
                    ],
                  )),
              const SizedBox(height: 24),
              Obx(() => AnimatedSwitcher(
                    duration: const Duration(milliseconds: 300),
                    child: _controller.showLogin.value
                        ? AuthFormCard(key: const ValueKey('login-form'), child: _buildLoginForm())
                        : AuthFormCard(key: const ValueKey('register-form'), child: _buildRegisterForm()),
                  )),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildLoginForm() {
    return Obx(() {
      final errorMessage = _controller.loginError.value;
      return Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          AuthInputField(
            controller: _loginEmailController,
            label: 'Correo electrónico',
            hint: 'tú@correo.com',
            type: TextInputType.emailAddress,
            error: _loginEmailError,
          ),
          const SizedBox(height: 16),
          AuthInputField(
            controller: _loginPasswordController,
            label: 'Contraseña',
            hint: '••••••••',
            obscure: true,
            error: _loginPasswordError,
            focusNode: _loginPasswordFocusNode,
          ),
          if (errorMessage != null) ...[
            const SizedBox(height: 14),
            Text(errorMessage, style: const TextStyle(color: Color(0xFFFF1A2E), fontSize: 12), textAlign: TextAlign.center),
          ],
          const SizedBox(height: 24),
          AuthPrimaryButton(
            text: 'ENTRAR A LA ARENA',
            onPressed: _submitLogin,
            isLoading: _controller.isLoading.value,
          ),
          const SizedBox(height: 16),
          AuthGoogleButton(
            isLoading: _controller.isGoogleLoading.value,
            onPressed: _signInWithGoogle,
          ),
        ],
      );
    });
  }

  Widget _buildRegisterForm() {
    return Obx(() {
      final errorMessage = _controller.registerError.value;
      final warningMessage = _controller.registerWarning.value;
      return Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          AuthInputField(
            controller: _registerUsernameController,
            label: 'Nombre de usuario',
            hint: 'Tu alias en batalla',
            error: _registerUsernameError,
          ),
          const SizedBox(height: 16),
          AuthInputField(
            controller: _registerEmailController,
            label: 'Correo electrónico',
            hint: 'tú@correo.com',
            type: TextInputType.emailAddress,
            error: _registerEmailError,
          ),
          const SizedBox(height: 16),
          AuthInputField(
            controller: _registerPasswordController,
            label: 'Contraseña',
            hint: '••••••••',
            obscure: true,
            error: _registerPasswordError,
          ),
          if (errorMessage != null) ...[
            const SizedBox(height: 14),
            Text(errorMessage, style: const TextStyle(color: Color(0xFFFF1A2E), fontSize: 12), textAlign: TextAlign.center),
          ],
          if (warningMessage != null) ...[
            const SizedBox(height: 10),
            Text(
              warningMessage,
              style: const TextStyle(color: Color(0xFFFFD479), fontSize: 12),
              textAlign: TextAlign.center,
            ),
          ],
          const SizedBox(height: 24),
          AuthPrimaryButton(
            text: 'FORJAR ALIANZA',
            onPressed: _submitRegister,
            isLoading: _controller.isLoading.value,
          ),
        ],
      );
    });
  }
}
