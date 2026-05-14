import 'dart:convert';
import 'dart:io';
import 'package:firebase_auth/firebase_auth.dart';
import '../models/chat_message_model.dart';
import 'api_config.dart';

const _kAvatars = ['⚔️', '🗡️', '🪓', '💀', '🏹', '🔱', '🌑', '🐉'];

class GlobalChatService {
  /// [characterId] es el Guid persistente del jugador (ver CharacterIdService).
  GlobalChatService({required this.characterId});

  final String characterId;

  /// Nombre visible del usuario autenticado en Firebase.
  static String get displayName {
    final user = FirebaseAuth.instance.currentUser;
    if (user == null) return 'Guerrero';
    if ((user.displayName ?? '').isNotEmpty) return user.displayName!;
    final email = user.email ?? '';
    return email.isNotEmpty ? email.split('@').first : 'Guerrero';
  }

  String _avatarFor(String senderId) {
    if (senderId == characterId) return '🛡️';
    return _kAvatars[senderId.hashCode.abs() % _kAvatars.length];
  }

  String _timeLabel(String sentAtUtc) {
    try {
      final dt = DateTime.parse(sentAtUtc).toLocal();
      final diff = DateTime.now().difference(dt);
      if (diff.inSeconds < 60) return 'ahora';
      if (diff.inMinutes < 60) return 'hace ${diff.inMinutes} min';
      if (diff.inHours < 24) return 'hace ${diff.inHours}h';
      return 'hace ${diff.inDays}d';
    } catch (_) {
      return '';
    }
  }

  ChatMessageModel _toModel(Map<String, dynamic> json) {
    final senderId = (json['senderAccountId'] as String?) ?? '';
    return ChatMessageModel(
      mine: senderId == characterId,
      user: (json['senderDisplayName'] as String?) ?? 'Guerrero',
      avatar: _avatarFor(senderId),
      message: (json['content'] as String?) ?? '',
      time: _timeLabel((json['sentAtUtc'] as String?) ?? ''),
      accountId: senderId.isNotEmpty ? senderId : null,
    );
  }

  HttpClient _client() =>
      HttpClient()..connectionTimeout = const Duration(seconds: 5);

  /// Devuelve null si el backend no está disponible, lista vacía si no hay mensajes.
  Future<List<ChatMessageModel>?> fetchMessages({int limit = 50}) async {
    try {
      final uri = Uri.parse('$apiBaseUrl/api/chat/global')
          .replace(queryParameters: {'limit': '$limit'});
      final client = _client();
      final req = await client.getUrl(uri);
      final res = await req.close();
      final body = await res.transform(utf8.decoder).join();
      client.close();
      if (res.statusCode == 200) {
        final data = jsonDecode(body) as List;
        return data.map((e) => _toModel(e as Map<String, dynamic>)).toList();
      }
    } catch (_) {}
    return null;
  }

  /// Envía un mensaje al chat global.
  Future<bool> sendMessage(String content) async {
    try {
      final uri = Uri.parse('$apiBaseUrl/api/chat/global');
      final client = _client();
      final req = await client.postUrl(uri);
      req.headers.contentType = ContentType.json;
      req.write(jsonEncode({
        'senderAccountId': characterId,
        'senderDisplayName': displayName,
        'content': content,
      }));
      final res = await req.close();
      await res.drain<void>();
      client.close();
      return res.statusCode == 200;
    } catch (_) {}
    return false;
  }
}
