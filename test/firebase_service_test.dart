import 'package:flutter_test/flutter_test.dart';
import 'package:frontend_war/services/firebase_service.dart';

void main() {
  test('usersStatsFromPlayersValue counts real Firebase player records', () {
    final stats = FirebaseService.usersStatsFromPlayersValue({
      'uid-a': {'online': true, 'username': 'A'},
      'uid-b': {'status': 'online', 'username': 'B'},
      'uid-c': {'online': false, 'username': 'C'},
    });

    expect(stats.accountsCreated, 3);
    expect(stats.connectedUsers, 2);
    expect(stats.offlineUsers, 1);
  });

  test('unityPlayersFromPlayersValue returns other online players in game', () {
    final players = FirebaseService.unityPlayersFromPlayersValue({
      'current-uid': {
        'online': true,
        'unity': {'inGame': true},
      },
      'remote-online': {
        'online': true,
        'username': 'Remote',
        'unity': {
          'inGame': true,
          'position': {'x': 4, 'y': 0, 'z': 7},
          'rotation': {'x': 0, 'y': 90, 'z': 0},
        },
      },
      'remote-offline': {
        'online': false,
        'unity': {'inGame': true},
      },
      'remote-not-ingame': {
        'online': true,
        'unity': {'inGame': false},
      },
    }, 'current-uid');

    expect(players, hasLength(1));
    expect(players.single.uid, 'remote-online');
    expect(players.single.username, 'Remote');
    expect(players.single.position['x'], 4);
    expect(players.single.rotation['y'], 90);
  });
}
