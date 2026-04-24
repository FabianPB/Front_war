import 'package:flutter/material.dart';
import 'package:get/get.dart';
import '../../models/store_item_model.dart';
import '../widgets/app_scaffold.dart'; // WarScaffold
import '../widgets/qr/item_found_sheet.dart';
import '../widgets/store/store_widgets.dart';
import 'qr_scanner_screen.dart';

class StoreScreen extends StatefulWidget {
  const StoreScreen({super.key});
  static const routeName = '/store';

  @override
  State<StoreScreen> createState() => _StoreScreenState();
}

class _StoreScreenState extends State<StoreScreen> {
  String _selectedCat = 'all';

  Future<void> _openScanner() async {
    debugPrint('[STORE] QR button pressed, opening scanner...');
    try {
      final scanned = await Get.to<StoreItemModel?>(() => const QrScannerScreen());
      debugPrint('[STORE] Scanner returned: ${scanned?.id ?? "null"}');
      if (scanned != null && mounted) {
        await ItemFoundSheet.show(context, scanned);
      }
    } catch (e, st) {
      debugPrint('[STORE] Error opening scanner: $e\n$st');
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('No se pudo abrir el escáner: $e')),
        );
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final filteredItems = _selectedCat == 'all'
        ? StoreItemModel.demoItems
        : StoreItemModel.demoItems.where((item) => item.category == _selectedCat).toList();

    return WarScaffold(
      title: 'Armería del Guerrero',
      actions: [
        IconButton(
          tooltip: 'Escanear QR',
          icon: const Icon(Icons.qr_code_scanner, color: Color(0xFFE6451C)),
          onPressed: _openScanner,
        ),
      ],
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 14, 16, 0),
            child: Wrap(
              spacing: 6,
              runSpacing: 6,
              children: [
                StoreFilterChip(label: 'Todos', isActive: _selectedCat == 'all', onTap: () => setState(() => _selectedCat = 'all')),
                StoreFilterChip(label: 'Armas', isActive: _selectedCat == 'arma', onTap: () => setState(() => _selectedCat = 'arma')),
                StoreFilterChip(label: 'Defensa', isActive: _selectedCat == 'defensa', onTap: () => setState(() => _selectedCat = 'defensa')),
                StoreFilterChip(label: 'Objetos', isActive: _selectedCat == 'objeto', onTap: () => setState(() => _selectedCat = 'objeto')),
              ],
            ),
          ),
          Expanded(
            child: GridView.builder(
              padding: const EdgeInsets.fromLTRB(16, 14, 16, 20),
              gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
                crossAxisCount: 2,
                crossAxisSpacing: 10,
                mainAxisSpacing: 10,
                childAspectRatio: 0.65,
              ),
              itemCount: filteredItems.length,
              itemBuilder: (context, index) {
                return StoreItemCard(item: filteredItems[index]);
              },
            ),
          ),
        ],
      ),
    );
  }
}
