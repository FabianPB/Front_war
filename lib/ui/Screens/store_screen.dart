import 'package:flutter/material.dart';
import '../../models/store_item_model.dart';
import '../widgets/app_scaffold.dart';
import '../widgets/store/store_widgets.dart';

class StoreScreen extends StatefulWidget {
  const StoreScreen({super.key});
  static const routeName = '/store';

  @override
  State<StoreScreen> createState() => _StoreScreenState();
}

class _StoreScreenState extends State<StoreScreen> {
  String _selectedCat = 'all';

  @override
  Widget build(BuildContext context) {
    final filteredItems = _selectedCat == 'all'
        ? StoreItemModel.demoItems
        : StoreItemModel.demoItems.where((item) => item.category == _selectedCat).toList();

    return AppScaffold(
      title: '🛒  Armería del Guerrero',
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
