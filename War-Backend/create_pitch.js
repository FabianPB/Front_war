const pptxgen = require("pptxgenjs");
const React = require("react");
const ReactDOMServer = require("react-dom/server");
const sharp = require("sharp");

// === ICON RENDERING ===
function renderIconSvg(IconComponent, color = "#FFFFFF", size = 256) {
  return ReactDOMServer.renderToStaticMarkup(
    React.createElement(IconComponent, { color, size: String(size) })
  );
}

async function iconToBase64Png(IconComponent, color, size = 256) {
  const svg = renderIconSvg(IconComponent, color, size);
  const pngBuffer = await sharp(Buffer.from(svg)).png().toBuffer();
  return "image/png;base64," + pngBuffer.toString("base64");
}

// === COLOR PALETTE ===
const C = {
  bg1: "0D1117",
  bg2: "161B22",
  bg3: "1C2333",
  gold: "C9A84C",
  red: "E63946",
  cyan: "4CC9F0",
  green: "2DD4BF",
  white: "FFFFFF",
  gray: "8B949E",
  grayDark: "30363D",
  goldDark: "3D2E0A",
  redDark: "3D0A0F",
  cyanDark: "0A2D3D",
  greenDark: "0A3D2E",
};

// === FACTORY FUNCTIONS (never reuse objects) ===
const makeShadow = (opts = {}) => ({
  type: "outer", color: "000000", blur: 8, offset: 3, angle: 135, opacity: 0.35,
  ...opts
});

const makeCardShadow = () => ({
  type: "outer", color: "000000", blur: 6, offset: 2, angle: 135, opacity: 0.25
});

const makeBorder = (color, width = 1) => ({ color, width, type: "solid" });

