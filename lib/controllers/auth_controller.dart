import 'package:get/get.dart';
import '../models/user_model.dart';
import '../services/auth_service.dart';
import '../ui/Screens/auth_screen.dart';

class AuthController extends GetxController {
  final AuthService _authService = const AuthService();

  final showLogin = true.obs;
  final isLoading = false.obs;
  final isGoogleLoading = false.obs;
  final loginError = RxnString();
  final registerError = RxnString();
  final registerWarning = RxnString();

  void toggleLogin(bool value) {
    showLogin.value = value;
    loginError.value = null;
    registerError.value = null;
    registerWarning.value = null;
  }

  Future<String?> submitLogin(String email, String password) async {
    loginError.value = null;
    isLoading.value = true;
    try {
      final result = await _authService.login(email, password);
      loginError.value = result;
      return result;
    } finally {
      isLoading.value = false;
    }
  }

  Future<RegisterResult> submitRegister(UserModel user) async {
    registerError.value = null;
    registerWarning.value = null;
    isLoading.value = true;
    try {
      final result = await _authService.register(user);
      registerError.value = result.errorMessage;
      registerWarning.value = result.warningMessage;
      return result;
    } finally {
      isLoading.value = false;
    }
  }

  Future<String?> submitGoogleLogin() async {
    loginError.value = null;
    isGoogleLoading.value = true;
    try {
      final result = await _authService.signInWithGoogle();
      loginError.value = result;
      return result;
    } finally {
      isGoogleLoading.value = false;
    }
  }

  Future<void> logout() async {
    await _authService.signOut();
    Get.offAll(
      () => const AuthScreen(infoMessage: 'Sesion cerrada exitosamente.'),
    );
  }
}
