import 'package:flutter/material.dart';

const _wText = Color(0xFFE8E8E8);
const _wAccent = Color(0xFFBF360C);
const _wMuted = Color(0xFF888888);
const _wSurface = Color(0xFF1A1A1E);
const _wBorder = Color(0xFF2A2A2E);

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
            color: _wMuted,
          ),
        ),
        const SizedBox(height: 6),
        TextField(
          maxLines: maxLines,
          style: const TextStyle(color: _wText, fontSize: 13),
          decoration: InputDecoration(
            hintText: hint,
            hintStyle: TextStyle(color: _wMuted.withValues(alpha: 0.6)),
            filled: true,
            fillColor: _wSurface,
            contentPadding:
                const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
            enabledBorder: _border(_wBorder),
            focusedBorder: _border(_wAccent),
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
            color: _wMuted,
          ),
        ),
        const SizedBox(height: 6),
        DropdownButtonFormField<String>(
          initialValue: initialValue,
          dropdownColor: const Color(0xFF1A1A1E),
          style: const TextStyle(color: _wText, fontSize: 13),
          iconEnabledColor: _wMuted,
          decoration: InputDecoration(
            filled: true,
            fillColor: _wSurface,
            contentPadding:
                const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
            enabledBorder: _border(_wBorder),
            focusedBorder: _border(_wAccent),
          ),
          items: options
              .map((o) => DropdownMenuItem(
                    value: o,
                    child: Text(o,
                        style: const TextStyle(color: _wText, fontSize: 12)),
                  ))
              .toList(),
          onChanged: onChanged ?? (_) {},
        ),
      ],
    );
  }
}

class SupportPrimaryButton extends StatelessWidget {
  const SupportPrimaryButton(
      {super.key, required this.label, required this.onPressed});

  final String label;
  final VoidCallback onPressed;

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: onPressed,
      child: Container(
        width: double.infinity,
        padding: const EdgeInsets.symmetric(vertical: 16),
        decoration: BoxDecoration(
          gradient: const LinearGradient(
            colors: [Color(0xFFBF360C), Color(0xFF8B1A00)],
          ),
          borderRadius: BorderRadius.circular(6),
          boxShadow: [
            BoxShadow(
              color: _wAccent.withValues(alpha: 0.35),
              blurRadius: 18,
              offset: const Offset(0, 4),
            ),
          ],
        ),
        alignment: Alignment.center,
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
                color: _wAccent,
              ),
            ),
            const SizedBox(height: 14),
            Text(
              'Tu caso fue registrado con éxito.\nNuestro equipo revisará tu reporte y te responderá al correo indicado en menos de 24 horas.',
              textAlign: TextAlign.center,
              style: TextStyle(
                fontSize: 12,
                height: 1.7,
                color: _wMuted,
              ),
            ),
            const SizedBox(height: 20),
            const Text(
              '¡SIGUE LUCHANDO CON HONOR!',
              style: TextStyle(
                fontSize: 12,
                fontWeight: FontWeight.w700,
                color: _wAccent,
              ),
            ),
          ],
        ),
      ),
    );
  }
}

OutlineInputBorder _border(Color c) =>
    OutlineInputBorder(
      borderRadius: BorderRadius.circular(6),
      borderSide: BorderSide(color: c),
    );
