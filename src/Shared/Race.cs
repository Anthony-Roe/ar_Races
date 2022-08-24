namespace Races.Shared
{
    using System.Collections.Generic;

    using Races.Shared;

    public class Race
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public RaceType Type { get; set; }

        public List<Point> PointList { get; set; }
    }
}