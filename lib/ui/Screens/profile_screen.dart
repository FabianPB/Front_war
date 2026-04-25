import 'dart:async';
import 'dart:io';
import 'package:battery_plus/battery_plus.dart';
import 'package:firebase_auth/firebase_auth.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:get/get.dart';
import '../../controllers/auth_controller.dart';
import '../../services/battery_service.dart';
import '../../services/camera_service.dart';
import '../../services/firebase_service.dart';
import '../../services/local_storage_service.dart';
import '../../services/location_service.dart';
import '../widgets/profile/avatar_picker.dart';

const _wAccent = Color(0xFFE6451C);
const _wText = Color(0xFFE8E8E8);
const _wMuted = Color(0xFF888888);
const _wSurface = Color(0xFF141418);
const _wBg = Color(0xFF0B0B0D);
const _wBorder = Color(0xFF2A2A2E);

class ProfileScreen extends StatefulWidget {
  const ProfileScreen({super.key});
  static const routeName = '/profile';

  @override
  State<ProfileScreen> createState() => _ProfileScreenState();
}

class _ProfileScreenState extends State<ProfileScreen> {
  final CameraService _cameraService = const CameraService();
  final LocationService _locationService = const LocationService();
  final BatteryService _batteryService = BatteryService();

  String? _photoPath;
  String? _usernameFromDb;
  String? _city;
  bool _isLoadingCity = false;
  BatteryStatus? _battery;
  StreamSubscription<BatteryState>? _batterySub;
  bool _lowBatteryWarned = false;
  bool _isBusy = false;

  @override
  void initState() {
    super.initState();
    // Allow automatic orientation rotation on profile screen
    SystemChrome.setPreferredOrientations(DeviceOrientation.values);
    _photoPath = LocalStorageService.getPhotoPath();
    _city = LocalStorageService.getCity();
    _loadUsernameFromDb();
    _refreshLocation();
    _initBattery();
  }

  @override
  void dispose() {
    _batterySub?.cancel();
    // Restore automatic orientation when leaving
    SystemChrome.setPreferredOrientations(DeviceOrientation.values);
    super.dispose();
  }

  Future<void> _loadUsernameFromDb() async {
    final user = FirebaseAuth.instance.currentUser;
    if (user == null) return;
    if ((user.displayName ?? '').isNotEmpty) return;
    try {
      final snapshot = await FirebaseService.playersRef(user.uid).get();
      if (!mounted) return;
      if (snapshot.exists && snapshot.value is Map) {
        final data = snapshot.value as Map;
        final stored = data['username']?.toString();
        if (stored != null && stored.isNotEmpty) {
          setState(() => _usernameFromDb = stored);
          try {
            await user.updateDisplayName(stored);
            await user.reload();
          } catch (_) {}
        }
      }
    } catch (_) {}
  }

  Future<void> _refreshLocation() async {
    if (_isLoadingCity) return;
    setState(() => _isLoadingCity = true);
    try {
      final location = await _locationService.resolveCurrentLocation();
      if (!mounted) return;
      if (location != null) {
        await LocalStorageService.saveCity(location.display);
        if (!mounted) return;
        setState(() => _city = location.display);
      }
    } finally {
      if (mounted) setState(() => _isLoadingCity = false);
    }
  }

  Future<void> _initBattery() async {
    final status = await _batteryService.current();
    if (!mounted) return;
    setState(() => _battery = status);
    _maybeWarnLowBattery(status);
    _batterySub = _batteryService.onStateChange.listen((_) async {
      final updated = await _batteryService.current();
      if (!mounted) return;
      setState(() => _battery = updated);
      _maybeWarnLowBattery(updated);
    });
  }

  void _maybeWarnLowBattery(BatteryStatus status) {
    if (status.isLow && !_lowBatteryWarned) {
      _lowBatteryWarned = true;
      _showSnack('⚠️ Energía baja (${status.level}%). El guerrero está débil — recarga.',
          isError: true);
    }
    if (!status.isLow) _lowBatteryWarned = false;
  }

