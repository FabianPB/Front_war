class ChatMessageModel {
  const ChatMessageModel({
    required this.mine,
    required this.user,
    required this.avatar,
    required this.message,
    required this.time,
    this.accountId, // character ID del emisor (usado para acciones sociales)
  });

  final bool mine;
  final String user;
  final String avatar;
  final String message;
  final String time;

  /// Character ID del emisor. Null en mensajes generados localmente.
  final String? accountId;
}
