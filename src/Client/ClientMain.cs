namespace Races.Client
{
    using System;
    using System.Collections.Generic;

    using CitizenFX.Core;

    using Newtonsoft.Json;

    using static CitizenFX.Core.Native.API;

    using Races.Client.RaceScript;
    using Races.Shared;

    public class ClientMain : BaseScript
    {
        private static RaceManager Manager;

        public ClientMain()
        {
            EventHandlers.Add("onClientResourceStart", new Action<string>(Init));
            EventHandlers.Add("ar_Races:SendRaces", new Action<string>(SetRaces));
        }

        private void Init(string name)
        {
            if (GetCurrentResourceName() != name)
                return;

            Manager = new RaceManager();
            Manager.Initialize();

            TriggerServerEvent("ar_Races:GetRaces");
        }

        private void SetRaces(string jsonData)
        {
            var races = JsonConvert.DeserializeObject<List<Race>>(jsonData);
            Manager.PopulateRaces(races);
        }
    }
}