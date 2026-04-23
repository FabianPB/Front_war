import 'dart:math';

import 'package:flutter/material.dart';

import 'home_styles.dart';

class HomeBackground extends StatelessWidget {
  const HomeBackground({super.key});

  @override
  Widget build(BuildContext context) {
    return Stack(
      children: [
        Positioned.fill(
          child: Container(
            decoration: const BoxDecoration(
              gradient: RadialGradient(
                center: Alignment(0, 1.2),
                radius: 1.5,
                colors: [Color.fromRGBO(140, 0, 20, 0.55), Colors.transparent],
                stops: [0, 0.7],
              ),
            ),
          ),
        ),
        Positioned.fill(
          child: Container(
            decoration: const BoxDecoration(
              gradient: RadialGradient(
                center: Alignment(-0.6, 0),
                radius: 0.8,
                colors: [Color.fromRGBO(100, 0, 10, 0.3), Colors.transparent],
                stops: [0, 0.6],
              ),
            ),
          ),
        ),
        Positioned.fill(
          child: Container(
            decoration: const BoxDecoration(
              gradient: RadialGradient(
                center: Alignment(0.6, 0),
                radius: 0.8,
                colors: [Color.fromRGBO(100, 0, 10, 0.3), Colors.transparent],
                stops: [0, 0.6],
              ),
            ),
          ),
        ),
        Positioned.fill(
          child: Container(
            decoration: const BoxDecoration(
              gradient: LinearGradient(
                begin: Alignment.topCenter,
                end: Alignment.bottomCenter,
                colors: [Color(0xFF050102), Color(0xFF0D0306), Color(0xFF130508)],
                stops: [0, 0.4, 1.0],
              ),
            ),
          ),
        ),
        const Positioned.fill(child: EmbersEffect()),
      ],
    );
  }
}

class EmbersEffect extends StatefulWidget {
  const EmbersEffect({super.key});

  @override
  State<EmbersEffect> createState() => _EmbersEffectState();
}

class _EmbersEffectState extends State<EmbersEffect> with SingleTickerProviderStateMixin {
  late AnimationController _controller;
  final Random _rand = Random();
  final List<_Ember> _embers = [];

  @override
  void initState() {
    super.initState();
    _controller = AnimationController(vsync: this, duration: const Duration(seconds: 10))..repeat();
    for (int i = 0; i < 20; i++) {
      _embers.add(_Ember(_rand));
    }
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return AnimatedBuilder(
      animation: _controller,
      builder: (_, _) {
        for (var ember in _embers) {
          ember.update();
        }
        return CustomPaint(painter: _EmbersPainter(_embers));
      },
    );
  }
}

class _Ember {
  double x = 0;
  double y = 0;
  double speed = 0;
  double wobbleSpeed = 0;
  double wobblePhase = 0;
  double wobbleAmount = 0;
  double size = 0;

  _Ember(Random r) {
    reset(r, initial: true);
  }

  void reset(Random r, {bool initial = false}) {
    x = r.nextDouble();
    y = initial ? r.nextDouble() : 1.1;
    speed = 0.001 + r.nextDouble() * 0.003;
    wobbleSpeed = 0.02 + r.nextDouble() * 0.05;
    wobblePhase = r.nextDouble() * pi * 2;
    wobbleAmount = 0.002 + r.nextDouble() * 0.005;
    size = 1.0 + r.nextDouble() * 2.5;
  }

  void update() {
    y -= speed;
    x += sin(wobblePhase) * wobbleAmount;
    wobblePhase += wobbleSpeed;
    if (y < -0.1) {
      reset(Random());
    }
  }
}

class _EmbersPainter extends CustomPainter {
  final List<_Ember> embers;

  _EmbersPainter(this.embers);

  @override
  void paint(Canvas canvas, Size size) {
    final paint = Paint()
      ..color = homeRedBright
      ..maskFilter = const MaskFilter.blur(BlurStyle.normal, 3.0);

    for (var ember in embers) {
      double opacity = 1.0;
      if (ember.y > 0.9) opacity = (1.0 - ember.y) * 10;
      if (ember.y < 0.2) opacity = ember.y * 5;

      paint.color = homeRedBright.withValues(alpha: opacity.clamp(0.0, 1.0));
      canvas.drawCircle(Offset(ember.x * size.width, ember.y * size.height), ember.size, paint);
    }
  }

  @override
  bool shouldRepaint(covariant CustomPainter oldDelegate) => true;
}
