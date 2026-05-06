using UnityEngine;

/// <summary>
/// Contrato que deben cumplir los proyectiles controlables por notas.
/// Permite que Corn_DetectEntities y SingleNotesListener no dependan
/// de la clase concreta HomingProjectile.
/// </summary>
public interface IControllableProjectile
{
    /// <summary>El GameObject que lanzó/activó el proyectil.</summary>
    GameObject Sender { get; }

    void SetMovementMode(Mode mode, GameObject sender);
    void SetTarget(GameObject newTarget);
    void SaveNotePosition(Transform noteTransform);
}
