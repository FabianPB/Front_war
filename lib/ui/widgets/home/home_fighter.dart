import 'dart:math';

import 'package:flutter/material.dart';

class HomeFighter extends StatefulWidget {
  const HomeFighter({super.key});

  @override
  State<HomeFighter> createState() => _HomeFighterState();
}

class _HomeFighterState extends State<HomeFighter> with SingleTickerProviderStateMixin {
  late AnimationController _ctrl;

  @override
  void initState() {
    super.initState();
    _ctrl = AnimationController(vsync: this, duration: const Duration(milliseconds: 500))..repeat(reverse: true);
  }

  @override
  void dispose() {
    _ctrl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      width: 180,
      height: 180,
      child: Stack(
        alignment: Alignment.center,
        children: [
          Container(
            width: 140,
            height: 140,
            decoration: const BoxDecoration(
              shape: BoxShape.circle,
              boxShadow: [
                BoxShadow(color: Color.fromRGBO(44, 123, 229, 0.2), blurRadius: 40, spreadRadius: 10),
              ],
            ),
          ),
          AnimatedBuilder(
            animation: _ctrl,
            builder: (context, child) {
              return CustomPaint(
                size: const Size(140, 140),
                painter: FighterPainter(_ctrl.value),
              );
            },
          ),
        ],
      ),
    );
  }
}

class FighterPainter extends CustomPainter {
  final double animationValue;

  FighterPainter(this.animationValue);

