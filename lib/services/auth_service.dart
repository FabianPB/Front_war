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

  Future<String?> login(String email, String password) async {
    try {
      await _auth.signInWithEmailAndPassword(email: email, password: password);
      return null;
    } on FirebaseAuthException catch (e) {
      return _authErrorMessage(e);
    } catch (e) {
      return 'Error inesperado: $e';
    }
  }

  Future<String?> register(UserModel user) async {
    try {
      final result = await _auth.createUserWithEmailAndPassword(
        email: user.email,
        password: user.password,
      );

      final firebaseUser = result.user;
      if (firebaseUser == null) {
        return 'No se pudo crear el usuario. Intenta nuevamente.';
      }

      await FirebaseService.savePlayerData(firebaseUser.uid, user.toPlayerData());
      return null;
    } on FirebaseAuthException catch (e) {
      return _authErrorMessage(e);
    } catch (e) {
      return 'Error inesperado: $e';
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
        await FirebaseService.savePlayerData(firebaseUser.uid, {
          'username': firebaseUser.displayName ?? 'Usuario de Google',
          'email': firebaseUser.email ?? '',
          'provider': 'google',
        });
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
}
