const fs = require("fs");
const {
  Document, Packer, Paragraph, TextRun, AlignmentType, HeadingLevel,
  LevelFormat, Footer, PageNumber, BorderStyle
} = require("docx");

// ─── Helpers ──────────────────────────────────────────────────────────────────

function H1(text) {
  return new Paragraph({
    heading: HeadingLevel.HEADING_1,
    children: [new TextRun({ text })],
  });
}
function H2(text) {
  return new Paragraph({
    heading: HeadingLevel.HEADING_2,
    children: [new TextRun({ text })],
  });
}
function P(text, opts = {}) {
  return new Paragraph({
    spacing: { after: 140 },
    alignment: opts.align || AlignmentType.JUSTIFIED,
    children: [new TextRun({ text, bold: opts.bold || false, italics: opts.italic || false })],
  });
}
function PX(runs, opts = {}) {
  // runs = [{text, bold?, italic?}, ...]
  return new Paragraph({
    spacing: { after: 140 },
    alignment: opts.align || AlignmentType.JUSTIFIED,
    children: runs.map(r => new TextRun({
      text: r.text,
      bold: r.bold || false,
      italics: r.italic || false
    })),
  });
}
function B(text) {
  return new Paragraph({
    numbering: { reference: "bullets", level: 0 },
    spacing: { after: 80 },
    children: [new TextRun({ text })],
  });
}
function BX(runs) {
  return new Paragraph({
    numbering: { reference: "bullets", level: 0 },
    spacing: { after: 80 },
    children: runs.map(r => new TextRun({
      text: r.text,
      bold: r.bold || false,
      italics: r.italic || false
    })),
  });
}
function Spacer() {
  return new Paragraph({ spacing: { after: 180 }, children: [new TextRun({ text: "" })] });
}

// ─── Contenido ────────────────────────────────────────────────────────────────

const children = [];

// Portada
children.push(new Paragraph({
  alignment: AlignmentType.CENTER,
  spacing: { before: 2400, after: 300 },
  children: [new TextRun({ text: "WAR", bold: true, size: 96 })],
}));
children.push(new Paragraph({
  alignment: AlignmentType.CENTER,
  spacing: { after: 300 },
  children: [new TextRun({ text: "Presentación general del backend", size: 36 })],
}));
children.push(new Paragraph({
  alignment: AlignmentType.CENTER,
  spacing: { after: 1200 },
  children: [new TextRun({
    text: "MMO RPG en tiempo real · C#/.NET 8 · SignalR · EF Core",
    italics: true, size: 24, color: "555555"
  })],
}));
children.push(new Paragraph({
  alignment: AlignmentType.CENTER,
  spacing: { after: 100 },
  children: [new TextRun({ text: "Rama: Rubén", size: 22, color: "777777" })],
}));
children.push(new Paragraph({
  alignment: AlignmentType.CENTER,
  spacing: { after: 100 },
  children: [new TextRun({ text: "Preparado para la reunión de acoplamiento con Unity", size: 22, color: "777777" })],
}));
children.push(new Paragraph({ pageBreakBefore: true, children: [new TextRun({ text: "" })] }));

// 1. Presentación general
children.push(H1("1. El juego, en pocas palabras"));
children.push(P("WAR es un MMO RPG de combate en tiempo real. Los jugadores se conectan al mismo mundo, se mueven en un grid, combaten entre sí o contra mobs, forman grupos para colaborar, suben de nivel, equipan piezas, crafetean mejoras, aprenden habilidades más poderosas y hablan en un chat local por proximidad."));
children.push(P("El backend que acabamos de construir es el cerebro de todo eso. Decide qué acciones son legítimas, resuelve el combate con fórmulas claras, guarda los recursos monetarios del jugador con auditoría estricta, sirve el catálogo de skills y equipo, y reparte la experiencia entre los miembros de un grupo. Todo esto, siguiendo una regla fundamental: el jugador sólo interactúa con el juego; nunca escribe directamente sobre el sistema. Los únicos que pueden modificar el contenido o corregir a un personaje son los administradores, siempre dejando huella."));

