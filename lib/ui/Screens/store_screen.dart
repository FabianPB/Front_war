import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
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
  void initState() {
    super.initState();
    // Allow automatic orientation rotation on store screen
    SystemChrome.setPreferredOrientations(DeviceOrientation.values);
  }

  @override
  void dispose() {
    // Restore automatic orientation when leaving store
    SystemChrome.setPreferredOrientations(DeviceOrientation.values);
    super.dispose();
  }

  void _onCategoryTap(String category) {
    setState(() => _selectedCat = category);
  }

  @override
  Widget build(BuildContext context) {
    final filteredItems = _selectedCat == 'all'
        ? StoreItemModel.demoItems
        : StoreItemModel.demoItems.where((item) => item.category == _selectedCat).toList();

    return WarScaffold(
      title: 'Armería del Guerrero',
      lockLandscape: false,
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 14, 16, 0),
            child: Wrap(
              spacing: 6,
              runSpacing: 6,
              children: [
                StoreFilterChip(label: 'Todos', isActive: _selectedCat == 'all', onTap: () => _onCategoryTap('all')),
                StoreFilterChip(label: 'Armas', isActive: _selectedCat == 'arma', onTap: () => _onCategoryTap('arma')),
                StoreFilterChip(label: 'Defensa', isActive: _selectedCat == 'defensa', onTap: () => _onCategoryTap('defensa')),
                StoreFilterChip(label: 'Objetos', isActive: _selectedCat == 'objeto', onTap: () => _onCategoryTap('objeto')),
              ],
            ),
          ),
          Expanded(
            child: LayoutBuilder(
              builder: (context, constraints) {
                // Responsive grid: 1 column on small screens, 2 on larger
                final crossAxisCount = constraints.maxWidth < 400 ? 1 : 2;
                final childAspectRatio = constraints.maxWidth < 400 ? 0.75 : 0.65;
                
                return GridView.builder(
                  padding: const EdgeInsets.fromLTRB(16, 14, 16, 20),
                  gridDelegate: SliverGridDelegateWithFixedCrossAxisCount(
                    crossAxisCount: crossAxisCount,
                    crossAxisSpacing: 10,
                    mainAxisSpacing: 10,
                    childAspectRatio: childAspectRatio,
                  ),
                  itemCount: filteredItems.length,
                  itemBuilder: (context, index) {
                    return StoreItemCard(item: filteredItems[index]);
                  },
                );
              },
            ),
          ),
        ],
      ),
    );
  }
}