// === MAIN ===
async function createPresentation() {
  // Load icons
  const {
    FaExclamationTriangle, FaDollarSign, FaSkull,
    FaShieldAlt, FaBrain, FaGlobeAmericas,
    FaFire, FaBolt, FaSnowflake, FaStar,
    FaUsers, FaUser, FaCrown, FaSwords,
    FaChartLine, FaCode, FaDatabase, FaServer,
    FaRocket, FaHandshake, FaEnvelope, FaGlobe,
    FaCheck, FaTimes, FaArrowRight, FaHeart,
    FaCog, FaLayerGroup, FaGem, FaTrophy,
    FaGamepad, FaLock, FaPaintBrush
  } = require("react-icons/fa");
  const {
    GiSwordman, GiMagicSwirl, GiSpearHook, GiBoxingGlove,
    GiCrystalBall, GiMeditation, GiChestArmor, GiHelmet,
    GiNecklace, GiBroadsword
  } = require("react-icons/gi");
  const {
    MdSecurity, MdAutoGraph, MdGroups, MdTimeline
  } = require("react-icons/md");

  // Pre-render icons
  const icons = {};
  const iconList = [
    ["warning", FaExclamationTriangle, C.red],
    ["dollar", FaDollarSign, C.red],
    ["skull", FaSkull, C.red],
    ["shield", FaShieldAlt, C.gold],
    ["brain", FaBrain, C.cyan],
    ["globe", FaGlobeAmericas, C.green],
    ["fire", FaFire, C.red],
    ["bolt", FaBolt, C.cyan],
    ["snow", FaSnowflake, C.cyan],
    ["star", FaStar, C.gold],
    ["users", FaUsers, C.white],
    ["user", FaUser, C.white],
    ["crown", FaCrown, C.gold],
    ["chart", FaChartLine, C.green],
    ["code", FaCode, C.cyan],
    ["database", FaDatabase, C.cyan],
    ["server", FaServer, C.green],
    ["rocket", FaRocket, C.gold],
    ["handshake", FaHandshake, C.gold],
    ["envelope", FaEnvelope, C.white],
    ["globeWeb", FaGlobe, C.cyan],
    ["check", FaCheck, C.green],
    ["times", FaTimes, C.red],
    ["arrow", FaArrowRight, C.gold],
    ["heart", FaHeart, C.red],
    ["cog", FaCog, C.gray],
    ["layers", FaLayerGroup, C.gold],
    ["gem", FaGem, C.cyan],
    ["trophy", FaTrophy, C.gold],
    ["gamepad", FaGamepad, C.cyan],
    ["lock", FaLock, C.green],
    ["paint", FaPaintBrush, C.gold],
    ["shieldGold", FaShieldAlt, C.gold],
    ["checkWhite", FaCheck, C.white],
    ["timesWhite", FaTimes, C.white],
  ];

  for (const [name, Component, color] of iconList) {
    try {
      icons[name] = await iconToBase64Png(Component, "#" + color, 256);
    } catch (e) {
      // Some icons may not exist in the package, skip silently
    }
  }

  const pres = new pptxgen();
  pres.layout = "LAYOUT_16x9";
  pres.author = "WAR Game Studio";
  pres.title = "WAR - MMORPG Investor Pitch";

  // Slide dimensions: 10" x 5.625"
  const SW = 10;
  const SH = 5.625;

  // Helper: add image placeholder
  function addPlaceholder(slide, x, y, w, h, label) {
    slide.addShape(pres.shapes.RECTANGLE, {
      x, y, w, h,
      fill: { color: C.bg3 },
      line: { color: C.gold, width: 1 },
      shadow: makeCardShadow(),
    });
    slide.addText(label, {
      x, y, w, h,
      fontSize: 11, color: C.gray, fontFace: "Calibri",
      align: "center", valign: "middle", margin: 0.15,
    });
  }

  // Helper: add icon in colored circle
  function addIconCircle(slide, iconData, x, y, size, bgColor) {
    if (!bgColor) bgColor = C.bg3;
    slide.addShape(pres.shapes.OVAL, {
      x, y, w: size, h: size,
      fill: { color: bgColor },
      line: { color: C.gold, width: 0.5 },
      shadow: makeCardShadow(),
    });
    if (iconData) {
      const pad = size * 0.22;
      slide.addImage({
        data: iconData,
        x: x + pad, y: y + pad,
        w: size - pad * 2, h: size - pad * 2,
      });
    }
  }

  // Helper: add dark card with top border
  function addCard(slide, x, y, w, h, borderColor) {
    slide.addShape(pres.shapes.RECTANGLE, {
      x, y, w, h,
      fill: { color: C.bg2 },
      line: { color: C.grayDark, width: 0.5 },
      shadow: makeCardShadow(),
    });
    // Top border accent
    slide.addShape(pres.shapes.RECTANGLE, {
      x, y, w, h: 0.06,
      fill: { color: borderColor || C.gold },
    });
  }

  // Helper: slide number
  function addSlideNumber(slide, num, total) {
    slide.addText(`${num} / ${total}`, {
      x: SW - 1.2, y: SH - 0.35, w: 1, h: 0.25,
      fontSize: 9, color: C.grayDark, fontFace: "Calibri",
      align: "right", margin: 0,
    });
  }

  // Helper: decorative corner element
  function addCornerDecor(slide, position) {
    if (position === "topRight") {
      slide.addShape(pres.shapes.RECTANGLE, {
        x: SW - 1.5, y: -0.3, w: 2, h: 0.06,
        fill: { color: C.gold }, rotate: -35,
      });
      slide.addShape(pres.shapes.RECTANGLE, {
        x: SW - 1.0, y: -0.1, w: 1.5, h: 0.04,
        fill: { color: C.gold, transparency: 50 }, rotate: -35,
      });
    } else if (position === "bottomLeft") {
      slide.addShape(pres.shapes.RECTANGLE, {
        x: -0.5, y: SH - 0.15, w: 2, h: 0.06,
        fill: { color: C.gold, transparency: 30 }, rotate: -15,
      });
    }
  }

  const TOTAL_SLIDES = 13;

  // ========================================================================
  // SLIDE 1 — PORTADA
  // ========================================================================
  {
    const slide = pres.addSlide();
    slide.background = { color: C.bg1 };

    // Decorative gold lines
    slide.addShape(pres.shapes.RECTANGLE, {
      x: 0, y: 0, w: 0.06, h: SH,
      fill: { color: C.gold, transparency: 40 },
    });
    slide.addShape(pres.shapes.RECTANGLE, {
      x: 0.15, y: 0, w: 0.03, h: SH,
      fill: { color: C.gold, transparency: 20 },
    });
    addCornerDecor(slide, "topRight");

    // Bottom decorative bar
    slide.addShape(pres.shapes.RECTANGLE, {
      x: 0, y: SH - 0.08, w: SW, h: 0.08,
      fill: { color: C.gold, transparency: 60 },
    });

    // WAR title
    slide.addText("WAR", {
      x: 0.5, y: 0.4, w: 3.5, h: 1.2,
      fontSize: 72, fontFace: "Trebuchet MS", bold: true,
      color: C.gold, margin: 0, charSpacing: 12,
    });

    // Subtitle
    slide.addText("MMORPG DE NUEVA GENERACION", {
      x: 0.5, y: 1.5, w: 4, h: 0.5,
      fontSize: 16, fontFace: "Calibri", bold: true,
      color: C.white, margin: 0, charSpacing: 4,
    });

    // Tagline
    slide.addText("La respuesta a la busqueda del MMORPG definitivo", {
      x: 0.5, y: 2.1, w: 4, h: 0.4,
      fontSize: 13, fontFace: "Calibri", italic: true,
      color: C.gray, margin: 0,
    });

    // Divider line
    slide.addShape(pres.shapes.RECTANGLE, {
      x: 0.5, y: 2.6, w: 2, h: 0.03,
      fill: { color: C.gold },
    });

    // Pitch label
    slide.addText("INVESTOR PITCH DECK  |  2026", {
      x: 0.5, y: 2.8, w: 3, h: 0.3,
      fontSize: 10, fontFace: "Calibri",
      color: C.gray, margin: 0, charSpacing: 2,
    });

    // Image placeholder on the right
    addPlaceholder(slide, 4.5, 0.8, 5, 3.2, "INSERTAR IMAGEN: Key Art / Logo del juego");

    // Confidential footer
    slide.addText("CONFIDENCIAL — Solo para uso de inversores autorizados", {
      x: 0.5, y: SH - 0.45, w: 6, h: 0.25,
      fontSize: 8, fontFace: "Calibri", color: C.grayDark, margin: 0,
    });

    slide.addNotes(
      "PORTADA - Puntos clave:\n" +
      "- WAR es un MMORPG backend-first con arquitectura .NET 8, diseñado para escalabilidad.\n" +
      "- El nombre 'WAR' comunica acción, competencia y ambición.\n" +
      "- Transición: 'Antes de hablar de lo que WAR ofrece, hablemos del problema que resuelve...'"
    );
  }

  // ========================================================================
  // SLIDE 2 — EL PROBLEMA
  // ========================================================================
  {
    const slide = pres.addSlide();
    slide.background = { color: C.bg1 };
    addSlideNumber(slide, 2, TOTAL_SLIDES);

    // Title
    slide.addText("EL MERCADO MMORPG ESTA ROTO", {
      x: 0.5, y: 0.3, w: 9, h: 0.6,
      fontSize: 36, fontFace: "Trebuchet MS", bold: true,
      color: C.white, margin: 0,
    });

    // Subtle red accent line under title
    slide.addShape(pres.shapes.RECTANGLE, {
      x: 0.5, y: 0.95, w: 1.5, h: 0.04,
      fill: { color: C.red },
    });

    // Three problem cards
    const cardW = 2.7;
    const cardH = 2.8;
    const cardY = 1.3;
    const gap = 0.35;
    const startX = 0.5;
    const problems = [
      {
        icon: icons.dollar, title: "Pay-to-Win Agresivo",
        desc: "Los juegos exitosos terminan priorizando a quienes mas pagan, alienando a la mayoria de jugadores.",
        borderColor: C.red,
      },
      {
        icon: icons.skull, title: "Progresion Tediosa",
        desc: "Cientos de horas de grind sin recompensa significativa. El jugador se agota antes de disfrutar.",
        borderColor: C.red,
      },
      {
        icon: icons.warning, title: "Sin Vida Propia",
        desc: "MMORPGs genericos sin identidad, que se copian entre si y mueren en meses.",
        borderColor: C.red,
      },
    ];

    problems.forEach((p, i) => {
      const cx = startX + i * (cardW + gap);
      addCard(slide, cx, cardY, cardW, cardH, p.borderColor);

      // Icon circle
      addIconCircle(slide, p.icon, cx + cardW / 2 - 0.3, cardY + 0.25, 0.6, C.redDark);

      // Card title
      slide.addText(p.title, {
        x: cx + 0.15, y: cardY + 1.0, w: cardW - 0.3, h: 0.4,
        fontSize: 15, fontFace: "Trebuchet MS", bold: true,
        color: C.white, align: "center", margin: 0,
      });

      // Card description
      slide.addText(p.desc, {
        x: cx + 0.2, y: cardY + 1.5, w: cardW - 0.4, h: 1.1,
        fontSize: 11, fontFace: "Calibri",
        color: C.gray, align: "center", margin: 0,
      });
    });

    // Big number stat on the right side
    slide.addShape(pres.shapes.RECTANGLE, {
      x: SW - 1.2, y: cardY, w: 0.04, h: cardH,
      fill: { color: C.red, transparency: 40 },
    });

    slide.addText("72%", {
      x: SW - 1.1, y: cardY + 0.3, w: 1.0, h: 0.8,
      fontSize: 42, fontFace: "Trebuchet MS", bold: true,
      color: C.red, align: "center", margin: 0, rotate: 0,
    });

    slide.addText([
      { text: "de jugadores", options: { breakLine: true } },
      { text: "abandonan un", options: { breakLine: true } },
      { text: "MMORPG en los", options: { breakLine: true } },
      { text: "primeros", options: { breakLine: true } },
      { text: "30 dias", options: { bold: true } },
    ], {
      x: SW - 1.15, y: cardY + 1.2, w: 1.05, h: 1.5,
      fontSize: 8, fontFace: "Calibri",
      color: C.gray, align: "center", margin: 0,
    });

    slide.addText("*Dato ilustrativo — actualizar con estudio de mercado", {
      x: 0.5, y: SH - 0.4, w: 5, h: 0.2,
      fontSize: 7, fontFace: "Calibri", italic: true, color: C.grayDark, margin: 0,
    });

    slide.addNotes(
      "EL PROBLEMA - Puntos clave:\n" +
      "- Enfatizar la frustración real de los jugadores con los MMORPGs actuales.\n" +
      "- El 72% es un dato ilustrativo — reemplazar con estadísticas reales.\n" +
      "- Los tres problemas principales: P2W, grind excesivo, falta de identidad.\n" +
      "- Transición: 'WAR nace precisamente para resolver estos tres problemas...'"
    );
  }

  // ========================================================================
  // SLIDE 3 — LA SOLUCIÓN: WAR
  // ========================================================================
  {
    const slide = pres.addSlide();
    slide.background = { color: C.bg1 };
    addSlideNumber(slide, 3, TOTAL_SLIDES);

    slide.addText("WAR: DONDE LA ESTRATEGIA SUPERA A LA BILLETERA", {
      x: 0.5, y: 0.3, w: 9, h: 0.6,
      fontSize: 32, fontFace: "Trebuchet MS", bold: true,
      color: C.white, margin: 0,
    });

    slide.addShape(pres.shapes.RECTANGLE, {
      x: 0.5, y: 0.92, w: 1.5, h: 0.04,
      fill: { color: C.gold },
    });

    // Left column: 3 value propositions
    const props = [
      {
        icon: icons.shield, title: "Progresion Justa",
        desc: "Sistema de Puntos de Poder balanceado por clase. Cada punto importa de forma diferente segun tu clase, eliminando la ventaja por pago.",
        color: C.gold,
      },
      {
        icon: icons.brain, title: "Profundidad Estrategica",
        desc: "Espiritus, cultivacion, ascension de habilidades y builds unicos. 13 habilidades por clase con 10 niveles de ascension cada una.",
        color: C.cyan,
      },
      {
        icon: icons.globe, title: "Comunidad Global",
        desc: "Guerras de clanes a escala mundial. Clanes, alianzas y combate PvP masivo donde la coordinacion supera al poder individual.",
        color: C.green,
      },
    ];

    props.forEach((p, i) => {
      const py = 1.2 + i * 1.3;

      // Icon circle
      addIconCircle(slide, p.icon, 0.5, py + 0.1, 0.55, C.bg2);

      // Title
      slide.addText(p.title, {
        x: 1.2, y: py, w: 3.3, h: 0.35,
        fontSize: 16, fontFace: "Trebuchet MS", bold: true,
        color: p.color, margin: 0,
      });

      // Desc
      slide.addText(p.desc, {
        x: 1.2, y: py + 0.38, w: 3.3, h: 0.7,
        fontSize: 10.5, fontFace: "Calibri",
        color: C.gray, margin: 0,
      });
    });

    // Right column: image placeholder
    addPlaceholder(slide, 5.0, 1.1, 4.5, 3.5, "INSERTAR IMAGEN: Screenshot del juego en accion");

    addCornerDecor(slide, "bottomLeft");

    slide.addNotes(
      "LA SOLUCIÓN - Puntos clave:\n" +
      "- WAR tiene un sistema de Puntos de Poder contextual: 10 ATK en Sorcerer no equivale a 10 ATK en Bruiser.\n" +
      "- 4 clases, cada una con 13 habilidades (12 + 1 ultimate), cada habilidad con 10 niveles de ascensión.\n" +
      "- Diseño anti-P2W por arquitectura, no por promesa de marketing.\n" +
      "- Transición: 'Veamos las clases que hacen único a WAR...'"
    );
  }

  // ========================================================================
  // SLIDE 4 — SISTEMA DE CLASES
  // ========================================================================
  {
    const slide = pres.addSlide();
    slide.background = { color: C.bg1 };
    addSlideNumber(slide, 4, TOTAL_SLIDES);

    slide.addText("CUATRO CAMINOS, INFINITAS POSIBILIDADES", {
      x: 0.5, y: 0.3, w: 9, h: 0.6,
      fontSize: 34, fontFace: "Trebuchet MS", bold: true,
      color: C.white, margin: 0,
    });

    slide.addShape(pres.shapes.RECTANGLE, {
      x: 0.5, y: 0.92, w: 1.5, h: 0.04,
      fill: { color: C.gold },
    });

    const classes = [
      {
        name: "SORCERER", role: "DPS Magico", color: C.cyan,
        desc: "Maestro elemental con dominio sobre fuego, rayo y hielo. 14 ATK magico/nivel. Fragil pero devastador a distancia (14m+).",
        stat1: "ATK Magico: 14/lvl", stat2: "HP: 42/lvl",
      },
      {
        name: "JURAMENTADA", role: "Hibrido / Soporte", color: C.gold,
        desc: "Caballera sagrada con balance entre magia y combate fisico. Maxima efectividad de curacion (0.025/lvl).",
        stat1: "ATK Magico: 10/lvl", stat2: "HP: 52/lvl",
      },
      {
        name: "LANCERO", role: "DPS Fisico", color: C.green,
        desc: "Combatiente de precision con la penetracion de armadura mas alta (0.012/lvl). Maximo ataque fisico y precision.",
        stat1: "ATK Fisico: 13/lvl", stat2: "HP: 56/lvl",
      },
      {
        name: "BRUISER", role: "Tanque", color: C.red,
        desc: "Especialista defensivo con mayor HP (68/lvl), defensa (5/lvl) y resistencia magica (4/lvl). Imbatible en primera linea.",
        stat1: "Defensa: 5/lvl", stat2: "HP: 68/lvl",
      },
    ];

    const classCardW = 2.1;
    const classCardH = 3.3;
    const classStartX = 0.5;
    const classGap = 0.27;
    const classY = 1.2;

    classes.forEach((c, i) => {
      const cx = classStartX + i * (classCardW + classGap);

      // Card background
      addCard(slide, cx, classY, classCardW, classCardH, c.color);

      // Image placeholder
      addPlaceholder(slide, cx + 0.25, classY + 0.2, classCardW - 0.5, 1.1,
        "INSERTAR IMAGEN:\n" + c.name);

      // Class name
      slide.addText(c.name, {
        x: cx + 0.1, y: classY + 1.4, w: classCardW - 0.2, h: 0.3,
        fontSize: 14, fontFace: "Trebuchet MS", bold: true,
        color: c.color, align: "center", margin: 0,
      });

      // Role badge
      slide.addShape(pres.shapes.RECTANGLE, {
        x: cx + classCardW / 2 - 0.55, y: classY + 1.72,
        w: 1.1, h: 0.22,
        fill: { color: c.color, transparency: 80 },
        line: { color: c.color, width: 0.5 },
      });
      slide.addText(c.role, {
        x: cx + classCardW / 2 - 0.55, y: classY + 1.72,
        w: 1.1, h: 0.22,
        fontSize: 8, fontFace: "Calibri", bold: true,
        color: c.color, align: "center", valign: "middle", margin: 0,
      });

      // Description
      slide.addText(c.desc, {
        x: cx + 0.12, y: classY + 2.05, w: classCardW - 0.24, h: 0.8,
        fontSize: 8.5, fontFace: "Calibri",
        color: C.gray, align: "center", margin: 0,
      });

      // Stats
      slide.addText(c.stat1, {
        x: cx + 0.1, y: classY + 2.85, w: classCardW - 0.2, h: 0.18,
        fontSize: 7.5, fontFace: "Calibri", bold: true,
        color: c.color, align: "center", margin: 0,
      });
      slide.addText(c.stat2, {
        x: cx + 0.1, y: classY + 3.03, w: classCardW - 0.2, h: 0.18,
        fontSize: 7.5, fontFace: "Calibri",
        color: C.gray, align: "center", margin: 0,
      });
    });

    slide.addNotes(
      "SISTEMA DE CLASES - Datos del código:\n" +
      "- 4 clases con curvas de crecimiento completamente diferenciadas.\n" +
      "- Sorcerer: 14 ATK mágico/nivel, 42 HP/nivel — Glass cannon a distancia.\n" +
      "- Juramentada: 10 ATK mágico + 8 ATK físico/nivel, 52 HP/nivel — Híbrido con healing.\n" +
      "- Lancero: 13 ATK físico/nivel, 0.012 penetración def/nivel — DPS físico preciso.\n" +
      "- Bruiser: 68 HP/nivel, 5 DEF/nivel, 4 MR/nivel — Tanque puro.\n" +
      "- Cada clase tiene 13 habilidades (12 + ultimate) con 10 niveles de ascensión.\n" +
      "- Transición: 'Este balance no es casual. Veamos cómo el sistema de PP lo garantiza...'"
    );
  }

  // ========================================================================
  // SLIDE 5 — SISTEMA DE COMBATE Y BALANCE
  // ========================================================================
  {
    const slide = pres.addSlide();
    slide.background = { color: C.bg1 };
    addSlideNumber(slide, 5, TOTAL_SLIDES);

    slide.addText("COMBATE BALANCEADO POR DISENO", {
      x: 0.5, y: 0.3, w: 9, h: 0.6,
      fontSize: 34, fontFace: "Trebuchet MS", bold: true,
      color: C.white, margin: 0,
    });

    slide.addShape(pres.shapes.RECTANGLE, {
      x: 0.5, y: 0.92, w: 1.5, h: 0.04,
      fill: { color: C.cyan },
    });

    // Left side: PP formula explanation
    slide.addText("PUNTOS DE PODER (PP)", {
      x: 0.5, y: 1.2, w: 4.5, h: 0.35,
      fontSize: 16, fontFace: "Trebuchet MS", bold: true,
      color: C.gold, margin: 0,
    });

    slide.addText("PP = Stat x UnitValue x ClassWeight x CategoryAdj", {
      x: 0.5, y: 1.6, w: 4.5, h: 0.3,
      fontSize: 11, fontFace: "Consolas",
      color: C.cyan, margin: 0,
    });

    slide.addText(
      "Cada estadistica tiene un valor unitario (ej: ATK = 9.0, HP = 4.0) que se multiplica por un peso de clase. Un Sorcerer valora su ATK Magico hasta 1.60x, mientras que un Bruiser lo valora a 0.55x.",
      {
        x: 0.5, y: 2.0, w: 4.5, h: 0.8,
        fontSize: 10.5, fontFace: "Calibri",
        color: C.gray, margin: 0,
      }
    );

    // Comparison boxes
    const boxY = 2.9;

    // Sorcerer box
    addCard(slide, 0.5, boxY, 2.1, 1.2, C.cyan);
    slide.addText("SORCERER", {
      x: 0.5, y: boxY + 0.15, w: 2.1, h: 0.25,
      fontSize: 11, fontFace: "Trebuchet MS", bold: true,
      color: C.cyan, align: "center", margin: 0,
    });
    slide.addText("+10 ATK Magico", {
      x: 0.5, y: boxY + 0.42, w: 2.1, h: 0.2,
      fontSize: 10, fontFace: "Calibri",
      color: C.white, align: "center", margin: 0,
    });
    slide.addText("= 144 PP", {
      x: 0.5, y: boxY + 0.65, w: 2.1, h: 0.3,
      fontSize: 20, fontFace: "Trebuchet MS", bold: true,
      color: C.cyan, align: "center", margin: 0,
    });
    slide.addText("(9.0 x 1.60)", {
      x: 0.5, y: boxY + 0.93, w: 2.1, h: 0.18,
      fontSize: 8, fontFace: "Calibri",
      color: C.gray, align: "center", margin: 0,
    });

    // "does not equal" sign
    slide.addText("=/=", {
      x: 2.7, y: boxY + 0.35, w: 0.5, h: 0.5,
      fontSize: 22, fontFace: "Trebuchet MS", bold: true,
      color: C.red, align: "center", valign: "middle", margin: 0,
    });

    // Bruiser box
    addCard(slide, 3.3, boxY, 2.1, 1.2, C.red);
    slide.addText("BRUISER", {
      x: 3.3, y: boxY + 0.15, w: 2.1, h: 0.25,
      fontSize: 11, fontFace: "Trebuchet MS", bold: true,
      color: C.red, align: "center", margin: 0,
    });
    slide.addText("+10 ATK Magico", {
      x: 3.3, y: boxY + 0.42, w: 2.1, h: 0.2,
      fontSize: 10, fontFace: "Calibri",
      color: C.white, align: "center", margin: 0,
    });
    slide.addText("= 49.5 PP", {
      x: 3.3, y: boxY + 0.65, w: 2.1, h: 0.3,
      fontSize: 20, fontFace: "Trebuchet MS", bold: true,
      color: C.red, align: "center", margin: 0,
    });
    slide.addText("(9.0 x 0.55)", {
      x: 3.3, y: boxY + 0.93, w: 2.1, h: 0.18,
      fontSize: 8, fontFace: "Calibri",
      color: C.gray, align: "center", margin: 0,
    });

    // Right side: diagram boxes
    const catX = 5.8;
    const catW = 3.7;
    const categories = [
      { name: "Ofensivo", desc: "ATK, Crit, Precision, Penetracion", color: C.red },
      { name: "Defensivo", desc: "HP, DEF, MR, Evasion, Tenacidad", color: C.cyan },
      { name: "Recuperacion", desc: "Regen HP/Mana, Efectividad Cura", color: C.green },
      { name: "Utilidad", desc: "Velocidad, Rango, EXP, Drop Rate", color: C.gold },
      { name: "Estados", desc: "Aplicacion/Evasion de 9 condiciones", color: C.gray },
    ];

    slide.addText("5 CATEGORIAS DE STATS", {
      x: catX, y: 1.2, w: catW, h: 0.3,
      fontSize: 14, fontFace: "Trebuchet MS", bold: true,
      color: C.white, margin: 0,
    });

    categories.forEach((cat, i) => {
      const cy = 1.6 + i * 0.62;
      // Small color bar
      slide.addShape(pres.shapes.RECTANGLE, {
        x: catX, y: cy, w: 0.08, h: 0.5,
        fill: { color: cat.color },
      });
      slide.addText(cat.name, {
        x: catX + 0.2, y: cy, w: catW - 0.3, h: 0.22,
        fontSize: 11, fontFace: "Trebuchet MS", bold: true,
        color: cat.color, margin: 0,
      });
      slide.addText(cat.desc, {
        x: catX + 0.2, y: cy + 0.22, w: catW - 0.3, h: 0.22,
        fontSize: 9, fontFace: "Calibri",
        color: C.gray, margin: 0,
      });
    });

    // Big number
    slide.addText("92", {
      x: catX + catW - 1.0, y: SH - 1.1, w: 1.0, h: 0.6,
      fontSize: 40, fontFace: "Trebuchet MS", bold: true,
      color: C.gold, align: "right", margin: 0,
    });
    slide.addText("stats totales", {
      x: catX + catW - 1.6, y: SH - 0.55, w: 1.6, h: 0.2,
      fontSize: 9, fontFace: "Calibri",
      color: C.gray, align: "right", margin: 0,
    });

    slide.addNotes(
      "COMBATE Y BALANCE - Datos del código:\n" +
      "- 92 stats totales: 5 recursos, 10 ofensivos, 4 defensivos, 4 recuperación, 5 progresión, 9 aplicación estados, 9 evasión estados, 10 incremento daño, 10 reducción daño, 1 utilidad.\n" +
      "- Sistema de PP contextual: cada stat se pondera según la clase.\n" +
      "- Ejemplo real: +10 ATK Mágico = 144 PP para Sorcerer (peso 1.60) vs 49.5 PP para Bruiser (peso 0.55).\n" +
      "- Valores unitarios: MaxHP=4.0, ATK=9.0, CritChance=55.0, Defensa=120.0 (con curva de mitigación).\n" +
      "- Pipeline de combate de 10 fases: Magnitud → Costo → Hit Check → Crit Check → Cálculo → Mitigación → Modificadores → Condiciones → Interacciones → Proyección.\n" +
      "- Transición: 'El balance se mantiene a través de múltiples capas de progresión...'"
    );
  }

  // ========================================================================
  // SLIDE 6 — PROFUNDIDAD DE PROGRESIÓN
  // ========================================================================
  {
    const slide = pres.addSlide();
    slide.background = { color: C.bg1 };
    addSlideNumber(slide, 6, TOTAL_SLIDES);

    slide.addText("MULTIPLES CAPAS DE CRECIMIENTO", {
      x: 0.5, y: 0.3, w: 9, h: 0.6,
      fontSize: 34, fontFace: "Trebuchet MS", bold: true,
      color: C.white, margin: 0,
    });

    slide.addShape(pres.shapes.RECTANGLE, {
      x: 0.5, y: 0.92, w: 1.5, h: 0.04,
      fill: { color: C.gold },
    });

    // Pyramid layers (bottom to top) — left side
    const layers = [
      { name: "PUNTOS DE PODER", desc: "Indicador global contextual", w: 4.2, color: C.gold, textColor: C.bg1 },
      { name: "CULTIVACION", desc: "Mejora espiritual interna", w: 3.6, color: C.cyan, textColor: C.bg1 },
      { name: "ESPIRITUS", desc: "Stats + pasivas unicas", w: 3.0, color: C.green, textColor: C.bg1 },
      { name: "EQUIPAMIENTO", desc: "7 slots de equipo", w: 2.4, color: C.red, textColor: C.white },
      { name: "NIVEL", desc: "80 niveles, formula x1.20", w: 1.8, color: C.bg2, textColor: C.white },
    ];

    const pyramidStartY = 1.3;
    const layerH = 0.7;
    const layerGap = 0.12;
    const pyramidCenterX = 2.8;

    layers.forEach((l, i) => {
      const ly = pyramidStartY + (layers.length - 1 - i) * (layerH + layerGap);
      const lx = pyramidCenterX - l.w / 2;

      slide.addShape(pres.shapes.RECTANGLE, {
        x: lx, y: ly, w: l.w, h: layerH,
        fill: { color: l.color },
        shadow: makeCardShadow(),
      });

      slide.addText([
        { text: l.name, options: { bold: true, fontSize: 11, breakLine: true } },
        { text: l.desc, options: { fontSize: 8, color: l.textColor === C.white ? C.gray : C.grayDark } },
      ], {
        x: lx, y: ly, w: l.w, h: layerH,
        fontFace: "Calibri", color: l.textColor,
        align: "center", valign: "middle", margin: 0.05,
      });
    });

    // Right side: Details
    const detailX = 5.4;
    const detailW = 4.2;
    const details = [
      {
        title: "80 Niveles de Progresion",
        desc: "Base: 1,000 XP. Multiplicador: x1.20 por nivel. Bonus de decada: +50% en niveles 10, 20, 30... 70.",
        color: C.white,
      },
      {
        title: "7 Slots de Equipamiento",
        desc: "Casco, Pechera, Vestido, Brazaletes, Collar, Aretes y Arma. Cada pieza afecta las stats del personaje.",
        color: C.red,
      },
      {
        title: "Sistema de Espiritus",
        desc: "Companeros con estadisticas propias y pasivas unicas que se desbloquean con niveles de cultivacion.",
        color: C.green,
      },
      {
        title: "Cultivacion Espiritual",
        desc: "Mejora interna que potencia al personaje y desbloquea niveles de pasivas de los espiritus.",
        color: C.cyan,
      },
    ];

    details.forEach((d, i) => {
      const dy = 1.2 + i * 1.0;
      slide.addShape(pres.shapes.RECTANGLE, {
        x: detailX, y: dy, w: 0.06, h: 0.7,
        fill: { color: d.color },
      });
      slide.addText(d.title, {
        x: detailX + 0.2, y: dy, w: detailW - 0.3, h: 0.25,
        fontSize: 12, fontFace: "Trebuchet MS", bold: true,
        color: d.color, margin: 0,
      });
      slide.addText(d.desc, {
        x: detailX + 0.2, y: dy + 0.28, w: detailW - 0.3, h: 0.45,
        fontSize: 9.5, fontFace: "Calibri",
        color: C.gray, margin: 0,
      });
    });

    slide.addNotes(
      "PROFUNDIDAD DE PROGRESIÓN - Datos del código:\n" +
      "- Nivel máximo: 80. Base XP: 1,000. Multiplicador compuesto: 1.20x/nivel. Bonus década: 1.50x.\n" +
      "- 7 slots de equipo: Casco, Pechera, Vestido, Brazaletes, Collar, Aretes, Arma.\n" +
      "- Espíritus: entidades con stats propias + pasivas únicas según nivel de cultivación.\n" +
      "- Cultivación: sistema de mejora espiritual interna (planificado).\n" +
      "- PP: indicador global que pondera todo según clase — no existe un 'mejor equipo' universal.\n" +
      "- Transición: 'Hablemos más en detalle del sistema de espíritus...'"
    );
  }

  // ========================================================================
  // SLIDE 7 — SISTEMA DE ESPÍRITUS
  // ========================================================================
  {
    const slide = pres.addSlide();
    slide.background = { color: C.bg1 };
    addSlideNumber(slide, 7, TOTAL_SLIDES);

    slide.addText("ESPIRITUS: TU VENTAJA ESTRATEGICA", {
      x: 0.5, y: 0.3, w: 9, h: 0.6,
      fontSize: 34, fontFace: "Trebuchet MS", bold: true,
      color: C.white, margin: 0,
    });

    slide.addShape(pres.shapes.RECTANGLE, {
      x: 0.5, y: 0.92, w: 1.5, h: 0.04,
      fill: { color: C.green },
    });

    // Explanation text
    slide.addText(
      "Los Espiritus son entidades que acompanan a tu personaje, otorgando estadisticas adicionales y pasivas unicas. Sus habilidades pasivas se desbloquean y potencian segun tu nivel de Cultivacion, creando una capa de progresion profundamente estrategica.",
      {
        x: 0.5, y: 1.15, w: 9, h: 0.6,
        fontSize: 11, fontFace: "Calibri",
        color: C.gray, margin: 0,
      }
    );

    // Three example spirit showcases
    const spirits = [
      {
        name: "Espiritu de Llamas",
        element: "Fuego",
        color: C.red,
        stats: "+ATK Magico, +Heat Apply",
        passive: "Pasiva: Aumenta dano contra objetivos con Heat a mayor cultivacion.",
      },
      {
        name: "Espiritu del Trueno",
        element: "Rayo",
        color: C.cyan,
        stats: "+ATK Magico, +Electrified Apply",
        passive: "Pasiva: Reduce cooldown de habilidades de rayo por cada estado Electrified activo.",
      },
      {
        name: "Espiritu de Escarcha",
        element: "Hielo",
        color: "87CEEB",
        stats: "+DEF, +Cold Apply, +Freeze Apply",
        passive: "Pasiva: Incrementa duracion de Freeze segun cultivacion.",
      },
    ];

    const spiritCardW = 2.8;
    const spiritGap = 0.3;
    const spiritY = 1.95;
    const spiritH = 2.5;

    spirits.forEach((s, i) => {
      const sx = 0.5 + i * (spiritCardW + spiritGap);

      addCard(slide, sx, spiritY, spiritCardW, spiritH, s.color);

      // Spirit image placeholder
      addPlaceholder(slide, sx + 0.2, spiritY + 0.2, spiritCardW - 0.4, 0.8,
        "INSERTAR IMAGEN:\n" + s.name);

      // Spirit name
      slide.addText(s.name, {
        x: sx + 0.1, y: spiritY + 1.1, w: spiritCardW - 0.2, h: 0.25,
        fontSize: 13, fontFace: "Trebuchet MS", bold: true,
        color: s.color, align: "center", margin: 0,
      });

      // Element badge
      slide.addText(s.element, {
        x: sx + spiritCardW / 2 - 0.4, y: spiritY + 1.38,
        w: 0.8, h: 0.2,
        fontSize: 8, fontFace: "Calibri", bold: true,
        color: s.color, align: "center", margin: 0,
      });

      // Stats
      slide.addText(s.stats, {
        x: sx + 0.15, y: spiritY + 1.6, w: spiritCardW - 0.3, h: 0.25,
        fontSize: 9, fontFace: "Calibri",
        color: C.white, align: "center", margin: 0,
      });

      // Passive description
      slide.addText(s.passive, {
        x: sx + 0.15, y: spiritY + 1.9, w: spiritCardW - 0.3, h: 0.5,
        fontSize: 8.5, fontFace: "Calibri", italic: true,
        color: C.gray, align: "center", margin: 0,
      });
    });

    // Note
    slide.addText("*Espiritus ilustrativos — sistema en desarrollo activo", {
      x: 0.5, y: SH - 0.4, w: 5, h: 0.2,
      fontSize: 7, fontFace: "Calibri", italic: true, color: C.grayDark, margin: 0,
    });

    slide.addNotes(
      "SISTEMA DE ESPÍRITUS - Notas:\n" +
      "- Los espíritus están planificados pero la arquitectura ya soporta stats adicionales y pasivas.\n" +
      "- El sistema de cultivación determina qué nivel de pasiva del espíritu se activa.\n" +
      "- Esto crea decisiones estratégicas: ¿cuál espíritu complementa mejor mi build?\n" +
      "- Los ejemplos son ilustrativos, pero representan la visión del diseño.\n" +
      "- Transición: 'Los espíritus y builds se prueban en la comunidad — veamos el aspecto social...'"
    );
  }

  // ========================================================================
  // SLIDE 8 — EXPERIENCIA SOCIAL: CLANES Y GUERRAS
  // ========================================================================
  {
    const slide = pres.addSlide();
    slide.background = { color: C.bg1 };
    addSlideNumber(slide, 8, TOTAL_SLIDES);

    slide.addText("DEL JUGADOR SOLITARIO AL LIDER DE GUERRA", {
      x: 0.5, y: 0.3, w: 9, h: 0.6,
      fontSize: 32, fontFace: "Trebuchet MS", bold: true,
      color: C.white, margin: 0,
    });

    slide.addShape(pres.shapes.RECTANGLE, {
      x: 0.5, y: 0.92, w: 1.5, h: 0.04,
      fill: { color: C.red },
    });

    // Social progression timeline (horizontal)
    const timelineY = 1.5;
    const phases = [
      { title: "AVENTURA SOLO", desc: "Explora, sube de nivel, domina tu clase y perfecciona tus habilidades.", color: C.cyan },
      { title: "UNETE A UN CLAN", desc: "Encuentra companeros, comparte estrategias y fortalecete en grupo.", color: C.gold },
      { title: "GUERRAS MASIVAS", desc: "Lidera ejercitos en batallas PvP a gran escala entre clanes rivales.", color: C.red },
    ];

    // Connecting line
    slide.addShape(pres.shapes.RECTANGLE, {
      x: 1.0, y: timelineY + 0.55, w: 8, h: 0.04,
      fill: { color: C.grayDark },
    });

    phases.forEach((p, i) => {
      const px = 0.5 + i * 3.2;

      // Circle node
      slide.addShape(pres.shapes.OVAL, {
        x: px + 1.05, y: timelineY + 0.3, w: 0.55, h: 0.55,
        fill: { color: p.color },
        shadow: makeShadow({ blur: 10, opacity: 0.4, color: p.color }),
      });

      // Number in circle
      slide.addText(String(i + 1), {
        x: px + 1.05, y: timelineY + 0.3, w: 0.55, h: 0.55,
        fontSize: 18, fontFace: "Trebuchet MS", bold: true,
        color: C.bg1, align: "center", valign: "middle", margin: 0,
      });

      // Title
      slide.addText(p.title, {
        x: px + 0.1, y: timelineY + 1.0, w: 2.6, h: 0.3,
        fontSize: 14, fontFace: "Trebuchet MS", bold: true,
        color: p.color, align: "center", margin: 0,
      });

      // Desc
      slide.addText(p.desc, {
        x: px + 0.1, y: timelineY + 1.35, w: 2.6, h: 0.6,
        fontSize: 10, fontFace: "Calibri",
        color: C.gray, align: "center", margin: 0,
      });
    });

    // Bottom section: key features
    const featY = 3.4;
    const feats = [
      { title: "Clanes Globales", desc: "Jugadores de cualquier parte del mundo pueden unirse y competir juntos." },
      { title: "Sistema de Alianzas", desc: "Los clanes pueden formar alianzas estrategicas para guerras a gran escala." },
      { title: "Recompensas de Guerra", desc: "Las victorias en guerras de clanes otorgan recursos exclusivos para el clan." },
    ];

    feats.forEach((f, i) => {
      const fx = 0.5 + i * 3.2;
      addCard(slide, fx, featY, 2.8, 1.2, C.gold);

      slide.addText(f.title, {
        x: fx + 0.15, y: featY + 0.15, w: 2.5, h: 0.3,
        fontSize: 12, fontFace: "Trebuchet MS", bold: true,
        color: C.gold, margin: 0,
      });
      slide.addText(f.desc, {
        x: fx + 0.15, y: featY + 0.5, w: 2.5, h: 0.55,
        fontSize: 9.5, fontFace: "Calibri",
        color: C.gray, margin: 0,
      });
    });

    slide.addNotes(
      "EXPERIENCIA SOCIAL - Notas:\n" +
      "- La progresión social es: Solo → Clan → Guerras masivas.\n" +
      "- El sistema de clanes y guerras está planificado pero es core al diseño del juego.\n" +
      "- Enfatizar que WAR es un juego de comunidad, no solo de stats.\n" +
      "- Transición: 'La comunidad necesita un modelo de negocio que la respete...'"
    );
  }

  // ========================================================================
  // SLIDE 9 — MODELO DE NEGOCIO
  // ========================================================================
  {
    const slide = pres.addSlide();
    slide.background = { color: C.bg1 };
    addSlideNumber(slide, 9, TOTAL_SLIDES);

    slide.addText("RENTABLE SIN SER INJUSTO", {
      x: 0.5, y: 0.3, w: 9, h: 0.6,
      fontSize: 34, fontFace: "Trebuchet MS", bold: true,
      color: C.white, margin: 0,
    });

    slide.addShape(pres.shapes.RECTANGLE, {
      x: 0.5, y: 0.92, w: 1.5, h: 0.04,
      fill: { color: C.green },
    });

    // Comparison table: WAR vs Typical MMORPG
    const compY = 1.3;
    const colW = 4.2;
    const rowH = 0.55;
    const leftX = 0.5;
    const rightX = leftX + colW + 0.35;

    // Headers
    slide.addShape(pres.shapes.RECTANGLE, {
      x: leftX, y: compY, w: colW, h: 0.45,
      fill: { color: C.green, transparency: 70 },
    });
    slide.addText("WAR", {
      x: leftX, y: compY, w: colW, h: 0.45,
      fontSize: 16, fontFace: "Trebuchet MS", bold: true,
      color: C.green, align: "center", valign: "middle", margin: 0,
    });

    slide.addShape(pres.shapes.RECTANGLE, {
      x: rightX, y: compY, w: colW, h: 0.45,
      fill: { color: C.red, transparency: 70 },
    });
    slide.addText("MMORPG TIPICO", {
      x: rightX, y: compY, w: colW, h: 0.45,
      fontSize: 16, fontFace: "Trebuchet MS", bold: true,
      color: C.red, align: "center", valign: "middle", margin: 0,
    });

    const rows = [
      { war: "Cosmeticos sin ventaja", typical: "Items P2W en tienda" },
      { war: "Battle Pass con recompensas cosmeticas", typical: "Gacha con probabilidades ocultas" },
      { war: "Expansiones con contenido real", typical: "DLCs fragmentados" },
      { war: "Progresion por habilidad del jugador", typical: "Progresion por gasto monetario" },
      { war: "Tienda transparente", typical: "Monedas premium confusas" },
    ];

    rows.forEach((r, i) => {
      const ry = compY + 0.55 + i * rowH;

      // WAR side (with check)
      slide.addShape(pres.shapes.RECTANGLE, {
        x: leftX, y: ry, w: colW, h: rowH - 0.05,
        fill: { color: C.bg2 },
      });
      if (icons.check) {
        slide.addImage({
          data: icons.check, x: leftX + 0.15, y: ry + 0.12, w: 0.25, h: 0.25,
        });
      }
      slide.addText(r.war, {
        x: leftX + 0.5, y: ry, w: colW - 0.6, h: rowH - 0.05,
        fontSize: 10.5, fontFace: "Calibri",
        color: C.white, valign: "middle", margin: 0,
      });

      // Typical side (with X)
      slide.addShape(pres.shapes.RECTANGLE, {
        x: rightX, y: ry, w: colW, h: rowH - 0.05,
        fill: { color: C.bg2 },
      });
      if (icons.times) {
        slide.addImage({
          data: icons.times, x: rightX + 0.15, y: ry + 0.12, w: 0.25, h: 0.25,
        });
      }
      slide.addText(r.typical, {
        x: rightX + 0.5, y: ry, w: colW - 0.6, h: rowH - 0.05,
        fontSize: 10.5, fontFace: "Calibri",
        color: C.gray, valign: "middle", margin: 0,
      });
    });

    // Bottom note
    slide.addText(
      "El diseno anti-P2W esta integrado en la arquitectura: todas las clases y habilidades son accesibles para todos. Los Puntos de Poder contextuales hacen imposible 'comprar' una ventaja universal.",
      {
        x: 0.5, y: SH - 0.7, w: 9, h: 0.45,
        fontSize: 10, fontFace: "Calibri", italic: true,
        color: C.gray, margin: 0,
      }
    );

    slide.addNotes(
      "MODELO DE NEGOCIO - Notas:\n" +
      "- No hay sistema de monetización implementado aún — el modelo se basa en la filosofía anti-P2W del diseño.\n" +
      "- Framework propuesto: Cosméticos, Battle Pass, Expansiones.\n" +
      "- La arquitectura de PP contextuales hace imposible 'comprar' ventaja: +10 ATK vale diferente para cada clase.\n" +
      "- Enfatizar: la monetización justa genera retención a largo plazo y LTV (lifetime value) superior.\n" +
      "- Transición: 'Veamos dónde estamos en el desarrollo...'"
    );
  }

  // ========================================================================
  // SLIDE 10 — ESTADO ACTUAL Y ROADMAP
  // ========================================================================
  {
    const slide = pres.addSlide();
    slide.background = { color: C.bg1 };
    addSlideNumber(slide, 10, TOTAL_SLIDES);

    slide.addText("DONDE ESTAMOS Y HACIA DONDE VAMOS", {
      x: 0.5, y: 0.3, w: 9, h: 0.6,
      fontSize: 32, fontFace: "Trebuchet MS", bold: true,
      color: C.white, margin: 0,
    });

    slide.addShape(pres.shapes.RECTANGLE, {
      x: 0.5, y: 0.92, w: 1.5, h: 0.04,
      fill: { color: C.gold },
    });

    // Timeline horizontal
    const tlY = 1.3;
    const tlH = 3.5;
    const phases = [
      {
        title: "FASE 1: FUNDACION",
        status: "COMPLETADO",
        statusColor: C.green,
        borderColor: C.green,
        items: [
          "Motor de combate (10 fases)",
          "4 clases diferenciadas",
          "13 skills Sorcerer + Ultimate",
          "Sistema de 92 stats",
          "Puntos de Poder contextuales",
          "80 niveles de progresion",
          "Sistema de combos (6 stages)",
          "Panel de admin de skills",
          "API REST + demo interactivo",
        ],
      },
      {
        title: "FASE 2: EXPANSION",
        status: "EN DESARROLLO",
        statusColor: C.gold,
        borderColor: C.gold,
        items: [
          "Skills Juramentada + Lancero + Bruiser",
          "Sistema de Equipamiento (7 slots)",
          "Sistema de Espiritus",
          "Sistema de Cultivacion",
          "Balance fino de curves/tuning",
          "AoE y multi-target real",
          "Persistencia completa",
        ],
      },
      {
        title: "FASE 3: LANZAMIENTO",
        status: "PLANIFICADO",
        statusColor: C.gray,
        borderColor: C.grayDark,
        items: [
          "Clanes y guerras masivas",
          "PvP matchmaking",
          "Dungeons y bosses",
          "Crafting y mercado",
          "Networking multiplayer",
          "Autenticacion de cuentas",
          "Nuevas clases y contenido",
        ],
      },
    ];

    // Background connecting line
    slide.addShape(pres.shapes.RECTANGLE, {
      x: 0.5, y: tlY + 0.25, w: 9, h: 0.04,
      fill: { color: C.grayDark },
    });

    phases.forEach((p, i) => {
      const px = 0.5 + i * 3.15;
      const cardW = 2.85;

      // Phase dot
      slide.addShape(pres.shapes.OVAL, {
        x: px + cardW / 2 - 0.17, y: tlY + 0.1, w: 0.35, h: 0.35,
        fill: { color: p.statusColor },
        shadow: makeShadow({ blur: 8, opacity: 0.3, color: p.statusColor }),
      });

      // Status badge
      slide.addText(p.status, {
        x: px + cardW / 2 - 0.6, y: tlY + 0.55, w: 1.2, h: 0.2,
        fontSize: 7, fontFace: "Calibri", bold: true,
        color: p.statusColor, align: "center", margin: 0, charSpacing: 1,
      });

      // Phase card
      addCard(slide, px, tlY + 0.85, cardW, tlH - 1.0, p.borderColor);

      slide.addText(p.title, {
        x: px + 0.1, y: tlY + 1.0, w: cardW - 0.2, h: 0.3,
        fontSize: 11, fontFace: "Trebuchet MS", bold: true,
        color: p.statusColor, align: "center", margin: 0,
      });

      // Items list
      const itemsText = p.items.map((item, idx) => ({
        text: item,
        options: { bullet: true, breakLine: idx < p.items.length - 1, color: C.gray },
      }));

      slide.addText(itemsText, {
        x: px + 0.15, y: tlY + 1.4, w: cardW - 0.3, h: tlH - 1.7,
        fontSize: 8.5, fontFace: "Calibri",
        color: C.gray, margin: 0,
      });
    });

    slide.addNotes(
      "ROADMAP - Datos del código:\n" +
      "- FASE 1 (Completada): Combat pipeline completo, 4 clases con stats, 13 skills Sorcerer + Ultimate, sistema PP, 80 niveles, admin panel.\n" +
      "- FASE 2 (En desarrollo): Skills de las 3 clases restantes, equipamiento, espíritus, cultivación, balance.\n" +
      "- FASE 3 (Planificada): Clanes, PvP, dungeons, crafting, networking, auth.\n" +
      "- La base técnica es sólida y extensible.\n" +
      "- Transición: 'La base técnica detrás de todo esto es robusta...'"
    );
  }

  // ========================================================================
  // SLIDE 11 — DATOS TÉCNICOS
  // ========================================================================
  {
    const slide = pres.addSlide();
    slide.background = { color: C.bg1 };
    addSlideNumber(slide, 11, TOTAL_SLIDES);

    slide.addText("ARQUITECTURA Y STACK TECNOLOGICO", {
      x: 0.5, y: 0.3, w: 9, h: 0.6,
      fontSize: 34, fontFace: "Trebuchet MS", bold: true,
      color: C.white, margin: 0,
    });

    slide.addShape(pres.shapes.RECTANGLE, {
      x: 0.5, y: 0.92, w: 1.5, h: 0.04,
      fill: { color: C.cyan },
    });

    // Left: Stack tech cards
    const techCards = [
      { title: ".NET 8 / C#", desc: "Framework enterprise de alto rendimiento", color: C.cyan },
      { title: "Entity Framework Core", desc: "ORM con migraciones y persistencia robusta", color: C.green },
      { title: "REST API", desc: "Endpoints bien definidos y documentados", color: C.gold },
      { title: "Clean Architecture", desc: "Core / API / Infrastructure separados", color: C.red },
    ];

    techCards.forEach((t, i) => {
      const ty = 1.2 + i * 0.9;
      addCard(slide, 0.5, ty, 4.2, 0.75, t.color);

      slide.addText(t.title, {
        x: 0.65, y: ty + 0.1, w: 3.9, h: 0.3,
        fontSize: 14, fontFace: "Trebuchet MS", bold: true,
        color: t.color, margin: 0,
      });
      slide.addText(t.desc, {
        x: 0.65, y: ty + 0.4, w: 3.9, h: 0.25,
        fontSize: 10, fontFace: "Calibri",
        color: C.gray, margin: 0,
      });
    });

    // Right: Big numbers / metrics
    const metricX = 5.3;
    const metrics = [
      { num: "92", label: "Stats del sistema", color: C.gold },
      { num: "13", label: "Habilidades por clase", color: C.cyan },
      { num: "10", label: "Niveles de ascension", color: C.red },
      { num: "80", label: "Niveles de personaje", color: C.green },
      { num: "10", label: "Fases del combat pipeline", color: C.gold },
      { num: "4", label: "Clases jugables", color: C.cyan },
    ];

    metrics.forEach((m, i) => {
      const col = i % 2;
      const row = Math.floor(i / 2);
      const mx = metricX + col * 2.2;
      const my = 1.2 + row * 1.3;

      addCard(slide, mx, my, 2.0, 1.1, m.color);

      slide.addText(m.num, {
        x: mx, y: my + 0.1, w: 2.0, h: 0.55,
        fontSize: 36, fontFace: "Trebuchet MS", bold: true,
        color: m.color, align: "center", margin: 0,
      });
      slide.addText(m.label, {
        x: mx + 0.1, y: my + 0.7, w: 1.8, h: 0.25,
        fontSize: 9, fontFace: "Calibri",
        color: C.gray, align: "center", margin: 0,
      });
    });

    // Design patterns note
    slide.addText("Patrones: Service, Provider, Catalog, Builder, Strategy", {
      x: 0.5, y: SH - 0.45, w: 9, h: 0.25,
      fontSize: 9, fontFace: "Calibri", italic: true,
      color: C.grayDark, margin: 0,
    });

    slide.addNotes(
      "STACK TÉCNICO - Datos del código:\n" +
      "- .NET 8.0+ con C# — framework enterprise, alto rendimiento.\n" +
      "- Arquitectura limpia: War.Core (dominio), War.Api (endpoints), War.Infrastructure (DB).\n" +
      "- Entity Framework Core con migraciones.\n" +
      "- Patrones de diseño: Service, Provider, Catalog, Builder, Strategy.\n" +
      "- Servicios clave: CombatEventResolver, PowerScoreCalculator, CharacterFinalStatsBuilder, SkillRuntimeCatalogProvider.\n" +
      "- 92 stats, 13 skills/clase, 10 ascensiones, 80 niveles, 10 fases de combate, 4 clases.\n" +
      "- Transición: 'Con esta base técnica, veamos la oportunidad de mercado...'"
    );
  }

  // ========================================================================
  // SLIDE 12 — OPORTUNIDAD DE MERCADO
  // ========================================================================
  {
    const slide = pres.addSlide();
    slide.background = { color: C.bg1 };
    addSlideNumber(slide, 12, TOTAL_SLIDES);

    slide.addText("UN MERCADO ESPERANDO DISRUPCION", {
      x: 0.5, y: 0.3, w: 9, h: 0.6,
      fontSize: 34, fontFace: "Trebuchet MS", bold: true,
      color: C.white, margin: 0,
    });

    slide.addShape(pres.shapes.RECTANGLE, {
      x: 0.5, y: 0.92, w: 1.5, h: 0.04,
      fill: { color: C.gold },
    });

    // Three big market stat boxes
    const statBoxes = [
      { num: "$XX B", label: "Tamano del mercado\nMMORPG global", color: C.gold, note: "[Actualizar con dato real]" },
      { num: "XX%", label: "Crecimiento\nanual (CAGR)", color: C.cyan, note: "[Actualizar con dato real]" },
      { num: "XXM", label: "Jugadores activos\nglobalmente", color: C.green, note: "[Actualizar con dato real]" },
    ];

    statBoxes.forEach((s, i) => {
      const sx = 0.5 + i * 3.15;
      const boxW = 2.85;

      addCard(slide, sx, 1.3, boxW, 1.8, s.color);

      slide.addText(s.num, {
        x: sx, y: 1.45, w: boxW, h: 0.7,
        fontSize: 44, fontFace: "Trebuchet MS", bold: true,
        color: s.color, align: "center", margin: 0,
      });

      slide.addText(s.label, {
        x: sx + 0.2, y: 2.15, w: boxW - 0.4, h: 0.5,
        fontSize: 11, fontFace: "Calibri",
        color: C.white, align: "center", margin: 0,
      });

      slide.addText(s.note, {
        x: sx + 0.2, y: 2.7, w: boxW - 0.4, h: 0.2,
        fontSize: 7, fontFace: "Calibri", italic: true,
        color: C.grayDark, align: "center", margin: 0,
      });
    });

    // Positioning section
    slide.addText("POSICIONAMIENTO DE WAR", {
      x: 0.5, y: 3.4, w: 9, h: 0.3,
      fontSize: 16, fontFace: "Trebuchet MS", bold: true,
      color: C.gold, margin: 0,
    });

    const positions = [
      { title: "Diferenciacion Clara", desc: "Sistema de PP contextual unico en el mercado. No existe otro MMORPG con balance por clase tan granular." },
      { title: "Segmento Desatendido", desc: "Jugadores hardcore que buscan profundidad estrategica sin P2W. Mercado creciente post-Lost Ark, Throne & Liberty." },
      { title: "Escalabilidad", desc: "Arquitectura .NET 8 enterprise-grade, diseñada para escalar a millones de usuarios concurrentes." },
    ];

    positions.forEach((p, i) => {
      const px = 0.5 + i * 3.15;
      slide.addShape(pres.shapes.RECTANGLE, {
        x: px, y: 3.8, w: 0.06, h: 0.8,
        fill: { color: C.gold },
      });
      slide.addText(p.title, {
        x: px + 0.2, y: 3.8, w: 2.65, h: 0.25,
        fontSize: 11, fontFace: "Trebuchet MS", bold: true,
        color: C.white, margin: 0,
      });
      slide.addText(p.desc, {
        x: px + 0.2, y: 4.08, w: 2.65, h: 0.55,
        fontSize: 8.5, fontFace: "Calibri",
        color: C.gray, margin: 0,
      });
    });

    slide.addNotes(
      "OPORTUNIDAD DE MERCADO - Notas:\n" +
      "- Los números son placeholders — el usuario DEBE actualizarlos con datos de mercado reales.\n" +
      "- Fuentes sugeridas: Newzoo, SuperData, Statista para datos del mercado MMORPG.\n" +
      "- Posicionamiento: sistema de PP contextual es genuinamente único en el mercado.\n" +
      "- Competidores relevantes: Lost Ark, WoW, FFXIV, Throne & Liberty — todos con modelos P2W.\n" +
      "- Transición: 'La oportunidad es clara. Ahora es el momento de unirse...'"
    );
  }

  // ========================================================================
  // SLIDE 13 — LLAMADO A LA ACCIÓN / CIERRE
  // ========================================================================
  {
    const slide = pres.addSlide();
    slide.background = { color: C.bg1 };

    // Full decorative elements
    slide.addShape(pres.shapes.RECTANGLE, {
      x: 0, y: 0, w: SW, h: 0.06,
      fill: { color: C.gold },
    });
    slide.addShape(pres.shapes.RECTANGLE, {
      x: 0, y: SH - 0.06, w: SW, h: 0.06,
      fill: { color: C.gold },
    });
    slide.addShape(pres.shapes.RECTANGLE, {
      x: 0, y: 0, w: 0.06, h: SH,
      fill: { color: C.gold, transparency: 40 },
    });
    slide.addShape(pres.shapes.RECTANGLE, {
      x: SW - 0.06, y: 0, w: 0.06, h: SH,
      fill: { color: C.gold, transparency: 40 },
    });

    slide.addText("UNETE A LA GUERRA", {
      x: 0.5, y: 0.25, w: 9, h: 0.7,
      fontSize: 42, fontFace: "Trebuchet MS", bold: true,
      color: C.gold, align: "center", margin: 0,
    });

    // Three key points
    const keyPoints = [
      { num: "01", title: "Balance Real", desc: "Sistema de PP contextual que elimina el P2W por diseno, no por promesa." },
      { num: "02", title: "Profundidad Unica", desc: "4 clases, 52+ habilidades, 10 ascensiones, espiritus y cultivacion." },
      { num: "03", title: "Arquitectura Solida", desc: ".NET 8, clean architecture, pipeline de combate de 10 fases." },
    ];

    keyPoints.forEach((k, i) => {
      const kx = 0.5 + i * 3.15;
      const ky = 1.1;

      // Number
      slide.addText(k.num, {
        x: kx, y: ky, w: 0.7, h: 0.6,
        fontSize: 28, fontFace: "Trebuchet MS", bold: true,
        color: C.gold, margin: 0,
      });

      slide.addText(k.title, {
        x: kx + 0.75, y: ky, w: 2.1, h: 0.3,
        fontSize: 14, fontFace: "Trebuchet MS", bold: true,
        color: C.white, margin: 0,
      });

      slide.addText(k.desc, {
        x: kx + 0.75, y: ky + 0.3, w: 2.1, h: 0.4,
        fontSize: 9.5, fontFace: "Calibri",
        color: C.gray, margin: 0,
      });
    });

    // Image placeholder
    addPlaceholder(slide, 2.0, 2.0, 6.0, 1.8, "INSERTAR IMAGEN: Arte final epico del juego");

    // Contact section
    const contactY = 4.1;
    slide.addShape(pres.shapes.RECTANGLE, {
      x: 1.5, y: contactY, w: 7, h: 0.03,
      fill: { color: C.gold, transparency: 50 },
    });

    slide.addText([
      { text: "[Tu Nombre]", options: { bold: true, fontSize: 13, color: C.white, breakLine: true } },
      { text: "[email@ejemplo.com]  |  [www.sitio.com]  |  [+XX XXX XXX XXXX]", options: { fontSize: 10, color: C.gray } },
    ], {
      x: 1.5, y: contactY + 0.15, w: 7, h: 0.55,
      fontFace: "Calibri", align: "center", margin: 0,
    });

    // Closing phrase
    slide.addText("\"El campo de batalla esta listo. La estrategia es tuya.\"", {
      x: 1.5, y: SH - 0.6, w: 7, h: 0.35,
      fontSize: 14, fontFace: "Calibri", italic: true,
      color: C.gold, align: "center", margin: 0,
    });

    slide.addNotes(
      "CIERRE - Puntos clave:\n" +
      "- Resumir los 3 diferenciadores: Balance real, Profundidad, Arquitectura sólida.\n" +
      "- Actualizar la información de contacto antes de presentar.\n" +
      "- Frase de cierre: 'El campo de batalla está listo. La estrategia es tuya.'\n" +
      "- Estar preparado para preguntas sobre: timeline de lanzamiento, inversión necesaria, equipo, métricas objetivo."
    );
  }

  // ========================================================================
  // SAVE
  // ========================================================================
  const outputPath = process.cwd() + "/WAR_Investor_Pitch.pptx";
  await pres.writeFile({ fileName: outputPath });
  console.log("Presentation saved to: " + outputPath);
}

createPresentation().catch(err => {
  console.error("Error creating presentation:", err);
  process.exit(1);
});
