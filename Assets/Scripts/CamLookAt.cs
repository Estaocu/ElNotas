using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class CamLookAt : MonoBehaviour
{
public Transform target;
void Update() {
    if (target != null) {
        transform.LookAt(target);
    }
}

}
