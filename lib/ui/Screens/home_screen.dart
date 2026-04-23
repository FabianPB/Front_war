import 'package:flutter/material.dart';
import 'package:get/get.dart';
import '../../controllers/auth_controller.dart';
import '../widgets/home/home_background.dart';
import '../widgets/home/home_fighter.dart';
import '../widgets/home/home_header.dart';
import '../widgets/home/home_menu_card.dart';
import '../widgets/home/home_styles.dart';

class HomeScreen extends StatefulWidget {
  const HomeScreen({super.key});
  static const routeName = '/home';

  @override
  State<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends State<HomeScreen> with SingleTickerProviderStateMixin {
  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: homeBackgroundColor,
      body: Stack(
        children: [
          const Positioned.fill(child: HomeBackground()),
          SafeArea(
            child: Stack(
              children: [
                Positioned(
                  top: 8,
                  right: 16,
                  child: IconButton(
                    icon: const Icon(Icons.logout, color: homeRed),
                    onPressed: () => Get.put(AuthController()).logout(),
                  ),
                ),
                Positioned.fill(
                  child: SingleChildScrollView(
                    padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 20),
                    child: Column(
                      children: [
                        const SizedBox(height: 30),
                        const HomeHeader(),
                        const SizedBox(height: 20),
                        const HomeFighter(),
                        const SizedBox(height: 30),
                        HomeMenuCard(context: context),
                        const SizedBox(height: 40),
                      ],
                    ),
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}
