import 'package:flutter/material.dart';

const _cOscuro = Color(0xFF110608);
const _cRojo = Color(0xFFC0001A);
const _cPlata = Color(0xFFC8BFBF);

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
      backgroundColor: _cOscuro,
      resizeToAvoidBottomInset: resizeToAvoidBottomInset,
      appBar: AppBar(
        title: Text(
          title.toUpperCase(),
          style: const TextStyle(
            color: Colors.white,
            fontFamily: 'serif',
            fontWeight: FontWeight.w900,
            letterSpacing: 2.0,
            shadows: [
              Shadow(color: Color.fromRGBO(255, 0, 30, 0.6), blurRadius: 10),
            ],
          ),
        ),
        centerTitle: true,
        elevation: 0,
        backgroundColor: Colors.transparent,
        iconTheme: const IconThemeData(color: _cRojo),
        leading: showBackButton
            ? IconButton(
                icon: const Icon(Icons.arrow_back_ios_new, color: _cRojo),
                onPressed: () => Navigator.of(context).pop(),
              )
            : null,
        actions: actions,
        bottom: PreferredSize(
          preferredSize: const Size.fromHeight(1.0),
          child: Container(
            decoration: const BoxDecoration(
              gradient: LinearGradient(
                colors: [Colors.transparent, _cRojo, Colors.transparent],
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
                  colors: [Color.fromRGBO(140, 0, 20, 0.4), Colors.transparent],
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
                  colors: [Color(0xFF050102), Color(0xFF0D0306), Color(0xFF130508)],
                  stops: [0, 0.4, 1.0],
                ),
              ),
            ),
          ),
          SafeArea(child: Theme(
            data: Theme.of(context).copyWith(
              textTheme: const TextTheme(
                bodyMedium: TextStyle(color: _cPlata),
                bodyLarge: TextStyle(color: _cPlata),
                titleMedium: TextStyle(color: Colors.white),
                titleLarge: TextStyle(color: Colors.white),
              ),
              listTileTheme: ListTileThemeData(
                iconColor: _cRojo,
                textColor: _cPlata,
                tileColor: const Color.fromRGBO(20, 3, 6, 0.8),
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(4),
                  side: const BorderSide(color: Color.fromRGBO(192, 0, 26, 0.3)),
                ),
              ),
              cardColor: const Color.fromRGBO(20, 3, 6, 0.8),
              iconTheme: const IconThemeData(color: _cRojo),
            ),
            child: body,
          )),
        ],
      ),
    );
  }
}




