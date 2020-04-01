namespace Races.Server
{
    using System;
    using System.Collections.Generic;

    using CitizenFX.Core;

    using Newtonsoft.Json;

    using Races.Shared;

    public class ServerMain : BaseScript
    {
        public static List<Race> Races;

        public ServerMain()
        {
            Races = new List<Race>();
            this.EventHandlers.Add("ar_Races:CreateRace", new Action<Player, string>(CreateRace));
            this.EventHandlers.Add("ar_Races:GetRaces", new Action<Player>(SendRaces));
        }

        /// <summary>
        /// Send races to client/all clients
        /// </summary>
        /// <param name="src">Client that requested races</param>
        public static void SendRaces([FromSource] Player src = null)
        {
            if (src != null)
                TriggerClientEvent(src, "ar_Races:SendRaces", JsonConvert.SerializeObject(Races));
            else
                TriggerClientEvent("ar_Races:SendRaces", JsonConvert.SerializeObject(Races));
        }

        /// <summary>
        /// Adds a race to the Races list.
        /// </summary>
        /// <param name="src">Player who called the event (if any)</param>
        /// <param name="jsonData">Data passed through, usually following Race type.</param>
        public static void CreateRace([FromSource] Player src = null, string jsonData = null)
        {
            // Convert json race data to race class
            var data = JsonConvert.DeserializeObject<Race>(jsonData);
            Races.Add(data);
            SendRaces();
        }
    }
}