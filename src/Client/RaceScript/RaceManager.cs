namespace Races.Client.RaceScript
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using CitizenFX.Core;
    using static CitizenFX.Core.Native.API;

    using Client.RaceScript.Modules;

    using Races.Client.RaceScript.Interfaces;

    using Shared;

    using Point = Races.Shared.Point;

    public class RaceManager : BaseScript
    {
        public static List<IRace> Races = null;

        public static RaceStatus Status = RaceStatus.Idle;

        public static Static_TimeAttackCreator Creator;

        public async void Initialize()
        {
            Races = new List<IRace>();
            Creator = new Static_TimeAttackCreator();

            RegisterCommand("createrace", new Action<int, List<object>, string>(
                (int source, List<object> args, string raw) => { if (Status == RaceStatus.Idle) Creator.CreateRace(); }), false);
            RegisterCommand("finish", new Action<int, List<object>, string>(
                (int source, List<object> args, string raw) => { if (Status == RaceStatus.Creating) Creator.Finish(); }), false);

            Tick();
            SlowTick();
        }

        public async Task PopulateRaces(List<Race> races)
        {
            try
            {
                Races = new List<IRace>();
                // TO DO: Get list of already made races
                foreach (var race in races)
                {
                    switch (race.Type)
                    {
                        case RaceType.Static_TimeAttack:
                            Races.Add(new Static_TimeAttack {Name = race.Name, PointList = race.PointList});
                            break;
                    }
                }
                
            }
            catch (Exception e)
            {
                Debug.WriteLine("Error in Populate: " + e);
            }
        }

        /// <summary>
        /// Starts checking
        /// </summary>
        private async Task Tick()
        {
            while (true)
            {
                try
                {
                    if (Status != RaceStatus.Creating && Races.Count > 0)
                    {
                        foreach (var race in Races)
                        {
                            await race.DrawStartMarker();
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Error in StartCheck: " + e);
                }

                await Delay(0);
            }
        }

        /// <summary>
        /// Slow tick
        /// </summary>
        private async Task SlowTick()
        {
            while (true)
            {
                try
                {
                    if (Status != RaceStatus.Creating && Races.Count > 0)
                    {
                        foreach (var race in Races)
                        {
                            race.distanceFromStart = Vector3.Distance(LocalPlayer.Character.Position, race.PointList[0].Position);
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Error in StartCheck: " + e);
                }

                await Delay(700);
            }
        }
    }
}