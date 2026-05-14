import 'dart:io';

// Para dispositivo físico real corre con:
//   flutter run --dart-define=API_URL=http://192.168.x.x:5041
const _kOverride = String.fromEnvironment('API_URL', defaultValue: '');

String get apiBaseUrl {
  if (_kOverride.isNotEmpty) return _kOverride;
  if (Platform.isAndroid) return 'http://192.168.168.208:5041';
  return 'http://localhost:5041';
}
