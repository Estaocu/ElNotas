using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DiscordManager : MonoBehaviour
{
    Discord.Discord discord;
    
    // Start is called before the first frame update
    void Start()
    {
        discord = new Discord.Discord(1540003265406570586, (ulong)Discord.CreateFlags.NoRequireDiscord);
        ChangeActivity();
    }
    
    void OnDisable() {
        discord.Dispose();
    }
    
    public void ChangeActivity()
    {
        var activityManager = discord.GetActivityManager();

        var activity = new Discord.Activity
        {
            Details = "In-Engine",
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
    
    // Update is called once per frame
    void Update()
    {
        discord.RunCallbacks();
    }
}