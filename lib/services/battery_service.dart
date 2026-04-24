import 'dart:async';
import 'package:battery_plus/battery_plus.dart';
import 'package:flutter/foundation.dart';

class BatteryStatus {
  const BatteryStatus({required this.level, required this.state});

  final int level;
  final BatteryState state;

  bool get isCharging =>
      state == BatteryState.charging || state == BatteryState.full;
  bool get isLow => level <= 20 && !isCharging;
  bool get isCritical => level <= 10 && !isCharging;

  /// Label temático según el nivel.
  String get warriorLabel {
    if (isCritical) return 'AGOTADO';
    if (isLow) return 'DÉBIL';
    if (isCharging) return 'RECARGANDO';
    if (level >= 80) return 'FURIOSO';
    if (level >= 50) return 'LISTO';
    return 'CANSADO';
  }
}

class BatteryService {
  BatteryService();

  final Battery _battery = Battery();

  Future<BatteryStatus> current() async {
    try {
      final level = await _battery.batteryLevel;
      final state = await _battery.batteryState;
      return BatteryStatus(level: level, state: state);
    } catch (e) {
      debugPrint('BatteryService.current error: $e');
      return const BatteryStatus(level: 0, state: BatteryState.unknown);
    }
  }

  Stream<BatteryState> get onStateChange => _battery.onBatteryStateChanged;
}
