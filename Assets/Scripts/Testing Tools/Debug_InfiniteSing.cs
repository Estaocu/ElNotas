using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Debug_InfiniteSing : MonoBehaviour
{
    
    [SerializeField] private Singer singer;
    [SerializeField] private PlayerInputs input;

    void Update()
    {
        if (Gamepad.current != null && Gamepad.current.leftStickButton.wasPressedThisFrame)
        {
            singer.Sing();
        }

    }




}
