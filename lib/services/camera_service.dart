import 'dart:io';
import 'package:flutter/foundation.dart';
import 'package:image_picker/image_picker.dart';
import 'package:path_provider/path_provider.dart';
import 'package:path/path.dart' as p;

class CameraService {
  const CameraService();

  static final ImagePicker _picker = ImagePicker();

  Future<File?> pickFromCamera() async {
    return _pick(ImageSource.camera);
  }

  Future<File?> pickFromGallery() async {
    return _pick(ImageSource.gallery);
  }

  Future<File?> _pick(ImageSource source) async {
    try {
      final picked = await _picker.pickImage(
        source: source,
        imageQuality: 85,
        maxWidth: 1080,
      );
      if (picked == null) return null;
      return _persistToAppDir(File(picked.path), ownerId: 'profile');
    } catch (e) {
      debugPrint('CameraService error: $e');
      return null;
    }
  }

  Future<File> _persistToAppDir(File source, {required String ownerId}) async {
    final docsDir = await getApplicationDocumentsDirectory();
    final avatarsDir = Directory(p.join(docsDir.path, 'avatars'));
    if (!await avatarsDir.exists()) {
      await avatarsDir.create(recursive: true);
    }
    final ext = p.extension(source.path).isEmpty ? '.jpg' : p.extension(source.path);
    final filename = '${ownerId}_${DateTime.now().millisecondsSinceEpoch}$ext';
    final destPath = p.join(avatarsDir.path, filename);
    return source.copy(destPath);
  }
}
