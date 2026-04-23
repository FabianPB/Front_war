import 'package:flutter/material.dart';
import '../../../models/chat_message_model.dart';

class ChatMessageBubble extends StatelessWidget {
  const ChatMessageBubble({
    super.key,
    required this.message,
  });

  final ChatMessageModel message;

  @override
  Widget build(BuildContext context) {
    final bubbleColor = message.mine ? const Color.fromRGBO(100, 0, 15, 0.35) : const Color.fromRGBO(22, 5, 8, 0.95);
    final borderColor = message.mine ? const Color.fromRGBO(192, 0, 26, 0.3) : const Color.fromRGBO(192, 0, 26, 0.18);
    final userColor = message.mine ? const Color(0xFFD4A03A) : const Color(0xFFC0001A);
    final messageColor = message.mine ? const Color(0xFFF5E8E8) : const Color(0xFFC8BFBF);

    return Row(
      mainAxisAlignment: message.mine ? MainAxisAlignment.end : MainAxisAlignment.start,
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        if (!message.mine) ChatAvatar(emoji: message.avatar),
        if (!message.mine) const SizedBox(width: 10),
        Flexible(
          child: Column(
            crossAxisAlignment: message.mine ? CrossAxisAlignment.end : CrossAxisAlignment.start,
            children: [
              Text(
                message.user.toUpperCase(),
                style: TextStyle(
                  fontSize: 10,
                  fontWeight: FontWeight.w700,
                  letterSpacing: 1.0,
                  color: userColor,
                ),
              ),
              const SizedBox(height: 3),
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 13, vertical: 9),
                decoration: BoxDecoration(
                  color: bubbleColor,
                  border: Border.all(color: borderColor),
                  borderRadius: BorderRadius.only(
                    topLeft: const Radius.circular(10),
                    topRight: const Radius.circular(10),
                    bottomLeft: Radius.circular(message.mine ? 10 : 3),
                    bottomRight: Radius.circular(message.mine ? 3 : 10),
                  ),
                ),
                child: Text(
                    message.message,
                  style: TextStyle(
                    fontSize: 13,
                    height: 1.45,
                    color: messageColor,
                  ),
                ),
              ),
              const SizedBox(height: 3),
              Text(
                message.time,
                style: const TextStyle(fontSize: 9, color: Color.fromRGBO(200, 170, 170, 0.28)),
              ),
            ],
          ),
        ),
        if (message.mine) const SizedBox(width: 10),
        if (message.mine) ChatAvatar(emoji: message.avatar),
      ],
    );
  }
}

class ChatAvatar extends StatelessWidget {
  const ChatAvatar({super.key, required this.emoji});

  final String emoji;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: 36,
      height: 36,
      decoration: BoxDecoration(
        color: const Color.fromRGBO(192, 0, 26, 0.2),
        border: Border.all(color: const Color.fromRGBO(192, 0, 26, 0.35)),
        shape: BoxShape.circle,
      ),
      alignment: Alignment.center,
      child: Text(emoji, style: const TextStyle(fontSize: 16)),
    );
  }
}

class ChatComposer extends StatelessWidget {
  const ChatComposer({
    super.key,
    required this.controller,
    required this.onSend,
  });

  final TextEditingController controller;
  final VoidCallback onSend;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
      decoration: const BoxDecoration(
        color: Color.fromRGBO(8, 1, 3, 0.6),
        border: Border(top: BorderSide(color: Color.fromRGBO(192, 0, 26, 0.18))),
      ),
      child: Row(
        children: [
          Expanded(
            child: TextField(
              controller: controller,
              style: const TextStyle(color: Color(0xFFF5E8E8), fontSize: 13),
              decoration: InputDecoration(
                hintText: 'Escribe tu mensaje, guerrero...',
                hintStyle: const TextStyle(color: Color.fromRGBO(200, 170, 170, 0.28)),
                filled: true,
                fillColor: const Color.fromRGBO(14, 3, 5, 0.9),
                contentPadding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
                enabledBorder: _outlineBorder(BorderRadius.circular(3), const Color.fromRGBO(192, 0, 26, 0.28)),
                focusedBorder: _outlineBorder(BorderRadius.circular(3), const Color.fromRGBO(255, 30, 50, 0.5)),
              ),
              onSubmitted: (_) => onSend(),
            ),
          ),
          const SizedBox(width: 8),
          InkWell(
            onTap: onSend,
            child: Container(
              decoration: BoxDecoration(
                gradient: const LinearGradient(
                  colors: [Color(0xFF7A0010), Color(0xFFC0001A)],
                  begin: Alignment.topLeft,
                  end: Alignment.bottomRight,
                ),
                borderRadius: BorderRadius.circular(3),
              ),
              padding: const EdgeInsets.symmetric(horizontal: 18, vertical: 12),
              child: const Icon(Icons.send, color: Colors.white, size: 18),
            ),
          ),
        ],
      ),
    );
  }

  InputBorder _outlineBorder(BorderRadius r, Color c) {
    return OutlineInputBorder(borderRadius: r, borderSide: BorderSide(color: c));
  }
}
