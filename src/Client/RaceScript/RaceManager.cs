namespace Races.Client.RaceScript
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
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

        public static List<Time> Times = null;

        public static RaceStatus Status = RaceStatus.Idle;

        public static Static_TimeAttackCreator TimeAttackCreator;

        public async void Initialize()
        {
            Races = new List<IRace>();
            Times = new List<Time>();
            TimeAttackCreator = new Static_TimeAttackCreator();

            // Time Attack
            this.Exports.Add("TimerCreateRace", new Action(TimeAttackCreator.CreateRace));
            this.Exports.Add("TimerFinishRace", new Action(TimeAttackCreator.Finish));
            this.Exports.Add("TimerPlacePoint", new Action(TimeAttackCreator.PlacePoint));
            this.Exports.Add("TimerCancelRace", new Action(TimeAttackCreator.Cancel));
            this.Exports.Add("TimerDeletePreviousCP", new Action(TimeAttackCreator.DeletePreviousCheckpoint));
            this.Exports.Add("TimerSetName", new Action<string>(TimeAttackCreator.Set));// Set scale
            this.Exports.Add("TimerSetScale", new Action<int, int, int>(TimeAttackCreator.Set));// Set scale
            this.Exports.Add("TimerSetOffset", new Action<float, float, float>(TimeAttackCreator.Set));// Set scale
            this.Exports.Add("TimerSetMarker", new Action<int>(TimeAttackCreator.Set)); // Set marker type
            this.Exports.Add("TimerSetColor", new Action<float, float, float, float>(TimeAttackCreator.Set)); // Set marker color


            this.Exports.Add("GetStatus", new Func<string>(() => Status.ToString()));

            Tick();
            SlowTick();
        }

        public async Task PopulateRaces(List<Race> races)
        {
            try
            {
                Races = new List<IRace>();
                foreach (var race in races)
                {
                    switch (race.Type)
                    {
                        case RaceType.Static_TimeAttack:
                            Races.Add(new Static_TimeAttack {Name = race.Name, PointList = race.PointList, Id = race.Id, Type = race.Type});
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("Error in Populate Races: " + e);
            }
        }

        public async Task PopulateTimes(List<Time> times)
        {
            try
            {
                Times = new List<Time>();
                Times = times;
                foreach (var race in Races)
                {
                    var best = Times.Where(r => r.MapId == race.Id).OrderBy(t => TimeSpan.Parse(t.TotalTime));
                    if (best.Any())
                        race.BestTime = best.First();
                    else
                        race.BestTime = new Time();
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("Error in Populate Times: " + e);
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
                            await race.Tick();
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

                await Delay(100);
            }
        }
    }
}