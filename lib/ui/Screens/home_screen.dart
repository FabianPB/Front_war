import 'dart:io';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:get/get.dart';
import 'package:path_provider/path_provider.dart';
import 'package:pdf/pdf.dart';
import 'package:pdf/widgets.dart' as pw;
import 'package:printing/printing.dart';
import '../../controllers/auth_controller.dart';
import '../../services/firebase_service.dart';
import '../../services/local_storage_service.dart';
import '../widgets/home/home_fighter.dart';
import 'profile_screen.dart';
import 'chat_screen.dart';
import 'form_screen.dart';
import 'game_screen.dart';
import 'store_screen.dart';

const _warAccent = Color(0xFFE6451C);
const _warText = Color(0xFFE8E8E8);
const _warMuted = Color(0xFF888888);
const _warBorder = Color(0xFF2A2A2E);

class HomeScreen extends StatefulWidget {
  const HomeScreen({super.key});
  static const routeName = '/home';

  @override
  State<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends State<HomeScreen> {
  String? _avatarPath;
  _ChartOption _selectedChart = _chartOptions.first;

  @override
  void initState() {
    super.initState();
    // Allow automatic orientation rotation on home screen (menu principal)
    SystemChrome.setPreferredOrientations(DeviceOrientation.values);
    _refreshAvatar();
  }

  @override
  void dispose() {
    // Restore automatic orientation when leaving
    SystemChrome.setPreferredOrientations(DeviceOrientation.values);
    super.dispose();
  }

  void _refreshAvatar() {
    setState(() => _avatarPath = LocalStorageService.getPhotoPath());
  }

  Future<void> _openProfile() async {
    await Get.toNamed(ProfileScreen.routeName);
    if (mounted) _refreshAvatar();
  }

  void _navigate(Widget screen) {
    Navigator.of(context).push(MaterialPageRoute(builder: (_) => screen));
  }

  void _selectChart(_ChartOption option) {
    setState(() => _selectedChart = option);
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Text('Gráfico seleccionado: ${option.label}'),
        duration: const Duration(milliseconds: 900),
        backgroundColor: const Color(0xFF1A1A1E),
        behavior: SnackBarBehavior.floating,
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final hasAvatar = _avatarPath != null && File(_avatarPath!).existsSync();
    final isPortrait =
        MediaQuery.of(context).orientation == Orientation.portrait;

    return isPortrait
        ? _buildPortraitLayout(hasAvatar)
        : _buildLandscapeLayout(hasAvatar);
  }

  // Portrait (vertical) layout with AppBar menu
  Widget _buildPortraitLayout(bool hasAvatar) {
    return Scaffold(
      backgroundColor: const Color(0xFF0A0707),
      appBar: AppBar(
        backgroundColor: const Color(0xFF141418),
        elevation: 0,
        title: Row(
          children: [
            GestureDetector(
              onTap: _openProfile,
              child: Container(
                width: 36,
                height: 36,
                decoration: BoxDecoration(
                  shape: BoxShape.circle,
                  border: Border.all(color: _warAccent, width: 1.5),
                  color: const Color(0xFF1A1A1E),
                ),
                child: ClipOval(
                  child: hasAvatar
                      ? Image.file(File(_avatarPath!), fit: BoxFit.cover)
                      : const Icon(Icons.person, color: _warMuted, size: 18),
                ),
              ),
            ),
            const SizedBox(width: 12),
            const Text(
              'W.A.R.',
              style: TextStyle(
                color: _warAccent,
                fontFamily: 'serif',
                fontSize: 18,
                fontWeight: FontWeight.w900,
                letterSpacing: 3,
              ),
            ),
          ],
        ),
        actions: [
          PopupMenuButton(
            icon: const Icon(Icons.menu, color: _warMuted),
            shape: RoundedRectangleBorder(
              borderRadius: BorderRadius.circular(4),
            ),
            color: const Color(0xFF1A1A1E),
            itemBuilder: (context) => [
              PopupMenuItem(
                value: 'tienda',
                child: Row(
                  children: [
                    const Icon(Icons.storefront, color: _warAccent, size: 18),
                    const SizedBox(width: 12),
                    const Text(
                      'TIENDA',
                      style: TextStyle(color: _warText, fontSize: 12),
                    ),
                    const Spacer(),
                    Container(
                      padding: const EdgeInsets.symmetric(
                        horizontal: 8,
                        vertical: 2,
                      ),
                      decoration: BoxDecoration(
                        color: _warAccent.withValues(alpha: 0.2),
                        borderRadius: BorderRadius.circular(3),
                      ),
                      child: const Text(
                        'NUEVO',
                        style: TextStyle(
                          color: _warAccent,
                          fontSize: 8,
                          fontWeight: FontWeight.bold,
                        ),
                      ),
                    ),
                  ],
                ),
              ),
              PopupMenuItem(
                value: 'chat',
                child: Row(
                  children: [
                    const Icon(
                      Icons.chat_bubble_outline,
                      color: _warAccent,
                      size: 18,
                    ),
                    const SizedBox(width: 12),
                    const Text(
                      'CHAT',
                      style: TextStyle(color: _warText, fontSize: 12),
                    ),
                    const Spacer(),
                    Container(
                      padding: const EdgeInsets.symmetric(
                        horizontal: 6,
                        vertical: 2,
                      ),
                      decoration: BoxDecoration(
                        color: _warAccent.withValues(alpha: 0.2),
                        borderRadius: BorderRadius.circular(3),
                      ),
                      child: const Text(
                        '12',
                        style: TextStyle(
                          color: _warAccent,
                          fontSize: 8,
                          fontWeight: FontWeight.bold,
                        ),
                      ),
                    ),
                  ],
                ),
              ),
              PopupMenuItem(
                value: 'eventos',
                child: Row(
                  children: [
                    const Icon(Icons.flash_on, color: _warAccent, size: 18),
                    const SizedBox(width: 12),
                    const Text(
                      'EVENTOS',
                      style: TextStyle(color: _warText, fontSize: 12),
                    ),
                    const Spacer(),
                    Container(
                      padding: const EdgeInsets.symmetric(
                        horizontal: 8,
                        vertical: 2,
                      ),
                      decoration: BoxDecoration(
                        color: _warAccent.withValues(alpha: 0.2),
                        borderRadius: BorderRadius.circular(3),
                      ),
                      child: const Text(
                        'LIVE',
                        style: TextStyle(
                          color: _warAccent,
                          fontSize: 8,
                          fontWeight: FontWeight.bold,
                        ),
                      ),
                    ),
                  ],
                ),
              ),
              PopupMenuItem(
                value: 'soporte',
                child: Row(
                  children: [
                    const Icon(Icons.security, color: _warAccent, size: 18),
                    const SizedBox(width: 12),
                    const Text(
                      'SOPORTE',
                      style: TextStyle(color: _warText, fontSize: 12),
                    ),
                  ],
                ),
              ),
            ],
            onSelected: (value) {
              switch (value) {
                case 'tienda':
                  _navigate(const StoreScreen());
                  break;
                case 'chat':
                  _navigate(const ChatScreen());
                  break;
                case 'eventos':
                  _navigate(const GameScreen());
                  break;
                case 'soporte':
                  _navigate(const FormScreen());
                  break;
              }
            },
          ),
          IconButton(
            icon: const Icon(Icons.logout, color: _warMuted, size: 18),
            tooltip: 'Cerrar sesión',
            onPressed: () => Get.put(AuthController()).logout(),
          ),
        ],
        bottom: PreferredSize(
          preferredSize: const Size.fromHeight(126),
          child: _ChartsAppBar(
            selected: _selectedChart,
            onSelected: _selectChart,
          ),
        ),
      ),
      body: Stack(
        fit: StackFit.expand,
        children: [
          Container(
            decoration: const BoxDecoration(
              gradient: LinearGradient(
                begin: Alignment.topLeft,
                end: Alignment.bottomRight,
                colors: [Color(0xFF0D0808), Color(0xFF120A06)],
              ),
            ),
          ),
          Positioned.fill(
            child: Container(
              decoration: const BoxDecoration(
                gradient: RadialGradient(
                  center: Alignment.bottomCenter,
                  radius: 1.2,
                  colors: [
                    Color(0xAAE6451C),
                    Color(0x55B83318),
                    Color(0x22802010),
                    Colors.transparent,
                  ],
                  stops: [0.0, 0.35, 0.65, 1.0],
                ),
              ),
            ),
          ),
          Center(
            child: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                Stack(
                  alignment: Alignment.center,
                  children: [
                    Container(
                      width: 130,
                      height: 130,
                      decoration: const BoxDecoration(
                        shape: BoxShape.circle,
                        boxShadow: [
                          BoxShadow(
                            color: Color(0x55E6451C),
                            blurRadius: 40,
                            spreadRadius: 8,
                          ),
                        ],
                      ),
                    ),
                    SizedBox(
                      width: 130,
                      height: 130,
                      child: FittedBox(
                        fit: BoxFit.contain,
                        child: const HomeFighter(),
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 16),
                const Text(
                  'EL HONOR SE GANA CON SANGRE',
                  style: TextStyle(
                    color: _warMuted,
                    fontSize: 8,
                    letterSpacing: 3,
                  ),
                ),
                const SizedBox(height: 20),
                SizedBox(
                  width: 200,
                  child: _BattleButton(
                    onTap: () => _navigate(const GameScreen()),
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  // Landscape layout with side panel
  Widget _buildLandscapeLayout(bool hasAvatar) {
    return Scaffold(
      backgroundColor: const Color(0xFF0A0707),
      body: Row(
        children: [
          Expanded(flex: 5, child: _buildLeftPanel(hasAvatar)),
          Expanded(flex: 4, child: _buildRightPanel()),
        ],
      ),
    );
  }

  Widget _buildLeftPanel(bool hasAvatar) {
    return Stack(
      fit: StackFit.expand,
      children: [
        // Dark warm base
        Container(
          decoration: const BoxDecoration(
            gradient: LinearGradient(
              begin: Alignment.topLeft,
              end: Alignment.bottomRight,
              colors: [Color(0xFF0D0808), Color(0xFF120A06)],
            ),
          ),
        ),
        // Fire glow from bottom
        Positioned.fill(
          child: Container(
            decoration: const BoxDecoration(
              gradient: RadialGradient(
                center: Alignment.bottomCenter,
                radius: 1.2,
                colors: [
                  Color(0xAAE6451C),
                  Color(0x55B83318),
                  Color(0x22802010),
                  Colors.transparent,
                ],
                stops: [0.0, 0.35, 0.65, 1.0],
              ),
            ),
          ),
        ),
        // Top-left ember
        Positioned(
          top: -30,
          left: -20,
          child: Container(
            width: 160,
            height: 160,
            decoration: const BoxDecoration(
              shape: BoxShape.circle,
              gradient: RadialGradient(
                colors: [Color(0x33E6451C), Colors.transparent],
              ),
            ),
          ),
        ),
        // Faded WAR texture
        Center(
          child: Opacity(
            opacity: 0.04,
            child: Text(
              'WAR',
              style: TextStyle(
                fontFamily: 'serif',
                fontSize: 180,
                fontWeight: FontWeight.w900,
                color: Colors.white,
                letterSpacing: 20,
              ),
            ),
          ),
        ),
        // Content
        SafeArea(
          right: false,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              // Top bar: avatar + branding
              Padding(
                padding: const EdgeInsets.fromLTRB(16, 10, 16, 0),
                child: Row(
                  children: [
                    GestureDetector(
                      onTap: _openProfile,
                      child: Container(
                        width: 42,
                        height: 42,
                        decoration: BoxDecoration(
                          shape: BoxShape.circle,
                          border: Border.all(color: _warAccent, width: 1.5),
                          color: const Color(0xFF1A1A1E),
                        ),
                        child: ClipOval(
                          child: hasAvatar
                              ? Image.file(
                                  File(_avatarPath!),
                                  fit: BoxFit.cover,
                                )
                              : const Icon(
                                  Icons.person,
                                  color: _warMuted,
                                  size: 22,
                                ),
                        ),
                      ),
                    ),
                    const SizedBox(width: 8),
                    Expanded(
                      child: _ChartsAppBar(
                        selected: _selectedChart,
                        onSelected: _selectChart,
                        compact: true,
                      ),
                    ),
                    const SizedBox(width: 12),
                    Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: const [
                        Text(
                          'W.A.R.',
                          style: TextStyle(
                            color: _warAccent,
                            fontFamily: 'serif',
                            fontSize: 20,
                            fontWeight: FontWeight.w900,
                            letterSpacing: 4,
                            shadows: [
                              Shadow(color: Color(0x88E6451C), blurRadius: 14),
                            ],
                          ),
                        ),
                        Text(
                          'MMORPG · PVP',
                          style: TextStyle(
                            color: _warMuted,
                            fontSize: 8,
                            letterSpacing: 3,
                          ),
                        ),
                      ],
                    ),
                  ],
                ),
              ),
              // Fighter centered in remaining space
              Expanded(
                child: Center(
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Stack(
                        alignment: Alignment.center,
                        children: [
                          Container(
                            width: 130,
                            height: 130,
                            decoration: const BoxDecoration(
                              shape: BoxShape.circle,
                              boxShadow: [
                                BoxShadow(
                                  color: Color(0x55E6451C),
                                  blurRadius: 40,
                                  spreadRadius: 8,
                                ),
                              ],
                            ),
                          ),
                          SizedBox(
                            width: 130,
                            height: 130,
                            child: FittedBox(
                              fit: BoxFit.contain,
                              child: const HomeFighter(),
                            ),
                          ),
                        ],
                      ),
                      const SizedBox(height: 16),
                      const Text(
                        'EL HONOR SE GANA CON SANGRE',
                        style: TextStyle(
                          color: _warMuted,
                          fontSize: 8,
                          letterSpacing: 3,
                        ),
                      ),
                      const SizedBox(height: 20),
                      SizedBox(
                        width: 200,
                        child: _BattleButton(
                          onTap: () => _navigate(const GameScreen()),
                        ),
                      ),
                    ],
                  ),
                ),
              ),
            ],
          ),
        ),
        // Right-edge fade into panel
        Positioned(
          right: 0,
          top: 0,
          bottom: 0,
          child: Container(
            width: 50,
            decoration: const BoxDecoration(
              gradient: LinearGradient(
                begin: Alignment.centerLeft,
                end: Alignment.centerRight,
                colors: [Colors.transparent, Color(0xFF141418)],
              ),
            ),
          ),
        ),
      ],
    );
  }

  Widget _buildRightPanel() {
    return Container(
      color: const Color(0xFF141418),
      child: SafeArea(
        left: false,
        child: LayoutBuilder(
          builder: (context, constraints) {
            final isSmallPanel = constraints.maxWidth < 250;
            final padding = isSmallPanel ? 12.0 : 18.0;
            final verticalSpacing = isSmallPanel ? 3.0 : 5.0;
            final fontSize = isSmallPanel ? 11.0 : 14.0;

            return Column(
              children: [
                // Logout row
                Align(
                  alignment: Alignment.topRight,
                  child: Padding(
                    padding: const EdgeInsets.only(right: 6, top: 2),
                    child: IconButton(
                      icon: const Icon(
                        Icons.logout,
                        color: _warMuted,
                        size: 18,
                      ),
                      tooltip: 'Cerrar sesión',
                      onPressed: () => Get.put(AuthController()).logout(),
                    ),
                  ),
                ),
                // Menu - scrollable to avoid overflow on small screens
                Expanded(
                  child: SingleChildScrollView(
                    padding: EdgeInsets.fromLTRB(padding, 0, padding, 12),
                    child: Column(
                      mainAxisAlignment: MainAxisAlignment.center,
                      crossAxisAlignment: CrossAxisAlignment.stretch,
                      children: [
                        Text(
                          'MENÚ PRINCIPAL',
                          style: TextStyle(
                            color: _warText,
                            fontFamily: 'serif',
                            fontSize: fontSize,
                            fontWeight: FontWeight.w900,
                            letterSpacing: 2,
                          ),
                        ),
                        SizedBox(height: isSmallPanel ? 8 : 12),
                        _WarMenuItem(
                          icon: Icons.storefront,
                          label: 'TIENDA',
                          badge: 'NUEVO',
                          onTap: () => _navigate(const StoreScreen()),
                        ),
                        SizedBox(height: verticalSpacing),
                        _WarMenuItem(
                          icon: Icons.chat_bubble_outline,
                          label: 'CHAT',
                          badge: '12',
                          onTap: () => _navigate(const ChatScreen()),
                        ),
                        SizedBox(height: verticalSpacing),
                        _WarMenuItem(
                          icon: Icons.flash_on,
                          label: 'EVENTOS',
                          badge: 'LIVE',
                          onTap: () => _navigate(const GameScreen()),
                        ),
                        SizedBox(height: verticalSpacing),
                        _WarMenuItem(
                          icon: Icons.security,
                          label: 'SOPORTE',
                          onTap: () => _navigate(const FormScreen()),
                        ),
                      ],
                    ),
                  ),
                ),
              ],
            );
          },
        ),
      ),
    );
  }
}

Future<void> _exportChartData(
  BuildContext context,
  UsersChartStats stats,
) async {
  try {
    final pdfBytes = await _buildStatsPdf(stats);
    final now = DateTime.now();
    final fileStamp =
        now.toIso8601String().replaceAll(':', '-').split('.').first;
    final filename = 'reporte_war_$fileStamp.pdf';

    // Save a local copy under the app's documents directory.
    final directory = await getApplicationDocumentsDirectory();
    final file = File('${directory.path}/$filename');
    await file.writeAsBytes(pdfBytes);

    // Open the system share sheet so the user can save / print / send the PDF.
    await Printing.sharePdf(bytes: pdfBytes, filename: filename);

    if (!context.mounted) return;
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Text('Reporte PDF generado: $filename'),
        duration: const Duration(seconds: 3),
        backgroundColor: const Color(0xFF1A1A1E),
        behavior: SnackBarBehavior.floating,
      ),
    );
  } catch (e) {
    if (!context.mounted) return;
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Text('No se pudo generar el PDF: $e'),
        backgroundColor: const Color(0xFF8B1A1A),
        behavior: SnackBarBehavior.floating,
      ),
    );
  }
}

Future<Uint8List> _buildStatsPdf(UsersChartStats stats) async {
  final doc = pw.Document();
  final now = DateTime.now();
  final dateLabel =
      '${now.day.toString().padLeft(2, '0')}/${now.month.toString().padLeft(2, '0')}/${now.year}'
      ' · ${now.hour.toString().padLeft(2, '0')}:${now.minute.toString().padLeft(2, '0')}';

  const accent = PdfColor.fromInt(0xFFE6451C);
  const text = PdfColor.fromInt(0xFF17324D);
  const muted = PdfColor.fromInt(0xFF6E7F92);
  const border = PdfColor.fromInt(0xFFE0E5EC);

  pw.Widget statRow(String label, int value, PdfColor color) {
    return pw.Container(
      padding: const pw.EdgeInsets.symmetric(vertical: 12, horizontal: 14),
      decoration: pw.BoxDecoration(
        border: pw.Border.all(color: border),
        borderRadius: pw.BorderRadius.circular(6),
      ),
      child: pw.Row(
        mainAxisAlignment: pw.MainAxisAlignment.spaceBetween,
        children: [
          pw.Row(children: [
            pw.Container(
              width: 10,
              height: 10,
              decoration: pw.BoxDecoration(
                color: color,
                shape: pw.BoxShape.circle,
              ),
            ),
            pw.SizedBox(width: 10),
            pw.Text(
              label,
              style: pw.TextStyle(
                fontSize: 12,
                color: text,
                fontWeight: pw.FontWeight.bold,
              ),
            ),
          ]),
          pw.Text(
            '$value',
            style: pw.TextStyle(
              fontSize: 18,
              color: color,
              fontWeight: pw.FontWeight.bold,
            ),
          ),
        ],
      ),
    );
  }

  doc.addPage(
    pw.Page(
      pageFormat: PdfPageFormat.a4,
      margin: const pw.EdgeInsets.all(40),
      build: (context) {
        return pw.Column(
          crossAxisAlignment: pw.CrossAxisAlignment.start,
          children: [
            pw.Text(
              'W.A.R.',
              style: pw.TextStyle(
                fontSize: 32,
                color: accent,
                fontWeight: pw.FontWeight.bold,
                letterSpacing: 4,
              ),
            ),
            pw.SizedBox(height: 2),
            pw.Text(
              'MMORPG · PVP — Reporte de usuarios',
              style: pw.TextStyle(fontSize: 11, color: muted, letterSpacing: 2),
            ),
            pw.SizedBox(height: 4),
            pw.Container(height: 2, color: accent, width: 60),
            pw.SizedBox(height: 24),
            pw.Text('Generado: $dateLabel',
                style: pw.TextStyle(fontSize: 10, color: muted)),
            pw.SizedBox(height: 30),
            pw.Text(
              'RESUMEN DE USUARIOS',
              style: pw.TextStyle(
                fontSize: 13,
                color: text,
                fontWeight: pw.FontWeight.bold,
                letterSpacing: 2,
              ),
            ),
            pw.SizedBox(height: 14),
            statRow('Usuarios conectados', stats.connectedUsers,
                const PdfColor.fromInt(0xFF22C55E)),
            pw.SizedBox(height: 8),
            statRow('Usuarios creados', stats.accountsCreated, accent),
            pw.SizedBox(height: 8),
            statRow('Usuarios offline', stats.offlineUsers, muted),
            pw.SizedBox(height: 30),
            pw.Container(
              padding: const pw.EdgeInsets.all(14),
              decoration: pw.BoxDecoration(
                color: const PdfColor.fromInt(0xFFFAF5F2),
                borderRadius: pw.BorderRadius.circular(6),
              ),
              child: pw.Column(
                crossAxisAlignment: pw.CrossAxisAlignment.start,
                children: [
                  pw.Text('Detalle',
                      style: pw.TextStyle(
                          fontSize: 11,
                          color: text,
                          fontWeight: pw.FontWeight.bold)),
                  pw.SizedBox(height: 6),
                  pw.Text(
                    'Total de cuentas registradas en Firebase: '
                    '${stats.accountsCreated}.\n'
                    'De ellas, ${stats.connectedUsers} están actualmente '
                    'conectadas y ${stats.offlineUsers} se encuentran offline.',
                    style: pw.TextStyle(
                      fontSize: 10,
                      color: text,
                      lineSpacing: 2,
                    ),
                  ),
                ],
              ),
            ),
            pw.Spacer(),
            pw.Container(
              alignment: pw.Alignment.center,
              child: pw.Text(
                'EL HONOR SE GANA CON SANGRE',
                style: pw.TextStyle(
                    fontSize: 9, color: muted, letterSpacing: 4),
              ),
            ),
          ],
        );
      },
    ),
  );

  return doc.save();
}

class _ChartOption {
  const _ChartOption({
    required this.label,
    required this.icon,
    required this.kind,
  });

  final String label;
  final IconData icon;
  final _ChartKind kind;
}

enum _ChartKind { bars, lines, pie, points }

const _chartOptions = [
  _ChartOption(label: 'Barras', icon: Icons.bar_chart, kind: _ChartKind.bars),
  _ChartOption(label: 'Líneas', icon: Icons.show_chart, kind: _ChartKind.lines),
  _ChartOption(label: 'Circular', icon: Icons.pie_chart, kind: _ChartKind.pie),
  _ChartOption(
    label: 'Puntos',
    icon: Icons.scatter_plot,
    kind: _ChartKind.points,
  ),
];

class _ChartsAppBar extends StatelessWidget {
  const _ChartsAppBar({
    required this.selected,
    required this.onSelected,
    this.compact = false,
  });

  final _ChartOption selected;
  final ValueChanged<_ChartOption> onSelected;
  final bool compact;

  @override
  Widget build(BuildContext context) {
    // TEMP: static data while real-time Firebase stream is debugged.
    // Replace this Builder with a StreamBuilder<UsersChartStats> using
    // FirebaseService.usersStatsStream() once live data is verified.
    return Builder(
      builder: (context) {
        const stats = UsersChartStats(
          connectedUsers: 1,
          accountsCreated: 10,
          offlineUsers: 3,
        );
        final isLoading = false;

        return Container(
          width: double.infinity,
          height: compact ? 76 : 118,
          margin: EdgeInsets.fromLTRB(compact ? 0 : 12, 0, compact ? 0 : 12, 8),
          padding: EdgeInsets.all(compact ? 8 : 10),
          decoration: BoxDecoration(
            color: const Color(0xFF1A1A1E),
            border: Border.all(color: _warBorder),
            borderRadius: BorderRadius.circular(6),
          ),
          child: Row(
            children: [
              _ChartTypePopup(
                selected: selected,
                onSelected: onSelected,
              ),
              SizedBox(width: compact ? 6 : 8),
              Expanded(
                child: CustomPaint(
                  painter: _UsersChartPainter(
                    stats: stats,
                    kind: selected.kind,
                    muted: isLoading,
                  ),
                  child: const SizedBox.expand(),
                ),
              ),
              if (!compact) ...[
                const SizedBox(width: 10),
                _StatsSummary(stats: stats, isLoading: isLoading),
              ],
              SizedBox(width: compact ? 4 : 6),
              Tooltip(
                message: 'Exportar datos',
                child: IconButton(
                  visualDensity: VisualDensity.compact,
                  icon: const Icon(Icons.download, color: _warAccent, size: 18),
                  onPressed: () => _exportChartData(context, stats),
                ),
              ),
            ],
          ),
        );
      },
    );
  }
}

class _ChartTypePopup extends StatelessWidget {
  const _ChartTypePopup({
    required this.selected,
    required this.onSelected,
  });

  final _ChartOption selected;
  final ValueChanged<_ChartOption> onSelected;

  @override
  Widget build(BuildContext context) {
    return Semantics(
      label: 'Tipo de gráfico',
      child: Container(
        decoration: BoxDecoration(
          color: _warAccent.withValues(alpha: 0.16),
          border: Border.all(color: _warAccent.withValues(alpha: 0.6)),
          borderRadius: BorderRadius.circular(6),
        ),
        child: PopupMenuButton<_ChartOption>(
          tooltip: 'Tipo de gráfico',
          padding: const EdgeInsets.all(6),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(6),
            side: const BorderSide(color: _warBorder),
          ),
          color: const Color(0xFF1A1A1E),
          onSelected: onSelected,
          itemBuilder: (context) => [
            for (final option in _chartOptions)
              PopupMenuItem<_ChartOption>(
                value: option,
                height: 38,
                child: Row(
                  children: [
                    Icon(
                      option.icon,
                      color: identical(selected, option)
                          ? _warAccent
                          : _warMuted,
                      size: 16,
                    ),
                    const SizedBox(width: 10),
                    Text(
                      option.label,
                      style: TextStyle(
                        color: identical(selected, option)
                            ? _warAccent
                            : _warText,
                        fontSize: 12,
                        fontWeight: identical(selected, option)
                            ? FontWeight.w700
                            : FontWeight.w500,
                        letterSpacing: 0.5,
                      ),
                    ),
                  ],
                ),
              ),
          ],
          child: Padding(
            padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 4),
            child: Row(
              mainAxisSize: MainAxisSize.min,
              children: [
                Icon(selected.icon, color: _warAccent, size: 18),
                const SizedBox(width: 4),
                const Icon(
                  Icons.arrow_drop_down,
                  color: _warAccent,
                  size: 16,
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

class _StatsSummary extends StatelessWidget {
  const _StatsSummary({required this.stats, required this.isLoading});

  final UsersChartStats stats;
  final bool isLoading;

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      width: 116,
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          _StatLine(
            label: 'Usuarios conectados',
            value: stats.connectedUsers,
            color: const Color(0xFF4ADE80),
            isLoading: isLoading,
          ),
          const SizedBox(height: 7),
          _StatLine(
            label: 'Usuarios creados',
            value: stats.accountsCreated,
            color: _warAccent,
            isLoading: isLoading,
          ),
          const SizedBox(height: 7),
          _StatLine(
            label: 'Offline',
            value: stats.offlineUsers,
            color: _warMuted,
            isLoading: isLoading,
          ),
        ],
      ),
    );
  }
}

class _StatLine extends StatelessWidget {
  const _StatLine({
    required this.label,
    required this.value,
    required this.color,
    required this.isLoading,
  });

  final String label;
  final int value;
  final Color color;
  final bool isLoading;

  @override
  Widget build(BuildContext context) {
    return Row(
      children: [
        Container(
          width: 7,
          height: 7,
          decoration: BoxDecoration(color: color, shape: BoxShape.circle),
        ),
        const SizedBox(width: 6),
        Expanded(
          child: Text(
            label,
            overflow: TextOverflow.ellipsis,
            style: const TextStyle(
              color: _warMuted,
              fontSize: 9,
              fontWeight: FontWeight.w700,
            ),
          ),
        ),
        Text(
          isLoading ? '...' : '$value',
          style: TextStyle(
            color: color,
            fontSize: 11,
            fontWeight: FontWeight.w900,
          ),
        ),
      ],
    );
  }
}

class _UsersChartPainter extends CustomPainter {
  const _UsersChartPainter({
    required this.stats,
    required this.kind,
    required this.muted,
  });

  final UsersChartStats stats;
  final _ChartKind kind;
  final bool muted;

  static const _colors = [Color(0xFF4ADE80), _warAccent, _warMuted];

  List<double> get _values => [
    stats.connectedUsers.toDouble(),
    stats.accountsCreated.toDouble(),
    stats.offlineUsers.toDouble(),
  ];

  @override
  void paint(Canvas canvas, Size size) {
    final values = _values;
    final maxValue = stats.maxValue <= 0 ? 1.0 : stats.maxValue.toDouble();
    final alpha = muted ? 0.35 : 1.0;
    final gridPaint = Paint()
      ..color = _warBorder.withValues(alpha: 0.75)
      ..strokeWidth = 1;

    for (var i = 1; i <= 3; i++) {
      final y = size.height * i / 4;
      canvas.drawLine(Offset(0, y), Offset(size.width, y), gridPaint);
    }

    switch (kind) {
      case _ChartKind.bars:
        _paintBars(canvas, size, values, maxValue, alpha);
        break;
      case _ChartKind.lines:
        _paintLines(canvas, size, values, maxValue, alpha);
        break;
      case _ChartKind.pie:
        _paintPie(canvas, size, values, alpha);
        break;
      case _ChartKind.points:
        _paintPoints(canvas, size, values, maxValue, alpha);
        break;
    }
  }

  void _paintBars(
    Canvas canvas,
    Size size,
    List<double> values,
    double maxValue,
    double alpha,
  ) {
    final barWidth = size.width / 7;
    for (var i = 0; i < values.length; i++) {
      final height = (values[i] / maxValue) * (size.height - 18);
      final left = size.width * (i + 1) / 4 - barWidth / 2;
      final rect = RRect.fromRectAndRadius(
        Rect.fromLTWH(left, size.height - height, barWidth, height),
        const Radius.circular(3),
      );
      canvas.drawRRect(
        rect,
        Paint()..color = _colors[i].withValues(alpha: alpha),
      );
    }
  }

  void _paintLines(
    Canvas canvas,
    Size size,
    List<double> values,
    double maxValue,
    double alpha,
  ) {
    final points = _chartPoints(size, values, maxValue);
    final path = Path()..moveTo(points.first.dx, points.first.dy);
    for (final point in points.skip(1)) {
      path.lineTo(point.dx, point.dy);
    }
    canvas.drawPath(
      path,
      Paint()
        ..color = _warAccent.withValues(alpha: alpha)
        ..strokeWidth = 3
        ..style = PaintingStyle.stroke
        ..strokeCap = StrokeCap.round,
    );
    _paintDots(canvas, points, alpha);
  }

  void _paintPoints(
    Canvas canvas,
    Size size,
    List<double> values,
    double maxValue,
    double alpha,
  ) {
    _paintDots(canvas, _chartPoints(size, values, maxValue), alpha, radius: 6);
  }

  void _paintPie(Canvas canvas, Size size, List<double> values, double alpha) {
    final total = values.fold<double>(0, (sum, value) => sum + value);
    final side = size.shortestSide - 8;
    final rect = Rect.fromCenter(
      center: Offset(size.width / 2, size.height / 2),
      width: side,
      height: side,
    );

    if (total <= 0) {
      canvas.drawOval(
        rect,
        Paint()
          ..color = _warBorder
          ..style = PaintingStyle.stroke
          ..strokeWidth = 4,
      );
      return;
    }

    var start = -1.5708;
    for (var i = 0; i < values.length; i++) {
      final sweep = (values[i] / total) * 6.2832;
      canvas.drawArc(
        rect,
        start,
        sweep,
        true,
        Paint()..color = _colors[i].withValues(alpha: alpha),
      );
      start += sweep;
    }
  }

  List<Offset> _chartPoints(Size size, List<double> values, double maxValue) {
    return [
      for (var i = 0; i < values.length; i++)
        Offset(
          size.width * (i + 1) / 4,
          size.height - (values[i] / maxValue) * (size.height - 18),
        ),
    ];
  }

  void _paintDots(
    Canvas canvas,
    List<Offset> points,
    double alpha, {
    double radius = 4,
  }) {
    for (var i = 0; i < points.length; i++) {
      canvas.drawCircle(
        points[i],
        radius,
        Paint()..color = _colors[i].withValues(alpha: alpha),
      );
      canvas.drawCircle(
        points[i],
        radius + 2,
        Paint()
          ..color = _colors[i].withValues(alpha: 0.16 * alpha)
          ..style = PaintingStyle.stroke
          ..strokeWidth = 2,
      );
    }
  }

  @override
  bool shouldRepaint(covariant _UsersChartPainter oldDelegate) {
    return oldDelegate.stats.connectedUsers != stats.connectedUsers ||
        oldDelegate.stats.accountsCreated != stats.accountsCreated ||
        oldDelegate.stats.offlineUsers != stats.offlineUsers ||
        oldDelegate.kind != kind ||
        oldDelegate.muted != muted;
  }
}

class _WarMenuItem extends StatefulWidget {
  const _WarMenuItem({
    required this.icon,
    required this.label,
    this.badge,
    required this.onTap,
  });

  final IconData icon;
  final String label;
  final String? badge;
  final VoidCallback onTap;

  @override
  State<_WarMenuItem> createState() => _WarMenuItemState();
}

class _WarMenuItemState extends State<_WarMenuItem> {
  bool _pressed = false;

  @override
  Widget build(BuildContext context) {
    return LayoutBuilder(
      builder: (context, constraints) {
        // Responsive sizing based on available width
        final isSmallScreen = constraints.maxWidth < 250;
        final padding = isSmallScreen ? 10.0 : 14.0;
        final fontSize = isSmallScreen ? 10.0 : 12.0;
        final iconSize = isSmallScreen ? 16.0 : 18.0;

        return InkWell(
          onTap: widget.onTap,
          onHighlightChanged: (h) => setState(() => _pressed = h),
          borderRadius: BorderRadius.circular(6),
          child: AnimatedContainer(
            duration: const Duration(milliseconds: 180),
            padding: EdgeInsets.symmetric(horizontal: padding, vertical: 11),
            decoration: BoxDecoration(
              color: _pressed
                  ? const Color(0x22E6451C)
                  : const Color(0xFF1A1A1E),
              border: Border.all(
                color: _pressed
                    ? _warAccent.withValues(alpha: 0.6)
                    : _warBorder,
              ),
              borderRadius: BorderRadius.circular(6),
            ),
            child: Row(
              children: [
                Icon(
                  widget.icon,
                  color: _pressed ? _warAccent : _warMuted,
                  size: iconSize,
                ),
                const SizedBox(width: 10),
                Expanded(
                  child: Text(
                    widget.label,
                    style: TextStyle(
                      color: _pressed ? _warText : _warMuted,
                      fontSize: fontSize,
                      fontWeight: FontWeight.w700,
                      letterSpacing: _pressed ? 2 : 1,
                      overflow: TextOverflow.ellipsis,
                    ),
                  ),
                ),
                if (widget.badge != null) ...[
                  const SizedBox(width: 6),
                  Container(
                    padding: const EdgeInsets.symmetric(
                      horizontal: 4,
                      vertical: 1,
                    ),
                    decoration: BoxDecoration(
                      color: _warAccent.withValues(alpha: 0.18),
                      border: Border.all(
                        color: _warAccent.withValues(alpha: 0.4),
                      ),
                      borderRadius: BorderRadius.circular(3),
                    ),
                    child: Text(
                      widget.badge!,
                      style: const TextStyle(
                        color: _warAccent,
                        fontSize: 8,
                        fontWeight: FontWeight.bold,
                      ),
                    ),
                  ),
                ],
                const SizedBox(width: 4),
                Icon(
                  Icons.chevron_right,
                  color: _warAccent.withValues(alpha: _pressed ? 1.0 : 0.0),
                  size: 14,
                ),
              ],
            ),
          ),
        );
      },
    );
  }
}

class _BattleButton extends StatelessWidget {
  const _BattleButton({required this.onTap});

  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: onTap,
      child: Container(
        padding: const EdgeInsets.symmetric(vertical: 14),
        decoration: BoxDecoration(
          gradient: const LinearGradient(
            colors: [Color(0xFFE6451C), Color(0xFFB83318)],
          ),
          borderRadius: BorderRadius.circular(6),
          boxShadow: [
            BoxShadow(
              color: const Color(0xFFE6451C).withValues(alpha: 0.35),
              blurRadius: 18,
              offset: const Offset(0, 4),
            ),
          ],
        ),
        child: FittedBox(
          fit: BoxFit.scaleDown,
          child: Row(
            mainAxisSize: MainAxisSize.min,
            children: const [
              Icon(Icons.sports_kabaddi, color: Colors.white, size: 18),
              SizedBox(width: 8),
              Text(
                '¡COMIENZA LA BATALLA!',
                style: TextStyle(
                  fontFamily: 'serif',
                  fontWeight: FontWeight.w900,
                  letterSpacing: 1.5,
                  color: Colors.white,
                  fontSize: 12,
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
