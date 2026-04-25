import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
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
  void initState() {
    super.initState();
    // Force landscape for better QR scanning
    SystemChrome.setPreferredOrientations([
      DeviceOrientation.landscapeLeft,
      DeviceOrientation.landscapeRight,
    ]);
  }

  @override
  void dispose() {
    _controller.dispose();
    // Restore automatic orientation when returning to store
    SystemChrome.setPreferredOrientations(DeviceOrientation.values);
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
            iconSize: 20,
            onPressed: () => Get.back(),
          ),
          const Spacer(),
          Flexible(
            child: Text(
              'ESCANEAR CÓDIGO',
              style: TextStyle(
                color: Colors.white,
                fontSize: MediaQuery.of(context).size.width < 300 ? 10 : 14,
                letterSpacing: 2,
                fontWeight: FontWeight.w600,
              ),
              overflow: TextOverflow.ellipsis,
            ),
          ),
          const Spacer(),
          IconButton(
            icon: const Icon(Icons.flash_on, color: Colors.white),
            iconSize: 20,
            onPressed: () => _controller.toggleTorch(),
          ),
        ],
      ),
    );
  }

  Widget _buildOverlay() {
    return LayoutBuilder(
      builder: (context, constraints) {
        // Responsive size: use 70% of smallest dimension, max 260px
        final maxSize = constraints.biggest.shortestSide * 0.7;
        final overlaySize = maxSize > 260 ? 260.0 : maxSize;
        
        return IgnorePointer(
          child: Center(
            child: Container(
              width: overlaySize,
              height: overlaySize,
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
      },
    );
  }

  Widget _buildHint() {
    return LayoutBuilder(
      builder: (context, constraints) {
        // Responsive padding and font size based on available space
        final horizontalMargin = constraints.maxWidth < 300 ? 16.0 : 32.0;
        final fontSize = constraints.maxWidth < 300 ? 11.0 : 13.0;
        
        return Container(
          margin: EdgeInsets.symmetric(horizontal: horizontalMargin),
          padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
          decoration: BoxDecoration(
            color: Colors.black.withValues(alpha: 0.65),
            borderRadius: BorderRadius.circular(12),
            border: Border.all(color: homeAccent.withValues(alpha: 0.4)),
          ),
          child: Text(
            'Apunta tu cámara al código QR de la armería',
            textAlign: TextAlign.center,
            style: TextStyle(
              color: Colors.white,
              fontSize: fontSize,
              letterSpacing: 0.5,
            ),
          ),
        );
      },
    );
  }
}
