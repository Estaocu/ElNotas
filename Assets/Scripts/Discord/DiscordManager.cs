using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DiscordManager : MonoBehaviour
{
    Discord.Discord discord;
    
    // Start is called before the first frame update
    void Start()
    {
        try
        {
            discord = new Discord.Discord(1540003265406570586, (ulong)Discord.CreateFlags.NoRequireDiscord);
            ChangeActivity();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Discord Rich Presence disabled or failed to initialize: {e.Message}");
            discord = null;
        }
    }
    
    void OnDisable()
    {
        if (discord != null)
        {
            discord.Dispose();
            discord = null;
        }
    }
    
    public void ChangeActivity()
    {

        if (discord == null)
        {
            return;
        }
        
        var activityManager = discord.GetActivityManager();

        var activity = new Discord.Activity
        {
            Details = "chat yipití ayuda porfa 😭",
            //State = "Tocando la gaita",

            Assets =
            {
                LargeImage = "josebacara",
                //LargeText = "Pueblo Flautín",

                //SmallImage = "josebacara",
                //SmallText = "Joseba"
            },

            Timestamps =
            {
                Start = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            }
        };

        activityManager.UpdateActivity(activity, (res) =>
        {
            Debug.Log("Discord Activity: " + res);
        });
    }
    
    void Update()
    {
        if (discord == null)
        {
            return;
        }

        discord.RunCallbacks();
    }
}