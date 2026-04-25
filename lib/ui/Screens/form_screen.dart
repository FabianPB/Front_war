import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import '../widgets/app_scaffold.dart'; // WarScaffold
import '../widgets/support/support_widgets.dart';

class FormScreen extends StatefulWidget {
  const FormScreen({super.key});
  static const routeName = '/support';

  @override
  State<FormScreen> createState() => _FormScreenState();
}

class _FormScreenState extends State<FormScreen> {
  bool _submitted = false;

  @override
  void initState() {
    super.initState();
    // Allow automatic orientation rotation on support screen
    SystemChrome.setPreferredOrientations(DeviceOrientation.values);
  }

  @override
  void dispose() {
    // Restore automatic orientation when leaving
    SystemChrome.setPreferredOrientations(DeviceOrientation.values);
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return WarScaffold(
      title: 'Centro de Soporte',
      lockLandscape: false,
      body: _submitted ? const SupportSuccessView() : _buildForm(),
    );
  }

  Widget _buildForm() {
    return SingleChildScrollView(
      padding: const EdgeInsets.all(24),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          const Text(
            '¿Encontraste un bug? ¿Un jugador haciendo trampa? ¿Problema con tu cuenta o pagos? Envía tu reporte y nuestros guardias lo atenderán en menos de 24 horas. ⚔️',
            style: TextStyle(
              fontSize: 12,
              height: 1.65,
              color: Color.fromRGBO(200, 170, 170, 0.55),
            ),
          ),
          const SizedBox(height: 22),
          Row(
            children: [
              const Expanded(child: SupportField(label: 'Usuario en batalla', hint: 'Tu alias')),
              const SizedBox(width: 12),
              const Expanded(child: SupportField(label: 'Correo electrónico', hint: 'correo@ejemplo.com')),
            ],
          ),
          const SizedBox(height: 15),
          SupportDropdown(
            label: 'Tipo de problema',
            options: const ['Error técnico / Bug en el juego', 'Jugador haciendo trampa', 'Problema con pago o tienda', 'Cuenta suspendida o bloqueada', 'Pérdida de objetos o progreso', 'Abuso o comportamiento tóxico', 'Otro'],
          ),
          const SizedBox(height: 15),
          SupportDropdown(
            label: 'Prioridad',
            options: const ['Baja — Puede esperar', 'Media — Afecta mi experiencia', 'Alta — Urgente, no puedo jugar'],
            initialValue: 'Media — Afecta mi experiencia',
          ),
          const SizedBox(height: 15),
          const SupportField(
            label: 'Descripción del problema',
            hint: 'Describe con detalle lo que ocurrió, cuándo y cómo reproducirlo...',
            maxLines: 4,
          ),
          const SizedBox(height: 20),
          SupportPrimaryButton(
            label: '🛡️   ENVIAR REPORTE',
            onPressed: () => setState(() => _submitted = true),
          ),
        ],
      ),
    );
  }
}
