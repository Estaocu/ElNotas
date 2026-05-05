using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.Localization.Plugins.XLIFF.V12;
using UnityEngine;

public class Corn_DetectEntities : MonoBehaviour
{
    private HomingProjectile projectile;
    void Awake()
    {
        projectile = transform.parent.GetComponent<HomingProjectile>();
    }



    void OnTriggerEnter(Collider other)
    {
        // Ignorar ondas sonoras para que no se conviertan en el objetivo del homing
        if (other.GetComponent<SingleNoteSoundwave>() != null) return;

        if (projectile.sender == other.gameObject){ Debug.Log("Me he chocado con quien me envia"); return; } //No vayas hacia quien te ha lanzado tio porfa
        projectile.target = other.gameObject;
        projectile.SetMovementMode(Mode.Homing, projectile.sender);
    }
}
