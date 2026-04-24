import 'package:flutter/foundation.dart';
import 'package:local_auth/local_auth.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

class BiometricService {
  const BiometricService();

  static final _localAuth = LocalAuthentication();
  static final _secureStorage = FlutterSecureStorage();

  // Keys para almacenar en secure storage
  static const String _biometricEnabledKey = 'biometric_enabled';
  static const String _emailKey = 'biometric_email';
  static const String _passwordKey = 'biometric_password';
  static const String _isGoogleLoginKey = 'biometric_is_google';

  /// Verifica si el dispositivo tiene soporte biométrico
  /// Detecta huella dactilar, Face ID o cualquier otra biometría disponible
  Future<bool> isBiometricAvailable() async {
    try {
      final biometrics = await _localAuth.getAvailableBiometrics();
      debugPrint('Biometrías disponibles: $biometrics');
      
      // En Android algunos dispositivos reportan weak/strong en lugar de face.
      final hasFingerprint = biometrics.contains(BiometricType.fingerprint);
      final hasFaceId = biometrics.contains(BiometricType.face);
      final hasStrong = biometrics.contains(BiometricType.strong);
      final hasWeak = biometrics.contains(BiometricType.weak);
      
      final isAvailable = hasFingerprint || hasFaceId || hasStrong || hasWeak;
      debugPrint('¿Biometría disponible? $isAvailable (Huella: $hasFingerprint, Face ID: $hasFaceId, Strong: $hasStrong, Weak: $hasWeak)');
      
      return isAvailable;
    } catch (e) {
      debugPrint('Error al verificar biometría disponible: $e');
      return false;
    }
  }

  /// Verifica si el usuario ya activó login biométrico
  Future<bool> isBiometricEnabled() async {
    try {
      final enabled = await _secureStorage.read(key: _biometricEnabledKey);
      if (enabled == 'true') return true;

      // Fallback robusto: si la bandera no está, inferir por datos guardados.
      final isGoogle = await _secureStorage.read(key: _isGoogleLoginKey);
      final email = await _secureStorage.read(key: _emailKey);
      final password = await _secureStorage.read(key: _passwordKey);
      final inferredEnabled = isGoogle == 'true' ||
          ((email != null && email.isNotEmpty) && (password != null && password.isNotEmpty));

      if (inferredEnabled) {
        // Autorrepara la bandera para próximas lecturas.
        await _secureStorage.write(key: _biometricEnabledKey, value: 'true');
      }

      return inferredEnabled;
    } catch (e) {
      debugPrint('Error al leer estado biométrico: $e');
      return false;
    }
  }

  /// Obtiene la lista de biometrías disponibles en el dispositivo
  Future<List<BiometricType>> getAvailableBiometrics() async {
    try {
      return await _localAuth.getAvailableBiometrics();
    } catch (e) {
      debugPrint('Error al obtener biometrías disponibles: $e');
      return [];
    }
  }

  /// Autentica al usuario usando biometría
  /// Detecta automáticamente si es huella dactilar o Face ID y ajusta el mensaje
  /// Retorna true si la autenticación fue exitosa
  Future<bool> authenticate() async {
    try {
      // Obtener biometrías disponibles para personalizar el mensaje
      final biometrics = await _localAuth.getAvailableBiometrics();
      final hasFaceId = biometrics.contains(BiometricType.face);
      final hasFingerprint = biometrics.contains(BiometricType.fingerprint);
      final hasStrong = biometrics.contains(BiometricType.strong);
      final hasWeak = biometrics.contains(BiometricType.weak);
      
      String reason;
      if (hasFaceId && hasFingerprint) {
        reason = 'Usa tu huella dactilar o reconocimiento facial para iniciar sesión';
      } else if (hasFaceId) {
        reason = 'Usa tu reconocimiento facial para iniciar sesión';
      } else if (hasStrong || hasWeak) {
        reason = 'Usa tu autenticación biométrica para iniciar sesión';
      } else {
        reason = 'Usa tu huella dactilar para iniciar sesión';
      }
      
      debugPrint('Autenticando con: $reason');
      
      final isAuthenticated = await _localAuth.authenticate(
        localizedReason: reason,
        options: const AuthenticationOptions(
          stickyAuth: true,
          biometricOnly: true,
        ),
      );
      return isAuthenticated;
    } catch (e) {
      debugPrint('Error durante autenticación biométrica: $e');
      return false;
    }
  }

  /// Guarda las credenciales de email/password de forma segura
  Future<void> saveEmailCredentials(String email, String password) async {
    try {
      await Future.wait([
        _secureStorage.write(key: _emailKey, value: email),
        _secureStorage.write(key: _passwordKey, value: password),
        _secureStorage.write(key: _isGoogleLoginKey, value: 'false'),
        _secureStorage.write(key: _biometricEnabledKey, value: 'true'),
      ]);
      debugPrint('Credenciales guardadas exitosamente');
    } catch (e) {
      debugPrint('Error guardando credenciales: $e');
      rethrow;
    }
  }

  /// Guarda la preferencia de Google Login
  Future<void> saveGoogleLoginPreference() async {
    try {
      await Future.wait([
        _secureStorage.write(key: _isGoogleLoginKey, value: 'true'),
        _secureStorage.write(key: _biometricEnabledKey, value: 'true'),
      ]);
      debugPrint('Preferencia de Google Login guardada');
    } catch (e) {
      debugPrint('Error guardando preferencia Google: $e');
      rethrow;
    }
  }

  /// Recupera las credenciales guardadas
  /// Retorna un mapa con email y password, o null si no hay credenciales guardadas
  Future<Map<String, String>?> getStoredEmailCredentials() async {
    try {
      final email = await _secureStorage.read(key: _emailKey);
      final password = await _secureStorage.read(key: _passwordKey);

      if (email == null || password == null) {
        return null;
      }

      return {'email': email, 'password': password};
    } catch (e) {
      debugPrint('Error recuperando credenciales: $e');
      return null;
    }
  }

  /// Verifica si el login guardado es de Google
  Future<bool> isGoogleLoginStored() async {
    try {
      final isGoogle = await _secureStorage.read(key: _isGoogleLoginKey);
      return isGoogle == 'true';
    } catch (e) {
      debugPrint('Error verificando tipo de login: $e');
      return false;
    }
  }

  /// Elimina las credenciales guardadas
  Future<void> clearBiometricData() async {
    try {
      await Future.wait([
        _secureStorage.delete(key: _emailKey),
        _secureStorage.delete(key: _passwordKey),
        _secureStorage.delete(key: _isGoogleLoginKey),
        _secureStorage.delete(key: _biometricEnabledKey),
      ]);
      debugPrint('Datos biométricos eliminados');
    } catch (e) {
      debugPrint('Error eliminando datos biométricos: $e');
      rethrow;
    }
  }
}
