import 'package:firebase_auth/firebase_auth.dart';
import 'package:flutter/foundation.dart';
import 'package:google_sign_in/google_sign_in.dart';
import 'package:frontend_war/services/firebase_service.dart';
import '../models/user_model.dart';
import 'package:flutter/services.dart';

class AuthService {
  const AuthService();

  static final FirebaseAuth _auth = FirebaseAuth.instance;
  static final GoogleSignIn _googleSignIn = GoogleSignIn();

  String _authErrorMessage(FirebaseAuthException exception) {
    switch (exception.code) {
      case 'invalid-email':
        return 'El correo electrónico no es válido.';
      case 'user-disabled':
        return 'La cuenta ha sido deshabilitada.';
      case 'user-not-found':
        return 'No existe una cuenta con ese correo.';
      case 'wrong-password':
        return 'Contraseña incorrecta.';
      case 'email-already-in-use':
        return 'El correo ya está registrado.';
      case 'operation-not-allowed':
        return 'El método de autenticación no está habilitado.';
      case 'weak-password':
        return 'La contraseña es demasiado débil.';
      default:
        return 'Error de autenticación: ${exception.message ?? exception.code}';
    }
  }

  RegisterResult _registerError(String message) {
    return RegisterResult(errorMessage: message);
  }

  Future<String?> login(String email, String password) async {
    try {
      final credential = await _auth.signInWithEmailAndPassword(
        email: email,
        password: password,
      );
      await _syncMissingProfileAfterAuth(credential.user, fallbackEmail: email);
      return null;
    } on FirebaseAuthException catch (e) {
      return _authErrorMessage(e);
    } catch (e) {
      return 'Error inesperado: $e';
    }
  }

  Future<RegisterResult> register(UserModel user) async {
    String? warningMessage;
    try {
      final result = await _auth.createUserWithEmailAndPassword(
        email: user.email,
        password: user.password,
      );

      final firebaseUser = result.user;
      if (firebaseUser == null) {
        return _registerError('No se pudo crear el usuario. Intenta nuevamente.');
      }

      try {
        // Registration should succeed even if profile sync is slow or temporarily unavailable.
        await FirebaseService.savePlayerData(firebaseUser.uid, user.toPlayerData());
      } catch (e) {
        debugPrint('No se pudo guardar perfil tras registro: $e');
        warningMessage = 'Tu cuenta se creó correctamente, pero hubo demora al sincronizar tu perfil. Se reintentará más tarde.';
      }
      return RegisterResult(warningMessage: warningMessage);
    } on FirebaseAuthException catch (e) {
      // Hard validation/auth errors must be shown as real registration failures.
      final isHardRegisterError = e.code == 'email-already-in-use' ||
          e.code == 'invalid-email' ||
          e.code == 'weak-password' ||
          e.code == 'operation-not-allowed';
      if (isHardRegisterError) {
        return _registerError(_authErrorMessage(e));
      }

      // For uncertain backend/network states, reconcile with current auth session.
      final currentUser = _auth.currentUser;
      if (currentUser != null &&
          currentUser.email?.toLowerCase() == user.email.toLowerCase()) {
        try {
          await FirebaseService.savePlayerData(currentUser.uid, user.toPlayerData());
        } catch (syncError) {
          debugPrint('No se pudo sincronizar perfil tras error de red: $syncError');
          warningMessage =
              'Tu cuenta se creó correctamente, pero la red estuvo inestable al sincronizar datos de perfil.';
        }
        return RegisterResult(warningMessage: warningMessage);
      }

      return _registerError(_authErrorMessage(e));
    } catch (e) {
      return _registerError('Error inesperado: $e');
    }
  }

  Future<String?> signInWithGoogle() async {
    try {
      final googleUser = await _googleSignIn.signIn();
      if (googleUser == null) {
        return 'Inicio de sesión con Google cancelado.';
      }

      final googleAuth = await googleUser.authentication;
      
      // Ensure we have the tokens required
      if (googleAuth.accessToken == null && googleAuth.idToken == null) {
          return 'Faltan credenciales de Google.';
      }

      final credential = GoogleAuthProvider.credential(
        accessToken: googleAuth.accessToken,
        idToken: googleAuth.idToken,
      );

      final result = await _auth.signInWithCredential(credential);
      final firebaseUser = result.user;
      if (firebaseUser == null) {
        return 'No se pudo iniciar sesión con Google.';
      }

      try {
        await _syncMissingProfileAfterAuth(
          firebaseUser,
          fallbackProvider: 'google',
          fallbackUsername: firebaseUser.displayName ?? 'Usuario de Google',
        );
      } catch (e) {
        // Google auth already succeeded; do not block user access by profile sync issues.
        debugPrint('No se pudo guardar perfil de Google en Realtime DB: $e');
      }
      return null;
    } on FirebaseAuthException catch (e) {
      return _authErrorMessage(e);
    } on PlatformException catch (e) {
      return _platformGoogleSignInError(e);
    } catch (e) {
      return 'Error inesperado con Google: $e';
    }
  }

  Future<void> signOut() async {
    await _safeSignOut(() => _auth.signOut(), 'Auth SignOut');
    await _safeSignOut(() => _googleSignIn.signOut(), 'Google SignOut');
  }

  String _platformGoogleSignInError(PlatformException exception) {
    final code = exception.code.toLowerCase();
    final details = (exception.details ?? '').toString();
    final message = (exception.message ?? '').toLowerCase();

    // Common Android Google Sign-In errors when OAuth/SHA are missing.
    if (code.contains('sign_in_failed') || details.contains('10') || message.contains('12500')) {
      return 'Fallo al iniciar con Google (OAuth Android no configurado). '
          'Agrega SHA-1/SHA-256 en Firebase, habilita proveedor Google Auth y descarga un nuevo google-services.json.';
    }

    return 'Error de plataforma: ${exception.message ?? exception.code}';
  }

  Future<void> _safeSignOut(Future<void> Function() action, String label) async {
    try {
      await action();
    } catch (e) {
      debugPrint('Error $label: $e');
    }
  }

  Future<void> _syncMissingProfileAfterAuth(
    User? user, {
    String? fallbackEmail,
    String? fallbackProvider,
    String? fallbackUsername,
  }) async {
    if (user == null) return;

    try {
      final snapshot = await FirebaseService.playersRef(user.uid)
          .get()
          .timeout(const Duration(seconds: 5));
      if (snapshot.exists) return;

      final provider = fallbackProvider ?? _resolveProvider(user);
      final email = user.email ?? fallbackEmail ?? '';
      final username = fallbackUsername ??
          user.displayName ??
          _usernameFromEmail(email, provider: provider);

      await FirebaseService.savePlayerData(user.uid, {
        'username': username,
        'email': email,
        'provider': provider,
      });
    } catch (e) {
      debugPrint('No se pudo sincronizar perfil faltante tras login: $e');
    }
  }

  String _resolveProvider(User user) {
    final hasGoogleProvider =
        user.providerData.any((p) => p.providerId == 'google.com');
    return hasGoogleProvider ? 'google' : 'password';
  }

  String _usernameFromEmail(String email, {String provider = 'password'}) {
    if (email.trim().isEmpty) {
      return provider == 'google' ? 'Usuario de Google' : 'Jugador';
    }
    return email.split('@').first;
  }
}

class RegisterResult {
  const RegisterResult({this.errorMessage, this.warningMessage});

  final String? errorMessage;
  final String? warningMessage;

  bool get isSuccess => errorMessage == null;
}
