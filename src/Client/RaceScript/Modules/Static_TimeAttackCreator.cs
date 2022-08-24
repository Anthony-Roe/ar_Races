namespace Races.Client.RaceScript.Modules
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using System.Threading.Tasks;

    using CitizenFX.Core;
    using static CitizenFX.Core.Native.API;

    using Newtonsoft.Json;

    using Races.Shared;

    using Point = Shared.Point;

    public class Static_TimeAttackCreator : BaseScript
    {

        public List<Point> Points;

        public string Name = "Change me!";
        public Vector3 MarkerScale = new Vector3(6, 6, 1);
        public Vector3 MarkerOffset = new Vector3(0, 0, 0);
        public MarkerType Type = MarkerType.ChevronUpx1;
        public Vector4 MarkerColors = new Vector4(255);

        public async void CreateRace()
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
                World.DrawMarker(Type, veh.GetOffsetPosition(this.MarkerOffset), Vector3.Zero, GetEntityRotation(veh.Handle, 4), MarkerScale, Color.FromArgb((int)MarkerColors.X, (int)MarkerColors.Y, (int)MarkerColors.Z, (int)MarkerColors.W));
                await Delay(0);
            }

        }

        public async void Finish()
        {
            if (Points.Count > 1)
            {
                RaceManager.Status = RaceStatus.Idle;
                Race race = new Race { Name = Name, PointList = Points, Type = RaceType.Static_TimeAttack };
                //RaceManager.Races.Add(race);
                TriggerServerEvent("ar_Races:CreateRace", JsonConvert.SerializeObject(race));
            }
            else
            {
                this.Cancel();
            }
        }

        public void Cancel()
        {
            Points = null;
            RaceManager.Status = RaceStatus.Idle;
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
                    World.DrawMarker((MarkerType)point.Marker, point.Position, Vector3.Zero, point.Rotation, point.Scale, Color.FromArgb((int)point.MarkerColor.X, (int)point.MarkerColor.Y, (int)point.MarkerColor.Z, (int)point.MarkerColor.W), true);
                }

                await Delay(0);
            }
        }

        public void PlacePoint()
        {
            var veh = LocalPlayer.Character.CurrentVehicle;
            this.Points.Add(new Point { Marker = (int)Type, Position = veh.GetOffsetPosition(this.MarkerOffset), Rotation = GetEntityRotation(veh.Handle, 4), Scale = this.MarkerScale, MarkerColor = new Vector4(MarkerColors.X, MarkerColors.Y, MarkerColors.Z, MarkerColors.W) });
        }

        public void DeletePreviousCheckpoint()
        {
            this.Points.RemoveAt(Points.Count-1);
        }

        /// <summary>
        /// Set Marker
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        public void Set(int x, int y, int z)
        {
            MarkerScale.X = x;
            MarkerScale.Y = y;
            MarkerScale.Z = z;
        }

        /// <summary>
        /// Set Marker
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        public void Set(float x, float y, float z)
        {
            MarkerOffset.X = x;
            MarkerOffset.Y = y;
            MarkerOffset.Z = z;
        }

        /// <summary>
        /// Set Marker
        /// </summary>
        /// <param name="type"></param>
        public void Set(int type)
        {
            this.Type = (MarkerType)type;
        }

        /// <summary>
        /// Set Color
        /// </summary>
        /// <param name="color"></param>
        public void Set(float a, float r, float g, float b)
        {
            this.MarkerColors = new Vector4(a, r, g, b);
        }

        public void Set(string text)
        {
            this.Name = text;
        }
    }
}