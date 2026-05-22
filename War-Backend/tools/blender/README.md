# WAR · Blender tooling

Scripts para acelerar el pipeline de Blender según las specs de `docs/ART_PIPELINE.md`.

## `base_humanoid_scaffold.py`

Crea una escena limpia en Blender con:
- Unidades métricas (1 unidad = 1 metro)
- Cubos de referencia con las proporciones exactas del género elegido
- Empties anatómicos en hip/waist/shoulder/neck/head/eye/etc.
- Cámara ortográfica para test de silueta
- Three-point lighting (key + fill + rim)
- Colecciones `_Reference`, `_Landmarks`, `_Mesh` listas

### Uso en Blender (GUI)
1. Abre Blender 4.0+.
2. Cambia a workspace **Scripting**.
3. `Text → Open` → selecciona `base_humanoid_scaffold.py`.
4. (Opcional) Edita `GENDER = "male"` o `"female"` en la parte superior.
5. `Run Script` (Alt+P).
6. Guarda como `Char_Base_M.blend` o `Char_Base_F.blend`.

### Uso headless (CLI)
```bash
# Male
blender --background --factory-startup --python tools/blender/base_humanoid_scaffold.py \
        -- --gender male --output Char_Base_M.blend

# Female
blender --background --factory-startup --python tools/blender/base_humanoid_scaffold.py \
        -- --gender female --output Char_Base_F.blend
```

### Qué NO hace este script
- No esculpe ni modela — deja las cajas de referencia para que tú esculpas el mesh dentro de `_Mesh`.
- No crea armadura Mixamo — eso se hace subiendo el FBX a Mixamo.com después de modelar.
- No genera texturas.

### Flujo recomendado tras correr el script
1. En la colección `_Mesh`, añade un cubo o esfera inicial en `LM_hip_pivot` (la altura de la cadera).
2. Activa shrinkwrap o proportional editing para esculpir el block-out dentro de las cajas de referencia.
3. Valida proporciones mirando el viewport: la cabeza del mesh debe entrar en `Ref_Head`, el torso en `Ref_Torso`, etc.
4. Retopo (usando RetopoFlow o manual) hasta llegar a ~22k tris según la tabla de `docs/ART_PIPELINE.md`.
5. UV unwrap a 2 atlas (head + body).
6. Bake de normals del sculpt al retopo.
7. Export FBX → upload a Mixamo → download rigged FBX → import en Unity.
