import 'package:flutter/material.dart';
import 'package:get/get.dart';
import 'package:mobile_scanner/mobile_scanner.dart';
import '../../models/store_item_model.dart';
import '../../services/local_storage_service.dart';
import '../../services/qr_service.dart';
import '../widgets/home/home_styles.dart';

class QrScannerScreen extends StatefulWidget {
  const QrScannerScreen({super.key});
  static const routeName = '/qr_scanner';

  @override
  State<QrScannerScreen> createState() => _QrScannerScreenState();
}

class _QrScannerScreenState extends State<QrScannerScreen> {
  final MobileScannerController _controller = MobileScannerController(
    detectionSpeed: DetectionSpeed.noDuplicates,
    facing: CameraFacing.back,
  );
  final QrService _qrService = const QrService();
  bool _handled = false;

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  Future<void> _onDetect(BarcodeCapture capture) async {
    if (_handled) return;
    final raw = capture.barcodes.firstOrNull?.rawValue;
    debugPrint('[SCANNER] Detected raw: $raw');
    if (raw == null) return;

    final itemId = _qrService.parseItemId(raw);

    if (itemId == null) {
      _showInvalid('Código QR no reconocido');
      return;
    }

    final item = StoreItemModel.findById(itemId);
    if (item == null) {
      _showInvalid('Ítem no encontrado en la armería');
      return;
    }

    _handled = true;
    await _controller.stop();
    await LocalStorageService.addScannedItem(item.id);
    if (!mounted) return;
    Get.back<StoreItemModel>(result: item);
  }

  void _showInvalid(String message) {
    if (!mounted) return;
    ScaffoldMessenger.of(context).clearSnackBars();
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Text(message),
        backgroundColor: const Color(0xFFFF6B6B),
        behavior: SnackBarBehavior.floating,
        duration: const Duration(seconds: 2),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: Colors.black,
      body: Stack(
        children: [
          MobileScanner(controller: _controller, onDetect: _onDetect),
          Positioned.fill(child: _buildOverlay()),
          SafeArea(
            child: Column(
              children: [
                _buildAppBar(),
                const Spacer(),
                _buildHint(),
                const SizedBox(height: 30),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildAppBar() {
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 8),
      child: Row(
        children: [
          IconButton(
            icon: const Icon(Icons.arrow_back, color: Colors.white),
            onPressed: () => Get.back(),
          ),
          const Spacer(),
          const Text(
            'ESCANEAR CÓDIGO',
            style: TextStyle(
              color: Colors.white,
              fontSize: 14,
              letterSpacing: 4,
              fontWeight: FontWeight.w600,
            ),
          ),
          const Spacer(),
          IconButton(
            icon: const Icon(Icons.flash_on, color: Colors.white),
            onPressed: () => _controller.toggleTorch(),
          ),
        ],
      ),
    );
  }

  Widget _buildOverlay() {
    return IgnorePointer(
      child: Center(
        child: Container(
          width: 260,
          height: 260,
          decoration: BoxDecoration(
            border: Border.all(color: homeAccent, width: 3),
            borderRadius: BorderRadius.circular(16),
            boxShadow: [
              BoxShadow(
                color: homeAccent.withValues(alpha: 0.35),
                blurRadius: 24,
                spreadRadius: 2,
              ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildHint() {
    return Container(
      margin: const EdgeInsets.symmetric(horizontal: 32),
      padding: const EdgeInsets.symmetric(horizontal: 18, vertical: 12),
      decoration: BoxDecoration(
        color: Colors.black.withValues(alpha: 0.65),
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: homeAccent.withValues(alpha: 0.4)),
      ),
      child: const Text(
        'Apunta tu cámara al código QR de la armería',
        textAlign: TextAlign.center,
        style: TextStyle(color: Colors.white, fontSize: 13, letterSpacing: 0.5),
      ),
    );
  }
}
