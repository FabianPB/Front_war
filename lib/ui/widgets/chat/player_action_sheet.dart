import 'package:flutter/material.dart';
import '../../../services/social_api_service.dart';
import '../../screens/private_chat_screen.dart';

const _accent = Color(0xFFE6451C);
const _surface = Color(0xFF1A1A1E);
const _text = Color(0xFFE8E8E8);
const _muted = Color(0xFF888888);
const _border = Color(0xFF2A2A2E);

/// Muestra la hoja de acciones al tocar el avatar de otro jugador.
Future<void> showPlayerActionSheet({
  required BuildContext context,
  required String playerName,
  required String playerAvatar,
  required String targetCharacterId,
  required String myCharacterId,
  required String myDisplayName,
  required SocialApiService socialService,
}) {
  // Capturamos el navigator ANTES de abrir el sheet para poder navegar
  // después de cerrarlo desde dentro del contexto modal.
  final nav = Navigator.of(context);

  return showModalBottomSheet(
    context: context,
    backgroundColor: _surface,
    shape: const RoundedRectangleBorder(
      borderRadius: BorderRadius.vertical(top: Radius.circular(18)),
    ),
    builder: (_) => _PlayerActionSheet(
      playerName: playerName,
      playerAvatar: playerAvatar,
      targetCharacterId: targetCharacterId,
      myCharacterId: myCharacterId,
      myDisplayName: myDisplayName,
      socialService: socialService,
      outerNavigator: nav,
    ),
  );
}

class _PlayerActionSheet extends StatefulWidget {
  const _PlayerActionSheet({
    required this.playerName,
    required this.playerAvatar,
    required this.targetCharacterId,
    required this.myCharacterId,
    required this.myDisplayName,
    required this.socialService,
    required this.outerNavigator,
  });

  final String playerName;
  final String playerAvatar;
  final String targetCharacterId;
  final String myCharacterId;
  final String myDisplayName;
  final SocialApiService socialService;
  final NavigatorState outerNavigator;

  @override
  State<_PlayerActionSheet> createState() => _PlayerActionSheetState();
}

class _PlayerActionSheetState extends State<_PlayerActionSheet> {
  bool _loading = false;

  Future<void> _execute(
    Future<SocialResult> Function() action,
  ) async {
    setState(() => _loading = true);
    final result = await action();
    if (!mounted) return;
    // Capturar el messenger antes de cerrar el sheet para evitar usar un contexto
    // que ya no está en el árbol.
    final messenger = ScaffoldMessenger.of(widget.outerNavigator.context);
    Navigator.of(context).pop();
    messenger.showSnackBar(
      SnackBar(
        content: Text(
          result.message.isNotEmpty
              ? result.message
              : (result.success ? '¡Listo!' : 'Error'),
          style: const TextStyle(color: _text),
        ),
        backgroundColor:
            result.success ? const Color(0xFF1E3A1E) : const Color(0xFF3A1E1E),
        behavior: SnackBarBehavior.floating,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
        margin: const EdgeInsets.all(12),
      ),
    );
  }

  void _openPrivateChat() {
    Navigator.of(context).pop(); // cierra el sheet
    widget.outerNavigator.push(
      MaterialPageRoute(
        builder: (_) => PrivateChatScreen(
          partnerCharacterId: widget.targetCharacterId,
          partnerName: widget.playerName,
          partnerAvatar: widget.playerAvatar,
        ),
      ),
    );
  }

