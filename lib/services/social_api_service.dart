import 'dart:convert';
import 'dart:io';
import 'api_config.dart';

class SocialResult {
  final bool success;
  final String message;

  const SocialResult._({required this.success, required this.message});

  factory SocialResult.ok(String msg) =>
      SocialResult._(success: true, message: msg);

  factory SocialResult.fail(String msg) =>
      SocialResult._(success: false, message: msg);
}

/// Servicio REST para las acciones sociales del chat:
/// solicitud de amistad y bloqueo de usuario.
class SocialApiService {
  SocialApiService(this.myCharacterId);

  final String myCharacterId;

  HttpClient _client() =>
      HttpClient()..connectionTimeout = const Duration(seconds: 5);

  Future<SocialResult> _post(String path, Map<String, dynamic> body) async {
    try {
      final uri = Uri.parse('$apiBaseUrl$path');
      final client = _client();
      final req = await client.postUrl(uri);
      req.headers.contentType = ContentType.json;
      req.headers.set('X-Character-Id', myCharacterId);
      req.write(jsonEncode(body));
      final res = await req.close();
      final raw = await res.transform(utf8.decoder).join();
      client.close();
      final json = jsonDecode(raw) as Map<String, dynamic>;
      final msg = (json['message'] as String?) ?? '';
      if (res.statusCode == 200) return SocialResult.ok(msg);
      return SocialResult.fail(
          msg.isNotEmpty ? msg : 'Error del servidor (${res.statusCode})');
    } on SocketException {
      return SocialResult.fail('Sin conexión con el servidor.');
    } catch (_) {
      return SocialResult.fail('Error inesperado. Intenta de nuevo.');
    }
  }

  /// Envía una solicitud de amistad al jugador con [targetCharacterId].
  Future<SocialResult> sendFriendRequest(String targetCharacterId) =>
      _post('/api/social/friends/request',
          {'TargetCharacterId': targetCharacterId});

  /// Bloquea al jugador con [targetCharacterId].
  Future<SocialResult> blockPlayer(String targetCharacterId) =>
      _post('/api/social/block', {'TargetCharacterId': targetCharacterId});
}
