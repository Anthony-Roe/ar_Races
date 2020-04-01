namespace Races.Client.RaceScript.Modules
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using System.Threading.Tasks;

    using CitizenFX.Core;

    using Newtonsoft.Json;

    using Races.Shared;

    using Point = Shared.Point;

    public class Static_TimeAttackCreator : BaseScript
    {

        public List<Point> Points;

        public async Task CreateRace()
        {
            this.Points = new List<Point>();

            RaceManager.Status = RaceStatus.Creating;
            DrawPointMarkers();
            while (RaceManager.Status == RaceStatus.Creating)
            {
                if (LocalPlayer.Character.CurrentVehicle == null)
                {
                    RaceManager.Status = RaceStatus.Idle;
                    break;
                }
                var veh = LocalPlayer.Character.CurrentVehicle;
                World.DrawMarker(MarkerType.HorizontalCircleSkinny, new Vector3(veh.Position.X, veh.Position.Y, veh.Position.Z - 0.2f), Vector3.Zero, new Vector3(0, 0, veh.Rotation.Z), new Vector3(6, 6, 1), Color.FromArgb(0, 145, 177), true);
                if (Game.IsControlJustReleased(2, Control.VehicleHorn))
                    await this.PlacePoint();
                else if (Game.IsControlJustReleased(2, Control.VehicleLookBehind))
                    RaceManager.Status = RaceStatus.Idle;
                await Delay(0);
            }

        }

        public async Task Finish()
        {
            RaceManager.Status = RaceStatus.Idle;
            Race race = new Race { Name = "Testing " + new Random().NextDouble(), PointList = Points, Type = RaceType.Static_TimeAttack};
            //RaceManager.Races.Add(race);
            TriggerServerEvent("ar_Races:CreateRace", JsonConvert.SerializeObject(race));
        }

        private async Task DrawPointMarkers()
        {
            while (RaceManager.Status == RaceStatus.Creating)
            {
                if (LocalPlayer.Character.CurrentVehicle == null)
                {
                    RaceManager.Status = RaceStatus.Idle;
                    break;
                }
                foreach (Point point in this.Points)
                {
                    World.DrawMarker((MarkerType)point.Marker, point.Position, Vector3.Zero, new Vector3(0, 0, point.Heading), point.Scale, Color.FromArgb(0, 145, 177), true);
                }

                await Delay(0);
            }
        }

        private Task PlacePoint()
        {
            var veh = LocalPlayer.Character.CurrentVehicle;

            if (this.Points.Count > 0)
            {
                if (this.Points.Count > 1)
                {
                    var lastPoint = this.Points.Last();
                    lastPoint.Marker = (int)MarkerType.HorizontalSplitArrowCircle;
                    lastPoint.Position = new Vector3(lastPoint.Position.X, lastPoint.Position.Y, lastPoint.Position.Z - 1.4f);

                }
                this.Points.Add(new Point { Marker = (int)MarkerType.CheckeredFlagRect, Position = new Vector3(veh.Position.X, veh.Position.Y, veh.Position.Z + 1.2f), Heading = veh.Rotation.Z, Scale = new Vector3(6, 6, 3)});
            }
            else
            {
                this.Points.Add(new Point { Marker = (int)MarkerType.ChevronUpx1, Position = new Vector3(veh.Position.X, veh.Position.Y, veh.Position.Z + 1.2f), Heading = veh.Rotation.Z, Scale = new Vector3(6, 6, 3) });
            }

            return Task.FromResult(0);
        }
    }
}