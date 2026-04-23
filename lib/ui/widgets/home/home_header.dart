import 'package:flutter/material.dart';
import 'home_styles.dart';

class HomeHeader extends StatelessWidget {
  const HomeHeader({super.key});

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        const Text(
          '⚔   COMBATE SIN PIEDAD   ⚔',
          style: TextStyle(
            color: homeRed,
            fontSize: 12,
            fontWeight: FontWeight.w600,
            letterSpacing: 4.0,
          ),
        ),
        const SizedBox(height: 10),
        RichText(
          textAlign: TextAlign.center,
          text: const TextSpan(
            children: [
              TextSpan(
                text: 'W.A.R.',
                style: TextStyle(
                  color: homeRedBright,
                  fontFamily: 'serif',
                  fontSize: 54,
                  fontWeight: FontWeight.w900,
                  letterSpacing: 4.0,
                  shadows: [
                    Shadow(color: Color.fromRGBO(255, 0, 30, 0.6), blurRadius: 20),
                  ],
                ),
              ),
            ],
          ),
        ),
        const SizedBox(height: 14),
        Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            Container(
              width: 80,
              height: 1,
              decoration: const BoxDecoration(
                gradient: LinearGradient(colors: [Colors.transparent, homeRed, Colors.transparent]),
              ),
            ),
            const SizedBox(width: 12),
            const Text('✦', style: TextStyle(color: homeRed, fontSize: 18)),
            const SizedBox(width: 12),
            Container(
              width: 80,
              height: 1,
              decoration: const BoxDecoration(
                gradient: LinearGradient(colors: [Colors.transparent, homeRed, Colors.transparent]),
              ),
            ),
          ],
        ),
        const SizedBox(height: 10),
        const Text(
          'EL HONOR SE GANA CON SANGRE',
          style: TextStyle(
            color: homeSilver,
            fontSize: 11,
            letterSpacing: 3,
            fontWeight: FontWeight.w300,
          ),
        ),
      ],
    );
  }
}
