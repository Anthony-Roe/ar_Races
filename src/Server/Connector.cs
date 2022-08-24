using CitizenFX.Core;
using NN_Framework;
using Races.Shared;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Shared;

using vNoSQL_Api;
using vNoSQL_Api.JsonQuery;

namespace Races.Server
{
    public class Data
    {
        private vNoSQLClient<DBPlayer> users = new vNoSQLClient<DBPlayer>("users");
        private vNoSQLClient<DBMap> maps = new vNoSQLClient<DBMap>("maps");

        public Data() { }

        public void AddRace(DBMap map) => maps.Insert(map);

        public async void AddTime(Player p, MapStat stats) => await users.Update(p.ToIdentifierFilter(), JsonBuilders<DBPlayer>.Update.AddToSet(e => e.Mapstats, stats));

        public async Task<List<DBMap>> GetAllRaces() => await maps.Find(JsonBuilders<DBMap>.Filter.Empty);

        public static async Task<List<Time>> GetPlayerTimes(Player player)
        {
            return null;
        }

        public static async Task<List<Time>> GetRaceTimes(Race race)
        {
            return null;
        }

        public static async Task<List<Time>> GetBestTimes()
        {
            return null;
        }
    }
}