// 2. Clases
children.push(H1("2. Las cuatro clases"));
children.push(P("Cada jugador elige una clase al entrar al juego y con ella juega hasta el final. Las cuatro tienen identidades muy distintas entre sí."));
children.push(BX([{ text: "Sorcerer — ", bold: true }, { text: "mago clásico. Reparte daño mágico a distancia con hechizos de fuego, hielo y rayo. Su ultimate es el Tornado Dragón." }]));
children.push(BX([{ text: "Juramentada — ", bold: true }, { text: "clase híbrida. Combina luz y oscuridad: cura al grupo, aplica estados sagrados y también puede infligir daño impío. Su ultimate es el Avatar del Juramento." }]));
children.push(BX([{ text: "Lancero — ", bold: true }, { text: "luchador físico muy rápido. Se especializa en ataques perforantes y estados de electrificación. Su ultimate es el Dragón de Mil Lanzas." }]));
children.push(BX([{ text: "Bruiser — ", bold: true }, { text: "clase de fuerza bruta. Tiene la pegada más alta del juego, pero también es la más lenta. Su ultimate es el Titán de Guerra." }]));
children.push(P("Cada clase tiene trece habilidades únicas, doce estándar y una ultimate. En total, el juego tiene cincuenta y dos habilidades distintas, todas con nombres propios y escalado de daño específico."));

// 3. Mundo online
children.push(H1("3. El mundo online"));
children.push(P("Al entrar, el jugador aparece en un mundo compartido con todos los demás. Se mueve con coordenadas continuas dentro de un espacio de cien por cien, y el servidor le dice en todo momento a quiénes tiene cerca. El rango de descubrimiento es lo suficientemente amplio para ver a otros jugadores y lo suficientemente corto para que interactuar (chatear, invitar a grupo, ver perfil) requiera acercarse."));
children.push(P("Un proceso interno llamado tick recorre el mundo cada segundo: aplica el daño continuo de los estados, descuenta las duraciones de los efectos, regenera maná, limpia a los jugadores inactivos y difunde los cambios a quienes corresponde ver. Todo el mundo es server-authoritative: los clientes proponen acciones, pero el servidor siempre decide qué pasa."));

// 4. Combate
children.push(H1("4. El combate"));
children.push(P("El combate es en tiempo real, pero con reglas estrictas que impiden hacer trampa. Hay dos tipos de acciones: el ataque básico y las habilidades."));
children.push(P("El ataque básico tiene un combo de seis etapas. Cada etapa pega un poco más fuerte que la anterior, siempre que el jugador siga atacando dentro de una ventana de tiempo corta. Si se detiene, el combo se reinicia. Cada clase tiene su propio arma y su propia cadena de golpes."));
children.push(P("Las habilidades son los golpes poderosos: hechizos, técnicas, cargas, curaciones, buffs, estados, áreas de efecto. Cada una tiene su propio coste de maná, su propio tiempo de cast, su propio cooldown y su propio rango. Antes de ejecutarse, el servidor pasa la acción por un pipeline de validación que comprueba, en orden, si el jugador está bajo lockout por spam, si está bajo efecto de control, si está silenciado (para skills), si ya usó una acción demasiado reciente, si está en el medio de un cast, si tiene maná suficiente y si la skill no está en cooldown. Si alguna fase falla, la acción se rechaza con una razón legible."));
children.push(P("Cuando la acción sí pasa, el motor de combate resuelve el resultado en fases ordenadas: chequea si acierta, calcula el daño base con la estadística que corresponda (ataque físico, ataque mágico o una combinación), aplica la crítica como bono aditivo, mitiga según la defensa del objetivo con una curva asintótica que nunca reduce más del noventa por ciento, suma los modificadores de daño recibido y entregado, detecta sinergias cuando el objetivo ya tiene estados activos, aplica las condiciones nuevas y ajusta los recursos. El resultado se emite al jugador atacante, al objetivo y a los que estén mirando."));
children.push(P("Los estados tienen un detalle importante: si ya hay dos estados distintos sobre un objetivo y llega un tercero que sinergiza con uno de ellos, se dispara una explosión de daño con bono multiplicativo y todos los estados se borran. Es una ventana de oportunidad para los jugadores que planean sus combos."));

