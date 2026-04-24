import 'package:flutter/material.dart';

class GameControlOverlay extends StatelessWidget {
  const GameControlOverlay({
    super.key,
    required this.systemUiVisible,
    required this.unityReady,
    required this.onBack,
  });

  final bool systemUiVisible;
  final bool unityReady;
  final VoidCallback onBack;

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
      ],
    );
  }
}
