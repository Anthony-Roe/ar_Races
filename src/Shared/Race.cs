namespace Races.Shared
{
    using System.Collections.Generic;

    using Races.Shared;

    public class Race
    {
        public string Name { get; set; }

        public RaceType Type { get; set; }

        public List<Point> PointList { get; set; }
    }
}