// 5. Habilidades y ascensión
children.push(H1("5. Habilidades y ascensión"));
children.push(P("Cada habilidad del juego puede subir de nivel, del cero al diez. Subir una habilidad no es algo automático: cuesta recursos concretos que el jugador tiene que conseguir jugando."));
children.push(P("Los primeros cinco niveles se pagan con libros de conocimiento comunes, que sirven para cualquier habilidad de cualquier clase, además de una cantidad de cobre y de energía que va creciendo. Del sexto al séptimo nivel, el libro común ya no alcanza: hay que usar libros específicos de calidad especial, hechos exclusivamente para esa habilidad. Del octavo al noveno, los libros pasan a ser épicos. Y el décimo, el último y más poderoso, exige libros legendarios, oro y una cantidad enorme de energía."));
children.push(P("Cada habilidad tiene su propio nombre épico para los libros. La habilidad de bola ígnea, por ejemplo, no tiene simplemente un “libro de bola de fuego”: tiene la Llamarada del Fénix Abisal. Y la ultimate del Bruiser tiene la Resurrección del Titán Primordial. En total, el juego cuenta con ciento cincuenta y siete definiciones de libros: uno común universal y cincuenta y dos específicos, cada uno en tres raridades."));
children.push(P("La ultimate de cada clase es más exigente que las habilidades normales: pide más libros y más recursos en todos los niveles. Llevarla al diez es un logro de largo plazo."));

// 6. Progresión del personaje
children.push(H1("6. Progresión del personaje"));
children.push(P("Los personajes van del nivel uno al ochenta. La experiencia se obtiene matando mobs, cumpliendo recompensas y, en el futuro, con actividades como meditar, recolectar o excavar. Cuando el personaje está en grupo, la experiencia del mob se reparte entre los miembros presentes. Una estadística llamada ganancia de experiencia, más los buffs temporales (como pastillas de vigor o eventos de servidor), multiplican la experiencia final que cada uno recibe."));
children.push(P("A medida que el personaje sube de nivel, sus estadísticas crecen siguiendo el perfil de su clase. Cada década del camino (niveles diez, veinte, treinta y así) trae un hito de recompensa más grande que el salto promedio."));

// 7. Economía
children.push(H1("7. La economía"));
children.push(P("El jugador maneja cuatro recursos que no viven en su inventario sino en un wallet aparte, con auditoría de cada movimiento."));
children.push(BX([{ text: "Cobre — ", bold: true }, { text: "la moneda más abundante. Se consigue matando mobs y vendiendo a NPCs. Se usa para pagar lo cotidiano: subir las primeras ascensiones, gastos pequeños." }]));
children.push(BX([{ text: "Plata — ", bold: true }, { text: "moneda intermedia. Mucho más difícil de conseguir: drops raros, meditación, recolección, excavación. Se usa para las ascensiones de nivel medio." }]));
children.push(BX([{ text: "Oro — ", bold: true }, { text: "moneda de élite. No se dropea directamente (salvo de jefes muy raros). Se obtiene principalmente craftéandolo desde plata, con un límite diario estricto. Es el recurso final para las ascensiones más altas." }]));
children.push(BX([{ text: "Energía — ", bold: true }, { text: "se consigue meditando en fuentes del mundo (común, especial, épica, legendaria, cada una arroja más). No regenera con el tiempo: siempre se gana por acciones. Es el combustible para todas las operaciones costosas." }]));
children.push(P("Cada transacción del wallet queda auditada con quién, cuándo, cuánto, por qué, cuál fue el saldo antes y después, y si hubo algún capado o reembolso. Nada se puede mover sin dejar huella. El jugador no puede simplemente ponerse oro: si lo intentara, el servidor lo rechaza sin excepción."));
children.push(P("Para convertir cobre en plata se necesitan quinientas unidades de cobre por cada plata. Para convertir plata en oro se necesitan mil unidades. La conversión es unidireccional: no se puede devolver oro a plata, ni plata a cobre. Además está limitada: el jugador solo puede crear una cierta cantidad al día, a la semana y al mes, y esos tres límites se aplican simultáneamente. Si no usó su cupo diario, se pierde al día siguiente: no se acumula."));

// 8. Capilla
children.push(H1("8. La Capilla de Economía"));
children.push(P("La Capilla es el sistema que rige los límites económicos del jugador. Tiene diez niveles. Se sube un nivel por cada ocho niveles de personaje, empezando en el ocho y terminando en el ochenta, que es el nivel máximo previsto en esta primera entrega."));
children.push(P("La Capilla define dos cosas importantes: cuánto puede tener el jugador de cada moneda en su wallet (su tope de posesión) y cuánto puede convertir en cada ventana (el cupo diario, semanal y mensual). Todo arranca pequeño y crece mucho: en el nivel uno permite mil de oro de tope y unas decenas de oro convertidos al día; en el nivel diez, medio millón de oro de tope y cinco mil de oro convertidos al día. Entre niveles, los saltos son geométricos."));
children.push(P("La Capilla es, por diseño, el contador de progresión económica del jugador. Incluso si consigue más dinero del que su Capilla permite, el exceso queda capado hasta que la suba."));

