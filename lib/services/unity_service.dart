import 'package:flutter_embed_unity/flutter_embed_unity.dart';

class UnityService {
  const UnityService();

  static const String unityGameObject = 'GameBridge';

  void sendMessage(String method, String message) {
    sendToUnity(unityGameObject, method, message);
  }

  void pause() {
    pauseUnity();
  }
}
