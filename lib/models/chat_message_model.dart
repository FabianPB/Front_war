class ChatMessageModel {
  const ChatMessageModel({
    required this.mine,
    required this.user,
    required this.avatar,
    required this.message,
    required this.time,
  });

  final bool mine;
  final String user;
  final String avatar;
  final String message;
  final String time;

  static const demoMessages = [
    ChatMessageModel(
      mine: false,
      user: 'Dragón_Rojo',
      avatar: '🗡️',
      message: '¿Alguien para el torneo de esta noche? Necesito aliados fuertes 💀',
      time: 'hace 14 min',
    ),
    ChatMessageModel(
      mine: false,
      user: 'SombraDeFuego',
      avatar: '🪓',
      message: 'Yo voy, pero alguien necesita traer escudo rúnico. Sin eso no sobrevivimos.',
      time: 'hace 12 min',
    ),
    ChatMessageModel(
      mine: false,
      user: 'ElVerdugo',
      avatar: '⚔️',
      message: 'Acabo de conseguir el Hacha de Guerra. Estoy listo para la batalla ⚔️🔥',
      time: 'hace 9 min',
    ),
    ChatMessageModel(
      mine: false,
      user: 'LaSicaria',
      avatar: '💀',
      message: 'El clan NocheNegra está reclutando para sabotear el torneo. Ojo con traidores 👁️',
      time: 'hace 5 min',
    ),
    ChatMessageModel(
      mine: false,
      user: 'GuerreroSinNombre',
      avatar: '🛡️',
      message: '¿Cuánto tiempo falta para el próximo torneo? Necesito prepararme bien.',
      time: 'hace 2 min',
    ),
    ChatMessageModel(
      mine: false,
      user: 'Dragón_Rojo',
      avatar: '🗡️',
      message: 'Faltan 2 horas. Sala privada: #Sangre-y-Honor. ¡Nos vemos en la arena! ⚔️',
      time: 'hace 1 min',
    ),
  ];
}
