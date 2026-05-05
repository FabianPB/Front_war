import 'dart:ui';
import 'package:flutter/material.dart';

// Medieval War color palette (shared with auth_screen)
const _burntOrange  = Color(0xFFBF360C);
const _burntGlow    = Color(0xFFE64A19);
const _warText      = Color(0xFFE8E8E8);
const _warMuted     = Color(0xFF888888);
const _glassBorder  = Color(0xFF3D3D3D);
const _goldMetal    = Color(0xFFFFB300);

class AuthFormCard extends StatelessWidget {
  const AuthFormCard({super.key, required this.child});

  final Widget child;

  @override
  Widget build(BuildContext context) {
    return ClipRRect(
      borderRadius: BorderRadius.circular(4),
      child: BackdropFilter(
        filter: ImageFilter.blur(sigmaX: 10, sigmaY: 10),
        child: Container(
          decoration: BoxDecoration(
            color: const Color(0xD91E1E1E),
            border: Border.all(color: _goldMetal.withValues(alpha: 0.45)),
            borderRadius: BorderRadius.circular(4),
          ),
          padding: const EdgeInsets.all(28),
          child: child,
        ),
      ),
    );
  }
}

class AuthInputField extends StatefulWidget {
  const AuthInputField({
    super.key,
    required this.controller,
    required this.label,
    required this.hint,
    this.obscure  = false,
    this.type,
    this.error,
    this.focusNode,
  });

  final TextEditingController controller;
  final String label;
  final String hint;
  final bool obscure;
  final TextInputType? type;
  final String? error;
  final FocusNode? focusNode;

  @override
  State<AuthInputField> createState() => _AuthInputFieldState();
}

class _AuthInputFieldState extends State<AuthInputField> {
  late bool _isObscured;

  @override
  void initState() {
    super.initState();
    _isObscured = widget.obscure;
  }

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          widget.label.toUpperCase(),
          style: TextStyle(
            fontSize: 9,
            fontWeight: FontWeight.w700,
            letterSpacing: 1.5,
            color: _warMuted.withValues(alpha: 0.85),
          ),
        ),
        const SizedBox(height: 6),
        TextField(
          controller:  widget.controller,
          focusNode:   widget.focusNode,
          obscureText: _isObscured,
          keyboardType: widget.type,
          style: const TextStyle(color: _warText, fontSize: 13),
          decoration: InputDecoration(
            hintText: widget.hint,
            errorText: widget.error,
            hintStyle: const TextStyle(color: Color(0xFF555555)),
            filled: true,
            fillColor: const Color(0xFF161616),
            contentPadding: const EdgeInsets.symmetric(horizontal: 14, vertical: 14),
            suffixIcon: widget.obscure
                ? IconButton(
                    splashRadius: 18,
                    tooltip: _isObscured ? 'Mostrar contraseña' : 'Ocultar contraseña',
                    icon: Icon(
                      _isObscured
                          ? Icons.visibility_off_outlined
                          : Icons.visibility_outlined,
                      color: _warMuted,
                      size: 20,
                    ),
                    onPressed: () => setState(() => _isObscured = !_isObscured),
                  )
                : null,
            enabledBorder: OutlineInputBorder(
              borderRadius: BorderRadius.circular(4),
              borderSide: BorderSide(color: _glassBorder.withValues(alpha: 0.8)),
            ),
            focusedBorder: OutlineInputBorder(
              borderRadius: BorderRadius.circular(4),
              borderSide: const BorderSide(color: _burntOrange, width: 1.5),
            ),
            errorBorder: OutlineInputBorder(
              borderRadius: BorderRadius.circular(4),
              borderSide: const BorderSide(color: Color(0xFFFF6B6B)),
            ),
            focusedErrorBorder: OutlineInputBorder(
              borderRadius: BorderRadius.circular(4),
              borderSide: const BorderSide(color: Color(0xFFFF6B6B), width: 1.5),
            ),
          ),
        ),
      ],
    );
  }
}

class AuthPrimaryButton extends StatelessWidget {
  const AuthPrimaryButton({
    super.key,
    required this.text,
    required this.onPressed,
    required this.isLoading,
  });

