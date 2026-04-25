import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import '../../models/chat_message_model.dart';
import '../widgets/app_scaffold.dart'; // WarScaffold
import '../widgets/chat/chat_widgets.dart';

class ChatScreen extends StatefulWidget {
  const ChatScreen({super.key});
  static const routeName = '/chat';

  @override
  State<ChatScreen> createState() => _ChatScreenState();
}

class _ChatScreenState extends State<ChatScreen> {
  final TextEditingController _msgCtrl = TextEditingController();
  final List<ChatMessageModel> _messages = List.of(ChatMessageModel.demoMessages);

  @override
  void initState() {
    super.initState();
    // Allow automatic orientation rotation on chat screen
    SystemChrome.setPreferredOrientations(DeviceOrientation.values);
  }

  void _sendMsg() {
    if (_msgCtrl.text.trim().isEmpty) return;
    setState(() {
      _messages.add(
        ChatMessageModel(
          mine: true,
          user: 'Tú',
          avatar: '🛡️',
          message: _msgCtrl.text.trim(),
          time: 'ahora',
        ),
      );
      _msgCtrl.clear();
    });
  }

  @override
  void dispose() {
    _msgCtrl.dispose();
    // Restore automatic orientation when leaving
    SystemChrome.setPreferredOrientations(DeviceOrientation.values);
    super.dispose();
  }
  @override
  Widget build(BuildContext context) {
    return WarScaffold(
      title: 'Chat de Guerreros',
      lockLandscape: false,
      body: Column(
        children: [
          Expanded(
            child: ListView.separated(
              padding: const EdgeInsets.all(16),
              itemCount: _messages.length,
              separatorBuilder: (_, _) => const SizedBox(height: 12),
              itemBuilder: (context, index) {
                return ChatMessageBubble(message: _messages[index]);
              },
            ),
          ),
          ChatComposer(controller: _msgCtrl, onSend: _sendMsg),
        ],
      ),
    );
  }
}
