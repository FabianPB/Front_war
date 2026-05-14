import 'dart:math';
import 'package:firebase_auth/firebase_auth.dart';
import 'package:hive_flutter/hive_flutter.dart';

/// Genera y persiste un UUID v4 por usuario de Firebase.
/// Este Guid es el "character ID" que identifica al jugador
/// en el sistema social del backend (amigos, bloqueos, chat privado).
class CharacterIdService {
  static const _boxName = 'war_chars';
  static const _prefix = 'cid_';

  static String _uuid() {
    final r = Random.secure();
    final b = List<int>.generate(16, (_) => r.nextInt(256));
    b[6] = (b[6] & 0x0f) | 0x40; // version 4
    b[8] = (b[8] & 0x3f) | 0x80; // variant
    String h(int n) => n.toRadixString(16).padLeft(2, '0');
    final x = b.map(h).join();
    return '${x.substring(0, 8)}-${x.substring(8, 12)}-'
        '${x.substring(12, 16)}-${x.substring(16, 20)}-${x.substring(20)}';
  }

  /// Devuelve el character ID del usuario autenticado.
  /// Si no existe en Hive, genera uno nuevo y lo guarda.
  static Future<String> get() async {
    final uid = FirebaseAuth.instance.currentUser?.uid ?? 'anon';
    final box = await Hive.openBox(_boxName);
    final key = '$_prefix$uid';
    final stored = box.get(key) as String?;
    if (stored != null) return stored;
    final id = _uuid();
    await box.put(key, id);
    return id;
  }
}
