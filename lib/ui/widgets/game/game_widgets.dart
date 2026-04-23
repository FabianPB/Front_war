import 'package:flutter/material.dart';

class GameControlOverlay extends StatelessWidget {
  const GameControlOverlay({
    super.key,
    required this.systemUiVisible,
    required this.unityReady,
    required this.onBack,
    required this.onEnterGame,
  });

  final bool systemUiVisible;
  final bool unityReady;
  final VoidCallback onBack;
  final VoidCallback onEnterGame;

  @override
  Widget build(BuildContext context) {
    return Stack(
      children: [
        if (!unityReady)
          const Center(child: CircularProgressIndicator()),
        if (systemUiVisible || !unityReady)
          Positioned(
            top: 16,
            left: 16,
            child: SafeArea(
              child: FloatingActionButton.small(
                onPressed: onBack,
                backgroundColor: Colors.black54,
                child: const Icon(Icons.arrow_back_ios_new, color: Colors.white),
              ),
            ),
          ),
        if (unityReady)
          Positioned(
            bottom: 26,
            left: 24,
            right: 24,
            child: ElevatedButton.icon(
              onPressed: onEnterGame,
              icon: const Icon(Icons.play_arrow),
              label: const Text('Entrar al juego'),
              style: ElevatedButton.styleFrom(
                padding: const EdgeInsets.symmetric(vertical: 16),
              ),
            ),
          ),
      ],
    );
  }
}