  Future<void> _handlePick(Future<File?> Function() action) async {
    if (_isBusy) return;
    setState(() => _isBusy = true);
    try {
      final file = await action();
      if (file == null) return;
      await LocalStorageService.savePhotoPath(file.path);
      if (!mounted) return;
      setState(() => _photoPath = file.path);
      _showSnack('Foto actualizada');
    } catch (e) {
      _showSnack('Error al guardar foto: $e', isError: true);
    } finally {
      if (mounted) setState(() => _isBusy = false);
    }
  }

  Future<void> _handleRemove() async {
    await LocalStorageService.clearPhotoPath();
    if (!mounted) return;
    setState(() => _photoPath = null);
    _showSnack('Foto eliminada');
  }

  void _showSnack(String msg, {bool isError = false}) {
    if (!mounted) return;
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Text(msg, style: const TextStyle(color: _wText)),
        backgroundColor: isError ? const Color(0xFF8B1A1A) : _wSurface,
        behavior: SnackBarBehavior.floating,
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final user = FirebaseAuth.instance.currentUser;
    final username = (user?.displayName?.isNotEmpty ?? false)
        ? user!.displayName!
        : (_usernameFromDb?.isNotEmpty ?? false)
            ? _usernameFromDb!
            : user?.email?.split('@').first ?? 'Guerrero';
    final email = user?.email ?? 'Sin correo';

    return Scaffold(
      backgroundColor: _wBg,
      body: Stack(
        children: [
          // Background fire glow
          Positioned.fill(
            child: Container(
              decoration: const BoxDecoration(
                gradient: RadialGradient(
                  center: Alignment.topRight,
                  radius: 1.4,
                  colors: [Color(0x33E6451C), _wBg],
                ),
              ),
            ),
          ),
          SafeArea(
            child: Column(
              children: [
                _buildAppBar(),
                Expanded(
                  child: SingleChildScrollView(
                    padding: const EdgeInsets.symmetric(
                        horizontal: 24, vertical: 12),
                    child: Column(
                      children: [
                        const SizedBox(height: 10),
                        AvatarPicker(
                          photoPath: _photoPath,
                          onPickFromCamera: () =>
                              _handlePick(_cameraService.pickFromCamera),
                          onPickFromGallery: () =>
                              _handlePick(_cameraService.pickFromGallery),
                          onRemove: _handleRemove,
                        ),
                        const SizedBox(height: 18),
                        Text(
                          username.toUpperCase(),
                          style: const TextStyle(
                            color: _wAccent,
                            fontSize: 22,
                            fontWeight: FontWeight.w900,
                            letterSpacing: 3,
                            fontFamily: 'serif',
                            shadows: [
                              Shadow(
                                  color: Color(0x88E6451C), blurRadius: 16)
                            ],
                          ),
                        ),
                        const SizedBox(height: 4),
                        Text(
                          email,
                          style: const TextStyle(
                              color: _wMuted,
                              fontSize: 12,
                              letterSpacing: 1.5),
                        ),
                        const SizedBox(height: 8),
                        _buildLocationLine(),
                        const SizedBox(height: 24),
                        _buildStatsCard(),
                        const SizedBox(height: 20),
                        _buildLogoutButton(),
                        const SizedBox(height: 20),
                      ],
                    ),
                  ),
                ),
                if (_isBusy)
                  const LinearProgressIndicator(
                      color: _wAccent,
                      backgroundColor: _wSurface),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildAppBar() {
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 6),
      child: Row(
        children: [
          IconButton(
            icon: const Icon(Icons.arrow_back, color: _wAccent),
            onPressed: () => Get.back(),
          ),
          const Spacer(),
          const Text(
            'MI PERFIL',
            style: TextStyle(
              color: _wText,
              fontSize: 13,
              letterSpacing: 4,
              fontWeight: FontWeight.w600,
              fontFamily: 'serif',
            ),
          ),
          const Spacer(),
          const SizedBox(width: 48),
        ],
      ),
    );
  }

  Widget _buildLocationLine() {
    final hasCity = (_city ?? '').isNotEmpty;
    final display = hasCity ? _city! : 'Ubicación desconocida';

    return InkWell(
      onTap: _isLoadingCity ? null : _refreshLocation,
      borderRadius: BorderRadius.circular(6),
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
        child: Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(
              hasCity ? Icons.location_on : Icons.location_off,
              size: 13,
              color: hasCity ? _wAccent : _wMuted,
            ),
            const SizedBox(width: 6),
            Flexible(
              child: Text(
                _isLoadingCity
                    ? 'Localizando guerrero…'
                    : 'Guerrero de $display',
                style: TextStyle(
                  color: hasCity ? _wAccent : _wMuted,
                  fontSize: 11,
                  letterSpacing: 1.2,
                  fontWeight: FontWeight.w600,
                ),
              ),
            ),
            const SizedBox(width: 6),
            if (_isLoadingCity)
              const SizedBox(
                width: 10,
                height: 10,
                child: CircularProgressIndicator(
                    strokeWidth: 1.5, color: _wAccent),
              )
            else
              Icon(Icons.refresh,
                  size: 13, color: _wMuted.withValues(alpha: 0.7)),
          ],
        ),
      ),
    );
  }

  Widget _buildStatsCard() {
    final batteryValue =
        _battery == null ? '—' : '${_battery!.level}%';
    final batteryLabel = _battery?.warriorLabel ?? 'ENERGÍA';
    final batteryColor = _battery == null
        ? _wAccent
        : _battery!.isCritical
            ? const Color(0xFFFF4444)
            : _battery!.isLow
                ? const Color(0xFFFF8C00)
                : _wAccent;

    return Container(
      padding: const EdgeInsets.symmetric(vertical: 18, horizontal: 16),
      decoration: BoxDecoration(
        color: _wSurface,
        border: Border.all(color: _wBorder),
        borderRadius: BorderRadius.circular(10),
      ),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceAround,
        children: [
          const _StatItem(label: 'PARTIDAS', value: '—'),
          _StatDivider(),
          const _StatItem(label: 'VICTORIAS', value: '—'),
          _StatDivider(),
          _StatItem(
              label: batteryLabel,
              value: batteryValue,
              color: batteryColor),
        ],
      ),
    );
  }

  Widget _buildLogoutButton() {
    return SizedBox(
      width: double.infinity,
      child: OutlinedButton.icon(
        style: OutlinedButton.styleFrom(
          side: const BorderSide(color: _wAccent, width: 1.5),
          padding: const EdgeInsets.symmetric(vertical: 14),
          shape:
              RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
        ),
        icon: const Icon(Icons.logout, color: _wAccent),
        label: const Text(
          'CERRAR SESIÓN',
          style: TextStyle(
              color: _wText, letterSpacing: 3, fontWeight: FontWeight.w600),
        ),
        onPressed: () => Get.put(AuthController()).logout(),
      ),
    );
  }
}

class _StatItem extends StatelessWidget {
  const _StatItem({required this.label, required this.value, this.color});
  final String label;
  final String value;
  final Color? color;

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        Text(
          value,
          style: TextStyle(
            color: color ?? _wAccent,
            fontSize: 20,
            fontWeight: FontWeight.w900,
          ),
        ),
        const SizedBox(height: 4),
        Text(
          label,
          style: const TextStyle(
              color: _wMuted, fontSize: 9, letterSpacing: 2),
        ),
      ],
    );
  }
}

class _StatDivider extends StatelessWidget {
  @override
  Widget build(BuildContext context) {
    return Container(
        width: 1,
        height: 32,
        color: _wBorder);
  }
}
