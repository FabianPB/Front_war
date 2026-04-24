import 'package:flutter/foundation.dart';
import 'package:geocoding/geocoding.dart';
import 'package:geolocator/geolocator.dart';

class WarriorLocation {
  const WarriorLocation({required this.city, required this.region});

  final String city;
  final String region;

  String get display {
    if (region.isEmpty) return city;
    if (city.isEmpty) return region;
    return '$city, $region';
  }
}

class LocationService {
  const LocationService();

  Future<WarriorLocation?> resolveCurrentLocation() async {
    try {
      final serviceEnabled = await Geolocator.isLocationServiceEnabled();
      if (!serviceEnabled) return null;

      var permission = await Geolocator.checkPermission();
      if (permission == LocationPermission.denied) {
        permission = await Geolocator.requestPermission();
      }
      if (permission == LocationPermission.denied ||
          permission == LocationPermission.deniedForever) {
        return null;
      }

      final position = await Geolocator.getCurrentPosition(
        locationSettings: const LocationSettings(
          accuracy: LocationAccuracy.low,
        ),
      ).timeout(const Duration(seconds: 10));

      final placemarks = await placemarkFromCoordinates(
        position.latitude,
        position.longitude,
      );
      if (placemarks.isEmpty) return null;
      final p = placemarks.first;

      final city = p.locality ?? p.subAdministrativeArea ?? '';
      final region = p.administrativeArea ?? p.country ?? '';

      if (city.isEmpty && region.isEmpty) return null;
      return WarriorLocation(city: city, region: region);
    } catch (e) {
      debugPrint('LocationService error: $e');
      return null;
    }
  }
}
