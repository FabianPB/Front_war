import 'package:get/get.dart';
import 'package:flutter/foundation.dart';
import '../models/user_model.dart';
import '../services/auth_service.dart';
import '../ui/Screens/auth_screen.dart';

class AuthController extends GetxController {
  final AuthService _authService = const AuthService();

  final showLogin = true.obs;
  final isLoading = false.obs;
  final isGoogleLoading = false.obs;
  final isBiometricLoading = false.obs;
  final loginError = RxnString();
  final registerError = RxnString();
  final registerWarning = RxnString();
  final isBiometricAvailable = false.obs;
  final isBiometricEnabled = false.obs;

  @override
  void onInit() {
    super.onInit();
    _checkBiometricAvailability();
  }

  Future<void> _checkBiometricAvailability() async {
    try {
      isBiometricAvailable.value = await _authService.isBiometricAvailable();
      debugPrint('Biometría disponible: ${isBiometricAvailable.value}');
      
      if (isBiometricAvailable.value) {
        isBiometricEnabled.value = await _authService.isBiometricEnabled();
        debugPrint('Biometría habilitada: ${isBiometricEnabled.value}');
      }
    } catch (e) {
      debugPrint('Error verificando biometría: $e');
      isBiometricAvailable.value = false;
      isBiometricEnabled.value = false;
    }
  }

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

  Future<String?> submitBiometricLogin() async {
    loginError.value = null;
    isBiometricLoading.value = true;
    try {
      final result = await _authService.loginWithBiometric();
      loginError.value = result;
      return result;
    } finally {
      isBiometricLoading.value = false;
    }
  }

  Future<void> saveBiometricEmailCredentials(String email, String password) async {
    try {
      await _authService.saveBiometricPreference(email, password);
      isBiometricEnabled.value = true;
    } catch (e) {
      debugPrint('Error guardando credenciales biométricas: $e');
    }
  }

  Future<void> saveBiometricGoogleLogin() async {
    try {
      await _authService.saveBiometricGooglePreference();
      isBiometricEnabled.value = true;
    } catch (e) {
      debugPrint('Error guardando preferencia Google biométrica: $e');
    }
  }

  void recheckBiometricStatus() {
    _checkBiometricAvailability();
  }

  Future<void> logout() async {
    await _authService.signOut();
    Get.offAll(
      () => const AuthScreen(infoMessage: 'Sesion cerrada exitosamente.'),
    );
  }
}
