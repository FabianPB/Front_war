import 'package:firebase_database/firebase_database.dart';
import 'package:flutter/foundation.dart';

class FirebaseService {
  FirebaseService._();

  static DatabaseReference get rootRef => FirebaseDatabase.instance.ref();

  static DatabaseReference playersRef(String uid) =>
      rootRef.child('players').child(uid);

  static DatabaseReference get playersListRef => rootRef.child('players');

  static Future<void> savePlayerData(
    String uid,
    Map<String, Object?> data,
  ) async {
    // Avoid indefinite waits when the database URL/rules are misconfigured.
    await playersRef(uid).update(data).timeout(const Duration(seconds: 8));
  }

  /// Real-time stream of user stats — fires every time the `players` node changes.
  /// First yields a one-shot fetch (so the UI populates immediately) and then
  /// continues with `onValue` for live updates.
  static Stream<UsersChartStats> usersStatsStream() async* {
    // 1) Fast initial value via a one-shot read, so "..." doesn't linger.
    try {
      yield await fetchUsersStats();
    } catch (e) {
      debugPrint('usersStatsStream initial fetch error: $e');
      yield const UsersChartStats.empty();
    }

    // 2) Live updates whenever /players changes.
    yield* playersListRef.onValue
        .map((event) => usersStatsFromPlayersValue(event.snapshot.value))
        .handleError((Object error) {
      debugPrint('usersStatsStream onValue error: $error');
    });
  }

  static Future<UsersChartStats> fetchUsersStats() async {
    final snapshot = await playersListRef.get().timeout(
      const Duration(seconds: 8),
    );
    return usersStatsFromPlayersValue(snapshot.value);
  }

  /// Computes:
  ///   - accountsCreated → total registered users in /players
  ///   - connectedUsers  → registered users currently online (phone connected)
  ///   - offlineUsers    → registered users that are NOT online right now
  static UsersChartStats usersStatsFromPlayersValue(Object? value) {
    if (value is! Map) return const UsersChartStats.empty();

    final players = value.values.whereType<Object?>().toList();
    final accountsCreated = players.length;
    var connectedUsers = 0;

    for (final player in players) {
      if (_isPlayerOnline(player)) connectedUsers++;
    }

    return UsersChartStats(
      connectedUsers: connectedUsers,
      accountsCreated: accountsCreated,
      offlineUsers: accountsCreated - connectedUsers,
    );
  }

  static Future<void> setUserPresence(
    String uid, {
    required bool isOnline,
  }) async {
    final presence = <String, Object?>{
      'online': isOnline,
      'status': isOnline ? 'online' : 'offline',
      if (isOnline) 'connectedAt': ServerValue.timestamp,
      if (!isOnline) 'lastSeen': ServerValue.timestamp,
    };

    await playersRef(uid).update(presence).timeout(const Duration(seconds: 8));

    if (isOnline) {
      await playersRef(uid).onDisconnect().update({
        'online': false,
        'status': 'offline',
        'lastSeen': ServerValue.timestamp,
      });
    }
  }

  static Stream<List<UnityPlayerState>> unityPlayersStream(String currentUid) {
    return playersListRef.onValue.map((event) {
      return unityPlayersFromPlayersValue(event.snapshot.value, currentUid);
    });
  }

  static List<UnityPlayerState> unityPlayersFromPlayersValue(
    Object? value,
    String currentUid,
  ) {
    if (value is! Map) return const <UnityPlayerState>[];

    final players = <UnityPlayerState>[];
    value.forEach((uid, rawPlayer) {
      final playerUid = uid.toString();
      if (playerUid == currentUid || rawPlayer is! Map) return;
      if (!_isPlayerOnline(rawPlayer)) return;

      final unity = rawPlayer['unity'];
      if (unity is! Map || unity['inGame'] != true) return;

      players.add(UnityPlayerState.fromFirebase(playerUid, rawPlayer, unity));
    });

    return players;
  }

  static Future<void> enterUnityGame(String uid, {String? username}) async {
    await playersRef(uid)
        .update({
          'unity': {
            'inGame': true,
            'username': username ?? 'Jugador',
            'position': {'x': 0.0, 'y': 0.0, 'z': 0.0},
            'rotation': {'x': 0.0, 'y': 0.0, 'z': 0.0},
            'updatedAt': ServerValue.timestamp,
          },
        })
        .timeout(const Duration(seconds: 8));

    await playersRef(uid).child('unity').onDisconnect().update({
      'inGame': false,
      'updatedAt': ServerValue.timestamp,
    });
  }

  static Future<void> leaveUnityGame(String uid) async {
    await playersRef(uid)
        .child('unity')
        .update({'inGame': false, 'updatedAt': ServerValue.timestamp})
        .timeout(const Duration(seconds: 8));
  }

  static Future<void> updateUnityPlayerState(
    String uid, {
    required Map<String, double> position,
    Map<String, double>? rotation,
  }) async {
    final data = <String, Object?>{
      'inGame': true,
      'position': position,
      'updatedAt': ServerValue.timestamp,
    };
    if (rotation != null) {
      data['rotation'] = rotation;
    }

    await playersRef(
      uid,
    ).child('unity').update(data).timeout(const Duration(seconds: 8));
  }

  static bool _isPlayerOnline(Object? player) {
    if (player is! Map) return false;

    final online =
        player['online'] ?? player['isOnline'] ?? player['connected'];
    if (online is bool) return online;
    if (online is num) return online > 0;
    if (online is String) {
      final normalized = online.toLowerCase().trim();
      if (normalized == 'true' ||
          normalized == 'online' ||
          normalized == 'conectado' ||
          normalized == 'connected') {
        return true;
      }
    }

    final status = player['status']?.toString().toLowerCase().trim();
    return status == 'online' || status == 'conectado' || status == 'connected';
  }
}

class UnityPlayerState {
  const UnityPlayerState({
    required this.uid,
    required this.username,
    required this.position,
    required this.rotation,
  });

  final String uid;
  final String username;
  final Map<String, double> position;
  final Map<String, double> rotation;

  factory UnityPlayerState.fromFirebase(String uid, Map player, Map unity) {
    return UnityPlayerState(
      uid: uid,
      username: (unity['username'] ?? player['username'] ?? 'Jugador')
          .toString(),
      position: _vectorFromMap(unity['position']),
      rotation: _vectorFromMap(unity['rotation']),
    );
  }

  Map<String, Object?> toJson() {
    return {
      'uid': uid,
      'username': username,
      'position': position,
      'rotation': rotation,
    };
  }

  static Map<String, double> _vectorFromMap(Object? value) {
    if (value is! Map) {
      return const {'x': 0.0, 'y': 0.0, 'z': 0.0};
    }

    return {
      'x': _toDouble(value['x']),
      'y': _toDouble(value['y']),
      'z': _toDouble(value['z']),
    };
  }

  static double _toDouble(Object? value) {
    if (value is num) return value.toDouble();
    return double.tryParse(value?.toString() ?? '') ?? 0.0;
  }
}

class UsersChartStats {
  const UsersChartStats({
    required this.connectedUsers,
    required this.accountsCreated,
    required this.offlineUsers,
  });

  const UsersChartStats.empty()
    : connectedUsers = 0,
      accountsCreated = 0,
      offlineUsers = 0;

  final int connectedUsers;
  final int accountsCreated;
  final int offlineUsers;

  int get maxValue {
    final values = [connectedUsers, accountsCreated, offlineUsers];
    return values.reduce((a, b) => a > b ? a : b);
  }
}
