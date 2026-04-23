import 'package:firebase_core/firebase_core.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart';
import 'package:frontend_war/firebase_options.dart';
import 'package:frontend_war/ui/screens/auth_screen.dart';
import 'package:frontend_war/ui/screens/chat_screen.dart';
import 'package:frontend_war/ui/screens/form_screen.dart';
import 'package:frontend_war/ui/screens/game_screen.dart';
import 'package:frontend_war/ui/screens/home_screen.dart';
import 'package:frontend_war/ui/screens/store_screen.dart';

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();
  await Firebase.initializeApp(options: DefaultFirebaseOptions.currentPlatform);

  runApp(const FrontendWarApp());
}

class FrontendWarApp extends StatelessWidget {
  const FrontendWarApp({super.key});

  @override
  Widget build(BuildContext context) {
    return GetMaterialApp(
      debugShowCheckedModeBanner: false,
      title: 'W.A.R',
      theme: ThemeData(
        colorScheme: ColorScheme.fromSeed(
          seedColor: const Color(0xFF8B0000),
          brightness: Brightness.dark,
        ),
        scaffoldBackgroundColor: Colors.black,
        useMaterial3: true,
      ),
      home: const AuthScreen(),
      getPages: [
        GetPage(name: HomeScreen.routeName, page: () => const HomeScreen()),
        GetPage(name: GameScreen.routeName, page: () => const GameScreen()),
        GetPage(name: StoreScreen.routeName, page: () => const StoreScreen()),
        GetPage(name: ChatScreen.routeName, page: () => const ChatScreen()),
        GetPage(name: FormScreen.routeName, page: () => const FormScreen()),
      ],
    );
  }
}
