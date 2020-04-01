namespace Races.Client.RaceScript.Interfaces
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Races.Shared;

    public interface IRace
    {
        string Name { get; set; }
        List<Point> PointList { get; set; }

        float distanceFromStart { get; set; }

        /// <summary>
        /// Starts the Race
        /// </summary>
        void StartRace();

        Task DrawStartMarker();

        /// <summary>
        /// Handles checkpoints and timer
        /// </summary>
        /// <returns></returns>
        Task HandleRace();

        void FinishRace();

        void CancelRace();
    }
}
