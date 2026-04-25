import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

// ─── Dark war-themed scaffold used by all sub-screens ────────────────────────
class WarScaffold extends StatefulWidget {
  const WarScaffold({
    super.key,
    required this.title,
    required this.body,
    this.actions,
    this.lockLandscape = true,
  });

  final String title;
  final Widget body;
  final List<Widget>? actions;
  final bool lockLandscape;

  @override
  State<WarScaffold> createState() => _WarScaffoldState();
}

class _WarScaffoldState extends State<WarScaffold> {
  @override
  void initState() {
    super.initState();
    if (widget.lockLandscape) {
      SystemChrome.setPreferredOrientations([
        DeviceOrientation.landscapeLeft,
        DeviceOrientation.landscapeRight,
      ]);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: const Color(0xFF0B0B0D),
      appBar: AppBar(
        backgroundColor: const Color(0xFF141418),
        elevation: 0,
        leading: IconButton(
          icon: const Icon(Icons.arrow_back, color: Color(0xFFE6451C)),
          onPressed: () => Navigator.of(context).pop(),
        ),
        title: Text(
          widget.title.toUpperCase(),
          style: const TextStyle(
            color: Color(0xFFE8E8E8),
            fontFamily: 'serif',
            fontWeight: FontWeight.w900,
            letterSpacing: 2,
            fontSize: 14,
          ),
        ),
        centerTitle: true,
        iconTheme: const IconThemeData(color: Color(0xFFE6451C)),
        actionsIconTheme: const IconThemeData(color: Color(0xFFE6451C)),
        bottom: PreferredSize(
          preferredSize: const Size.fromHeight(1),
          child: Container(
            height: 1,
            decoration: const BoxDecoration(
              gradient: LinearGradient(
                colors: [
                  Colors.transparent,
                  Color(0xFFE6451C),
                  Colors.transparent,
                ],
              ),
            ),
          ),
        ),
        actions: widget.actions,
      ),
      body: widget.body,
    );
  }
}

const _cBase = Color(0xFFF7FAFF);
const _cPrimary = Color(0xFF2C7BE5);
const _cAccent = Color(0xFFFF8A5B);
const _cSurface = Color(0xFFFFFFFF);
const _cText = Color(0xFF17324D);

class AppScaffold extends StatelessWidget {
  const AppScaffold({
    super.key,
    required this.title,
    required this.body,
    this.showBackButton = false,
    this.actions,
    this.resizeToAvoidBottomInset = true,
  });

  final String title;
  final Widget body;
  final bool showBackButton;
  final List<Widget>? actions;
  final bool resizeToAvoidBottomInset;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: _cBase,
      resizeToAvoidBottomInset: resizeToAvoidBottomInset,
      appBar: AppBar(
        title: Text(
          title.toUpperCase(),
          style: const TextStyle(
            color: _cText,
            fontFamily: 'serif',
            fontWeight: FontWeight.w900,
            letterSpacing: 2.0,
            shadows: [
              Shadow(color: Color.fromRGBO(44, 123, 229, 0.22), blurRadius: 10),
            ],
          ),
        ),
        centerTitle: true,
        elevation: 0,
        backgroundColor: Colors.transparent,
        iconTheme: const IconThemeData(color: _cPrimary),
        leading: showBackButton
            ? IconButton(
          icon: const Icon(Icons.arrow_back_ios_new, color: _cPrimary),
                onPressed: () => Navigator.of(context).pop(),
              )
            : null,
        actions: actions,
        bottom: PreferredSize(
          preferredSize: const Size.fromHeight(1.0),
          child: Container(
            decoration: const BoxDecoration(
              gradient: LinearGradient(
                colors: [Colors.transparent, _cAccent, Colors.transparent],
              ),
            ),
            height: 1.0,
          ),
        ),
      ),
      extendBodyBehindAppBar: true,
      body: Stack(
        children: [
          Positioned.fill(
            child: Container(
              decoration: const BoxDecoration(
                gradient: RadialGradient(
                  center: Alignment(0, 1.2),
                  radius: 1.5,
                    colors: [Color.fromRGBO(0, 184, 169, 0.16), Colors.transparent],
                  stops: [0, 0.7],
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
                  colors: [Color(0xFFF7FAFF), Color(0xFFEFF6FF), Color(0xFFFFF6EE)],
                  stops: [0, 0.55, 1.0],
                ),
              ),
            ),
          ),
          SafeArea(child: Theme(
            data: Theme.of(context).copyWith(
              textTheme: const TextTheme(
                bodyMedium: TextStyle(color: _cText),
                bodyLarge: TextStyle(color: _cText),
                titleMedium: TextStyle(color: _cText),
                titleLarge: TextStyle(color: _cText),
              ),
              listTileTheme: ListTileThemeData(
                iconColor: _cPrimary,
                textColor: _cText,
                tileColor: _cSurface,
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(14),
                  side: const BorderSide(color: Color.fromRGBO(44, 123, 229, 0.18)),
                ),
              ),
              cardColor: _cSurface,
              iconTheme: const IconThemeData(color: _cPrimary),
            ),
            child: body,
          )),
        ],
      ),
    );
  }
}




