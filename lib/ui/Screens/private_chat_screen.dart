import 'dart:async';
import 'package:flutter/material.dart';
import '../../models/chat_message_model.dart';
import '../../services/character_id_service.dart';
import '../../services/global_chat_service.dart';
import '../../services/private_chat_service.dart';
import '../widgets/app_scaffold.dart';
import '../widgets/chat/chat_widgets.dart';

/// Pantalla de chat privado 1-a-1 entre el jugador actual y [partnerCharacterId].
class PrivateChatScreen extends StatefulWidget {
  const PrivateChatScreen({
    super.key,
    required this.partnerCharacterId,
    required this.partnerName,
    required this.partnerAvatar,
  });

  final String partnerCharacterId;
  final String partnerName;
  final String partnerAvatar;

  @override
  State<PrivateChatScreen> createState() => _PrivateChatScreenState();
}

class _PrivateChatScreenState extends State<PrivateChatScreen> {
  final _msgCtrl = TextEditingController();
  final _scrollCtrl = ScrollController();
  final _messages = <ChatMessageModel>[];

  PrivateChatService? _chatService;
  Timer? _pollTimer;

  bool _loading = true;
  bool _connected = false;

  @override
  void initState() {
    super.initState();
    _initialize();
  }

  Future<void> _initialize() async {
    final myCharacterId = await CharacterIdService.get();
    final myDisplayName = GlobalChatService.displayName;
    _chatService = PrivateChatService(
      myCharacterId: myCharacterId,
      myDisplayName: myDisplayName,
      partnerCharacterId: widget.partnerCharacterId,
    );
    await _loadMessages();
    _pollTimer =
        Timer.periodic(const Duration(seconds: 3), (_) => _loadMessages());
  }

  Future<void> _loadMessages() async {
    final msgs = await _chatService?.fetchMessages();
    if (!mounted) return;
    final wasAtBottom = _isAtBottom;
    setState(() {
      _loading = false;
      if (msgs != null) {
        _connected = true;
        _messages
          ..clear()
          ..addAll(msgs);
      } else {
        _connected = false;
      }
    });
    if (msgs != null && wasAtBottom) {
      WidgetsBinding.instance.addPostFrameCallback((_) => _scrollToBottom());
    }
  }

  bool get _isAtBottom {
    if (!_scrollCtrl.hasClients) return true;
    final pos = _scrollCtrl.position;
    return pos.pixels >= pos.maxScrollExtent - 80;
  }

  void _scrollToBottom() {
    if (_scrollCtrl.hasClients) {
      _scrollCtrl.animateTo(
        _scrollCtrl.position.maxScrollExtent,
        duration: const Duration(milliseconds: 300),
        curve: Curves.easeOut,
      );
    }
  }

  void _sendMsg() {
    final text = _msgCtrl.text.trim();
    if (text.isEmpty) return;
    _msgCtrl.clear();
    _chatService?.sendMessage(text).whenComplete(_loadMessages);
  }

  @override
  void dispose() {
    _pollTimer?.cancel();
    _msgCtrl.dispose();
    _scrollCtrl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return WarScaffold(
      title: '${widget.partnerAvatar}  ${widget.partnerName}',
      lockLandscape: false,
      body: Column(
        children: [
          if (!_connected && !_loading) const _OfflineBanner(),
          Expanded(
            child: _loading
                ? const Center(
                    child:
                        CircularProgressIndicator(color: Color(0xFFE6451C)),
                  )
                : _messages.isEmpty
                    ? Center(
                        child: Text(
                          'Sin mensajes aún.\n¡Escribe algo a ${widget.partnerName}!',
                          textAlign: TextAlign.center,
                          style: const TextStyle(
                              color: Color(0xFF888888), fontSize: 14),
                        ),
                      )
                    : ListView.separated(
                        controller: _scrollCtrl,
                        padding: const EdgeInsets.all(16),
                        itemCount: _messages.length,
                        separatorBuilder: (_, _) =>
                            const SizedBox(height: 12),
                        itemBuilder: (_, i) =>
                            ChatMessageBubble(message: _messages[i]),
                      ),
          ),
          ChatComposer(controller: _msgCtrl, onSend: _sendMsg),
        ],
      ),
    );
  }
}

class _OfflineBanner extends StatelessWidget {
  const _OfflineBanner();

  @override
  Widget build(BuildContext context) {
    return Container(
      width: double.infinity,
      color: const Color(0xFF2A1A1A),
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
      child: const Row(
        children: [
          Icon(Icons.wifi_off, color: Color(0xFFE6451C), size: 14),
          SizedBox(width: 8),
          Expanded(
            child: Text(
              'Backend no disponible — inicia el servidor para chatear',
              style: TextStyle(color: Color(0xFF888888), fontSize: 11),
            ),
          ),
        ],
      ),
    );
  }
}