  final String text;
  final VoidCallback onPressed;
  final bool isLoading;

  @override
  Widget build(BuildContext context) {
    return ElevatedButton(
      onPressed: isLoading ? null : onPressed,
      style: ElevatedButton.styleFrom(
        padding: EdgeInsets.zero,
        backgroundColor: Colors.transparent,
        shadowColor: Colors.transparent,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(4)),
      ).copyWith(elevation: WidgetStateProperty.all(0)),
      child: Ink(
        decoration: BoxDecoration(
          gradient: const LinearGradient(
            begin: Alignment.topLeft,
            end: Alignment.bottomRight,
            colors: [Color(0xFFBF360C), Color(0xFF8B1A00)],
          ),
          borderRadius: BorderRadius.circular(4),
          boxShadow: [
            BoxShadow(
              color: _burntGlow.withValues(alpha: 0.50),
              blurRadius: 20,
              offset: const Offset(0, 4),
            ),
            BoxShadow(
              color: _burntOrange.withValues(alpha: 0.25),
              blurRadius: 35,
            ),
          ],
        ),
        child: Container(
          alignment: Alignment.center,
          padding: const EdgeInsets.symmetric(vertical: 14),
          child: isLoading
              ? const SizedBox(
                  width: 18, height: 18,
                  child: CircularProgressIndicator(color: Colors.white, strokeWidth: 2),
                )
              : Text(
                  text,
                  style: const TextStyle(
                    fontFamily: 'serif',
                    fontSize: 12,
                    fontWeight: FontWeight.w900,
                    letterSpacing: 2.0,
                    color: Colors.white,
                    shadows: [
                      Shadow(color: Color(0x66FFFFFF), blurRadius: 6),
                    ],
                  ),
                ),
        ),
      ),
    );
  }
}

class AuthGoogleButton extends StatelessWidget {
  const AuthGoogleButton({
    super.key,
    required this.isLoading,
    required this.onPressed,
  });

  final bool isLoading;
  final VoidCallback onPressed;

  @override
  Widget build(BuildContext context) {
    return OutlinedButton.icon(
      onPressed: isLoading ? null : onPressed,
      icon: isLoading
          ? const SizedBox(
              width: 16, height: 16,
              child: CircularProgressIndicator(color: _warMuted, strokeWidth: 2),
            )
          : const Icon(Icons.g_mobiledata, color: _warText, size: 24),
      label: const Text(
        'ACCEDER CON GOOGLE',
        style: TextStyle(
            fontSize: 10, fontWeight: FontWeight.w700, letterSpacing: 1.5, color: _warText),
      ),
      style: OutlinedButton.styleFrom(
        padding: const EdgeInsets.symmetric(vertical: 14),
        side: BorderSide(color: _glassBorder.withValues(alpha: 0.8)),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(4)),
        backgroundColor: const Color(0xFF161616),
      ),
    );
  }
}

class AuthBiometricButton extends StatelessWidget {
  const AuthBiometricButton({
    super.key,
    required this.isLoading,
    required this.onPressed,
  });

  final bool isLoading;
  final VoidCallback onPressed;

  @override
  Widget build(BuildContext context) {
    return OutlinedButton.icon(
      onPressed: isLoading ? null : onPressed,
      icon: isLoading
          ? const SizedBox(
              width: 16, height: 16,
              child: CircularProgressIndicator(color: _burntOrange, strokeWidth: 2),
            )
          : const Icon(Icons.fingerprint, color: _burntOrange, size: 24),
      label: const Text(
        'ACCEDER CON HUELLA DACTILAR',
        style: TextStyle(
            fontSize: 10, fontWeight: FontWeight.w700, letterSpacing: 1.5, color: _warText),
      ),
      style: OutlinedButton.styleFrom(
        padding: const EdgeInsets.symmetric(vertical: 14),
        side: BorderSide(color: _burntOrange.withValues(alpha: 0.4)),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(4)),
        backgroundColor: const Color(0xFF161616),
      ),
    );
  }
}
