# WAR · Pipeline Artístico

Documento vivo. Actualizado conforme se completan pasos. Referencia canónica para modelado, texturizado, rigging y exportación a Unity.

## Dirección artística

**Mezcla**: MIR4 (detalle realista idealizado, PBR denso, rostros humanos) × Genshin (paleta saturada, efectos cinematográficos, emisivos vibrantes) × Shadowlands (split warm-cool en iluminación, estilización ligera).

**NO hacer**: photoreal puro, cartoon plano, cell-shading anime.

**Referencia primaria**: MIR4. Cuando haya duda, se tira hacia MIR4 antes que hacia Genshin.

## Código de rareza

Colores de rareza (decisión del equipo, **invertida respecto al estándar MMO**):

| Rareza | Color | Hex guía | Notas |
|---|---|---|---|
| Común | verde | `#4CAF50` | Base, sin gemas, emisivo muy bajo |
| Especial | azul | `#2196F3` | 1 gema pequeña, emisivo sutil |
| Épico | rojo | `#E53935` | 2-3 gemas, emisivo notable |
| Legendario | dorado-fuego | `#FFB300` con pulso `#FF6B1A` | Runas talladas + aura emisiva animada |

**Importante**: Los nombres del enum en código son `Common/Special/Epic/Legendary` (inglés). Los nombres narrativos visibles al jugador son `Común/Especial/Épico/Legendario` (español masculino). La conversión está centralizada en `EquipmentCatalog.RarityLabel()`.

## Paletas por clase

### Sorcerer — arcano elemental
- Primaria: púrpura real `#4A1F6B` + negro obsidiana `#1A1A2E`
- Acento: dorado ritual pálido `#D4AF37` (runas, bordes de túnica)
- Emisivos por hechizo (swap dinámico): fuego `#FF4D1A`, hielo `#3FE4FF`, rayo `#B347FF`
- Feel: místico, distante, aura fría sobre ropa oscura

### Juramentada — luz + oscuridad sagrada
- Primaria: marfil cálido `#E8DFC4` (placas) + negro profundo `#1A1A1F` (interior de capa)
- Acento: dorado antiguo `#B8860B` (filigrana) + blanco-azulado `#E0F0FF` (aura sagrada)
- Emisivos: dorado celestial en modo luz, violeta-negro en modo oscuridad
- Feel: solemne, dualidad visible, pesado pero noble

### Lancero — físico rápido + rayo
- Primaria: azul acero oscuro `#2E4E6F` + cuero negro `#2B2B2B`
- Acento: plata bruñida `#BCC6CC` + cian eléctrico `#3FC5FF` (venas en lanza y cinturón)
- Emisivos: arcos cian en movimiento, chispas en hit
- Feel: aerodinámico, afilado, eléctrico

### Bruiser — fuerza bruta terrenal
- Primaria: bronce oxidado `#8B4513` / `#A0522D` + rojo óxido `#722F37`
- Acento: negro humeado `#2B2B2B` + grietas ígneas `#FF6B1A`
- Emisivos: grietas naranja-lava en hombreras y guantes al cargar básico 6/6 o ult
- Feel: masivo, terrestre, cicatrizado, contundente

---

# Paso 1 — Base humanoide (hombre + mujer)

**Objetivo**: dos meshes base intercambiables, topología idéntica (mismas UVs), listos para reskin por clase. Mixamo-compatibles para auto-rig.

## Proporciones maestras

| Medida | ♂ (base) | ♀ (base) | Notas |
|---|---|---|---|
| Altura total | 1.80m (Unity 1u = 1m) | 1.72m | Eje Y vertical |
| Proporción heads | 7.75 heads | 7.5 heads | MIR4-like, ligeramente idealizadas |
| Head height | ~23.2 cm | ~22.9 cm | Tamaño del "head" módulo |
| Hombros (ancho) | 2.1 × head | 1.7 × head | ♀ más estrechos |
| Cadera (ancho) | 1.5 × head | 1.7 × head | ♀ más anchas (curva) |
| Cintura (ancho) | 1.2 × head | 0.95 × head | ♀ cintura marcada |
| Longitud de brazo | 3 heads | 2.9 heads | Desde hombro a muñeca |
| Longitud de pierna | 4 heads (incluyendo pie) | 4 heads | |

