import 'package:firebase_auth/firebase_auth.dart';
import 'package:hive_flutter/hive_flutter.dart';

class LocalStorageService {
  static const String _profileBoxName = 'profile_box';
  static const String _photoPathKey = 'photoPath';
  static const String _usernameKey = 'username';
  static const String _scannedItemsKey = 'scannedItems';
  static const String _cityKey = 'city';

  static Future<void> init() async {
    await Hive.initFlutter();
    await Hive.openBox(_profileBoxName);
  }

  static Box get _box => Hive.box(_profileBoxName);

  static String _scopedKey(String key, {String? uid}) {
    final effectiveUid = uid ?? FirebaseAuth.instance.currentUser?.uid ?? 'anonymous';
    return '${effectiveUid}__$key';
  }

  static Future<void> savePhotoPath(String path, {String? uid}) {
    return _box.put(_scopedKey(_photoPathKey, uid: uid), path);
  }

  static String? getPhotoPath({String? uid}) {
    return _box.get(_scopedKey(_photoPathKey, uid: uid)) as String?;
  }

  static Future<void> clearPhotoPath({String? uid}) {
    return _box.delete(_scopedKey(_photoPathKey, uid: uid));
  }

  static Future<void> saveUsername(String username, {String? uid}) {
    return _box.put(_scopedKey(_usernameKey, uid: uid), username);
  }

  static String? getUsername({String? uid}) {
    return _box.get(_scopedKey(_usernameKey, uid: uid)) as String?;
  }

  static List<String> getScannedItems({String? uid}) {
    final raw = _box.get(_scopedKey(_scannedItemsKey, uid: uid));
    if (raw is List) return raw.cast<String>();
    return const [];
  }

  static Future<void> addScannedItem(String itemId, {String? uid}) {
    final current = getScannedItems(uid: uid).toList();
    if (!current.contains(itemId)) current.add(itemId);
    return _box.put(_scopedKey(_scannedItemsKey, uid: uid), current);
  }

  static Future<void> clearScannedItems({String? uid}) {
    return _box.delete(_scopedKey(_scannedItemsKey, uid: uid));
  }

  static Future<void> saveCity(String city, {String? uid}) {
    return _box.put(_scopedKey(_cityKey, uid: uid), city);
  }

  static String? getCity({String? uid}) {
    return _box.get(_scopedKey(_cityKey, uid: uid)) as String?;
  }
}
