const fs = require("fs");
const {
  Document, Packer, Paragraph, TextRun, Table, TableRow, TableCell,
  Header, Footer, AlignmentType, LevelFormat,
  TableOfContents, HeadingLevel, BorderStyle, WidthType, ShadingType,
  PageNumber, PageBreak, SectionType
} = require("docx");

// ─── CONSTANTS ───
const PAGE_W = 12240, PAGE_H = 15840, MARGIN = 1440;
const CONTENT_W = PAGE_W - 2 * MARGIN; // 9360
const BLUE_DARK = "1B2A4A";
const BLUE_MED = "2E75B6";
const BLUE_LIGHT = "D5E8F0";
const GRAY_ALT = "F2F2F2";
const WHITE = "FFFFFF";
const BLACK = "000000";
const GOLD = "C9A84C";
const RED_ACCENT = "E63946";
const GREEN = "2DD4BF";
const CYAN = "4CC9F0";
const GRAY_TEXT = "8B949E";

const TODAY = new Date().toLocaleDateString("es-MX", { year: "numeric", month: "long", day: "numeric" });

// ─── HELPER FACTORIES ───
function border(color = "CCCCCC", size = 1) {
  return { style: BorderStyle.SINGLE, size, color };
}
function borders(color = "CCCCCC", size = 1) {
  const b = border(color, size);
  return { top: b, bottom: b, left: b, right: b };
}
function cellMargins() {
  return { top: 80, bottom: 80, left: 120, right: 120 };
}
function headerCell(text, width, color = BLUE_DARK) {
  return new TableCell({
    borders: borders("888888"),
    width: { size: width, type: WidthType.DXA },
    shading: { fill: color, type: ShadingType.CLEAR },
    margins: cellMargins(),
    verticalAlign: "center",
    children: [new Paragraph({ alignment: AlignmentType.LEFT, children: [new TextRun({ text, bold: true, font: "Arial", size: 20, color: WHITE })] })]
  });
}
function dataCell(text, width, shaded = false) {
  return new TableCell({
    borders: borders("CCCCCC"),
    width: { size: width, type: WidthType.DXA },
    shading: shaded ? { fill: GRAY_ALT, type: ShadingType.CLEAR } : undefined,
    margins: cellMargins(),
    children: [new Paragraph({ children: [new TextRun({ text: String(text), font: "Calibri", size: 20 })] })]
  });
}
function dataCellBold(text, width, shaded = false) {
  return new TableCell({
    borders: borders("CCCCCC"),
    width: { size: width, type: WidthType.DXA },
    shading: shaded ? { fill: GRAY_ALT, type: ShadingType.CLEAR } : undefined,
    margins: cellMargins(),
    children: [new Paragraph({ children: [new TextRun({ text: String(text), font: "Calibri", size: 20, bold: true })] })]
  });
}
function h1(text) { return new Paragraph({ heading: HeadingLevel.HEADING_1, spacing: { before: 360, after: 200 }, children: [new TextRun({ text, font: "Arial", bold: true, size: 32, color: BLUE_DARK })] }); }
function h2(text) { return new Paragraph({ heading: HeadingLevel.HEADING_2, spacing: { before: 280, after: 160 }, children: [new TextRun({ text, font: "Arial", bold: true, size: 28, color: BLUE_MED })] }); }
function h3(text) { return new Paragraph({ heading: HeadingLevel.HEADING_3, spacing: { before: 200, after: 120 }, children: [new TextRun({ text, font: "Arial", bold: true, size: 24, color: "444444" })] }); }
function p(text, opts = {}) {
  return new Paragraph({
    spacing: { after: 120 },
    alignment: opts.align || AlignmentType.JUSTIFIED,
    children: [new TextRun({ text, font: "Calibri", size: opts.size || 22, bold: opts.bold, italics: opts.italic, color: opts.color || BLACK })]
  });
}
function pRuns(runs) {
  return new Paragraph({
    spacing: { after: 120 },
    alignment: AlignmentType.JUSTIFIED,
    children: runs.map(r => new TextRun({ font: "Calibri", size: 22, ...r }))
  });
}
function codeBlock(text) {
  return new Paragraph({
    spacing: { before: 80, after: 80 },
    shading: { fill: "F5F5F5", type: ShadingType.CLEAR },
    indent: { left: 360, right: 360 },
    children: [new TextRun({ text, font: "Consolas", size: 18, color: "333333" })]
  });
}
function emptyLine() { return new Paragraph({ spacing: { after: 80 }, children: [] }); }
function statusBadge(status) {
  const map = { "COMPLETO": "27AE60", "PARCIAL": "F39C12", "PENDIENTE": "E74C3C", "PROPUESTO": "9B59B6" };
  const icons = { "COMPLETO": "\u2705", "PARCIAL": "\uD83D\uDD36", "PENDIENTE": "\uD83D\uDD34", "PROPUESTO": "\uD83D\uDCA1" };
  return new TextRun({ text: `${icons[status] || ""} ${status}`, bold: true, font: "Arial", size: 22, color: map[status] || BLACK });
}
function bulletItem(text, ref = "bullets") {
  return new Paragraph({
    numbering: { reference: ref, level: 0 },
    spacing: { after: 80 },
    children: [new TextRun({ text, font: "Calibri", size: 22 })]
  });
}
function bulletItemBold(label, desc, ref = "bullets") {
  return new Paragraph({
    numbering: { reference: ref, level: 0 },
    spacing: { after: 80 },
    children: [
      new TextRun({ text: label + ": ", font: "Calibri", size: 22, bold: true }),
      new TextRun({ text: desc, font: "Calibri", size: 22 })
    ]
  });
}
function numberedItem(text, ref = "numbers") {
  return new Paragraph({
    numbering: { reference: ref, level: 0 },
    spacing: { after: 80 },
    children: [new TextRun({ text, font: "Calibri", size: 22 })]
  });
}
function makeTable(headers, rows, colWidths) {
  const totalW = colWidths.reduce((a, b) => a + b, 0);
  const headerRow = new TableRow({
    children: headers.map((h, i) => headerCell(h, colWidths[i]))
  });
  const dataRows = rows.map((row, ri) =>
    new TableRow({
      children: row.map((cell, ci) => dataCell(cell, colWidths[ci], ri % 2 === 1))
    })
  );
  return new Table({
    width: { size: totalW, type: WidthType.DXA },
    columnWidths: colWidths,
    rows: [headerRow, ...dataRows]
  });
}