**Heroica pero creíble**: 7.5-8 heads (MIR4) en lugar de 9-10 de Genshin o 6 de Fortnite. Sirve tanto cinemática como PvP a 30m.

## Musculatura por clase (definición superficial)

Escala 0-10 donde 0 = niño, 10 = culturista competitivo:

| Clase | ♂ | ♀ | Notas |
|---|---|---|---|
| Sorcerer | 5 | 5 | Magro, delts y pecho poco marcados, sin venas. Postura algo encorvada (libros, cast) |
| Juramentada | 6 | 6 | Atlético proporcional, torso en V suave, abdomen definido pero no hipertrófico |
| Lancero | 7 | 7 | Velocista: pantorrillas marcadas, delts desarrollados, serratos visibles |
| Bruiser | 10 | 10 | Hipertrofia extrema: trapecio gigante, cuello grueso, torso masivo, venas visibles |

**Femenino ≠ menos musculatura**: la ♀ Bruiser sigue siendo masiva y fornida, con curvas superpuestas. Se logra con morph targets de busto/caderas/cintura sobre la misma base musculada.

**Curvas ♀**: busto realista atractivo (copa C estándar), cintura 0.7 × caderas (golden ratio), caderas anchas. Voluptuoso sin caer en hipersexualización de MMO coreano barato.

## Topología

### Target tris
- **Base humanoide** (hero player character, LOD0): 18,000–22,000 tris.
- Se incluye cabello/barba básicos como mesh separado (~2k–3k extra).
- LOD1 a 60% tris, LOD2 a 30%, LOD3 (silueta) a 15% para MMO con 20+ en pantalla.

### Reglas de topología
1. **Quads exclusivamente** — cero tris y cero n-gons salvo:
   - Puntos forzados donde 5 ≤ valencia ≤ 5 (aceptables en zonas no deformables como corona del cráneo, dedos).
   - Nunca polos en articulaciones que deforman.
2. **Loops en articulaciones** para deformación limpia:
   - Cuello: 2 loops
   - Hombros: 5 loops (evita "candy wrap")
   - Codo: 3 loops (doble loop en pliegue)
   - Muñeca: 2 loops
   - Cadera: 5 loops
   - Rodilla: 3 loops (doble loop en pliegue)
   - Tobillo: 2 loops
   - Finger joints: 3 loops cada uno
3. **Evita pinching** en axila, ingle, cuello — valencia 4 consistente.
4. **Edge flow** que siga grupos musculares (pectoral, serrato, latissimus, deltoides posterior/anterior/lateral).

### Distribución aproximada de tris (♂ base)

| Zona | Tris |
|---|---|
| Cabeza (sin pelo) | 4,500 |
| Cuello + clavículas | 800 |
| Torso anterior | 3,200 |
| Torso posterior | 2,800 |
| Brazos (2) | 3,600 |
| Manos (2) | 2,600 |
| Piernas (2) | 3,400 |
| Pies (2) | 1,500 |
| **Total** | ≈22,400 |

Cabello/barba mesh separado: 2–3k extra según estilo.

## UVs

**Objetivo**: 2 atlas compartidos entre ♂ y ♀ para reutilizar texturas.

- **Atlas A (2048 × 2048)**: cabeza + manos + mesh visible sin armadura (piel). Prioriza textel density en cara.
- **Atlas B (2048 × 2048)**: torso + brazos + piernas + pies (zonas normalmente cubiertas por armadura). Textel density menor, reutilizable.

**Rule of thumb**: 10 px/cm en cara, 6 px/cm en torso, 4 px/cm en piernas.

UDIMs **no** — queremos atlas únicos por simplicidad de runtime.

Simetría: UV simétrico izquierda/derecha (overlap). Evita mirror para tattoos/marcas — para eso, usa un atlas separado de decals.

## Rigging — Mixamo-compatible

Armadura estándar Mixamo **Humanoid** (62 huesos). Unity Mecanim la detecta automáticamente.

