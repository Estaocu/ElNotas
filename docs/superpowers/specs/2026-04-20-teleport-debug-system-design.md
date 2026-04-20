# Teleport Debug System — Design Spec
Date: 2026-04-20

## Context

Sistema de teletransporte para debug y testing en la escena `BlenderScene`. Permite al desarrollador moverse rápidamente entre puntos clave de la escena pulsando Numpad 0–9. El player usa CMF (Character Movement Fundamentals) con un `Rigidbody` interno.

**Problemas del sistema anterior:**
- `FindObjectsOfType<TeleportPoint>()` en cada keypress y en `OnValidate` → errores al cargar escena y overhead innecesario
- No reseteaba `Rigidbody.velocity` al teleportar → el player conservaba inercia
- Variable `target` en `TeleportPointEditor` colisionaba con `Editor.target` heredado
- Gizmos solo visibles al seleccionar el objeto → difícil localizar puntos en escena

## Arquitectura

Dos scripts, sin dependencias externas:

```
TeleportPoint.cs         — marcador de datos + visual (gizmos + editor)
TeleportPlayer.cs        — lógica de input y teletransporte
```

## TeleportPoint.cs

**Responsabilidad:** Identificar un punto de destino en el mundo con un número 0–9.

- `[SerializeField] int number = -1` — rango 0–9, -1 = desactivado
- Propiedad pública `int Number`
- `OnValidate`: solo clampea el valor al rango válido. Sin `FindObjectsOfType`.
- `OnDrawGizmos` (siempre, no solo al seleccionar): esfera wireframe cyan + `Handles.Label` con el número encima
- Editor `TeleportPointEditor`: dropdown con labels "(used)" para números ya asignados. Variable local renombrada a `tp` para no colisionar con `Editor.target`

## TeleportPlayer.cs

**Responsabilidad:** Escuchar input de Numpad y teletransportar al player.

- `[SerializeField] Transform playerOverride` — si asignado, lo usa directamente. Si es null, busca `"ThirdPersonWalker_B"` como fallback en `Start`
- Cache `TeleportPoint[]` construido en `Start`
- Método público `RefreshPoints()` — recachea manualmente si se añaden/quitan puntos en runtime
- `Update`: escucha Numpad 0–9 con `Input.GetKeyDown`
- `TeleportTo(int number)`:
  1. Valida que `playerTransform != null`
  2. Busca en el cache (no `FindObjectsOfType`)
  3. Mueve `transform.position` y `transform.rotation`
  4. Obtiene `Rigidbody` del player y pone `velocity = angularVelocity = Vector3.zero`

## Flujo de datos

```
Numpad keypress
  → TeleportPlayer.Update
    → TeleportTo(number)
      → busca en _points[] (cache)
        → mueve Transform
        → resetea Rigidbody
```

## Lo que se elimina

- `FindObjectsOfType` en `OnValidate`
- `FindObjectsOfType` en cada keypress
- Bug de colisión de variable `target` en editor
- Gizmos invisibles hasta selección

## Archivos afectados

- `Assets/Scripts/Testing Tools/TeleportPlayer.cs` — reescritura completa
- `Assets/Scripts/Testing Tools/TeleportPoint.cs` — reescritura completa
- `Assets/Prefabs/DebugAndTesting/TeleportPoint.prefab` — sin cambios de script, solo verificar que el número sigue asignado