// ─── BUILD DOCUMENT ───
function buildDocument() {
  const children = [];

  // ════════════════════════════════════════════════
  // PORTADA
  // ════════════════════════════════════════════════
  const coverSection = {
    properties: {
      page: { size: { width: PAGE_W, height: PAGE_H }, margin: { top: MARGIN, right: MARGIN, bottom: MARGIN, left: MARGIN } },
      type: SectionType.NEXT_PAGE
    },
    children: [
      emptyLine(), emptyLine(), emptyLine(), emptyLine(), emptyLine(), emptyLine(),
      new Paragraph({ alignment: AlignmentType.CENTER, spacing: { after: 200 }, children: [
        new TextRun({ text: "WAR", font: "Arial", bold: true, size: 72, color: BLUE_DARK })
      ]}),
      new Paragraph({ alignment: AlignmentType.CENTER, spacing: { after: 100 }, children: [
        new TextRun({ text: "\u2501\u2501\u2501\u2501\u2501\u2501\u2501\u2501\u2501\u2501\u2501\u2501\u2501\u2501\u2501\u2501\u2501\u2501\u2501\u2501\u2501\u2501\u2501\u2501\u2501\u2501\u2501\u2501\u2501\u2501", font: "Arial", size: 20, color: GOLD })
      ]}),
      new Paragraph({ alignment: AlignmentType.CENTER, spacing: { after: 400 }, children: [
        new TextRun({ text: "Documentaci\u00f3n T\u00e9cnica Completa", font: "Arial", size: 36, color: BLUE_MED })
      ]}),
      new Paragraph({ alignment: AlignmentType.CENTER, spacing: { after: 80 }, children: [
        new TextRun({ text: "Auditor\u00eda de Arquitectura, Estado del Proyecto", font: "Calibri", size: 26, color: "555555" })
      ]}),
      new Paragraph({ alignment: AlignmentType.CENTER, spacing: { after: 400 }, children: [
        new TextRun({ text: "y Estrategia Multiplataforma", font: "Calibri", size: 26, color: "555555" })
      ]}),
      emptyLine(), emptyLine(), emptyLine(), emptyLine(),
      new Paragraph({ alignment: AlignmentType.CENTER, spacing: { after: 100 }, children: [
        new TextRun({ text: `Fecha de generaci\u00f3n: ${TODAY}`, font: "Calibri", size: 22, color: "666666" })
      ]}),
      new Paragraph({ alignment: AlignmentType.CENTER, spacing: { after: 100 }, children: [
        new TextRun({ text: "Versi\u00f3n: v1.0", font: "Calibri", size: 22, color: "666666" })
      ]}),
      emptyLine(),
      new Paragraph({ alignment: AlignmentType.CENTER, spacing: { after: 0 }, border: { top: { style: BorderStyle.SINGLE, size: 2, color: RED_ACCENT } }, children: [
        new TextRun({ text: "CONFIDENCIAL \u2014 Solo para uso interno y evaluaci\u00f3n de inversores", font: "Arial", bold: true, size: 20, color: RED_ACCENT })
      ]})
    ]
  };

  // ════════════════════════════════════════════════
  // TOC + MAIN CONTENT (single section with headers/footers)
  // ════════════════════════════════════════════════
  const mainChildren = [];

  // TOC
  mainChildren.push(
    new Paragraph({ heading: HeadingLevel.HEADING_1, spacing: { after: 300 }, children: [new TextRun({ text: "Tabla de Contenidos", font: "Arial", bold: true, size: 32, color: BLUE_DARK })] }),
    new TableOfContents("Tabla de Contenidos", { hyperlink: true, headingStyleRange: "1-3" }),
    new Paragraph({ children: [new PageBreak()] })
  );

  // ════════════════════════════════════════════════
  // SECCI\u00d3N 1 — RESUMEN EJECUTIVO
  // ════════════════════════════════════════════════
  mainChildren.push(
    h1("1. Resumen Ejecutivo"),
    p("WAR es un MMORPG (Massively Multiplayer Online Role-Playing Game) de nueva generaci\u00f3n en desarrollo activo. El proyecto implementa un backend completo de juego con sistemas de combate, progresi\u00f3n de personajes, habilidades, balanceo por puntos de poder y persistencia en base de datos. El enfoque central es crear un sistema de combate profundo y estrat\u00e9gico donde la habilidad del jugador y las decisiones de build superen cualquier ventaja monetaria."),
    p("El backend est\u00e1 construido sobre una arquitectura limpia (Clean Architecture) en C# con .NET 8, utilizando PostgreSQL como motor de base de datos. El proyecto sigue patrones de dise\u00f1o de grado empresarial con separaci\u00f3n estricta entre l\u00f3gica de dominio, infraestructura y capa de presentaci\u00f3n."),
    emptyLine(),
    h2("1.1 Stack Tecnol\u00f3gico Principal"),
    makeTable(
      ["Capa", "Tecnolog\u00eda", "Versi\u00f3n", "Prop\u00f3sito"],
      [
        ["Backend Framework", "ASP.NET Core", "8.0", "API REST y servidor web"],
        ["Lenguaje", "C#", "12", "L\u00f3gica de dominio y servicios"],
        ["Base de Datos", "PostgreSQL", "16+", "Persistencia de datos del juego"],
        ["ORM", "Entity Framework Core", "8.0.11", "Mapeo objeto-relacional"],
        ["DB Provider", "Npgsql", "8.0.11", "Conector PostgreSQL para EF Core"],
        ["API Docs", "Swashbuckle (Swagger)", "6.4.0", "Documentaci\u00f3n interactiva de API"],
        ["Frontend Demo", "HTML/CSS/JS (Vanilla)", "ES2022", "Interfaz de demo de combate"],
      ],
      [2000, 2500, 1200, 3660]
    ),
    emptyLine(),
    h2("1.2 M\u00e9tricas del Proyecto"),
    makeTable(
      ["M\u00e9trica", "Valor"],
      [
        ["Archivos de c\u00f3digo fuente (C#)", "~60 archivos .cs"],
        ["Archivos frontend (HTML/JS/CSS)", "6 archivos"],
        ["Migraciones de base de datos", "4 migraciones"],
        ["Sistemas implementados (completos/parciales)", "10 sistemas"],
        ["Sistemas pendientes/propuestos", "12+ sistemas"],
        ["Clases jugables definidas", "4 (Sorcerer, Juramentada, Lancero, Bruiser)"],
        ["Habilidades implementadas (Sorcerer)", "13 (12 base + 1 ultimate)"],
        ["Stats del sistema", "93 tipos de estad\u00edsticas"],
        ["Condiciones de combate", "9 tipos (4 estados + 5 CC)"],
        ["Nivel m\u00e1ximo de personaje", "80"],
        ["Arquitectura", "Clean Architecture (3 capas)"],
      ],
      [5000, 4360]
    ),
    emptyLine(),
    h2("1.3 Completitud Global Estimada"),
    p("Basado en el an\u00e1lisis exhaustivo del c\u00f3digo fuente, se estima que el proyecto tiene aproximadamente un 30-35% de completitud respecto a un producto MMORPG lanzable. Los sistemas de dominio core (combate, stats, progresi\u00f3n, habilidades, puntos de poder) est\u00e1n s\u00f3lidamente implementados, pero faltan sistemas cr\u00edticos de infraestructura multijugador (networking, autenticaci\u00f3n, clanes, matchmaking) y contenido de juego (dungeons, misiones, equipamiento avanzado, m\u00e1s clases completas)."),
    emptyLine(),
    h2("1.4 Arquitectura de Alto Nivel"),
    codeBlock("                    \u250C\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2510"),
    codeBlock("                    \u2502          War.Api (HTTP)             \u2502"),
    codeBlock("                    \u2502  Controllers \u2502 Application Services \u2502"),
    codeBlock("                    \u2502  wwwroot     \u2502 Startup / DI          \u2502"),
    codeBlock("                    \u2514\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u252C\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2518"),
    codeBlock("                                    \u2502"),
    codeBlock("                    \u250C\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2534\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2510"),
    codeBlock("                    \u2502       War.Core (Dominio)           \u2502"),
    codeBlock("                    \u2502  Combat \u2502 Stats \u2502 Skills \u2502 Progr.  \u2502"),
    codeBlock("                    \u2502  PowerScore \u2502 Resources \u2502 Entities\u2502"),
    codeBlock("                    \u2514\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u252C\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2518"),
    codeBlock("                                    \u2502"),
    codeBlock("                    \u250C\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2534\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2510"),
    codeBlock("                    \u2502   War.Infrastructure (Datos)       \u2502"),
    codeBlock("                    \u2502  WarDbContext \u2502 Entities \u2502 Migrations\u2502"),
    codeBlock("                    \u2514\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u252C\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2518"),
    codeBlock("                                    \u2502"),
    codeBlock("                              \u250C\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2510"),
    codeBlock("                              \u2502 PostgreSQL\u2502"),
    codeBlock("                              \u2514\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2518"),
    new Paragraph({ children: [new PageBreak()] })
  );

  // ════════════════════════════════════════════════
  // SECCI\u00d3N 2 — ARQUITECTURA DEL SISTEMA
  // ════════════════════════════════════════════════
  mainChildren.push(
    h1("2. Arquitectura del Sistema"),
    h2("2.1 Descripci\u00f3n General"),
    p("El proyecto WAR sigue el patr\u00f3n de Clean Architecture (Arquitectura Limpia) con tres capas bien diferenciadas que mantienen una direcci\u00f3n de dependencia estricta: la capa de dominio (War.Core) no depende de nada externo, la capa de infraestructura (War.Infrastructure) depende solo del dominio, y la capa de API (War.Api) orquesta ambas."),
    p("Actualmente opera como un monolito bien estructurado con una API REST que sirve tanto endpoints de datos como una interfaz web de demo. El dise\u00f1o facilita una futura migraci\u00f3n a microservicios gracias a la separaci\u00f3n de responsabilidades."),
    emptyLine(),
    h2("2.2 Stack Tecnol\u00f3gico Detallado"),
    makeTable(
      ["Capa", "Tecnolog\u00eda", "Versi\u00f3n", "Prop\u00f3sito"],
      [
        ["Runtime", ".NET 8.0", "8.0", "Plataforma de ejecuci\u00f3n"],
        ["Web Framework", "ASP.NET Core", "8.0", "HTTP pipeline, routing, middleware"],
        ["ORM", "EF Core + Npgsql", "8.0.11", "Persistencia relacional con PostgreSQL"],
        ["Serializaci\u00f3n", "System.Text.Json", "8.0", "JSON para API y skill storage"],
        ["API Documentation", "Swashbuckle/Swagger", "6.4.0", "OpenAPI specification"],
        ["Frontend", "Vanilla JS + CSS", "ES2022", "SPA de demo sin framework"],
        ["Build System", "MSBuild / dotnet CLI", "8.0", "Compilaci\u00f3n y publicaci\u00f3n"],
        ["Control de Versiones", "Git", "-", "Gesti\u00f3n de c\u00f3digo fuente"],
      ],
      [2000, 2500, 1000, 3860]
    ),
    emptyLine(),
    h2("2.3 Estructura de Directorios"),
    makeTable(
      ["Directorio", "Prop\u00f3sito", "Archivos Clave"],
      [
        ["War.Core/Stats/", "93 tipos de stats, c\u00e1lculos, resolvers", "StatType.cs, StatCatalog.cs, StatsCalculator.cs, StatResolvers.cs"],
        ["War.Core/Combat/", "Pipeline de resoluci\u00f3n de combate", "CombatEventResolver.cs, CombatConditionCatalog.cs"],
        ["War.Core/Skills/", "Definiciones de habilidades y cat\u00e1logos", "SkillModels.cs, SorcererSkillCatalog.cs"],
        ["War.Core/Progression/", "Sistema de niveles y crecimiento", "CharacterLevelProgression.cs, ClassLevelGrowthProfiles.cs"],
        ["War.Core/PowerScore/", "Puntuaci\u00f3n de poder balanceada", "PowerScoreCalculator.cs, PowerScorePolicyCatalog.cs"],
        ["War.Core/Resources/", "HP, Mana, UltimateCharge", "CharacterResources.cs, CharacterResourceType.cs"],
        ["War.Core/Entities/", "Entidad principal del personaje", "Character.cs"],
        ["War.Infrastructure/", "EF Core, migraciones, entidades DB", "WarDbContext.cs, CharacterEntity.cs"],
        ["War.Api/Controllers/", "Endpoints REST", "CharactersController.cs, CombatDemoController.cs"],
        ["War.Api/Application/", "Servicios de aplicaci\u00f3n", "DemoCombatExecutionService.cs, SkillAdminCatalogService.cs"],
        ["War.Api/wwwroot/", "Demo web frontend", "index.html, app.js, styles.css"],
      ],
      [2200, 3000, 4160]
    ),
    emptyLine(),
    h2("2.4 Patrones de Dise\u00f1o Identificados"),
    bulletItemBold("IStatSource (Strategy/Composite)", "M\u00faltiples fuentes de stats implementan una interfaz com\u00fan. StatsCalculator las agrega de forma uniforme, permitiendo a\u00f1adir nuevas fuentes (equipamiento, espíritus, buffs) sin modificar el calculador."),
    bulletItemBold("Cat\u00e1logo Inmutable (Registry/Catalog)", "StatCatalog, CombatConditionCatalog, PowerScorePolicyCatalog, SkillCatalogRegistry: diccionarios read-only thread-safe que validan consistencia en construcci\u00f3n."),
    bulletItemBold("Pipeline de Resoluci\u00f3n", "CombatEventResolver ejecuta una cadena secuencial de etapas (magnitud \u2192 recursos \u2192 hit \u2192 crit \u2192 da\u00f1o \u2192 condiciones \u2192 interacciones \u2192 proyecci\u00f3n)."),
    bulletItemBold("Builder", "CharacterFinalStatsBuilder y CharacterProfileSnapshotFactory componen objetos complejos paso a paso."),
    bulletItemBold("Dual-Sourcing (Composici\u00f3n)", "RuntimeSkillCatalogComposer fusiona habilidades programadas en c\u00f3digo con las publicadas desde la base de datos, con pol\u00edtica de fallback."),
    bulletItemBold("Records Inmutables (Value Objects)", "Uso extensivo de C# records para DTOs y modelos de dominio, garantizando inmutabilidad."),
    bulletItemBold("Dependency Injection", "Todos los servicios se registran en el contenedor de DI de ASP.NET Core, facilitando testing y desacoplamiento."),
    emptyLine(),
    h2("2.5 Flujo de Datos"),
    p("El flujo principal de una acci\u00f3n de combate sigue este recorrido:"),
    numberedItem("El cliente env\u00eda un POST a /api/combat/demo/execute con la acci\u00f3n deseada.", "numbers1"),
    numberedItem("DemoCombatExecutionService carga los personajes desde la base de datos v\u00eda PersistedCharacterRuntimeService.", "numbers1"),
    numberedItem("Se traduce la acci\u00f3n a un CombatEventContext (BasicAttackCombatTranslator o SkillCombatTranslator).", "numbers1"),
    numberedItem("CombatEventResolver procesa el evento a trav\u00e9s de 11 etapas de resoluci\u00f3n.", "numbers1"),
    numberedItem("CombatResultPersistenceService guarda los cambios de recursos y estado de combo en PostgreSQL.", "numbers1"),
    numberedItem("Se retorna el resultado completo con snapshots actualizados de ambos personajes.", "numbers1"),
    new Paragraph({ children: [new PageBreak()] })
  );

  // ════════════════════════════════════════════════
  // SECCI\u00d3N 3 — SISTEMAS COMPLETADOS
  // ════════════════════════════════════════════════
  mainChildren.push(
    h1("3. Sistemas Completados e Implementados"),
    emptyLine(),

    // 3.1 STATS
    h2("3.1 Sistema de Estad\u00edsticas (Stats)"),
    pRuns([{ text: "Estado: ", bold: true }, statusBadge("COMPLETO"), { text: " \u2014 Funcional y extensivo" }]),
    h3("Descripci\u00f3n Funcional"),
    p("El sistema de stats define todas las propiedades num\u00e9ricas que afectan al personaje en combate y progresi\u00f3n. Con 93 tipos de estad\u00edsticas organizados en categor\u00edas sem\u00e1nticas, es el sistema m\u00e1s amplio del proyecto y la base sobre la que operan todos los dem\u00e1s sistemas."),
    h3("Implementaci\u00f3n T\u00e9cnica"),
    bulletItemBold("StatType.cs", "Enum con 93 valores organizados en: Recursos de Combate, Ofensivas, Defensivas, Recuperaci\u00f3n/Utilidad, Progresi\u00f3n, Aplicaci\u00f3n/Evasi\u00f3n de Estados, y Modificadores de Da\u00f1o."),
    bulletItemBold("StatCatalog.cs", "Cat\u00e1logo inmutable con metadatos por stat: descripci\u00f3n, categor\u00eda, tipo de valor, unidad, familia de comportamiento, tipo de uso, tipo de resoluci\u00f3n, restricciones, y notas de balance provisional."),
    bulletItemBold("StatsCalculator.cs", "Motor de agregaci\u00f3n que recolecta contribuciones de todas las fuentes IStatSource. Actualmente soporta solo contribuciones tipo Flat."),
    bulletItemBold("StatResolvers.cs", "Implementa 6 resolvers no lineales para stats que requieren procesamiento previo al uso en combate."),
    h3("F\u00f3rmulas y Configuraci\u00f3n"),
    p("Resolvers implementados con sus f\u00f3rmulas exactas:", { bold: true }),
    makeTable(
      ["Resolver", "F\u00f3rmula", "Par\u00e1metros"],
      [
        ["Mitigaci\u00f3n (Defense)", "ratio = cap * (raw / (raw + softness))", "cap=0.90, softness=100"],
        ["Hit Chance", "Curva no lineal con base, smoothing, min/max", "base=0.70, min=0.05, max=0.95, smooth=100"],
        ["Crit Chance", "critChance - critEvasion (clamped 0-1)", "Sustracci\u00f3n lineal directa"],
        ["Duraci\u00f3n CC", "baseMult - (tenacity * reductionPerTenacity)", "base=1.0, min/max configurables"],
        ["Da\u00f1o Modificado", "Mapea contextos a pares increase/reduction", "Por tipo de da\u00f1o/contexto"],
        ["Status Chance", "Resuelve apply vs evade para condiciones", "Por tipo de condici\u00f3n"],
      ],
      [2000, 4000, 3360]
    ),
    h3("Categor\u00edas de Stats (muestra)"),
    makeTable(
      ["Categor\u00eda", "Stats Incluidos", "Cantidad"],
      [
        ["Recursos de Combate", "MaxHp, MaxMana, HpRegen, ManaRegen, UltimateChargeMax", "5"],
        ["Ofensivas", "PhysicalAttack, MagicAttack, AttackSpeed, CritChance, CritDamage, Accuracy, etc.", "10"],
        ["Defensivas", "Defense, MagicResistance, Evasion, Tenacity", "4"],
        ["Aplicaci\u00f3n de Estados", "HeatApplyChance, ColdApplyChance, StunApplyChance, etc.", "18 (9 pares)"],
        ["Modificadores de Da\u00f1o", "BasicAttackDmgIncrease, SkillDmgIncrease, CritDmgIncrease, PvPDmgIncrease, etc.", "20+"],
        ["Progresi\u00f3n", "ExpGain, DropRate, DropQuality, GatheringSpeed, MeditationSpeed", "5"],
      ],
      [2400, 5160, 1800]
    ),
    h3("Observaciones T\u00e9cnicas"),
    bulletItem("Solo se soporta el tipo de contribuci\u00f3n Flat. Tipos multiplicativos y porcentuales est\u00e1n previstos pero no implementados."),
    bulletItem("Varias curvas de balance est\u00e1n marcadas como ProvisionalBalanceFormula, indicando que los valores exactos requieren ajuste con datos de playtesting."),
    bulletItem("El sistema est\u00e1 dise\u00f1ado para ser extensible: a\u00f1adir nuevos stats solo requiere a\u00f1adirlos al enum y al cat\u00e1logo."),
    new Paragraph({ children: [new PageBreak()] }),

    // 3.2 POWER SCORE
    h2("3.2 Sistema de Puntos de Poder (Power Score)"),
    pRuns([{ text: "Estado: ", bold: true }, statusBadge("COMPLETO"), { text: " \u2014 Motor de c\u00e1lculo completo con perfiles por clase" }]),
    h3("Descripci\u00f3n Funcional"),
    p("El Power Score (PP) es el indicador unificado de la fuerza de un personaje. A diferencia de otros MMORPGs donde los puntos de poder son una suma simple, WAR utiliza un sistema de valoraci\u00f3n contextual: cada stat tiene un peso diferente seg\u00fan la clase que lo use. Esto evita que 10 puntos de ATK f\u00edsico tengan el mismo valor para un Sorcerer (que apenas lo usa) que para un Bruiser (que depende de \u00e9l)."),
    h3("F\u00f3rmula de C\u00e1lculo"),
    codeBlock("ContribucionFinal = ValorActual * CantidadEfectiva(transform) * ValorUnitario * PesoDeClase * AjusteCategoria"),
    p("Donde:"),
    bulletItemBold("ValorActual", "El valor num\u00e9rico real del stat del personaje."),
    bulletItemBold("CantidadEfectiva", "Se aplica una de 10 transformaciones posibles: DirectFraction, RatioToReference, MitigationRatio, EffectiveHitChance, HitAvoidance, EffectiveCritChance, CCResistance, StatusApplyChance, StatusResistance, RecoveryAcceleration."),
    bulletItemBold("ValorUnitario", "Cu\u00e1nto vale en PP una unidad de ese stat. Ej: MaxHp=4.0, PhysicalAttack=9.0, Defense=120.0"),
    bulletItemBold("PesoDeClase", "Multiplicador 0.55x\u20131.60x seg\u00fan cu\u00e1nto usa esa clase el stat (determinado por an\u00e1lisis de kit de habilidades)."),
    bulletItemBold("AjusteCategor\u00eda", "Ajuste fino por categor\u00eda (ofensivo, defensivo, utilidad)."),
    h3("Pesos de Stats Clave"),
    makeTable(
      ["Stat", "Valor Unitario (PP)", "Valor Referencia", "Rango Peso Clase"],
      [
        ["MaxHp", "4.0", "-", "Neutral"],
        ["MaxMana", "Pendiente datos de skills", "-", "Neutral"],
        ["PhysicalAttack", "9.0", "-", "0.55x \u2013 1.60x"],
        ["MagicAttack", "9.0", "-", "0.55x \u2013 1.60x"],
        ["Defense", "120.0", "1 (referencia)", "Neutral"],
        ["MagicResistance", "120.0", "1 (referencia)", "Neutral"],
        ["Accuracy", "Contextual", "100", "Variable"],
        ["Evasion", "Contextual", "100", "Variable"],
        ["CritChance", "Contextual", "0.10 (CritEvasion ref)", "Variable"],
        ["AttackSpeed", "Pendiente", "-", "Variable"],
      ],
      [2400, 2200, 2200, 2560]
    ),
    h3("Perfiles de Clase Implementados"),
    bulletItemBold("Sorcerer", "Perfil completo con an\u00e1lisis de kit de 13 habilidades. Skills como Tempestad Drac\u00f3nica tienen peso 1.40x. B\u00e1sico aporta escalado m\u00e1gico."),
    bulletItemBold("Juramentada, Lancero, Bruiser", "Perfiles pendientes de cat\u00e1logo de skills. Usan peso neutral (1.0x) temporalmente."),
    h3("Observaciones"),
    bulletItem("El sistema es extremadamente sofisticado para un proyecto en desarrollo temprano."),
    bulletItem("Los pesos para MaxMana, ManaRegen y AttackRange est\u00e1n pendientes de datos completos de costos de habilidades."),
    bulletItem("Las cadenas de aplicaci\u00f3n de estados y sinergias de condiciones necesitan afinamiento autoritativo."),
    new Paragraph({ children: [new PageBreak()] }),

    // 3.3 PROGRESION
    h2("3.3 Sistema de Progresi\u00f3n de Nivel"),
    pRuns([{ text: "Estado: ", bold: true }, statusBadge("COMPLETO"), { text: " \u2014 Completamente funcional" }]),
    h3("Descripci\u00f3n Funcional"),
    p("Los personajes progresan del nivel 1 al 80 ganando experiencia. Cada nivel incrementa las estad\u00edsticas base seg\u00fan el perfil de crecimiento de la clase. La curva de XP usa crecimiento compuesto con multiplicadores de hito en niveles m\u00faltiplos de 10."),
    h3("Reglas de Nivel"),
    makeTable(
      ["Par\u00e1metro", "Valor", "Descripci\u00f3n"],
      [
        ["Nivel M\u00ednimo", "1", "Todos los personajes comienzan aqu\u00ed"],
        ["Nivel M\u00e1ximo", "80", "Tope de progresi\u00f3n"],
        ["XP Base", "1,000", "XP requerida para nivel 1\u21922"],
        ["Multiplicador Compuesto", "1.20x", "Cada nivel requiere 20% m\u00e1s XP"],
        ["Multiplicador de Hito", "1.50x", "Bonus adicional en niveles 10, 20, 30..."],
      ],
      [2800, 1600, 4960]
    ),
    h3("Tabla de XP (Primeros 20 Niveles)"),
    makeTable(
      ["Nivel", "XP Requerida", "XP Acumulada", "Hito"],
      [
        ["1\u21922", "1,000", "1,000", ""],
        ["2\u21923", "1,200", "2,200", ""],
        ["3\u21924", "1,440", "3,640", ""],
        ["4\u21925", "1,728", "5,368", ""],
        ["5\u21926", "2,074", "7,442", ""],
        ["6\u21927", "2,488", "9,930", ""],
        ["7\u21928", "2,986", "12,916", ""],
        ["8\u21929", "3,583", "16,499", ""],
        ["9\u219210", "4,300", "20,799", ""],
        ["10\u219211", "7,740", "28,539", "\u2B50 x1.50"],
        ["11\u219212", "6,192", "34,731", ""],
        ["12\u219213", "7,430", "42,162", ""],
        ["13\u219214", "8,916", "51,078", ""],
        ["14\u219215", "10,700", "61,778", ""],
        ["15\u219216", "12,839", "74,617", ""],
        ["16\u219217", "15,407", "90,025", ""],
        ["17\u219218", "18,489", "108,514", ""],
        ["18\u219219", "22,187", "130,700", ""],
        ["19\u219220", "26,624", "157,324", ""],
        ["20\u219221", "47,923", "205,247", "\u2B50 x1.50"],
      ],
      [1800, 2400, 2700, 2460]
    ),
    p("Nota: Los valores de XP se calculan con la f\u00f3rmula XP(n) = XP(n-1) * 1.20, con un multiplicador adicional de 1.50x en niveles m\u00faltiplos de 10.", { italic: true }),
    h3("Crecimiento de Stats por Clase (por nivel)"),
    makeTable(
      ["Stat", "Sorcerer", "Juramentada", "Lancero", "Bruiser"],
      [
        ["MaxHp", "+42", "+52", "+56", "+68"],
        ["MaxMana", "+30", "+26", "+20", "+16"],
        ["PhysicalAttack", "+4", "+8", "+13", "+11"],
        ["MagicAttack", "+14", "+10", "+5", "+4"],
        ["Defense", "+2", "+3", "+3", "+5"],
        ["MagicResistance", "+3", "+3", "+2", "+4"],
        ["HpRegen", "+0.8", "+1.0", "+1.0", "+1.2"],
        ["ManaRegen", "+1.5", "+1.2", "+0.9", "+0.8"],
      ],
      [2200, 1600, 1900, 1800, 1860]
    ),
    p("Los incrementos son lineales a partir del nivel 2 (nivel 1 = 0 stacks de crecimiento). Esto significa que un Sorcerer nivel 80 tendr\u00e1 79 * 42 = 3,318 HP adicionales solo por progresi\u00f3n de nivel."),
    new Paragraph({ children: [new PageBreak()] }),

    // 3.4 SKILLS
    h2("3.4 Sistema de Habilidades (Skills)"),
    pRuns([{ text: "Estado: ", bold: true }, statusBadge("PARCIAL"), { text: " \u2014 ~30% (Framework completo, solo Sorcerer implementado)" }]),
    h3("Descripci\u00f3n Funcional"),
    p("Cada clase tiene 13 habilidades (12 base + 1 ultimate). Las habilidades son completamente configurables con tipos de da\u00f1o, escalados, cooldowns, costos de recurso, efectos de estado, sinergias de condici\u00f3n, y un sistema de ascensi\u00f3n de 10 niveles que desbloquea mejoras progresivas."),
    h3("Reglas del Sistema"),
    makeTable(
      ["Regla", "Valor"],
      [
        ["Clases iniciales", "4"],
        ["Habilidades por clase", "13 (12 + 1 ultimate)"],
        ["Nivel m\u00ednimo de personaje", "1"],
        ["Nivel m\u00ednimo de ascensi\u00f3n", "1"],
        ["Nivel m\u00e1ximo de ascensi\u00f3n", "10"],
        ["Total de habilidades planeadas", "52 (4 clases x 13)"],
        ["Habilidades implementadas", "13 (solo Sorcerer)"],
      ],
      [5000, 4360]
    ),
    h3("Modelo de Habilidad"),
    p("Cada habilidad se define con:"),
    bulletItemBold("SkillActionType", "Damage, Heal, Utility"),
    bulletItemBold("SkillDamageType", "Physical, Magical, True"),
    bulletItemBold("SkillScalingType", "FixedOnly, PhysicalAttack, MagicAttack, TargetMissingHp"),
    bulletItemBold("SkillElementType", "Arcane, Fire, Lightning, Ice, Poison, Neutral"),
    bulletItemBold("SkillCombatRole", "Poke, Burst, Control, Area, Pressure, Detonation, Chain, MultiHit, Ultimate"),
    bulletItemBold("SkillTargetingPattern", "Self, SingleTarget, Area, Line, Cone, GroundPoint"),
    h3("Cat\u00e1logo del Sorcerer (13 habilidades)"),
    makeTable(
      ["Skill", "Slot", "Nivel", "Tipo", "Escalado", "CD", "Costo", "Elemento"],
      [
        ["Chispa \u00cdgnea", "1", "1", "Damage", "110% MagAtk", "4s", "18 mana", "Fire"],
        ["Orbe Voltaico", "2", "4", "Damage", "122% MagAtk", "6s", "22 mana", "Lightning"],
        ["Pulso G\u00e9lido", "3", "7", "Damage/CC", "~100% MagAtk", "5s", "20 mana", "Ice"],
        ["Cadenas Arcanas", "4", "10", "Control", "Variable", "8s", "25 mana", "Arcane"],
        ["Llama Interior", "5", "14", "Buff/Heal", "Self-buff", "12s", "30 mana", "Fire"],
        ["Rel\u00e1mpago Bifurcado", "6", "18", "Damage", "Chain", "7s", "28 mana", "Lightning"],
        ["Ventisca", "7", "22", "AoE/CC", "Area", "10s", "35 mana", "Ice"],
        ["Escudo Arcano", "8", "26", "Shield", "Protection", "15s", "40 mana", "Arcane"],
        ["Ignici\u00f3n", "9", "30", "Detonation", "Synergy", "9s", "32 mana", "Fire"],
        ["Tormenta El\u00e9ctrica", "10", "35", "AoE", "Multi-hit", "11s", "38 mana", "Lightning"],
        ["Criog\u00e9nesis", "11", "40", "Burst/CC", "Burst", "13s", "42 mana", "Ice"],
        ["Tormenta Fractal", "12", "45", "AoE", "Sustained", "14s", "45 mana", "Arcane"],
        ["Tempestad Drac\u00f3nica", "Ult", "50", "Ultimate", "680-3965%", "Largo", "Ultimate", "Multi"],
      ],
      [1600, 600, 700, 1100, 1300, 600, 1000, 900]
    ),
    p("Nota: Los slots 3-12 tienen datos parcialmente inferidos de la estructura del cat\u00e1logo. Los slots 1, 2 y Ultimate tienen datos completamente verificados del c\u00f3digo.", { italic: true }),
    h3("Sistema de Ascensi\u00f3n"),
    p("Cada habilidad tiene 10 niveles de ascensi\u00f3n con progresiones predefinidas:"),
    makeTable(
      ["Progresi\u00f3n", "A1", "A3", "A5", "A7", "A9", "Uso"],
      [
        ["Standard", "1.08x", "1.25x", "1.46x", "1.71x", "2.00x", "Habilidades de da\u00f1o est\u00e1ndar"],
        ["Burst", "1.10x", "1.31x", "1.56x", "1.85x", "2.18x", "Habilidades de alto impacto"],
        ["MultiHit", "1.07x", "1.22x", "1.40x", "1.60x", "2.00x", "Habilidades multi-golpe"],
        ["Control", "1.07x", "1.22x", "1.39x", "1.58x", "1.92x", "Habilidades de control"],
      ],
      [1800, 1000, 1000, 1000, 1000, 1000, 2560]
    ),
    p("Las ascensiones 5, 8 y 10 t\u00edpicamente desbloquean nuevos efectos (aplicaci\u00f3n de estados, mejora de duraci\u00f3n, incremento de probabilidad)."),
    new Paragraph({ children: [new PageBreak()] }),

    // 3.5 COMBAT
    h2("3.5 Sistema de Combate"),
    pRuns([{ text: "Estado: ", bold: true }, statusBadge("COMPLETO"), { text: " \u2014 Pipeline de resoluci\u00f3n completo" }]),
    h3("Descripci\u00f3n Funcional"),
    p("El sistema de combate resuelve cada acci\u00f3n (ataque b\u00e1sico o habilidad) a trav\u00e9s de un pipeline de 11 etapas. Soporta da\u00f1o f\u00edsico, m\u00e1gico y verdadero, verificaciones de golpe y cr\u00edtico, aplicaci\u00f3n de estados y crowd control, protecci\u00f3n e invulnerabilidad, y proyecci\u00f3n de recursos."),
    h3("Pipeline de Resoluci\u00f3n (11 Etapas)"),
    numberedItem("Resoluci\u00f3n de magnitud base (fija + escalado por stat)", "numbers2"),
    numberedItem("Validaci\u00f3n de recursos (verificar mana/ultimate suficiente)", "numbers2"),
    numberedItem("Verificaci\u00f3n de golpe (Accuracy vs Evasion)", "numbers2"),
    numberedItem("Verificaci\u00f3n de cr\u00edtico (CritChance vs CriticalEvasion)", "numbers2"),
    numberedItem("C\u00e1lculo de da\u00f1o/curaci\u00f3n final", "numbers2"),
    numberedItem("Aplicaci\u00f3n de mitigaciones (Defense, MagicResistance)", "numbers2"),
    numberedItem("Evaluaci\u00f3n de protecciones activas (shields, invulnerabilidad)", "numbers2"),
    numberedItem("Aplicaci\u00f3n de condiciones (Heat, Cold, Stun, Freeze, etc.)", "numbers2"),
    numberedItem("Resoluci\u00f3n de interacciones entre condiciones (sinergias elementales)", "numbers2"),
    numberedItem("Proyecci\u00f3n de cambios de recursos (HP, Mana, UltimateCharge)", "numbers2"),
    numberedItem("Generaci\u00f3n de resultado con auditor\u00eda completa", "numbers2"),
    h3("Sistema de Condiciones"),
    makeTable(
      ["Condici\u00f3n", "Categor\u00eda", "Efecto", "Reducida por Tenacity"],
      [
        ["Heat", "Estado", "Sinergia con Cold/Detonation", "No"],
        ["Cold", "Estado", "Sinergia con Heat/Freeze", "No"],
        ["Electrified", "Estado", "Sinergia con otros elementales", "No"],
        ["Poison", "Estado", "Da\u00f1o continuo", "No"],
        ["Weaken", "Crowd Control", "Reduce stats ofensivos", "S\u00ed"],
        ["Blind", "Crowd Control", "Reduce precisi\u00f3n", "S\u00ed"],
        ["Stun", "Crowd Control", "Impide acciones", "S\u00ed"],
        ["Freeze", "Crowd Control", "Impide movimiento y acciones", "S\u00ed"],
        ["Paralyze", "Crowd Control", "Impide movimiento", "S\u00ed"],
      ],
      [1800, 1800, 3200, 2560]
    ),
    h3("Sistema de Combos (Ataques B\u00e1sicos)"),
    makeTable(
      ["Par\u00e1metro", "Valor"],
      [
        ["Etapas del combo", "6"],
        ["Ventana de continuaci\u00f3n", "2 segundos"],
        ["Crecimiento por etapa", "1.5% secuencial"],
        ["Tipo de da\u00f1o", "Clase-dependiente (Sorcerer: M\u00e1gico, Bruiser: F\u00edsico)"],
        ["Escalado", "Dual-stat (PhysicalAttack + MagicAttack con coeficientes por clase)"],
      ],
      [4000, 5360]
    ),
    new Paragraph({ children: [new PageBreak()] }),

    // 3.6 RESOURCES
    h2("3.6 Sistema de Recursos"),
    pRuns([{ text: "Estado: ", bold: true }, statusBadge("COMPLETO"), { text: "" }]),
    p("Gestiona tres recursos principales del personaje: HP (salud), Mana (costo de habilidades) y UltimateCharge (recurso para habilidad ultimate). Incluye validaci\u00f3n de disponibilidad, normalizaci\u00f3n de valores negativos y seguimiento de transacciones."),
    makeTable(
      ["Recurso", "Stat M\u00e1ximo", "Regeneraci\u00f3n", "Uso Principal"],
      [
        ["HP", "MaxHp", "HpRegen", "Salud del personaje"],
        ["Mana", "MaxMana", "ManaRegen", "Costo de habilidades normales"],
        ["UltimateCharge", "UltimateChargeMax", "Generado en combate", "Costo de habilidad ultimate"],
      ],
      [2000, 2400, 2400, 2560]
    ),
    emptyLine(),

    // 3.7 PERSISTENCIA
    h2("3.7 Sistema de Persistencia"),
    pRuns([{ text: "Estado: ", bold: true }, statusBadge("COMPLETO"), { text: " \u2014 4 migraciones, esquema robusto" }]),
    p("Entity Framework Core con PostgreSQL. El esquema incluye tablas para personajes, progreso de habilidades, registros administrativos de skills, recursos del juego, balances de recursos, transacciones de recursos, definiciones de items, instancias de items, contenedores de inventario y slots de inventario."),
    h3("Tablas Principales"),
    makeTable(
      ["Tabla", "Prop\u00f3sito", "Campos Clave"],
      [
        ["characters", "Datos del personaje", "Id, Name, ClassType, Level, XP, HP, Mana, UltCharge, ComboState"],
        ["character_skill_progress", "Progreso de habilidades", "CharacterId, SkillId, IsUnlocked, AscensionLevel"],
        ["admin_skill_records", "Skills del panel admin", "Id, Draft/Published JSON, Estado, Versi\u00f3n, IsDeleted"],
        ["character_resource_balances", "Saldos de recursos", "CharacterId, ResourceType, CurrentAmount"],
        ["character_resource_transactions", "Historial de transacciones", "Id, CorrelationId, Raz\u00f3n, Before/After, Timestamp"],
        ["item_definitions", "Definiciones de items", "Plantillas de objetos del juego"],
        ["item_instances", "Instancias de items", "Items concretos de jugadores"],
        ["inventory_slots", "Slots de inventario", "Ubicaci\u00f3n f\u00edsica de items"],
      ],
      [2800, 2600, 3960]
    ),
    emptyLine(),

    // 3.8 API REST
    h2("3.8 API REST"),
    pRuns([{ text: "Estado: ", bold: true }, statusBadge("COMPLETO"), { text: " \u2014 Endpoints funcionales con Swagger" }]),
    makeTable(
      ["Endpoint", "M\u00e9todo", "Prop\u00f3sito"],
      [
        ["/api/characters/demo/snapshot", "GET", "Snapshot del personaje demo principal"],
        ["/api/characters/demo/a/snapshot", "GET", "Snapshot del combatiente A"],
        ["/api/characters/demo/b/snapshot", "GET", "Snapshot del combatiente B"],
        ["/api/characters/demo/combatants", "GET", "Ambos combatientes + comandos disponibles"],
        ["/api/combat/demo/execute", "POST", "Ejecutar acci\u00f3n de combate"],
        ["/api/combat/demo/reset", "POST", "Resetear demo a estado inicial"],
        ["/api/admin/skills/overview", "GET", "Vista general de todas las skills"],
        ["/api/admin/skills/{id}", "GET/PUT", "CRUD de skill individual"],
        ["/api/admin/skills", "POST", "Crear nueva skill"],
        ["/api/admin/skills/{id}/publish", "POST", "Publicar skill al runtime"],
      ],
      [3200, 1000, 5160]
    ),
    emptyLine(),

    // 3.9 PANEL ADMIN
    h2("3.9 Panel de Administraci\u00f3n de Skills"),
    pRuns([{ text: "Estado: ", bold: true }, statusBadge("COMPLETO"), { text: " \u2014 CRUD completo con flujo draft/publish" }]),
    p("Interfaz web para crear, editar y publicar habilidades sin modificar c\u00f3digo. Soporta borrado l\u00f3gico, versionado, validaci\u00f3n antes de publicaci\u00f3n, y composici\u00f3n runtime que fusiona skills programadas en c\u00f3digo con las publicadas desde la base de datos."),
    emptyLine(),

    // 3.10 FRONTEND DEMO
    h2("3.10 Frontend de Demo de Combate"),
    pRuns([{ text: "Estado: ", bold: true }, statusBadge("COMPLETO"), { text: " \u2014 SPA funcional con UI pulida" }]),
    p("Aplicaci\u00f3n web single-page con HTML/CSS/JS vanilla que muestra un duelo entre dos Sorcerers nivel 10. Incluye: cards de combatientes, consola de comandos, log de combate con historial, panel de informaci\u00f3n detallada (stats, recursos, power score, progreso, combo), y refresco autom\u00e1tico de estado. El CSS usa glass-morphism, animaciones, y un sistema de componentes completo."),
    new Paragraph({ children: [new PageBreak()] })
  );

  // ════════════════════════════════════════════════
  // SECCI\u00d3N 4 — SISTEMAS PENDIENTES
  // ════════════════════════════════════════════════
  mainChildren.push(
    h1("4. Sistemas Pendientes de Implementaci\u00f3n"),
    emptyLine(),

    // 4.1
    h2("4.1 Cat\u00e1logos de Habilidades (Juramentada, Lancero, Bruiser)"),
    pRuns([{ text: "Estado: ", bold: true }, statusBadge("PENDIENTE"), { text: "" }]),
    h3("Evidencia en el C\u00f3digo"),
    p("SkillCatalogRegistry registra expl\u00edcitamente las clases Juramentada, Lancero y Bruiser con cat\u00e1logos vac\u00edos. ClassLevelGrowthProfiles define perfiles de crecimiento para las 4 clases. PowerScoreClassProfileRegistry tiene perfiles placeholder con peso neutral."),
    h3("Propuesta de Implementaci\u00f3n"),
    p("Replicar la estructura de SorcererSkillCatalog para cada clase. Definir 13 habilidades con identidad \u00fanica por clase:"),
    bulletItemBold("Juramentada", "Clase h\u00edbrida m\u00e1gica/f\u00edsica. Habilidades de espada arcana, buffs de aliados, escudos sagrados. Escalado dual."),
    bulletItemBold("Lancero", "Clase de movilidad y burst f\u00edsico. Habilidades de carga, embestida, ataques en l\u00ednea. Escalado f\u00edsico alto."),
    bulletItemBold("Bruiser", "Tanque con da\u00f1o sostenido. Habilidades de CC, resistencia, provocaci\u00f3n, regeneraci\u00f3n. Escalado f\u00edsico con alta HP."),
    h3("Esfuerzo Estimado"),
    p("Medio-Alto (4-6 semanas por clase, 12-18 semanas total). Incluye dise\u00f1o, implementaci\u00f3n, balance y ajuste de Power Score."),
    pRuns([{ text: "Prioridad: ", bold: true }, { text: "CR\u00cdTICA", bold: true, color: RED_ACCENT }, { text: " \u2014 Sin las 3 clases restantes no hay diversidad de gameplay." }]),
    emptyLine(),

    // 4.2
    h2("4.2 Sistema de Equipamiento Avanzado"),
    pRuns([{ text: "Estado: ", bold: true }, statusBadge("PENDIENTE"), { text: "" }]),
    h3("Evidencia en el C\u00f3digo"),
    p("Las tablas item_definitions, item_instances e inventory_slots existen en el esquema de base de datos, indicando que la infraestructura de persistencia est\u00e1 preparada. Sin embargo, no existe l\u00f3gica de dominio para equipamiento en War.Core."),
    h3("Propuesta de Implementaci\u00f3n"),
    p("7 slots de equipamiento: casco, pechera, vestido, brazaletes, collar, aretes, arma. Cada pieza a\u00f1ade stats como IStatSource. Sistema de mejora (+1 a +15) con probabilidades decrecientes. Raridades: Com\u00fan, Raro, \u00c9pico, Legendario, M\u00edtico."),
    h3("Esfuerzo Estimado"),
    p("Alto (6-8 semanas). Incluye modelos de dominio, EquipmentStatSource, UI de inventario, sistema de mejora, dropeo."),
    pRuns([{ text: "Prioridad: ", bold: true }, { text: "CR\u00cdTICA", bold: true, color: RED_ACCENT }, { text: " \u2014 Core loop de progresi\u00f3n." }]),
    emptyLine(),

    // 4.3
    h2("4.3 Sistema de Esp\u00edritus"),
    pRuns([{ text: "Estado: ", bold: true }, statusBadge("PROPUESTO"), { text: "" }]),
    p("No existe referencia directa en el c\u00f3digo, pero el stat MeditationSpeed y el dise\u00f1o de la arquitectura (m\u00faltiples IStatSource) sugieren que est\u00e1 planificado."),
    h3("Propuesta de Implementaci\u00f3n"),
    p("Entidades acompa\u00f1antes que otorgan stats pasivos y habilidades pasivas \u00fanicas. Cada esp\u00edritu tendr\u00eda: stats base, pasiva \u00fanica (ej: +15% da\u00f1o de fuego si enemigo tiene Heat), nivel de v\u00ednculo que escala stats, y condiciones de activaci\u00f3n ligadas al sistema de cultivaci\u00f3n."),
    p("Esfuerzo: Alto (8-10 semanas). Prioridad: Alta."),
    emptyLine(),

    // 4.4
    h2("4.4 Sistema de Cultivaci\u00f3n"),
    pRuns([{ text: "Estado: ", bold: true }, statusBadge("PROPUESTO"), { text: "" }]),
    p("El stat MeditationSpeed en StatType sugiere una mec\u00e1nica de meditaci\u00f3n/cultivaci\u00f3n planificada. No hay implementaci\u00f3n en el c\u00f3digo."),
    h3("Propuesta de Implementaci\u00f3n"),
    p("Sistema de reinos espirituales (ej: Mortal \u2192 Condensaci\u00f3n \u2192 Fundaci\u00f3n \u2192 N\u00facleo \u2192 Nascente \u2192 Soberano). Cada reino desbloquea: multiplicadores de stats base, slots de esp\u00edritu adicionales, habilidades pasivas, y acceso a contenido de alto nivel. La progresi\u00f3n usa MeditationSpeed para determinar velocidad de avance."),
    p("Esfuerzo: Alto (8-10 semanas). Prioridad: Alta."),
    emptyLine(),

    // 4.5
    h2("4.5 Sistema de Clanes y Guerras"),
    pRuns([{ text: "Estado: ", bold: true }, statusBadge("PROPUESTO"), { text: "" }]),
    p("No existe implementaci\u00f3n. Es un pilar del concepto MMORPG del juego."),
    h3("Propuesta"),
    p("Clanes con jerarqu\u00eda (L\u00edder, Oficiales, Miembros), sistema de territorios, guerras programadas con matchmaking por Power Score total del clan, sistema de alianzas, chat de clan, buff de clan compartido, ranking de clanes."),
    p("Esfuerzo: Muy Alto (3+ meses). Prioridad: Alta. Dependencias: Networking, Autenticaci\u00f3n, Matchmaking."),
    emptyLine(),

    // 4.6
    h2("4.6 Sistema de Networking/Multiplayer"),
    pRuns([{ text: "Estado: ", bold: true }, statusBadge("PROPUESTO"), { text: "" }]),
    p("El proyecto actual es un backend API REST sin protocolo de comunicaci\u00f3n en tiempo real. Para un MMORPG funcional se necesita un sistema de networking dedicado."),
    h3("Propuesta"),
    p("WebSocket o SignalR para comunicaci\u00f3n bidireccional. Implementar: sincronizaci\u00f3n de estado del mundo, interpolaci\u00f3n de movimiento, predicci\u00f3n del lado cliente, resoluci\u00f3n de conflictos autoritativa en servidor, y gesti\u00f3n de latencia."),
    p("Esfuerzo: Muy Alto (4+ meses). Prioridad: CR\u00cdTICA. Sin esto no hay MMORPG funcional."),
    emptyLine(),

    // 4.7
    h2("4.7 Sistema de Autenticaci\u00f3n y Cuentas"),
    pRuns([{ text: "Estado: ", bold: true }, statusBadge("PROPUESTO"), { text: "" }]),
    p("No existe autenticaci\u00f3n. Los endpoints son p\u00fablicos."),
    p("Propuesta: JWT + OAuth2, registro/login, gesti\u00f3n de sesiones, protecci\u00f3n anti-trampas, rate limiting."),
    p("Esfuerzo: Medio (4-6 semanas). Prioridad: CR\u00cdTICA."),
    emptyLine(),

    // 4.8 - 4.12 compactos
    h2("4.8 Sistemas Adicionales Pendientes"),
    makeTable(
      ["Sistema", "Estado", "Descripci\u00f3n", "Esfuerzo", "Prioridad"],
      [
        ["Misiones/Quests", "\uD83D\uDCA1 Propuesto", "Sistema de misiones principales, secundarias y diarias", "Alto (8 sem)", "Alta"],
        ["Dungeons/Instancias", "\uD83D\uDCA1 Propuesto", "Contenido PvE instanciado con bosses y recompensas", "Muy Alto (3+ meses)", "Alta"],
        ["Matchmaking PvP", "\uD83D\uDCA1 Propuesto", "Emparejamiento por PP, ranking ELO, modos competitivos", "Alto (6-8 sem)", "Media"],
        ["Crafting", "\uD83D\uDCA1 Propuesto", "Fabricaci\u00f3n de equipamiento y consumibles", "Medio (4-6 sem)", "Media"],
        ["Mercado/Trading", "\uD83D\uDCA1 Propuesto", "Comercio entre jugadores, subasta, econom\u00eda", "Alto (6-8 sem)", "Media"],
        ["Chat", "\uD83D\uDCA1 Propuesto", "Chat global, de clan, whisper, grupo", "Medio (3-4 sem)", "Alta"],
        ["Ranking/Leaderboard", "\uD83D\uDCA1 Propuesto", "Clasificaciones por PP, PvP, clan", "Bajo (2-3 sem)", "Media"],
        ["Audio", "\uD83D\uDCA1 Propuesto", "M\u00fasica, efectos de sonido, ambient", "Medio (4-6 sem)", "Baja"],
      ],
      [1600, 1200, 3200, 1400, 960]
    ),
    new Paragraph({ children: [new PageBreak()] })
  );

  // ════════════════════════════════════════════════
  // SECCI\u00d3N 5 — DEUDA T\u00c9CNICA
  // ════════════════════════════════════════════════
  mainChildren.push(
    h1("5. Deuda T\u00e9cnica y Recomendaciones"),
    emptyLine(),
    h2("5.1 Estado de Deuda T\u00e9cnica"),
    p("El proyecto presenta un nivel de deuda t\u00e9cnica sorprendentemente bajo para su etapa de desarrollo. No se encontraron TODOs, FIXMEs ni HACKs en el c\u00f3digo fuente. Sin embargo, el sistema de Power Score y Skills utiliza un mecanismo formal de PendingDatum para rastrear datos incompletos, lo cual es una pr\u00e1ctica ejemplar."),
    h3("Datos Pendientes Rastreados Formalmente"),
    bulletItem("Tempestad Drac\u00f3nica (Ultimate Sorcerer): 8+ datos pendientes incluyendo targeting, cooldown definitivo, costo de recurso, probabilidades de CC, y timings de auto-curaci\u00f3n."),
    bulletItem("PowerScore: Pesos de MaxMana, ManaRegen y AttackRange pendientes de datos completos de costos de skills."),
    bulletItem("Stats con ProvisionalBalanceFormula: Defense, Evasion, Accuracy requieren afinamiento con datos de playtesting."),
    bulletItem("Sinergias de condiciones y cadenas de aplicaci\u00f3n de estados necesitan tuning autoritativo."),
    emptyLine(),
    h2("5.2 Problemas de Arquitectura Detectados"),
    bulletItemBold("Sin tipos de contribuci\u00f3n m\u00e1s all\u00e1 de Flat", "StatsCalculator solo agrega contribuciones aditivas. Para un sistema maduro, se necesitan contribuciones multiplicativas (% increase) y caps."),
    bulletItemBold("Credenciales en appsettings.json", "La cadena de conexi\u00f3n incluye contrase\u00f1a en texto plano. Debe migrarse a User Secrets o Azure Key Vault."),
    bulletItemBold("Sin capa de caching", "Todos los snapshots de personaje se recalculan en cada petici\u00f3n. Para m\u00faltiples usuarios simult\u00e1neos se necesita caching."),
    bulletItemBold("Class1.cs placeholder", "Tanto War.Core como War.Infrastructure contienen archivos Class1.cs vac\u00edos generados por el template de proyecto."),
    emptyLine(),
    h2("5.3 Recomendaciones Priorizadas"),
    h3("Prioridad Inmediata"),
    numberedItem("Mover credenciales de DB a User Secrets o variables de entorno.", "numbers3"),
    numberedItem("Eliminar archivos Class1.cs placeholder.", "numbers3"),
    numberedItem("Implementar tipos de contribuci\u00f3n multiplicativos en StatsCalculator.", "numbers3"),
    numberedItem("A\u00f1adir unit tests para los resolvers de combate y calculadoras de Power Score.", "numbers3"),
    h3("Prioridad Alta"),
    numberedItem("Implementar capa de caching para snapshots de personaje (Redis o MemoryCache).", "numbers4"),
    numberedItem("A\u00f1adir middleware de logging estructurado (Serilog) para auditor\u00eda.", "numbers4"),
    numberedItem("Implementar health checks para la base de datos.", "numbers4"),
    numberedItem("A\u00f1adir validaci\u00f3n de input con FluentValidation en los controllers.", "numbers4"),
    h3("Prioridad Media"),
    numberedItem("Implementar rate limiting en endpoints p\u00fablicos.", "numbers5"),
    numberedItem("A\u00f1adir integration tests con TestContainers (PostgreSQL en Docker).", "numbers5"),
    numberedItem("Configurar CI/CD pipeline (GitHub Actions).", "numbers5"),
    numberedItem("Documentar API con XML comments para Swagger.", "numbers5"),
    h3("Recomendaciones de Seguridad"),
    bulletItem("Implementar autenticaci\u00f3n JWT antes de exponer endpoints a produccion."),
    bulletItem("A\u00f1adir CORS restrictivo (actualmente permite todo)."),
    bulletItem("Implementar anti-cheat: validaci\u00f3n server-side de todas las acciones de combate (ya parcialmente implementado en el pipeline)."),
    bulletItem("Rate limiting por IP y por cuenta para prevenir abuso."),
    bulletItem("Encriptar datos sensibles del jugador en la base de datos."),
    new Paragraph({ children: [new PageBreak()] })
  );

  // ════════════════════════════════════════════════
  // SECCI\u00d3N 6 — MIGRACI\u00d3N A M\u00d3VILES
  // ════════════════════════════════════════════════
  mainChildren.push(
    h1("6. Estrategia de Migraci\u00f3n a M\u00f3viles"),
    emptyLine(),
    h2("6.1 An\u00e1lisis de Viabilidad"),
    p("El proyecto WAR utiliza C# con .NET 8 y ASP.NET Core como backend, con un frontend de demo en HTML/CSS/JS vanilla. Esta arquitectura presenta ventajas significativas para la migraci\u00f3n a m\u00f3viles:"),
    h3("Componentes Portables"),
    bulletItemBold("Backend completo (War.Core + War.Infrastructure + War.Api)", "100% portable. El servidor es independiente del cliente y puede servir a cualquier plataforma."),
    bulletItemBold("L\u00f3gica de dominio (War.Core)", "C# puro sin dependencias de UI. Puede compartirse con un cliente mobile via Xamarin/MAUI o compilarse a librer\u00eda nativa."),
    bulletItemBold("API REST", "Los endpoints actuales pueden ser consumidos directamente por cualquier cliente m\u00f3vil."),
    h3("Componentes que Requieren Reescritura"),
    bulletItemBold("Frontend de demo", "El HTML/CSS/JS actual es solo una demo, no un cliente de juego. Se necesita un cliente completo."),
    bulletItemBold("Renderizado de juego", "No existe. Se necesita un motor gr\u00e1fico completo."),
    bulletItemBold("Input handling", "Se necesita sistema de controles t\u00e1ctiles."),
    bulletItemBold("Networking en tiempo real", "WebSocket/SignalR para mobile con manejo de reconexiones."),
    emptyLine(),
    h2("6.2 Opciones Estrat\u00e9gicas"),
    emptyLine()
  );

  // Options table
  const options = [
    ["1. Port Nativo", "Clientes nativos iOS (Swift/Metal) y Android (Kotlin/Vulkan)", "M\u00e1ximo rendimiento, UX nativa", "Doble codebase, doble equipo, costo 2x", "Muy Alto (12+ meses)", "$$$$$"],
    ["2. Engine Multiplataforma (Unity)", "Un codebase C# en Unity para PC, iOS, Android", "C# nativo (compatible con War.Core), ecosistema maduro, un equipo", "Licencias, peso del engine, curva de aprendizaje Unity", "Alto (8-10 meses)", "$$$"],
    ["3. Engine Multiplataforma (Godot)", "Godot con GDScript/C# para todas las plataformas", "Open source, ligero, soporte C#", "Ecosistema m\u00e1s peque\u00f1o, menos recursos MMORPG", "Alto (8-10 meses)", "$$"],
    ["4. Cliente H\u00edbrido", "Cliente PC completo + cliente mobile ligero (misma API)", "Cada plataforma optimizada", "Dos clientes para mantener", "Muy Alto (14+ meses)", "$$$$"],
    ["5. Cloud Gaming", "Streaming del juego PC a mobile", "Sin desarrollo de cliente mobile", "Latencia, costo de servidores, dependencia de conexi\u00f3n", "Bajo (2-3 meses)", "$$ (servidor alto)"],
    ["6. PWA", "Aplicaci\u00f3n web progresiva optimizada", "Un codebase web, sin App Store", "Rendimiento limitado, sin acceso nativo completo", "Medio (5-6 meses)", "$"],
  ];
  options.forEach(opt => {
    mainChildren.push(
      h3(opt[0]),
      pRuns([{ text: "Descripci\u00f3n: ", bold: true }, { text: opt[1] }]),
      pRuns([{ text: "Pros: ", bold: true, color: "27AE60" }, { text: opt[2] }]),
      pRuns([{ text: "Contras: ", bold: true, color: RED_ACCENT }, { text: opt[3] }]),
      pRuns([{ text: "Esfuerzo: ", bold: true }, { text: opt[4] + " | Costo relativo: " + opt[5] }]),
      emptyLine()
    );
  });

  mainChildren.push(
    h2("6.3 Recomendaci\u00f3n Principal: Unity (Opci\u00f3n 2)"),
    p("Basado en el an\u00e1lisis del c\u00f3digo real del proyecto WAR, la recomendaci\u00f3n es migrar a Unity como engine multiplataforma. Las razones son:", { bold: true }),
    numberedItem("Compatibilidad directa con C#: War.Core (toda la l\u00f3gica de dominio) puede importarse directamente como librer\u00eda en Unity sin reescritura.", "numbers6"),
    numberedItem("Un solo codebase para PC, iOS y Android: Reduce costos de desarrollo y mantenimiento a la mitad vs port nativo.", "numbers6"),
    numberedItem("Ecosistema MMORPG maduro: Mirror/Fishnet para networking, asset store para UI, herramientas de optimizaci\u00f3n m\u00f3vil.", "numbers6"),
    numberedItem("El backend actual (War.Api + War.Infrastructure) permanece intacto: Solo se reemplaza el frontend de demo por un cliente Unity.", "numbers6"),
    numberedItem("Equipo de C#: El equipo actual no necesita aprender un lenguaje nuevo.", "numbers6"),
    emptyLine(),
    h3("Estimaci\u00f3n de Esfuerzo"),
    makeTable(
      ["Fase", "Duraci\u00f3n", "Equipo"],
      [
        ["Setup Unity + importar War.Core", "2-3 semanas", "1 dev senior"],
        ["Sistema de renderizado y UI base", "6-8 semanas", "2-3 devs"],
        ["Controles t\u00e1ctiles + adaptaci\u00f3n mobile", "3-4 semanas", "1-2 devs"],
        ["Networking (Mirror/Fishnet)", "6-8 semanas", "1-2 devs networking"],
        ["Integraci\u00f3n completa con backend", "4-6 semanas", "2 devs"],
        ["Optimizaci\u00f3n mobile", "4-6 semanas", "1-2 devs"],
        ["QA y beta mobile", "4-6 semanas", "Equipo completo"],
        ["Total estimado", "8-10 meses", "Equipo de 3-5 personas"],
      ],
      [4000, 2500, 2860]
    ),
    emptyLine(),
    h2("6.4 Plan de Implementaci\u00f3n Mobile"),
    h3("Adaptaciones de UI/UX"),
    bulletItem("Reemplazar click/hover por tap/swipe/pinch."),
    bulletItem("Bot\u00f3n virtual de joystick para movimiento."),
    bulletItem("Botones de habilidades en arc inferior (estilo MOBA m\u00f3vil)."),
    bulletItem("Redise\u00f1ar paneles de inventario/stats para pantalla vertical."),
    bulletItem("Auto-target opcional para facilitar combate t\u00e1ctil."),
    h3("Optimizaciones de Performance"),
    bulletItem("LOD (Level of Detail) para modelos 3D en dispositivos de gama baja."),
    bulletItem("Compresi\u00f3n de texturas (ASTC para Android, PVRTC para iOS)."),
    bulletItem("Object pooling para entidades de combate y efectos visuales."),
    bulletItem("Reducci\u00f3n de draw calls con batching y atlasing."),
    bulletItem("Battery management: reducir FPS en background, limitar efectos de part\u00edculas."),
    h3("Networking M\u00f3vil"),
    bulletItem("Reconexi\u00f3n autom\u00e1tica transparente al cambiar de WiFi a datos m\u00f3viles."),
    bulletItem("Compresi\u00f3n de paquetes para reducir consumo de datos."),
    bulletItem("Modo offline parcial para inventario y configuraci\u00f3n."),
    bulletItem("Predicci\u00f3n agresiva del lado cliente para compensar latencia variable."),
    h3("Distribuci\u00f3n"),
    bulletItem("App Store (iOS): Requiere Apple Developer Account ($99/a\u00f1o), revisi\u00f3n de guidelines de IAP."),
    bulletItem("Google Play (Android): Requiere Google Play Console ($25 una vez), pol\u00edticas de monetizaci\u00f3n."),
    bulletItem("Hot updates v\u00eda asset bundles para contenido sin pasar por revisi\u00f3n de stores."),
    emptyLine(),
    h2("6.5 Arquitectura de Servidor Compartido"),
    p("El backend actual (War.Api) ya est\u00e1 dise\u00f1ado como API REST independiente del cliente, lo cual facilita enormemente servir a m\u00faltiples plataformas simult\u00e1neamente."),
    h3("Consideraciones Clave"),
    bulletItemBold("Versionado de API", "Implementar /api/v1/, /api/v2/ para soportar clientes mobile y PC con diferentes versiones."),
    bulletItemBold("Cross-platform matchmaking", "Recomendaci\u00f3n: separar pools de PvP (mobile vs PC) por diferencias de input, pero unificar PvE y features sociales."),
    bulletItemBold("Sincronizaci\u00f3n de cuentas", "Una cuenta = un personaje accesible desde cualquier dispositivo. Implementar bloqueo de sesi\u00f3n para evitar login simult\u00e1neo."),
    bulletItemBold("Escalabilidad", "Considerar migrar a microservicios cuando la base de jugadores lo justifique: servicio de combate, servicio de inventario, servicio de chat como servicios independientes."),
    new Paragraph({ children: [new PageBreak()] })
  );

  // ════════════════════════════════════════════════
  // SECCI\u00d3N 7 — ROADMAP
  // ════════════════════════════════════════════════
  mainChildren.push(
    h1("7. Roadmap T\u00e9cnico Consolidado"),
    p("Basado en el estado real del proyecto, se propone el siguiente roadmap realista para llevar WAR de su estado actual a un lanzamiento multiplataforma:"),
    emptyLine(),
    h2("Fase 1 \u2014 Estabilizaci\u00f3n (Mes 1-2)"),
    pRuns([{ text: "Objetivo: ", bold: true }, { text: "Completar los sistemas parciales y resolver deuda t\u00e9cnica cr\u00edtica." }]),
    bulletItem("Completar cat\u00e1logos de habilidades para Juramentada, Lancero y Bruiser (39 habilidades)."),
    bulletItem("Implementar tipos de contribuci\u00f3n multiplicativos en StatsCalculator."),
    bulletItem("Mover credenciales a User Secrets. Eliminar Class1.cs."),
    bulletItem("A\u00f1adir unit tests para resolvers de combate y PowerScore."),
    bulletItem("Actualizar perfiles de Power Score para todas las clases."),
    emptyLine(),
    h2("Fase 2 \u2014 Completar Core (Mes 3-6)"),
    pRuns([{ text: "Objetivo: ", bold: true }, { text: "Implementar todos los sistemas de gameplay esenciales." }]),
    bulletItem("Sistema de equipamiento completo (7 slots, mejoras, raridades)."),
    bulletItem("Sistema de esp\u00edritus (entidades companion con pasivas)."),
    bulletItem("Sistema de cultivaci\u00f3n (reinos espirituales, progresi\u00f3n)."),
    bulletItem("Sistema de autenticaci\u00f3n (JWT + OAuth2)."),
    bulletItem("Sistema de chat (WebSocket/SignalR)."),
    bulletItem("Primeras misiones y contenido PvE b\u00e1sico."),
    emptyLine(),
    h2("Fase 3 \u2014 Multijugador y Beta PC (Mes 7-10)"),
    pRuns([{ text: "Objetivo: ", bold: true }, { text: "Networking en tiempo real y primera beta jugable." }]),
    bulletItem("Implementar networking con SignalR/WebSocket para combate en tiempo real."),
    bulletItem("Sistema de clanes y guerras."),
    bulletItem("Matchmaking PvP y ranking."),
    bulletItem("Dungeons e instancias."),
    bulletItem("Beta cerrada PC con 100-500 jugadores."),
    bulletItem("Balance intensivo con datos de playtesting."),
    emptyLine(),
    h2("Fase 4 \u2014 Migraci\u00f3n a Unity y Mobile (Mes 11-16)"),
    pRuns([{ text: "Objetivo: ", bold: true }, { text: "Cliente multiplataforma en Unity." }]),
    bulletItem("Setup proyecto Unity e importar War.Core."),
    bulletItem("Desarrollar cliente gr\u00e1fico completo."),
    bulletItem("Implementar controles t\u00e1ctiles para mobile."),
    bulletItem("Optimizaciones de performance m\u00f3vil."),
    bulletItem("Integraci\u00f3n con networking existente."),
    bulletItem("Beta mobile cerrada."),
    emptyLine(),
    h2("Fase 5 \u2014 Lanzamiento Multiplataforma (Mes 17-20)"),
    pRuns([{ text: "Objetivo: ", bold: true }, { text: "Lanzamiento en PC + iOS + Android." }]),
    bulletItem("QA intensivo multiplataforma."),
    bulletItem("Soft launch en regiones seleccionadas."),
    bulletItem("Implementar tienda y monetizaci\u00f3n (cosm\u00e9ticos, battle pass)."),
    bulletItem("Preparar infraestructura de servidores para escala."),
    bulletItem("Lanzamiento global."),
    emptyLine(),
    h3("Resumen del Roadmap"),
    makeTable(
      ["Fase", "Per\u00edodo", "Hito Principal", "Equipo Estimado"],
      [
        ["1. Estabilizaci\u00f3n", "Mes 1-2", "4 clases completas, tests, deuda resuelta", "2-3 devs"],
        ["2. Core Completo", "Mes 3-6", "Equipamiento, esp\u00edritus, cultivaci\u00f3n, auth", "3-4 devs"],
        ["3. Multiplayer + Beta", "Mes 7-10", "Networking, clanes, PvP, beta PC", "4-5 devs"],
        ["4. Unity + Mobile", "Mes 11-16", "Cliente Unity, controles t\u00e1ctiles, beta mobile", "5-6 devs"],
        ["5. Lanzamiento", "Mes 17-20", "Launch global PC + iOS + Android", "6-8 personas"],
      ],
      [2000, 1400, 3600, 2360]
    ),
    p("Nota: Las estimaciones asumen un equipo dedicado a tiempo completo. El timeline total de ~20 meses es realista para el alcance del proyecto, considerando que los sistemas de dominio core ya est\u00e1n s\u00f3lidamente implementados.", { italic: true }),
    new Paragraph({ children: [new PageBreak()] })
  );

  // ════════════════════════════════════════════════
  // AP\u00c9NDICE A — GLOSARIO
  // ════════════════════════════════════════════════
  mainChildren.push(
    h1("Ap\u00e9ndice A \u2014 Glosario T\u00e9cnico"),
    emptyLine(),
    makeTable(
      ["T\u00e9rmino", "Definici\u00f3n"],
      [
        ["PP (Power Score)", "Puntuaci\u00f3n unificada de fuerza del personaje. Calculada con pesos contextuales por clase."],
        ["Cultivaci\u00f3n", "Sistema de progresi\u00f3n espiritual que desbloquea reinos y mejoras internas (planificado)."],
        ["Esp\u00edritus", "Entidades companion que otorgan stats pasivos y habilidades \u00fanicas (planificado)."],
        ["Ascensi\u00f3n", "Nivel de mejora de una habilidad (1-10). Incrementa magnitud y desbloquea efectos."],
        ["Estado (State)", "Condici\u00f3n elemental (Heat, Cold, Electrified, Poison) no reducida por tenacity."],
        ["CC (Crowd Control)", "Condiciones de control (Stun, Freeze, Paralyze, Blind, Weaken) reducidas por tenacity."],
        ["Tenacity", "Stat defensivo que reduce la duraci\u00f3n de efectos de Crowd Control."],
        ["Mitigaci\u00f3n", "Reducci\u00f3n de da\u00f1o basada en Defense/MagicResistance con curva asint\u00f3tica (cap 90%)."],
        ["IStatSource", "Interfaz para fuentes de estad\u00edsticas (nivel, equipo, esp\u00edritus, buffs)."],
        ["Combo", "Secuencia de 6 ataques b\u00e1sicos con ventana de 2 segundos y da\u00f1o creciente."],
        ["Draft/Published", "Flujo de skills admin: borrador editable \u2192 publicaci\u00f3n al runtime del juego."],
        ["Clean Architecture", "Patr\u00f3n de arquitectura con separaci\u00f3n estricta: Dominio \u2192 Infraestructura \u2192 API."],
      ],
      [2400, 6960]
    ),
    new Paragraph({ children: [new PageBreak()] })
  );

  // ════════════════════════════════════════════════
  // AP\u00c9NDICE B — INVENTARIO DE ARCHIVOS
  // ════════════════════════════════════════════════
  mainChildren.push(
    h1("Ap\u00e9ndice B \u2014 Inventario Completo de Archivos"),
    emptyLine(),
    makeTable(
      ["Ruta", "Sistema", "Estado"],
      [
        ["War.Core/Stats/StatType.cs", "Stats", "\u2705 Completo"],
        ["War.Core/Stats/StatCatalog.cs", "Stats", "\u2705 Completo"],
        ["War.Core/Stats/StatsCalculator.cs", "Stats", "\u2705 Completo"],
        ["War.Core/Stats/FinalStats.cs", "Stats", "\u2705 Completo"],
        ["War.Core/Stats/StatResolvers.cs", "Stats", "\u2705 Completo"],
        ["War.Core/Stats/StatResolutionModels.cs", "Stats", "\u2705 Completo"],
        ["War.Core/Stats/IStatSource.cs", "Stats", "\u2705 Completo"],
        ["War.Core/Stats/StatDefinition.cs", "Stats", "\u2705 Completo"],
        ["War.Core/Stats/StatContribution.cs", "Stats", "\u2705 Completo"],
        ["War.Core/Stats/StatSemantics.cs", "Stats", "\u2705 Completo"],
        ["War.Core/Stats/StatCategory.cs", "Stats", "\u2705 Completo"],
        ["War.Core/Stats/DummyStatSource.cs", "Stats", "\u2705 Completo"],
        ["War.Core/PowerScore/PowerScoreCalculator.cs", "PowerScore", "\u2705 Completo"],
        ["War.Core/PowerScore/PowerScorePolicyCatalog.cs", "PowerScore", "\u2705 Completo"],
        ["War.Core/PowerScore/PowerScoreClassProfiles.cs", "PowerScore", "\uD83D\uDD36 Parcial"],
        ["War.Core/PowerScore/PowerScoreModels.cs", "PowerScore", "\u2705 Completo"],
        ["War.Core/PowerScore/PowerScoreUsageAnalysis.cs", "PowerScore", "\u2705 Completo"],
        ["War.Core/Progression/CharacterLevelProgression.cs", "Progresi\u00f3n", "\u2705 Completo"],
        ["War.Core/Progression/CharacterLevelRules.cs", "Progresi\u00f3n", "\u2705 Completo"],
        ["War.Core/Progression/ClassLevelGrowthProfiles.cs", "Progresi\u00f3n", "\u2705 Completo"],
        ["War.Core/Progression/CharacterLevelModels.cs", "Progresi\u00f3n", "\u2705 Completo"],
        ["War.Core/Progression/CharacterLevelStatSource.cs", "Progresi\u00f3n", "\u2705 Completo"],
        ["War.Core/Progression/CharacterFinalStatsBuilder.cs", "Progresi\u00f3n", "\u2705 Completo"],
        ["War.Core/Progression/CharacterSnapshotFactory.cs", "Progresi\u00f3n", "\u2705 Completo"],
        ["War.Core/Progression/CharacterSnapshotModels.cs", "Progresi\u00f3n", "\u2705 Completo"],
        ["War.Core/Progression/LevelProgressionFairnessAudit.cs", "Progresi\u00f3n", "\u2705 Completo"],
        ["War.Core/Skills/SkillModels.cs", "Skills", "\u2705 Completo"],
        ["War.Core/Skills/SkillProgressModels.cs", "Skills", "\u2705 Completo"],
        ["War.Core/Skills/SkillCatalogRegistry.cs", "Skills", "\u2705 Completo"],
        ["War.Core/Skills/SkillCatalogRules.cs", "Skills", "\u2705 Completo"],
        ["War.Core/Skills/SkillCatalogModels.cs", "Skills", "\u2705 Completo"],
        ["War.Core/Skills/SkillCombatIntegration.cs", "Skills", "\u2705 Completo"],
        ["War.Core/Skills/SkillSlot.cs", "Skills", "\u2705 Completo"],
        ["War.Core/Skills/ClassType.cs", "Skills", "\u2705 Completo"],
        ["War.Core/Skills/Catalogs/SorcererSkillCatalog.cs", "Skills", "\u2705 Completo"],
        ["War.Core/Combat/CombatEventResolver.cs", "Combate", "\u2705 Completo"],
        ["War.Core/Combat/CombatEventModels.cs", "Combate", "\u2705 Completo"],
        ["War.Core/Combat/CombatActionMagnitudeService.cs", "Combate", "\u2705 Completo"],
        ["War.Core/Combat/CombatActionResourceService.cs", "Combate", "\u2705 Completo"],
        ["War.Core/Combat/CombatConditionApplicationService.cs", "Combate", "\u2705 Completo"],
        ["War.Core/Combat/CombatConditionCatalog.cs", "Combate", "\u2705 Completo"],
        ["War.Core/Combat/CombatConditionInteractionService.cs", "Combate", "\u2705 Completo"],
        ["War.Core/Combat/CombatConditionInteractions.cs", "Combate", "\u2705 Completo"],
        ["War.Core/Combat/CombatConditionSemantics.cs", "Combate", "\u2705 Completo"],
        ["War.Core/Combat/CombatProbabilityChecks.cs", "Combate", "\u2705 Completo"],
        ["War.Core/Combat/CombatProtectionModels.cs", "Combate", "\u2705 Completo"],
        ["War.Core/Combat/CombatResourceProjectionService.cs", "Combate", "\u2705 Completo"],
        ["War.Core/Combat/BasicAttackCatalog.cs", "Combate", "\u2705 Completo"],
        ["War.Core/Combat/BasicAttackModels.cs", "Combate", "\u2705 Completo"],
        ["War.Core/Resources/CharacterResources.cs", "Recursos", "\u2705 Completo"],
        ["War.Core/Resources/CharacterResourceType.cs", "Recursos", "\u2705 Completo"],
        ["War.Core/Resources/CharacterResourceCatalog.cs", "Recursos", "\u2705 Completo"],
        ["War.Core/Resources/CharacterResourceDefinition.cs", "Recursos", "\u2705 Completo"],
        ["War.Core/Entities/Character.cs", "Entidades", "\u2705 Completo"],
        ["War.Infrastructure/Persistence/WarDbContext.cs", "Persistencia", "\u2705 Completo"],
        ["War.Infrastructure/Persistence/CharacterEntity.cs", "Persistencia", "\u2705 Completo"],
        ["War.Infrastructure/Persistence/CharacterSkillProgressEntity.cs", "Persistencia", "\u2705 Completo"],
        ["War.Infrastructure/Persistence/AdminSkillRecordEntity.cs", "Persistencia", "\u2705 Completo"],
        ["War.Api/Program.cs", "API", "\u2705 Completo"],
        ["War.Api/Controllers/CharactersController.cs", "API", "\u2705 Completo"],
        ["War.Api/Controllers/CombatDemoController.cs", "API", "\u2705 Completo"],
        ["War.Api/Controllers/SkillAdminController.cs", "API", "\u2705 Completo"],
        ["War.Api/Controllers/StatsTestController.cs", "API", "\u2705 Completo"],
        ["War.Api/wwwroot/index.html", "Frontend", "\u2705 Completo"],
        ["War.Api/wwwroot/app.js", "Frontend", "\u2705 Completo"],
        ["War.Api/wwwroot/styles.css", "Frontend", "\u2705 Completo"],
      ],
      [4000, 1800, 1560]
    )
  );

  // Build final document
  const doc = new Document({
    styles: {
      default: { document: { run: { font: "Calibri", size: 22 } } },
      paragraphStyles: [
        { id: "Heading1", name: "Heading 1", basedOn: "Normal", next: "Normal", quickFormat: true,
          run: { size: 32, bold: true, font: "Arial", color: BLUE_DARK },
          paragraph: { spacing: { before: 360, after: 200 }, outlineLevel: 0 } },
        { id: "Heading2", name: "Heading 2", basedOn: "Normal", next: "Normal", quickFormat: true,
          run: { size: 28, bold: true, font: "Arial", color: BLUE_MED },
          paragraph: { spacing: { before: 280, after: 160 }, outlineLevel: 1 } },
        { id: "Heading3", name: "Heading 3", basedOn: "Normal", next: "Normal", quickFormat: true,
          run: { size: 24, bold: true, font: "Arial", color: "444444" },
          paragraph: { spacing: { before: 200, after: 120 }, outlineLevel: 2 } },
      ]
    },
    numbering: {
      config: [
        { reference: "bullets", levels: [{ level: 0, format: LevelFormat.BULLET, text: "\u2022", alignment: AlignmentType.LEFT, style: { paragraph: { indent: { left: 720, hanging: 360 } } } }] },
        { reference: "numbers1", levels: [{ level: 0, format: LevelFormat.DECIMAL, text: "%1.", alignment: AlignmentType.LEFT, style: { paragraph: { indent: { left: 720, hanging: 360 } } } }] },
        { reference: "numbers2", levels: [{ level: 0, format: LevelFormat.DECIMAL, text: "%1.", alignment: AlignmentType.LEFT, style: { paragraph: { indent: { left: 720, hanging: 360 } } } }] },
        { reference: "numbers3", levels: [{ level: 0, format: LevelFormat.DECIMAL, text: "%1.", alignment: AlignmentType.LEFT, style: { paragraph: { indent: { left: 720, hanging: 360 } } } }] },
        { reference: "numbers4", levels: [{ level: 0, format: LevelFormat.DECIMAL, text: "%1.", alignment: AlignmentType.LEFT, style: { paragraph: { indent: { left: 720, hanging: 360 } } } }] },
        { reference: "numbers5", levels: [{ level: 0, format: LevelFormat.DECIMAL, text: "%1.", alignment: AlignmentType.LEFT, style: { paragraph: { indent: { left: 720, hanging: 360 } } } }] },
        { reference: "numbers6", levels: [{ level: 0, format: LevelFormat.DECIMAL, text: "%1.", alignment: AlignmentType.LEFT, style: { paragraph: { indent: { left: 720, hanging: 360 } } } }] },
      ]
    },
    sections: [
      coverSection,
      {
        properties: {
          page: { size: { width: PAGE_W, height: PAGE_H }, margin: { top: MARGIN, right: MARGIN, bottom: MARGIN, left: MARGIN } },
          type: SectionType.NEXT_PAGE
        },
        headers: {
          default: new Header({ children: [
            new Paragraph({
              border: { bottom: { style: BorderStyle.SINGLE, size: 6, color: BLUE_MED, space: 1 } },
              children: [new TextRun({ text: "WAR \u2014 Documentaci\u00f3n T\u00e9cnica Completa  |  CONFIDENCIAL", font: "Arial", size: 16, color: GRAY_TEXT, italics: true })]
            })
          ] })
        },
        footers: {
          default: new Footer({ children: [
            new Paragraph({
              alignment: AlignmentType.CENTER,
              border: { top: { style: BorderStyle.SINGLE, size: 4, color: "CCCCCC", space: 1 } },
              children: [
                new TextRun({ text: "P\u00e1gina ", font: "Arial", size: 16, color: GRAY_TEXT }),
                new TextRun({ children: [PageNumber.CURRENT], font: "Arial", size: 16, color: GRAY_TEXT })
              ]
            })
          ] })
        },
        children: mainChildren
      }
    ]
  });

  return doc;
}

// ─── GENERATE ───
async function main() {
  console.log("Generando documento WAR_Documentacion_Tecnica.docx...");
  const doc = buildDocument();
  const buffer = await Packer.toBuffer(doc);
  fs.writeFileSync("WAR_Documentacion_Tecnica.docx", buffer);
  console.log("Documento generado exitosamente: WAR_Documentacion_Tecnica.docx");
  console.log(`Tama\u00f1o: ${(buffer.length / 1024).toFixed(1)} KB`);
}

main().catch(err => { console.error("Error:", err); process.exit(1); });