Jerarquía:
```
mixamorig:Hips
├── mixamorig:Spine → Spine1 → Spine2
│   ├── mixamorig:LeftShoulder → LeftArm → LeftForeArm → LeftHand → {5 dedos × 4 huesos}
│   ├── mixamorig:RightShoulder → ... (simétrico)
│   └── mixamorig:Neck → Head
│       ├── HeadTop_End
│       ├── LeftEye (opcional)
│       └── RightEye (opcional)
└── mixamorig:LeftUpLeg → LeftLeg → LeftFoot → LeftToeBase → LeftToe_End
    └── (simétrico derecha)
```

**Huesos extra permitidos** (no romperán Mixamo retarget):
- `Cape_01`, `Cape_02`, `Cape_03` (capa de Juramentada) — hasta 3 bones
- `Skirt_FL`, `Skirt_FR`, `Skirt_BL`, `Skirt_BR` (faldón Sorcerer) — 4 bones de 2 segmentos c/u
- `Hair_Ponytail_01..03` (pelo largo) — hasta 3 bones
- `Pauldron_L`, `Pauldron_R` (hombreras rígidas del Bruiser) — 1 bone c/u

**Weight painting**:
- Máximo 4 bones influyentes por vértice (límite Unity estándar).
- Partes rígidas (hombreras pesadas, cinturón, calzado): weight 100% a un solo bone.
- Partes blandas (piel, tela fina): blend suave, suma = 1.0 por vértice.

## Pose inicial

**A-pose**, no T-pose. Mixamo trabaja mejor con A-pose y evita cluster en axilas.

- Brazos 45° abajo desde horizontal
- Palmas apuntando ligeramente hacia atrás (pulgar hacia adelante)
- Pies ancho de hombros, punteras mirando ligeramente hacia afuera (~15°)
- Peso centrado, postura neutral

## Export a Unity

- Formato: **FBX** (binario, Unity 2022.3 LTS native)
- Escala: 1 unidad Blender = 1 metro Unity. Aplica "Apply All Transforms" antes de exportar.
- Axis convention: Forward = -Z, Up = Y (default Unity).
- Pivote: en los pies (origen entre talones, Y=0).
- Smoothing: edges
- Include: geometry + armature + skin weights + blend shapes. Sin animaciones embebidas (se hace retarget Mixamo después).
- Do NOT include: cameras, lights, empty nodes, modifiers no aplicados.

**Unity import settings** (Inspector):
- Rig: Humanoid, Avatar Definition: Create From This Model
- Materials: Import/Extract from Model
- Animation: None (animaciones las traemos desde Mixamo posterior)

## Naming de archivos y assets

```
Assets/WAR/Characters/Bases/
  Char_Base_M.blend      (base masculino)
  Char_Base_M.fbx
  Char_Base_F.blend
  Char_Base_F.fbx

Assets/WAR/Characters/Sorcerer/
  Char_Sorcerer_M.blend
  Char_Sorcerer_M.fbx
  Char_Sorcerer_F.blend
  Char_Sorcerer_F.fbx
  T_Char_Sorcerer_M_BaseColor_2K.png
  T_Char_Sorcerer_M_Normal_2K.png
  T_Char_Sorcerer_M_ORM_2K.png       (AO/Rough/Metal packed)
  M_Char_Sorcerer_M.mat              (Unity material)
  Prefab_Char_Sorcerer_M.prefab

(y así para Juramentada, Lancero, Bruiser × M/F)
```

Convenciones globales:
- **Char_** prefijo para personajes
- **Weapon_** armas
- **Armor_** armadura
- **Prop_** props de entorno
- **VFX_** efectos visuales
- **T_** texturas
- **M_** materials Unity
- **Prefab_** prefabs Unity
- Sufijo `_M` / `_F` para género
- Sufijo `_2K` / `_4K` para resolución de textura

## Checklist de entrega Paso 1

Para considerar "Paso 1" terminado, el artista debe entregar:

- [ ] `Char_Base_M.fbx` con rig Mixamo + skin + pose A + 22k tris
- [ ] `Char_Base_F.fbx` con rig Mixamo + skin + pose A + 22k tris + morph targets de busto/cintura/caderas
- [ ] Ambos con UVs en 2 atlas compartidos (2K cabeza, 2K cuerpo)
- [ ] Textura de piel placeholder (base color tipo 3 calabaza, normal baked del sculpt)
- [ ] Import verificado en Unity 2022.3 URP (escala correcta, rig reconocido como Humanoid)
- [ ] Render de silueta de ambos sobre fondo neutro para validar proporciones