  void _blockWithConfirmation() {
    showDialog(
      context: context,
      builder: (ctx) => AlertDialog(
        backgroundColor: _surface,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(12),
          side: const BorderSide(color: _border),
        ),
        title: Text(
          '¿Bloquear a ${widget.playerName}?',
          style: const TextStyle(color: _text, fontSize: 15),
        ),
        content: const Text(
          'Esta acción eliminará la amistad y cancelará solicitudes pendientes.',
          style: TextStyle(color: _muted, fontSize: 13),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx),
            child: const Text('Cancelar', style: TextStyle(color: _muted)),
          ),
          TextButton(
            onPressed: () {
              Navigator.pop(ctx);
              _execute(
                () => widget.socialService.blockPlayer(widget.targetCharacterId),
              );
            },
            child: const Text(
              'Bloquear',
              style: TextStyle(color: _accent, fontWeight: FontWeight.w700),
            ),
          ),
        ],
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return SafeArea(
      child: Padding(
        padding: const EdgeInsets.fromLTRB(24, 12, 24, 24),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            // Handle
            Container(
              width: 40,
              height: 4,
              decoration: BoxDecoration(
                color: const Color(0xFF444448),
                borderRadius: BorderRadius.circular(2),
              ),
            ),
            const SizedBox(height: 20),

            // Cabecera del jugador
            Row(
              children: [
                Container(
                  width: 52,
                  height: 52,
                  decoration: BoxDecoration(
                    color: _accent.withValues(alpha: 0.14),
                    border:
                        Border.all(color: _accent.withValues(alpha: 0.45)),
                    shape: BoxShape.circle,
                  ),
                  alignment: Alignment.center,
                  child: Text(widget.playerAvatar,
                      style: const TextStyle(fontSize: 24)),
                ),
                const SizedBox(width: 14),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        widget.playerName.toUpperCase(),
                        style: const TextStyle(
                          color: _text,
                          fontSize: 15,
                          fontWeight: FontWeight.w700,
                          letterSpacing: 1.1,
                        ),
                      ),
                      const SizedBox(height: 2),
                      const Text(
                        'Guerrero del mundo WAR',
                        style: TextStyle(color: _muted, fontSize: 11),
                      ),
                    ],
                  ),
                ),
              ],
            ),

            const SizedBox(height: 20),
            const Divider(color: _border, height: 1),
            const SizedBox(height: 8),

            // Acciones o indicador de carga
            if (_loading)
              const Padding(
                padding: EdgeInsets.symmetric(vertical: 24),
                child: CircularProgressIndicator(
                    color: _accent, strokeWidth: 2.5),
              )
            else ...[
              _ActionRow(
                icon: Icons.person_add_rounded,
                label: 'Solicitud de amistad',
                sublabel: 'Agregar a tu lista de amigos',
                color: const Color(0xFF4CAF50),
                onTap: () => _execute(
                  () => widget.socialService
                      .sendFriendRequest(widget.targetCharacterId),
                ),
              ),
              _ActionRow(
                icon: Icons.chat_bubble_rounded,
                label: 'Chat privado',
                sublabel: 'Enviar un mensaje directo',
                color: const Color(0xFF2196F3),
                onTap: _openPrivateChat,
              ),
              _ActionRow(
                icon: Icons.lock_person_rounded,
                label: 'Bloquear jugador',
                sublabel: 'No podrá interactuar contigo',
                color: _accent,
                onTap: _blockWithConfirmation,
              ),
            ],
          ],
        ),
      ),
    );
  }
}

class _ActionRow extends StatelessWidget {
  const _ActionRow({
    required this.icon,
    required this.label,
    required this.sublabel,
    required this.color,
    required this.onTap,
  });

  final IconData icon;
  final String label;
  final String sublabel;
  final Color color;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(10),
      child: Padding(
        padding: const EdgeInsets.symmetric(vertical: 10, horizontal: 4),
        child: Row(
          children: [
            Container(
              width: 42,
              height: 42,
              decoration: BoxDecoration(
                color: color.withValues(alpha: 0.14),
                borderRadius: BorderRadius.circular(10),
              ),
              alignment: Alignment.center,
              child: Icon(icon, color: color, size: 20),
            ),
            const SizedBox(width: 14),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(label,
                      style: const TextStyle(
                          color: _text,
                          fontSize: 14,
                          fontWeight: FontWeight.w600)),
                  const SizedBox(height: 2),
                  Text(sublabel,
                      style: const TextStyle(color: _muted, fontSize: 11)),
                ],
              ),
            ),
            const Icon(Icons.chevron_right_rounded, color: _muted, size: 18),
          ],
        ),
      ),
    );
  }
}
