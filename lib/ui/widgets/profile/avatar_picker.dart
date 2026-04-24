import 'dart:io';
import 'package:flutter/material.dart';
import '../home/home_styles.dart';

class AvatarPicker extends StatelessWidget {
  const AvatarPicker({
    super.key,
    required this.photoPath,
    required this.onPickFromCamera,
    required this.onPickFromGallery,
    required this.onRemove,
    this.size = 140,
  });

  final String? photoPath;
  final Future<void> Function() onPickFromCamera;
  final Future<void> Function() onPickFromGallery;
  final Future<void> Function() onRemove;
  final double size;

  void _showOptions(BuildContext context) {
    final hasPhoto = photoPath != null && File(photoPath!).existsSync();

    showModalBottomSheet<void>(
      context: context,
      backgroundColor: homeSurfaceColor,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(24)),
      ),
      builder: (ctx) {
        return SafeArea(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              const SizedBox(height: 12),
              Container(
                width: 40,
                height: 4,
                decoration: BoxDecoration(
                  color: homeMutedText.withValues(alpha: 0.25),
                  borderRadius: BorderRadius.circular(2),
                ),
              ),
              const SizedBox(height: 16),
              ListTile(
                leading: const Icon(Icons.camera_alt, color: homePrimary),
                title: const Text('Tomar foto', style: TextStyle(color: homeText)),
                onTap: () async {
                  Navigator.of(ctx).pop();
                  await onPickFromCamera();
                },
              ),
              ListTile(
                leading: const Icon(Icons.photo_library, color: homeAccent),
                title: const Text('Elegir de galería', style: TextStyle(color: homeText)),
                onTap: () async {
                  Navigator.of(ctx).pop();
                  await onPickFromGallery();
                },
              ),
              if (hasPhoto)
                ListTile(
                  leading: const Icon(Icons.delete_outline, color: Colors.redAccent),
                  title: const Text('Eliminar foto', style: TextStyle(color: Colors.redAccent)),
                  onTap: () async {
                    Navigator.of(ctx).pop();
                    await onRemove();
                  },
                ),
              const SizedBox(height: 8),
            ],
          ),
        );
      },
    );
  }

  @override
  Widget build(BuildContext context) {
    final hasPhoto = photoPath != null && File(photoPath!).existsSync();

    return GestureDetector(
      onTap: () => _showOptions(context),
      child: Stack(
        children: [
          Container(
            width: size,
            height: size,
            decoration: BoxDecoration(
              shape: BoxShape.circle,
              gradient: const RadialGradient(
                colors: [homeAccent, homeBackgroundColor],
              ),
              border: Border.all(color: homePrimary, width: 2.5),
              boxShadow: const [
                BoxShadow(color: Color.fromRGBO(44, 123, 229, 0.25), blurRadius: 20, spreadRadius: 1),
              ],
            ),
            child: ClipOval(
              child: hasPhoto
                  ? Image.file(File(photoPath!), fit: BoxFit.cover, width: size, height: size)
                  : const Icon(Icons.person, size: 64, color: homeText),
            ),
          ),
          Positioned(
            right: 0,
            bottom: 0,
            child: Container(
              padding: const EdgeInsets.all(8),
              decoration: BoxDecoration(
                color: homeAccent,
                shape: BoxShape.circle,
                border: Border.all(color: homeBackgroundColor, width: 2),
              ),
              child: const Icon(Icons.camera_alt, size: 18, color: Colors.white),
            ),
          ),
        ],
      ),
    );
  }
}
