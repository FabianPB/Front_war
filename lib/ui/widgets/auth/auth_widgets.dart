import 'package:flutter/material.dart';

class AuthTabButton extends StatelessWidget {
  const AuthTabButton({
    super.key,
    required this.label,
    required this.isActive,
    required this.onTap,
  });

  final String label;
  final bool isActive;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return InkWell(
      onTap: onTap,
      child: AnimatedContainer(
        duration: const Duration(milliseconds: 200),
        alignment: Alignment.center,
        padding: const EdgeInsets.symmetric(vertical: 14),
        decoration: BoxDecoration(
          color: isActive ? const Color.fromRGBO(192, 0, 26, 0.2) : const Color.fromRGBO(14, 3, 5, 0.85),
          border: Border.all(color: isActive ? const Color(0xFFC0001A) : const Color.fromRGBO(192, 0, 26, 0.25)),
          borderRadius: BorderRadius.circular(3),
        ),
        child: Text(
          label,
          style: TextStyle(
            fontFamily: 'serif',
            fontSize: 12,
            fontWeight: FontWeight.w700,
            letterSpacing: 2.0,
            color: isActive ? const Color(0xFFF5E8E8) : const Color.fromRGBO(200, 170, 170, 0.5),
          ),
        ),
      ),
    );
  }
}

class AuthFormCard extends StatelessWidget {
  const AuthFormCard({super.key, required this.child});

  final Widget child;

  @override
  Widget build(BuildContext context) {
    return Container(
      decoration: BoxDecoration(
        gradient: const LinearGradient(
          colors: [Color.fromRGBO(20, 3, 6, 0.95), Color.fromRGBO(12, 2, 4, 0.98)],
          begin: Alignment.topLeft,
          end: Alignment.bottomRight,
        ),
        border: Border.all(color: const Color.fromRGBO(192, 0, 26, 0.3)),
        borderRadius: BorderRadius.circular(4),
        boxShadow: const [
          BoxShadow(color: Color.fromRGBO(255, 0, 30, 0.08), spreadRadius: 1),
          BoxShadow(color: Colors.black87, blurRadius: 40, offset: Offset(0, 20)),
        ],
      ),
      padding: const EdgeInsets.all(24),
      child: child,
    );
  }
}

class AuthInputField extends StatelessWidget {
  const AuthInputField({
    super.key,
    required this.controller,
    required this.label,
    required this.hint,
    this.obscure = false,
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
          controller: controller,
          focusNode: focusNode,
          obscureText: obscure,
          keyboardType: type,
          style: const TextStyle(color: Color(0xFFF5E8E8), fontSize: 13),
          decoration: InputDecoration(
            hintText: hint,
            errorText: error,
            hintStyle: const TextStyle(color: Color.fromRGBO(200, 170, 170, 0.22)),
            filled: true,
            fillColor: const Color.fromRGBO(12, 2, 4, 0.75),
            contentPadding: const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
            enabledBorder: OutlineInputBorder(
              borderRadius: BorderRadius.circular(3),
              borderSide: const BorderSide(color: Color.fromRGBO(192, 0, 26, 0.22)),
            ),
            focusedBorder: OutlineInputBorder(
              borderRadius: BorderRadius.circular(3),
              borderSide: const BorderSide(color: Color.fromRGBO(255, 30, 50, 0.5)),
            ),
            errorBorder: OutlineInputBorder(
              borderRadius: BorderRadius.circular(3),
              borderSide: const BorderSide(color: Color(0xFFFF1A2E)),
            ),
            focusedErrorBorder: OutlineInputBorder(
              borderRadius: BorderRadius.circular(3),
              borderSide: const BorderSide(color: Color(0xFFFF1A2E)),
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
        padding: const EdgeInsets.symmetric(vertical: 0),
        backgroundColor: Colors.transparent,
        shadowColor: Colors.transparent,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(3)),
      ).copyWith(elevation: WidgetStateProperty.all(0)),
      child: Ink(
        decoration: BoxDecoration(
          gradient: const LinearGradient(colors: [Color(0xFF8A0010), Color(0xFFC0001A), Color(0xFFFF1A2E)]),
          borderRadius: BorderRadius.circular(3),
          boxShadow: const [BoxShadow(color: Color.fromRGBO(255, 0, 30, 0.3), blurRadius: 20)],
        ),
        child: Container(
          alignment: Alignment.center,
          padding: const EdgeInsets.symmetric(vertical: 16),
          child: isLoading
              ? const SizedBox(
                  width: 20,
                  height: 20,
                  child: CircularProgressIndicator(color: Colors.white, strokeWidth: 2),
                )
              : Text(
                  text,
                  style: const TextStyle(
                    fontFamily: 'serif',
                    fontSize: 13,
                    fontWeight: FontWeight.w900,
                    letterSpacing: 2.0,
                    color: Colors.white,
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
              width: 16,
              height: 16,
              child: CircularProgressIndicator(color: Color(0xFFC0001A), strokeWidth: 2),
            )
          : const Icon(Icons.g_mobiledata, color: Color(0xFFC0001A), size: 24),
      label: const Text(
        'ACCEDER CON GOOGLE',
        style: TextStyle(fontSize: 10, fontWeight: FontWeight.w700, letterSpacing: 1.5, color: Color(0xFFF5E8E8)),
      ),
      style: OutlinedButton.styleFrom(
        padding: const EdgeInsets.symmetric(vertical: 16),
        side: const BorderSide(color: Color.fromRGBO(192, 0, 26, 0.4)),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(3)),
        backgroundColor: const Color.fromRGBO(12, 2, 4, 0.5),
      ),
    );
  }
}
