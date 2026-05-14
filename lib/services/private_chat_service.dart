import 'dart:convert';
import 'dart:io';
import '../models/chat_message_model.dart';
import 'api_config.dart';

const _kAvatars = ['⚔️', '🗡️', '🪓', '💀', '🏹', '🔱', '🌑', '🐉'];

/// Servicio REST para el chat privado 1-a-1.
/// Endpoints: POST /api/chat/private  |  GET /api/chat/private/{partnerId}
class PrivateChatService {
  PrivateChatService({
    required this.myCharacterId,
    required this.myDisplayName,
    required this.partnerCharacterId,
  });

  final String myCharacterId;
  final String myDisplayName;
  final String partnerCharacterId;

  HttpClient _client() =>
      HttpClient()..connectionTimeout = const Duration(seconds: 5);

  String _avatarFor(String senderId) {
    if (senderId == myCharacterId) return '🛡️';
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
    final senderId = (json['senderCharacterId'] as String?) ?? '';
    final displayName = (json['senderDisplayName'] as String?) ?? 'Guerrero';
    return ChatMessageModel(
      mine: senderId == myCharacterId,
      user: displayName,
      avatar: _avatarFor(senderId),
      message: (json['content'] as String?) ?? '',
      time: _timeLabel((json['sentAtUtc'] as String?) ?? ''),
      accountId: senderId.isNotEmpty ? senderId : null,
    );
  }

  /// Devuelve null si el backend no está disponible.
  Future<List<ChatMessageModel>?> fetchMessages({int limit = 100}) async {
    try {
      final uri = Uri.parse('$apiBaseUrl/api/chat/private/$partnerCharacterId')
          .replace(queryParameters: {'limit': '$limit'});
      final client = _client();
      final req = await client.getUrl(uri);
      req.headers.set('X-Character-Id', myCharacterId);
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

  /// Envía un mensaje privado. Devuelve true si fue aceptado.
  Future<bool> sendMessage(String content) async {
    try {
      final uri = Uri.parse('$apiBaseUrl/api/chat/private');
      final client = _client();
      final req = await client.postUrl(uri);
      req.headers.contentType = ContentType.json;
      req.write(jsonEncode({
        'senderCharacterId': myCharacterId,
        'senderDisplayName': myDisplayName,
        'recipientCharacterId': partnerCharacterId,
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