  @override
  void paint(Canvas canvas, Size size) {
    final s1 = Paint()..color = const Color(0xFF2C7BE5)..style = PaintingStyle.stroke..strokeWidth = 1.2;
    final s2 = Paint()..color = const Color(0xFFFF8A5B)..style = PaintingStyle.stroke..strokeWidth = 1.5;
    final f1 = Paint()..color = const Color(0xFFFFFFFF)..style = PaintingStyle.fill;
    final fRojo = Paint()..color = const Color(0xFF00B8A9)..style = PaintingStyle.fill;
    final fSangre = Paint()..color = const Color(0xFFFFD166)..style = PaintingStyle.fill;
    final sPlata = Paint()..color = const Color(0xFF17324D).withValues(alpha: 0.55)..style = PaintingStyle.stroke..strokeWidth = 2.5..strokeCap = StrokeCap.round;

    canvas.save();
    canvas.scale(size.width / 100, size.height / 120);

    final bob = sin(animationValue * pi) * 2 - 2;
    canvas.translate(0, bob);

    canvas.drawOval(Rect.fromCenter(center: const Offset(50, 18), width: 22, height: 24), f1);
    canvas.drawOval(Rect.fromCenter(center: const Offset(50, 18), width: 22, height: 24), s1);

    final crestPath = Path()..moveTo(39, 14)..quadraticBezierTo(50, 2, 61, 14);
    canvas.drawPath(crestPath, s2);
    canvas.drawLine(const Offset(50, 2), const Offset(50, 8), Paint()..color = const Color(0xFF00B8A9)..strokeWidth = 2);

    final torsoPath = Path()..moveTo(38, 30)..quadraticBezierTo(36, 50, 35, 68)..lineTo(65, 68)..quadraticBezierTo(64, 50, 62, 30)..close();
    canvas.drawPath(torsoPath, f1);
    canvas.drawPath(torsoPath, Paint()..color = const Color(0xFF2C7BE5)..style = PaintingStyle.stroke..strokeWidth = 1);

    canvas.drawRect(const Rect.fromLTWH(45, 28, 10, 5), f1);

    final sh1 = Path()..moveTo(38, 30)..quadraticBezierTo(28, 28, 25, 38)..quadraticBezierTo(30, 40, 35, 38)..close();
    canvas.drawPath(sh1, fRojo);
    final sh2 = Path()..moveTo(62, 30)..quadraticBezierTo(72, 28, 75, 38)..quadraticBezierTo(70, 40, 65, 38)..close();
    canvas.drawPath(sh2, fRojo);

    canvas.save();
    canvas.translate(35, 38);
    canvas.rotate(-animationValue * 0.15);
    canvas.translate(-35, -38);
    final armPaint = Paint()..color = const Color(0xFF2C7BE5)..style = PaintingStyle.stroke..strokeWidth = 6..strokeCap = StrokeCap.round;
    canvas.drawLine(const Offset(35, 38), const Offset(22, 62), armPaint);
    canvas.drawCircle(const Offset(21, 64), 4, fRojo);

    final shieldPath = Path()..moveTo(18, 62)..quadraticBezierTo(10, 60, 10, 72)..quadraticBezierTo(10, 82, 18, 85)..quadraticBezierTo(26, 82, 26, 72)..quadraticBezierTo(26, 60, 18, 62)..close();
    canvas.drawPath(shieldPath, fSangre);
    canvas.drawPath(shieldPath, Paint()..color = const Color(0xFF00B8A9)..style = PaintingStyle.stroke..strokeWidth = 1);
    final shieldDeco = Paint()..color = const Color(0xFFFF8A5B)..strokeWidth = 1;
    canvas.drawLine(const Offset(18, 64), const Offset(18, 82), shieldDeco);
    canvas.drawLine(const Offset(11, 73), const Offset(25, 73), shieldDeco);
    canvas.restore();

    canvas.save();
    canvas.translate(65, 38);
    canvas.rotate(animationValue * 0.6);
    canvas.translate(-65, -38);
    canvas.drawLine(const Offset(65, 38), const Offset(78, 62), armPaint);
    canvas.drawCircle(const Offset(79, 64), 4, fRojo);

    canvas.drawLine(const Offset(80, 62), const Offset(95, 30), sPlata);
    canvas.drawLine(const Offset(88, 50), const Offset(82, 54), Paint()..color = const Color(0xFF17324D).withValues(alpha: 0.55)..style = PaintingStyle.stroke..strokeWidth = 1.5);
    canvas.restore();

    final pLegs = Paint()..color = const Color(0xFFF7FAFF)..style = PaintingStyle.fill;
    final sLegs = Paint()..color = const Color(0xFF2C7BE5)..style = PaintingStyle.stroke..strokeWidth = 0.8;
    final leg1 = Path()..moveTo(35, 68)..lineTo(32, 95)..quadraticBezierTo(36, 97, 40, 95)..lineTo(42, 68)..close();
    canvas.drawPath(leg1, pLegs);
    canvas.drawPath(leg1, sLegs);
    final leg2 = Path()..moveTo(65, 68)..lineTo(68, 95)..quadraticBezierTo(64, 97, 60, 95)..lineTo(58, 68)..close();
    canvas.drawPath(leg2, pLegs);
    canvas.drawPath(leg2, sLegs);

    final boot1 = Path()..moveTo(30, 92)..quadraticBezierTo(28, 100, 32, 103)..lineTo(42, 103)..quadraticBezierTo(44, 100, 40, 95)..close();
    canvas.drawPath(boot1, fSangre);
    final boot2 = Path()..moveTo(70, 92)..quadraticBezierTo(72, 100, 68, 103)..lineTo(58, 103)..quadraticBezierTo(56, 100, 60, 95)..close();
    canvas.drawPath(boot2, fSangre);

    final emblem = Path()..moveTo(43, 40)..lineTo(50, 34)..lineTo(57, 40)..lineTo(55, 50)..lineTo(50, 53)..lineTo(45, 50)..close();
    canvas.drawPath(emblem, Paint()..color = const Color(0xFFFF8A5B)..style = PaintingStyle.stroke..strokeWidth = 0.8..color = const Color(0xFFFF8A5B).withValues(alpha: 0.7));

    canvas.restore();
  }

  @override
  bool shouldRepaint(FighterPainter oldDelegate) => oldDelegate.animationValue != animationValue;
}