Siguiente paso (Paso 2): variantes por clase sobre estas bases. Spec en sección aparte.

---

# Paso 2 — Variantes por clase (stub — se detalla al terminar Paso 1)

## Sorcerer (♂ / ♀)

Músculatura 5/10. Túnica larga + capucha generosa + cinturón rúnico. Báculo enlazado a mano derecha via bone socket `RightHand_WeaponSocket`.

**Silueta distintiva**: capucha grande + faldón ondeando + báculo alto → silueta "T" alargada.

## Juramentada (♂ / ♀)

Musculatura 6/10. Armadura paladín con filigrana dorada sobre placas marfil. Capa corta con 3 bones para animación. Espada de luz 1H en mano derecha, escudo opcional en mano izquierda.

**Silueta distintiva**: hombreras medianas + capa + espada vertical → silueta cruciforme.

## Lancero (♂ / ♀)

Musculatura 7/10. Placas ligeras azul acero, hombros altos y puntiagudos, cintura entallada. Lanza 2H larga con vena eléctrica cian.

**Silueta distintiva**: hombros anchos + lanza horizontal al correr → silueta "X" con lanza transversal.

## Bruiser (♂ / ♀)

Musculatura 10/10, proporción 7 heads (bajo + ancho). Hombreras masivas, armadura pesada de bronce oxidado. Martillo 2H o hacha masiva en mano derecha. Grietas ígneas emisivas en hombreras y guantes.

**Silueta distintiva**: proporción compacta y ancha + hombreras gigantes → silueta "rectangular robusta".

### Test de silueta a 30m

Renderizar los 8 personajes en negro sobre fondo blanco, tamaño aparente simulando 30m en viewport. Requisitos:
1. ♂ y ♀ de misma clase se distinguen entre sí.
2. Dos clases distintas NO se confunden entre sí.
3. Bruiser identificable por anchura extrema; Sorcerer por capucha; Juramentada por capa; Lancero por lanza y hombros.

Si falla algún criterio → iterar silueta antes de avanzar.

---

# Paso 3 — Armaduras modulares, armas, entorno (stub)

(Se detalla al terminar Paso 2.)

---

# Paso 4 — Integración Unity + demo (stub)

(Se detalla al terminar Paso 3. Ver `docs/UNITY_INTEGRATION.md` para la base del cliente.)

---

# Apéndice A — Performance budget MMO

- Personajes en pantalla simultáneos (target): 20 hero chars con LOD0, 50+ con LOD1/2.
- Draw calls por frame (target): <1200.
- Texturas activas: atlas compartidos, BC7 compression.
- Bones por skin: ≤62 (Mixamo Humanoid) + ≤8 extras.
- Blend shapes: ≤20 activos simultáneos por skin.
- Shader: URP Lit con características mínimas (sin parallax, sin tessellation).

# Apéndice B — Enums y valores confirmados del backend

| Enum | Valores (en código) | Labels UI (español) |
|---|---|---|
| `ClassType` | Sorcerer, Juramentada, Lancero, Bruiser | Idénticos |
| `CharacterGender` | Male, Female | Hombre, Mujer |
| `EquipmentSlot` | Weapon, Helmet, Chestplate, Boots, Bracers, Gloves, Earrings, Ring, Necklace | arma, casco, pechera, botas, brazaletes, guantes, aretes, anillo, collar |
| `EquipmentRarity` | Common, Special, Epic, Legendary | Común, Especial, Épico, Legendario |
| `SkillBookRarity` | Common, Special, Epic, Legendary | (ídem) |
| `CurrencyType` | Copper, Silver, Gold, Energy | Cobre, Plata, Oro, Energía |

## Patrones DefinitionId (lowercase, punto-separado)

- Equipo: `{slot}.{class|global}.{rarity}.{variant}`
  - Ej: `weapon.sorcerer.common.offensive`, `necklace.global.legendary.hybrid`
  - Variantes: `offensive`, `defensive`, `hybrid` (solo legendary)
- Libros: `book.common.knowledge` (universal) | `book.{class}.skill.{skillId}.{rarity}`
  - Ej: `book.sorcerer.skill.chispa-ignea.special`
