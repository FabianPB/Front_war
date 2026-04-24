import 'package:flutter/material.dart';
import '../../../models/chat_message_model.dart';

const _wText = Color(0xFFE8E8E8);
const _wAccent = Color(0xFFE6451C);
const _wMuted = Color(0xFF888888);
const _wSurface = Color(0xFF1A1A1E);
const _wBorder = Color(0xFF2A2A2E);

class ChatMessageBubble extends StatelessWidget {
  const ChatMessageBubble({super.key, required this.message});

  final ChatMessageModel message;

  @override
  Widget build(BuildContext context) {
    final bubbleColor = message.mine
        ? _wAccent.withValues(alpha: 0.18)
        : _wSurface;
    final borderColor = message.mine
        ? _wAccent.withValues(alpha: 0.35)
        : _wBorder;
    final userColor = message.mine ? _wAccent : _wMuted;

    return Row(
      mainAxisAlignment:
          message.mine ? MainAxisAlignment.end : MainAxisAlignment.start,
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        if (!message.mine) ChatAvatar(emoji: message.avatar),
        if (!message.mine) const SizedBox(width: 10),
        Flexible(
          child: Column(
            crossAxisAlignment: message.mine
                ? CrossAxisAlignment.end
                : CrossAxisAlignment.start,
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
                padding:
                    const EdgeInsets.symmetric(horizontal: 13, vertical: 9),
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
                  style: const TextStyle(
                    fontSize: 13,
                    height: 1.45,
                    color: _wText,
                  ),
                ),
              ),
              const SizedBox(height: 3),
              Text(
                message.time,
                style: TextStyle(
                    fontSize: 9,
                    color: _wMuted.withValues(alpha: 0.7)),
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
        color: _wAccent.withValues(alpha: 0.14),
        border: Border.all(color: _wAccent.withValues(alpha: 0.35)),
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
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 10),
      decoration: BoxDecoration(
        color: const Color(0xFF141418),
        border: Border(top: BorderSide(color: _wBorder)),
      ),
      child: Row(
        children: [
          Expanded(
            child: TextField(
              controller: controller,
              style: const TextStyle(color: _wText, fontSize: 13),
              decoration: InputDecoration(
                hintText: 'Escribe tu mensaje, guerrero…',
                hintStyle:
                    TextStyle(color: _wMuted.withValues(alpha: 0.6)),
                filled: true,
                fillColor: _wSurface,
                contentPadding:
                    const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
                enabledBorder: OutlineInputBorder(
                  borderRadius: BorderRadius.circular(6),
                  borderSide: const BorderSide(color: _wBorder),
                ),
                focusedBorder: OutlineInputBorder(
                  borderRadius: BorderRadius.circular(6),
                  borderSide: const BorderSide(color: _wAccent),
                ),
              ),
              onSubmitted: (_) => onSend(),
            ),
          ),
          const SizedBox(width: 8),
          GestureDetector(
            onTap: onSend,
            child: Container(
              decoration: BoxDecoration(
                gradient: const LinearGradient(
                  colors: [Color(0xFFE6451C), Color(0xFFB83318)],
                ),
                borderRadius: BorderRadius.circular(6),
              ),
              padding:
                  const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
              child: const Icon(Icons.send, color: Colors.white, size: 18),
            ),
          ),
        ],
      ),
    );
  }
}