// 9. Inventario y equipamiento
children.push(H1("9. Inventario y equipamiento"));
children.push(P("El inventario del jugador es la única fuente de verdad de sus objetos. Arranca con ochenta casillas y puede expandirse cuatro veces, añadiendo cincuenta espacios cada vez, hasta un tope de doscientas ochenta. Cada expansión cuesta recursos y queda auditada."));
children.push(P("Hay cuatro tipos de objetos: equipamiento, gemas, recursos y objetos especiales. El equipamiento y las gemas ocupan una casilla cada uno, sin apilar. Los recursos y los especiales (libros, materiales, consumibles) se apilan indefinidamente en la misma casilla, siempre que sean exactamente del mismo tipo y la misma calidad."));
children.push(P("El equipamiento tiene nueve slots: arma, casco, pechera, botas, brazaletes, guantes, aretes, anillo y collar. Hay cuatro raridades (común, especial, épica, legendaria), cuatro tiers (del uno al cuatro) y treinta niveles de desarrollo por pieza. Una pieza en tier cuatro, legendaria, desarrollo treinta es el tope absoluto del juego."));
children.push(P("Cuando el jugador equipa una pieza, no se saca del inventario: simplemente se marca como equipada. Así nunca hay ni duplicados, ni riesgo de perder una pieza, y los stats siempre se calculan a partir del mismo lugar."));

// 10. Crafteo
children.push(H1("10. Crafteo de equipo"));
children.push(P("Hay dos operaciones de crafteo sobre las piezas."));
children.push(P("La primera es el desarrollo: llevar una pieza del desarrollo uno al dos, del dos al tres, y así hasta treinta. Cada paso sale más caro que el anterior, porque el crecimiento del coste es geométrico. Al final del camino, pasar del veintinueve al treinta vale cincuenta veces lo que costó pasar del uno al dos."));
children.push(P("La segunda es el tier-up: combinar dos piezas del mismo tier, misma rareza y mismo slot para producir una pieza del tier siguiente, con desarrollo reducido a uno. Es el único modo de llegar a tiers altos, y cada escalón es diez veces más caro que el anterior en oro. Un tier cuatro legendario desarrollo treinta acumulado es un proyecto de meses, estilo Black Desert."));
children.push(P("Todo el crafteo es atómico: o el jugador paga el coste y recibe la pieza mejorada, o no se hace nada. No hay medio crafteo. Cada operación deja una entrada en el audit log con las piezas de entrada, la de salida, el coste y el timestamp."));

// 11. Materiales y actividades
children.push(H1("11. Materiales y actividades del mundo"));
children.push(P("Aparte de las monedas, el jugador recoge materiales: plantas, minerales y, en el futuro, otros recursos. Estos se guardan siempre en el inventario (nunca en el wallet), también con cuatro calidades, y sirven como ingredientes del crafteo de armas y de las mejoras de largo plazo."));
children.push(P("Las actividades que dan materiales son tres: meditar en fuentes de energía (da energía y, con suerte, plata), recolectar plantas (da materias vegetales y plata) y excavar minerales (da materias minerales y plata). La plata directa es rara; el grueso se consigue craftéandola desde cobre."));
children.push(P("Hay también un sistema futuro de entrenamiento de cuerpo, alma y espíritu. Será una fuente directa de estadísticas, que consume energía y materiales específicos para cada rama. El backend ya tiene reservados los movimientos de auditoría para esos consumos; la mecánica misma se define en la siguiente iteración."));

// 12. Social
children.push(H1("12. Amistades, bloqueos y chat"));
children.push(P("El sistema social permite que los jugadores se añadan como amigos, se rechacen, se bloqueen o se desbloqueen. Las solicitudes se envían por proximidad: hay que estar cerca para enviar una. Los bloqueos son absolutos: un bloqueado no puede enviar solicitudes, y sus mensajes no llegan al que lo bloqueó."));
children.push(P("El chat es local: cada mensaje sólo llega a los jugadores dentro del rango de descubrimiento del emisor. No hay chat global. También se puede consultar el perfil público de un jugador cercano para ver sus datos básicos (nombre, clase, nivel, apariencia cuando Unity lo tenga)."));
children.push(P("Todo el chat tiene un limitador de velocidad para evitar spam: diez mensajes cada cinco segundos, con penalización de treinta segundos si se excede. El contenido está sanitizado: límite de quinientos caracteres, sin HTML, sin caracteres Unicode invisibles."));

