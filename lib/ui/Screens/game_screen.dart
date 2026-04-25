import 'dart:async';
import 'dart:convert';

import 'package:firebase_auth/firebase_auth.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_embed_unity/flutter_embed_unity.dart';
import '../../services/firebase_service.dart';
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
  StreamSubscription<List<UnityPlayerState>>? _unityPlayersSubscription;
  User? get _currentUser => FirebaseAuth.instance.currentUser;

  @override
  void initState() {
    super.initState();
    SystemChrome.setEnabledSystemUIMode(SystemUiMode.immersiveSticky);
    _startUnityReadyFallback();
  }

  @override
  void dispose() {
    _unityReadyFallbackTimer?.cancel();
    _unityPlayersSubscription?.cancel();
    final uid = _currentUser?.uid;
    if (uid != null) {
      FirebaseService.leaveUnityGame(uid);
    }
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
      return;
    }

    _syncLocalPlayerState(message);
  }

  Map<String, double>? _readVector(Object? raw) {
    if (raw is! Map) return null;
    return {
      'x': _toDouble(raw['x']),
      'y': _toDouble(raw['y']),
      'z': _toDouble(raw['z']),
    };
  }

  double _toDouble(Object? value) {
    if (value is num) return value.toDouble();
    return double.tryParse(value?.toString() ?? '') ?? 0.0;
  }

  Future<void> _syncLocalPlayerState(String message) async {
    final uid = _currentUser?.uid;
    if (uid == null || message.trim().isEmpty) return;

    try {
      final decoded = jsonDecode(message);
      if (decoded is! Map) return;

      final event =
          decoded['event']?.toString().toLowerCase() ??
          decoded['type']?.toString().toLowerCase();
      if (event != 'player_state' &&
          event != 'player_position' &&
          event != 'position') {
        return;
      }

      final position = _readVector(decoded['position']);
      if (position == null) return;

      await FirebaseService.updateUnityPlayerState(
        uid,
        position: position,
        rotation: _readVector(decoded['rotation']),
      );
    } catch (e) {
      debugPrint('No se pudo sincronizar estado de Unity: $e');
    }
  }

  Future<void> _enterGame() async {
    if (_didRequestEnterGame) {
      return;
    }
    _didRequestEnterGame = true;
    final user = _currentUser;
    if (user != null) {
      await FirebaseService.enterUnityGame(
        user.uid,
        username: user.displayName ?? user.email?.split('@').first,
      );
      _listenRemoteUnityPlayers(user.uid);
    }
    UnityService().sendMessage('OnEnterGame', '');
  }

  void _listenRemoteUnityPlayers(String currentUid) {
    _unityPlayersSubscription?.cancel();
    _unityPlayersSubscription = FirebaseService.unityPlayersStream(currentUid)
        .listen((players) {
          final payload = jsonEncode({
            'event': 'players_changed',
            'players': players.map((player) => player.toJson()).toList(),
          });
          UnityService().sendMessage('OnMultiplayerPlayersChanged', payload);
        });
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: Stack(
        children: [EmbedUnity(onMessageFromUnity: _onMessageFromUnity)],
      ),
    );
  }
}
