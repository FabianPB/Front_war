import 'package:flutter/material.dart';

class SupportField extends StatelessWidget {
  const SupportField({
    super.key,
    required this.label,
    required this.hint,
    this.maxLines = 1,
  });

  final String label;
  final String hint;
  final int maxLines;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          label.toUpperCase(),
          style: const TextStyle(
            fontSize: 9,
            fontWeight: FontWeight.w700,
            letterSpacing: 1.5,
            color: Color(0xFFC0001A),
          ),
        ),
        const SizedBox(height: 6),
        TextField(
          maxLines: maxLines,
          style: const TextStyle(color: Color(0xFFF5E8E8), fontSize: 13),
          decoration: InputDecoration(
            hintText: hint,
            hintStyle: const TextStyle(color: Color.fromRGBO(200, 170, 170, 0.22)),
            filled: true,
            fillColor: const Color.fromRGBO(12, 2, 4, 0.75),
            contentPadding: const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
            enabledBorder: _outlineBorder(BorderRadius.circular(3), const Color.fromRGBO(192, 0, 26, 0.22)),
            focusedBorder: _outlineBorder(BorderRadius.circular(3), const Color.fromRGBO(255, 30, 50, 0.5)),
          ),
        ),
      ],
    );
  }
}

class SupportDropdown extends StatelessWidget {
  const SupportDropdown({
    super.key,
    required this.label,
    required this.options,
    this.initialValue,
    this.onChanged,
  });

  final String label;
  final List<String> options;
  final String? initialValue;
  final ValueChanged<String?>? onChanged;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          label.toUpperCase(),
          style: const TextStyle(
            fontSize: 9,
            fontWeight: FontWeight.w700,
            letterSpacing: 1.5,
            color: Color(0xFFC0001A),
          ),
        ),
        const SizedBox(height: 6),
        DropdownButtonFormField<String>(
          initialValue: initialValue,
          dropdownColor: const Color(0xFF1A0305),
          style: const TextStyle(color: Color(0xFFF5E8E8), fontSize: 13),
          decoration: InputDecoration(
            filled: true,
            fillColor: const Color.fromRGBO(12, 2, 4, 0.75),
            contentPadding: const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
            enabledBorder: _outlineBorder(BorderRadius.circular(3), const Color.fromRGBO(192, 0, 26, 0.22)),
            focusedBorder: _outlineBorder(BorderRadius.circular(3), const Color.fromRGBO(255, 30, 50, 0.5)),
          ),
          items: options.map((option) => DropdownMenuItem(value: option, child: Text(option))).toList(),
          onChanged: onChanged ?? (_) {},
        ),
      ],
    );
  }
}

class SupportPrimaryButton extends StatelessWidget {
  const SupportPrimaryButton({super.key, required this.label, required this.onPressed});

  final String label;
  final VoidCallback onPressed;

  @override
  Widget build(BuildContext context) {
    return ElevatedButton(
      onPressed: onPressed,
      style: ElevatedButton.styleFrom(
        padding: const EdgeInsets.symmetric(vertical: 18),
        backgroundColor: Colors.transparent,
        shadowColor: Colors.transparent,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(3)),
      ).copyWith(elevation: WidgetStateProperty.all(0)),
      child: Ink(
        decoration: BoxDecoration(
          gradient: const LinearGradient(
            colors: [Color(0xFF8A0010), Color(0xFFC0001A), Color(0xFFFF1A2E)],
          ),
          borderRadius: BorderRadius.circular(3),
          boxShadow: const [BoxShadow(color: Color.fromRGBO(255, 0, 30, 0.3), blurRadius: 20)],
        ),
        child: Container(
          alignment: Alignment.center,
          padding: const EdgeInsets.symmetric(vertical: 16),
          child: Text(
            label,
            style: const TextStyle(
              fontFamily: 'serif',
              fontSize: 13,
              fontWeight: FontWeight.w700,
              letterSpacing: 2.0,
              color: Colors.white,
            ),
          ),
        ),
      ),
    );
  }
}

class SupportSuccessView extends StatelessWidget {
  const SupportSuccessView({super.key});

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 32),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            const Text('⚔️', style: TextStyle(fontSize: 50)),
            const SizedBox(height: 14),
            const Text(
              '¡REPORTE ENVIADO, GUERRERO!',
              textAlign: TextAlign.center,
              style: TextStyle(
                fontFamily: 'serif',
                fontSize: 16,
                fontWeight: FontWeight.w700,
                letterSpacing: 2.0,
                color: Color(0xFFFF1A2E),
              ),
            ),
            const SizedBox(height: 14),
            const Text(
              'Tu caso fue registrado con éxito.\nNuestro equipo revisará tu reporte y te responderá al correo indicado en menos de 24 horas.',
              textAlign: TextAlign.center,
              style: TextStyle(
                fontSize: 12,
                height: 1.7,
                color: Color.fromRGBO(200, 170, 170, 0.55),
              ),
            ),
            const SizedBox(height: 20),
            const Text(
              '¡SIGUE LUCHANDO CON HONOR!',
              style: TextStyle(
                fontSize: 12,
                fontWeight: FontWeight.w700,
                color: Color(0xFFFF1A2E),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

InputBorder _outlineBorder(BorderRadius r, Color c) {
  return OutlineInputBorder(borderRadius: r, borderSide: BorderSide(color: c));
}
