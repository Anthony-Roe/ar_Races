using System;
using System.Collections.Generic;
using System.Linq;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using Newtonsoft.Json;
using Races.Shared;
using Debug = NN_Framework.Debug;

namespace Races.Server
{

    public class ServerMain : BaseScript
    {
        public static List<Race> Races;

        public static List<Time> Times;

        public ServerMain()
        {
            Initialize();
        }

        private void Initialize()
        {
            Exports["vSql"].ready(new Action(
                () =>
                    {
                        this.PopulateRaces();
                        this.PopulateTimes();
                        // Setup events
                        this.EventHandlers.Add("ar_Races:CreateRace", new Action<Player, string>(CreateRace));
                        this.EventHandlers.Add("ar_Races:GetRaces", new Action<Player>(SendRaces));
                        this.EventHandlers.Add("ar_Races:GetTimes", new Action<Player>(SendTimes));
                        this.EventHandlers.Add("ar_Races:RegisterTime", new Action<Player, string>(RegisterTime));
                    })
            );
        }

        public void PopulateRaces()
        {
            Races = new List<Race>();
            Exports["vSql"].fetch_all_async(
                "SELECT * FROM nonamedrift.maps", new Dictionary<string,object>(),
                new Action<List<dynamic>>(
                    (result) =>
                        {
                            foreach (IDictionary<string, object> row in result)
                            {
                                var race = new Race
                                               {
                                                   Id = (int)row["mapId"],
                                                   Name = (string)row["mapName"],
                                                   Type = (RaceType)row["type"],
                                                   PointList = JsonConvert.DeserializeObject<List<Point>>((string)row["checkpoints"])
                                               };
                                Races.Add(race);
                            }
                            SendRaces();
                        }));
        }

        public void PopulateTimes()
        {
            Times = new List<Time>();
            Exports["vSql"].fetch_all_async(
                "SELECT * FROM nonamedrift.scores", new Dictionary<string, object>(),
                new Action<List<dynamic>>(
                    (result) =>
                        {
                            foreach (IDictionary<string, object> row in result)
                            {
                                var time = new Time
                                               {
                                                   License = (string)row["license"],
                                                   MapId = (int)row["map"],
                                                   Name = (string)row["username"],
                                                   Car = (string)row["car"],
                                                   TotalTime = (string)row["total_time"],
                                                   GapTimesData = (string)row["gap_times"],
                                               };
                                //Debug.WriteLine("[ar_Races]: Timelicense = "+time.License);
                                Times.Add(time);
                            }
                            SendTimes();
                        }));
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
        /// Sends best times to client/all
        /// </summary>
        /// <param name="src">Client that requested times</param>
        public static void SendTimes([FromSource] Player src = null)
        {
            if (src != null)
                TriggerClientEvent(src, "ar_Races:SendTimes", JsonConvert.SerializeObject(Times));
            else
                TriggerClientEvent("ar_Races:SendTimes", JsonConvert.SerializeObject(Times));
        }

        /// <summary>
        /// Adds a race to the Races list.
        /// </summary>
        /// <param name="src">Player who called the event (if any)</param>
        /// <param name="jsonData">Data passed through, usually following Race type.</param>
        public void CreateRace([FromSource] Player src = null, string jsonData = null)
        {
            // Convert json race data to race class
            var data = JsonConvert.DeserializeObject<Race>(jsonData);
            Exports["vSql"].execute_async("INSERT HIGH_PRIORITY INTO nonamedrift.maps SET type=@type, mapName=@mapName, checkpoints=@checkpoints", 
                new Dictionary<string, dynamic>{ { "@type", (int)data.Type}, { "@mapName", (string)data.Name }, { "@checkpoints", JsonConvert.SerializeObject(data.PointList) } }, 
                new Action<int>(
                    (rows) =>
                        {
                            Races.Add(data);
                            this.PopulateRaces();
                            this.PopulateTimes();
                        })
                );
        }

        public void RegisterTime([FromSource] Player src, string jsonData)
        {
            Time time = JsonConvert.DeserializeObject<Time>(jsonData);
            time.License = time.License == null ? src.Identifiers["license"] : time.License;
            Exports["vSql"].execute_async("INSERT HIGH_PRIORITY IGNORE INTO nonamedrift.identifiers SET license=@license, discord=@discord, steam=@steam",
                new Dictionary<string, dynamic> { { "@license", src.Identifiers["license"] }, { "@discord", src.Identifiers["discord"] }, { "@steam", src.Identifiers["steam"] } },
                new Action<int>(
                    (rows) =>
                        {
                            var newTotal = TimeSpan.Parse(time.TotalTime);
                            var oldTimes = Times.Where(r => r.MapId == time.MapId).OrderBy(t => TimeSpan.Parse(t.TotalTime));
                            var oldPlayerTime = oldTimes.Where(r => r.License == time.License);
                            if (oldPlayerTime.Any())
                            {
                                Debug.WriteLine("Player found");
                                if (newTotal < TimeSpan.Parse(oldPlayerTime.First().TotalTime))
                                {
                                    Debug.WriteLine("Writing new time");
                                    Exports["vSql"].execute_async("UPDATE nonamedrift.scores SET username=@username, car=@car, total_time=@totalTime, gap_times=@gapTimes WHERE license=@license AND map=@mapId",
                                        new Dictionary<string, dynamic> { { "@car", time.Car }, { "@username", time.Name }, { "@license", time.License }, { "@mapId", time.MapId }, { "@totalTime", time.TotalTime }, { "@gapTimes", time.GapTimesData } },
                                        new Action<int>(
                                            (rows2) =>
                                                {
                                                    if (rows2 > 0)
                                                    {
                                                        if (oldTimes.Any() == false || newTotal < TimeSpan.Parse(oldTimes.First().TotalTime))
                                                            TriggerClientEvent("chat:addMessage", $"^1^*^_{time.Name}^7^r has set a new best time of, ^1^*^_{TimeSpan.Parse(time.TotalTime):mm\\:ss\\.ff's'}^7^r at, ^1^*^_{Races.First(r => r.Id == time.MapId).Name}");
                                                        Times.Add(time);
                                                        SendTimes();
                                                    }
                                                })
                                    );
                                }
                            }
                            else
                            {
                                Debug.WriteLine("Player not found");
                                this.Exports["vSql"].execute_async("INSERT INTO nonamedrift.scores SET license=@license, map=@mapId, username=@username, car=@car, total_time=@totalTime, gap_times=@gapTimes",
                                    new Dictionary<string, dynamic> { { "@license", time.License }, { "@mapId", time.MapId }, { "@username", time.Name }, { "@car", time.Car }, { "@totalTime", time.TotalTime }, { "@gapTimes", time.GapTimesData } },
                                    new Action<int>(
                                        (rows2) =>
                                            {
                                                if (rows2 > 0)
                                                {
                                                    if (oldTimes.Any() == false || newTotal < TimeSpan.Parse(oldTimes.First().TotalTime))
                                                        TriggerClientEvent("chat:addMessage", $"^1^*^_{time.Name}^7^r has set a new best time of, ^1^*^_{TimeSpan.Parse(time.TotalTime):mm\\:ss\\.ff's'}^7^r at, ^1^*^_{Races.First(r => r.Id == time.MapId).Name}");
                                                    Times.Add(time);
                                                    SendTimes();
                                                }
                                            })
                                );
                            }
                        })
            );
        }
    }
}