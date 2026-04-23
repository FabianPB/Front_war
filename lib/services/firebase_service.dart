import 'package:firebase_database/firebase_database.dart';

class FirebaseService {
  FirebaseService._();

  static DatabaseReference get rootRef => FirebaseDatabase.instance.ref();

  static DatabaseReference playersRef(String uid) =>
      rootRef.child('players').child(uid);

  static Future<void> savePlayerData(
    String uid,
    Map<String, Object?> data,
  ) async {
    // Avoid indefinite waits when the database URL/rules are misconfigured.
    await playersRef(uid)
        .update(data)
        .timeout(const Duration(seconds: 8));
  }
}
