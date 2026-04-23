import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_embed_unity/flutter_embed_unity.dart';
import '../../services/unity_service.dart';
import '../widgets/game/game_widgets.dart';

class GameScreen extends StatefulWidget {
  const GameScreen({super.key});

  static const routeName = '/game';

  @override
  State<GameScreen> createState() => _GameScreenState();
}

class _GameScreenState extends State<GameScreen> {
  bool _unityReady = false;
  bool _isSystemUiVisible = false;

  @override
  void initState() {
    super.initState();
    SystemChrome.setEnabledSystemUIMode(SystemUiMode.immersiveSticky);
    SystemChrome.setSystemUIChangeCallback((systemUIVisible) async {
      if (mounted) {
        setState(() {
          _isSystemUiVisible = systemUIVisible;
        });
      }
    });
  }

  @override
  void dispose() {
    SystemChrome.setEnabledSystemUIMode(SystemUiMode.edgeToEdge);
    SystemChrome.setSystemUIChangeCallback(null);
    super.dispose();
  }

  void _onMessageFromUnity(String message) {
    debugPrint('Mensaje Unity: $message');
    if (message == 'unity_ready') {
      setState(() {
        _unityReady = true;
      });
    }
  }

  void _enterGame() {
    UnityService().sendMessage('OnEnterGame', '');
  }

  void _returnToFlutter() {
    UnityService().pause();
    Navigator.pop(context);
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: Stack(
        children: [
          EmbedUnity(onMessageFromUnity: _onMessageFromUnity),
          GameControlOverlay(
            systemUiVisible: _isSystemUiVisible,
            unityReady: _unityReady,
            onBack: _returnToFlutter,
            onEnterGame: _enterGame,
          ),
        ],
      ),
    );
  }
}
