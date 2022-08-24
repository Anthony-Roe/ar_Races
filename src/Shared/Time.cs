namespace Races.Shared
{
    using System;
    using System.Collections.Generic;

    public class Time
    {
        public int MapId { get; set; }

        public string License { get; set; }
        public string Name { get; set; }

        public string Car { get; set; }

        public string TotalTime { get; set; }

        public string GapTimesData { get; set; }
    }
}