using System;
using System.Collections.Generic;
using CitizenFX.Core;

namespace Shared
{
    public class DBPlayer : NN_Framework.Data.DBPlayer
    {
        public List<MapStat> Mapstats { get; set; }
    }

    public class MapStat
    {
        public int MapID { get; set; }
        public string Car { get; set; }
        public int Score { get; set; }
        public DateTime dateTime { get; set; }
    }

    public class DBMap
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public List<Point> PointList { get; set; }
    }

    public class Point 
    {
        public Vector4 MarkerColor { get; set; }
        public Vector3 Position { get; set; }
        public Vector3 Rotation { get; set; }
        public Vector3 Scale { get; set; }
        public int Marker { get; set; }
    }
}
