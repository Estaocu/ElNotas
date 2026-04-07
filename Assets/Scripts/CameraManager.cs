using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    private Camera playerCam;
    public Camera currentCam;


    void Start()
    {
        playerCam = Camera.main;
        ReturnCamToPlayer();
        if (playerCam == null)
        {
            Debug.LogError("No camera tagged 'MainCamera' found in the scene!");
        }
    }
    public void ReturnCamToPlayer()
    {
        currentCam.enabled = false;
        playerCam.enabled = true;
        currentCam = playerCam;


        return;
    }

    public void SwapCamera(Camera newCamera)
    {
        currentCam.enabled = false;
        currentCam = newCamera;
        currentCam.enabled = true;

        return;
    }



}