// 13. Grupos
children.push(H1("13. Grupos (party play)"));
children.push(P("Los jugadores pueden formar grupos para colaborar. El líder del grupo puede invitar a cualquiera dentro del rango, y el invitado acepta o rechaza. Un grupo tiene líder y miembros; el líder puede expulsar a alguien o salirse (en cuyo caso el liderazgo pasa al siguiente miembro). Cuando todos salen, el grupo desaparece."));
children.push(P("Estar en grupo desbloquea tres cosas importantes: las curaciones del Juramentada y de otros aliados pueden llegar a los miembros aunque estén ligeramente fuera del rango clásico; los buffs de apoyo se reparten entre todos; y la experiencia de los mobs se divide entre los miembros presentes al matarlos, con la ventaja de que el stat individual de ganancia de experiencia sigue aplicándose después del reparto."));

// 14. Admin
children.push(H1("14. El administrador"));
children.push(P("El administrador es un actor separado del jugador. No juega: gestiona. Se conecta por REST, con autenticación de rol admin, y tiene control de todo el contenido del juego."));
children.push(P("En la entrega final, el administrador podrá: gestionar los catálogos de habilidades, equipamiento, libros y materiales; crear nuevas skills, editarlas en borrador, publicarlas o archivarlas; ver el wallet de cualquier jugador y ajustar saldos con fuente AdminGrant o AdminDeduct, siempre dejando huella; consultar y forzar el nivel de Capilla de cualquier personaje; modificar las reglas globales de conversión de monedas; ver personajes persistidos, buscar por filtros, otorgar experiencia o corregir niveles; auditar el sistema social y el chat con fines de moderación; aplicar sanciones y revertirlas; y observar en tiempo real el mundo online, con los grupos activos y los combates en curso."));
children.push(P("En esta primera iteración, el único controller admin implementado al cien por ciento es el de habilidades. Los demás dominios ya tienen toda la capa de servicios detrás; sólo falta añadir la superficie HTTP siguiendo el mismo patrón. La arquitectura ya está lista para recibirlos."));

// 15. Seguridad y auditoría
children.push(H1("15. Seguridad, auditoría y anti-trampa"));
children.push(P("El principio de diseño del backend es estricto: cada acción mutadora pasa por un servicio que valida, cobra, ejecuta y registra. No hay atajo posible. El jugador no puede setear su HP, ni su maná, ni su saldo de oro, ni su nivel de skill, ni ningún otro valor del sistema: todas esas operaciones están fuera del hub. Sólo puede interactuar con los métodos naturales del juego (atacar, mover, equipar, craftear, ascender con coste, convertir con cupo, etc.)."));
children.push(P("Cada transacción monetaria queda guardada en el audit log con el jugador, la moneda, la dirección (ingreso o egreso), el monto, la fuente, la descripción, el saldo antes y después, y un ID opcional de entidad relacionada. El log es append-only: nunca se edita ni se borra; las correcciones se aplican como transacciones nuevas. Lo mismo ocurre con el crafteo: cada desarrollo y cada tier-up tiene su entrada auditada."));
children.push(P("El combate también es server-authoritative. Las acciones del cliente se validan en siete fases antes de llegar al motor, y los cálculos de daño usan constantes centralizadas que ningún módulo puede sobrescribir en caliente. Los valores absolutos de caps monetarios (diez mil millones de cobre, cinco millones de oro) son un último muro de seguridad por si algún bug intentara saltarse a la Capilla."));

