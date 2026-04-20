# Teleport Debug System — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Reescribir los dos scripts del sistema de teletransporte de debug para eliminar todos los errores, cachear correctamente los puntos y resetear la física del player al teleportar.

**Architecture:** `TeleportPoint` es un marcador puro de datos con gizmos siempre visibles. `TeleportPlayer` cachea los puntos en `Start`, resuelve el player por `SerializeField` o por nombre como fallback, y resetea `Rigidbody` al teleportar. El editor de `TeleportPoint` solo corre en `UNITY_EDITOR`.

**Tech Stack:** Unity (C#), CMF (Character Movement Fundamentals), Legacy Input System (`Input.GetKeyDown`)

---

### Task 1: Reescribir TeleportPoint.cs

**Files:**
- Modify: `Assets/Scripts/Testing Tools/TeleportPoint.cs`

- [ ] **Step 1: Reemplazar contenido completo del archivo**

```csharp
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using System.Collections.Generic;
#endif

public class TeleportPoint : MonoBehaviour
{
    [SerializeField, Range(-1, 9)] private int number = -1;

    public int Number => number;

    private void OnValidate()
    {
        if (number < -1) number = -1;
        else if (number > 9) number = 9;
    }

    private void OnDrawGizmos()
    {
        if (number < 0) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
#if UNITY_EDITOR
        Handles.Label(transform.position + Vector3.up * 0.8f, number.ToString());
#endif
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(TeleportPoint))]
public class TeleportPointEditor : Editor
{
    public override void OnInspectorGUI()
    {
        if (target == null) return;
        serializedObject.Update();

        TeleportPoint tp = (TeleportPoint)target;
        SerializedProperty numberProp = serializedObject.FindProperty("number");

        TeleportPoint[] allPoints = FindObjectsOfType<TeleportPoint>();
        var usedNumbers = new List<int>();
        foreach (TeleportPoint point in allPoints)
        {
            if (point != tp && point.Number >= 0)
                usedNumbers.Add(point.Number);
        }

        string[] labels = new string[11];
        int[] values = new int[11];
        labels[0] = "-- None --";
        values[0] = -1;
        for (int i = 0; i < 10; i++)
        {
            values[i + 1] = i;
            labels[i + 1] = usedNumbers.Contains(i) ? $"{i} (used)" : i.ToString();
        }

        int currentValue = numberProp.intValue;
        int currentIndex = System.Array.IndexOf(values, currentValue);
        if (currentIndex < 0) currentIndex = 0;

        int newIndex = EditorGUILayout.Popup("Number", currentIndex, labels);
        if (newIndex != currentIndex)
        {
            numberProp.intValue = values[newIndex];
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
```

- [ ] **Step 2: Verificar en Unity Editor**

  - Abrir `BlenderScene`
  - Los 4 `TeleportPoint` en escena deben mostrar esfera cyan con número encima **sin seleccionar** el objeto
  - En el inspector de cualquier `TeleportPoint`, el dropdown debe mostrar los números ya usados con "(used)"
  - No deben aparecer errores en la consola al cargar la escena

- [ ] **Step 3: Commit**

```bash
git add "Assets/Scripts/Testing Tools/TeleportPoint.cs"
git commit -m "refactor: TeleportPoint sin FindObjectsOfType en OnValidate, gizmos siempre visibles"
```

---

### Task 2: Reescribir TeleportPlayer.cs

**Files:**
- Modify: `Assets/Scripts/Testing Tools/TeleportPlayer.cs`

- [ ] **Step 1: Reemplazar contenido completo del archivo**

```csharp
using UnityEngine;

public class TeleportPlayer : MonoBehaviour
{
    [SerializeField] private Transform playerOverride;

    private Transform _playerTransform;
    private Rigidbody _playerRigidbody;
    private TeleportPoint[] _points;

    private void Start()
    {
        ResolvePlayer();
        RefreshPoints();
    }

    public void RefreshPoints()
    {
        _points = FindObjectsOfType<TeleportPoint>();
        Debug.Log($"[TeleportPlayer] {_points.Length} puntos cacheados.");
    }

    private void ResolvePlayer()
    {
        Transform source = playerOverride != null
            ? playerOverride
            : GameObject.Find("ThirdPersonWalker_B")?.transform;

        if (source == null)
        {
            Debug.LogError("[TeleportPlayer] Player no encontrado. Asigna 'Player Override' o comprueba que 'ThirdPersonWalker_B' está en escena.");
            return;
        }

        _playerTransform = source;
        _playerRigidbody = source.GetComponent<Rigidbody>();
        Debug.Log($"[TeleportPlayer] Player resuelto: {source.name}");
    }

    private void Update()
    {
        for (int i = 0; i < 10; i++)
        {
            if (Input.GetKeyDown(KeyCode.Keypad0 + i))
                TeleportTo(i);
        }
    }

    private void TeleportTo(int number)
    {
        if (_playerTransform == null)
        {
            Debug.LogError("[TeleportPlayer] Player no asignado.");
            return;
        }

        TeleportPoint point = FindPoint(number);
        if (point == null)
        {
            Debug.LogWarning($"[TeleportPlayer] No hay TeleportPoint con número {number}.");
            return;
        }

        _playerTransform.position = point.transform.position;
        _playerTransform.rotation = point.transform.rotation;

        if (_playerRigidbody != null)
        {
            _playerRigidbody.velocity = Vector3.zero;
            _playerRigidbody.angularVelocity = Vector3.zero;
        }

        Debug.Log($"[TeleportPlayer] → punto {number} en {point.transform.position}");
    }

    private TeleportPoint FindPoint(int number)
    {
        foreach (TeleportPoint point in _points)
        {
            if (point != null && point.Number == number)
                return point;
        }
        return null;
    }
}
```

- [ ] **Step 2: Verificar en Unity Editor**

  - Entrar en Play Mode en `BlenderScene`
  - Consola debe mostrar: `[TeleportPlayer] Player resuelto: ThirdPersonWalker_B` y `[TeleportPlayer] X puntos cacheados.`
  - Pulsar Numpad 1, 2, 3 → el player debe aparecer en el punto correspondiente sin conservar velocidad
  - Pulsar Numpad 5 (sin punto asignado) → debe aparecer `LogWarning` en consola, sin crash

- [ ] **Step 3: Commit**

```bash
git add "Assets/Scripts/Testing Tools/TeleportPlayer.cs"
git commit -m "refactor: TeleportPlayer cachea puntos, resetea Rigidbody, fallback por nombre"
```

---

### Task 3: Verificación final

**Files:** ninguno (solo verificación)

- [ ] **Step 1: Abrir BlenderScene y comprobar consola limpia**

  Sin entrar en Play Mode, la consola debe tener cero errores relacionados con `TeleportPoint` o `TeleportPlayer`.

- [ ] **Step 2: Entrar en Play Mode y probar todos los puntos existentes**

  Pulsar Numpad para cada número asignado en escena (1, 2, 3 y el cuarto punto). Verificar posición correcta y sin inercia residual.

- [ ] **Step 3: Verificar que el prefab TeleportPoint sigue funcionando**

  Arrastrar `Assets/Prefabs/DebugAndTesting/TeleportPoint.prefab` a la escena desde el editor. El inspector debe mostrar el dropdown. Asignar un número libre. El gizmo debe aparecer inmediatamente en la Scene View.

- [ ] **Step 4: Commit final si hay cambios pendientes**

```bash
git add -A
git commit -m "fix: sistema teleporte debug refactorizado y verificado"
```
