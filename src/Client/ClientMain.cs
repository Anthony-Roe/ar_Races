using System;
using System.Collections.Generic;

using CitizenFX.Core;

using Newtonsoft.Json;

using static CitizenFX.Core.Native.API;

using Races.Client.RaceScript;
using Races.Shared;

namespace Races.Client
{
    public class ClientMain : BaseScript
    {
        private static RaceManager Manager;

        public ClientMain()
        {
            RegisterFontFile("Lulo");
            EventHandlers.Add("onClientResourceStart", new Action<string>(Init));
            EventHandlers.Add("ar_Races:SendRaces", new Action<string>(SetRaces));
            EventHandlers.Add("ar_Races:SendTimes", new Action<string>(SetTimes));
        }

        private void Init(string name)
        {
            if (GetCurrentResourceName() != name)
                return;

            Manager = new RaceManager();
            Manager.Initialize();

            TriggerServerEvent("ar_Races:GetRaces");
            TriggerServerEvent("ar_Races:GetTimes");
        }

        private void SetRaces(string jsonData)
        {
            var races = JsonConvert.DeserializeObject<List<Race>>(jsonData);
            Manager.PopulateRaces(races);
        }

        private void SetTimes(string jsonData)
        {
            var times = JsonConvert.DeserializeObject<List<Time>>(jsonData);
            Manager.PopulateTimes(times);
        }
    }
}