// 16. Flujo típico del jugador
children.push(H1("16. Un día en la vida del jugador"));
children.push(P("Un jugador se conecta. Elige clase, entra al mundo y aparece en un punto aleatorio. Revisa su estado: nivel, stats, habilidades, inventario, wallet y Capilla. Se mueve hacia una zona de mobs; mientras viaja pasa cerca de otro jugador, le mira el perfil, le envía solicitud de amistad y si este acepta, ahora aparecen en sus listas. Siguen camino juntos."));
children.push(P("Llegan al área de combate. El primero invita al segundo al grupo. Comienzan a pelear: ataques básicos en combo, skills estratégicas, uno aplica un estado frío y el otro uno caliente para disparar una sinergia. Los mobs mueren, el sistema reparte la experiencia entre ambos, los dos reciben drops de cobre, y con suerte una plata o un libro común. Sube un nivel, suben stats, mejora su HP máximo."));
children.push(P("Vuelven al refugio. El primer jugador abre su inventario y ve que tiene suficientes libros comunes para subir una habilidad. Abre el preview de ascensión: le dice que cuesta siete libros, mil quinientos cobre y dieciocho mil de energía. Confirma. El sistema cobra, consume los libros, y la habilidad sube al siguiente nivel. El audit log queda con la huella."));
children.push(P("Con el tiempo, junta más plata y oro. Al llegar a nivel ocho, desbloquea su Capilla de Economía. La sube y ahora su wallet puede contener más oro y puede convertir más plata al día. Más adelante, cuando junta dos piezas del mismo tipo y rareza, hace un tier-up y obtiene una pieza de tier superior. La equipa y sus stats mejoran."));
children.push(P("Al cabo de muchas horas, llega al nivel ochenta, ultimate al diez, Capilla al máximo, equipo en tier cuatro legendario desarrollo treinta. Es el tope del juego. Y a partir de ahí sigue jugando con otros, apoyando gremios, compitiendo en territorios, moderado por el backend que siempre garantiza que cada cambio deja huella."));

// 17. Cierre
children.push(H1("17. En resumen"));
children.push(P("WAR es, para el jugador, un mundo donde combatir, progresar y socializar. Para Unity, es un servicio con un contrato claro: un hub de tiempo real y una API REST admin, que devuelven estados y eventos listos para renderizar. Para nosotros, el equipo del backend, es una máquina server-authoritative con auditoría total, caps dinámicos, pipelines explícitos y separación estricta entre lo que el jugador hace y lo que el administrador puede hacer."));
children.push(P("La arquitectura está preparada para crecer: cada subsistema tiene sus puntos de extensión bien definidos, y añadir nuevas mecánicas (mobs con más variedad, dungeons, PvP territorial, entrenamiento de cuerpo/alma/espíritu) es una extensión natural, no una reescritura."));
children.push(P("La reunión de acoplamiento se celebra sabiendo que el backend ya está listo, las dependencias cruzadas están validadas, los diagramas UML detallan cada clase y cada caso de uso, y las pruebas de integración pasan al cien por ciento."));

// ─── Document ────────────────────────────────────────────────────────────────

const doc = new Document({
  creator: "WAR Backend Team",
  title: "WAR - Presentación general del backend",
  description: "Documento narrativo del sistema completo para la reunión de acoplamiento con Unity.",
  styles: {
    default: { document: { run: { font: "Arial", size: 22 } } },
    paragraphStyles: [
      {
        id: "Heading1", name: "Heading 1", basedOn: "Normal", next: "Normal", quickFormat: true,
        run: { size: 36, bold: true, font: "Arial", color: "1F3A5F" },
        paragraph: { spacing: { before: 360, after: 220 }, outlineLevel: 0 },
      },
      {
        id: "Heading2", name: "Heading 2", basedOn: "Normal", next: "Normal", quickFormat: true,
        run: { size: 28, bold: true, font: "Arial", color: "3E5A7F" },
        paragraph: { spacing: { before: 240, after: 160 }, outlineLevel: 1 },
      },
    ],
  },
  numbering: {
    config: [{
      reference: "bullets",
      levels: [{
        level: 0,
        format: LevelFormat.BULLET,
        text: "•",
        alignment: AlignmentType.LEFT,
        style: { paragraph: { indent: { left: 720, hanging: 360 } } },
      }],
    }],
  },
  sections: [{
    properties: {
      page: {
        size: { width: 11906, height: 16838 }, // A4
        margin: { top: 1440, right: 1440, bottom: 1440, left: 1440 },
      },
    },
    footers: {
      default: new Footer({
        children: [new Paragraph({
          alignment: AlignmentType.CENTER,
          children: [
            new TextRun({ text: "WAR · Presentación general del backend · página ", size: 18, color: "777777" }),
            new TextRun({ children: [PageNumber.CURRENT], size: 18, color: "777777" }),
            new TextRun({ text: " de ", size: 18, color: "777777" }),
            new TextRun({ children: [PageNumber.TOTAL_PAGES], size: 18, color: "777777" }),
          ],
        })],
      }),
    },
    children,
  }],
});

Packer.toBuffer(doc).then(buffer => {
  const output = "PRESENTACION_WAR.docx";
  fs.writeFileSync(output, buffer);
  console.log(`✓ Generado: ${output} (${buffer.length} bytes)`);
});
