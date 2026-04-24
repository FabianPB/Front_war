import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_embed_unity/flutter_embed_unity.dart';
import '../../services/unity_service.dart';

class GameScreen extends StatefulWidget {
  const GameScreen({super.key});

  static const routeName = '/game';

  @override
  State<GameScreen> createState() => _GameScreenState();
}

class _GameScreenState extends State<GameScreen> {
  bool _unityReady = false;
  bool _didRequestEnterGame = false;
  Timer? _unityReadyFallbackTimer;

  @override
  void initState() {
    super.initState();
    SystemChrome.setEnabledSystemUIMode(SystemUiMode.immersiveSticky);
    _startUnityReadyFallback();
  }

  @override
  void dispose() {
    _unityReadyFallbackTimer?.cancel();
    SystemChrome.setEnabledSystemUIMode(SystemUiMode.edgeToEdge);
    super.dispose();
  }

  void _startUnityReadyFallback() {
    _unityReadyFallbackTimer?.cancel();
    _unityReadyFallbackTimer = Timer(const Duration(seconds: 15), () {
      if (!mounted || _unityReady) {
        return;
      }
      debugPrint('Unity no envio evento ready, activando fallback de carga.');
      _markUnityReady();
    });
  }

  bool _isUnityReadyMessage(String message) {
    final normalized = message.trim().toLowerCase();
    if (normalized.isEmpty) {
      return false;
    }

    const exactReadyMessages = <String>{
      'unity_ready',
      'unity ready',
      'ready',
      'game_ready',
      'game ready',
    };

    if (exactReadyMessages.contains(normalized)) {
      return true;
    }

    return normalized.contains('unity_ready') ||
        normalized.contains('unity ready') ||
        normalized.contains('"event":"ready"') ||
        normalized.contains('"status":"ready"') ||
        normalized.contains('"type":"ready"');
  }

  void _markUnityReady() {
    if (_unityReady || !mounted) {
      return;
    }
    _unityReadyFallbackTimer?.cancel();
    setState(() {
      _unityReady = true;
    });
    _enterGame();
  }

  void _onMessageFromUnity(String message) {
    debugPrint('Mensaje Unity: $message');
    if (_isUnityReadyMessage(message)) {
      _markUnityReady();
    }
  }

  void _enterGame() {
    if (_didRequestEnterGame) {
      return;
    }
    _didRequestEnterGame = true;
    UnityService().sendMessage('OnEnterGame', '');
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: Stack(
        children: [
          EmbedUnity(onMessageFromUnity: _onMessageFromUnity),
        ],
      ),
    );
  }
}
