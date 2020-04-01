namespace Races.Client.RaceScript.Modules
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using System.Threading.Tasks;

    using CitizenFX.Core;
    using CitizenFX.Core.UI;

    using Races.Client.RaceScript.Interfaces;
    using Races.Shared;

    using Font = CitizenFX.Core.UI.Font;
    using Point = Shared.Point;

    public class Static_TimeAttack : BaseScript, IRace
    {
        public string Name { get; set; }
        private DateTime LastTime { get; set; }

        private List<TimeSpan> Times { get; set; }

        public List<Point> PointList { get; set; }
        private Vehicle RaceVehicle { get; set; }

        private int NextPointIndex { get; set; }

        private int startSound { get; set; }

        private bool RemovingCollisions { get; set; }

        public float distanceFromStart { get; set; } = 0.0f;

        public Static_TimeAttack()
        {
            Debug.WriteLine("Init time attack");
        }

        public async void StartRace()
        {
            try
            {
                if (this.LocalPlayer.Character.CurrentVehicle == null)
                    return;
                if (this.PointList.Count < 2)
                    return;

                // Set point after start
                NextPointIndex = 1;
                RaceManager.Status = RaceStatus.Starting;

                await CountDown();

                Times = new List<TimeSpan>();
                this.LastTime = DateTime.UtcNow;

                // Start race handler
                if (RaceManager.Status != RaceStatus.Cancelled)
                {
                    RaceManager.Status = RaceStatus.Racing;
                    await this.HandleRace();
                }
                else
                {
                    RaceManager.Status = RaceStatus.Idle;
                }
                    
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }

        public async Task DrawStartMarker()
        {
            if (distanceFromStart < 100)
            {
                if (RaceManager.Status != RaceStatus.Starting)
                    World.DrawMarker((MarkerType)PointList[0].Marker, PointList[0].Position, Vector3.Zero, new Vector3(0, 0, PointList[0].Heading), PointList[0].Scale, Color.FromArgb(0, 145, 177));
                if (distanceFromStart < PointList[0].Scale.X)
                {
                    this.RaceVehicle = this.LocalPlayer.Character.CurrentVehicle;
                    if (this.RaceVehicle != null)
                    {
                        RemoveCollisions();
                        if (Game.IsControlJustReleased(2, Control.VehicleHorn) && (RaceManager.Status == RaceStatus.Idle || RaceManager.Status == RaceStatus.Cancelled))
                            this.StartRace();
                        else if (Game.IsControlJustReleased(2, Control.VehicleHorn) && (RaceManager.Status == RaceStatus.Starting || RaceManager.Status == RaceStatus.Racing))
                            this.CancelRace();
                    }
                }
            }
        }

        private async Task RemoveCollisions()
        {
            if (RemovingCollisions)
                return;

            RemovingCollisions = true;
            while (Vector3.Distance(PointList[0].Position, LocalPlayer.Character.Position) < 7)
            {
                List<Vehicle> Vehicles = World.GetAllVehicles().Where(veh => (veh.Handle != RaceVehicle.Handle)).ToList();
                Vehicles.ForEach(
                    veh =>
                        {
                            veh.SetNoCollision(RaceVehicle, true);
                            veh.Opacity = 155;


                        });
                await Delay(0);
            }
            List<Vehicle> VehicleList = World.GetAllVehicles().Where(veh => (veh.Handle != RaceVehicle.Handle)).ToList();
            VehicleList.ForEach(
                veh =>
                    {
                        if (veh.IsCollisionEnabled == false)
                        {
                            veh.SetNoCollision(RaceVehicle, false);
                            //veh.IsCollisionEnabled = true;
                            
                        }

                        if (veh.Opacity == 155)
                        {
                            veh.Opacity = 255;
                        }

                    });

            RemovingCollisions = false;
        }

        public async Task CountDown()
        {
            Text countText = new Text("Starts in: 10 seconds", new PointF(640f, 355f), 1.0f, Color.FromArgb(255, 0, 0, 255), Font.Pricedown, Alignment.Center);
            var time = DateTime.UtcNow.AddSeconds(10.0);
            countText.Enabled = true;

            startSound = Audio.PlaySoundFromEntity(RaceVehicle, "10s", "MP_MISSION_COUNTDOWN_SOUNDSET");
            //Set start position
            RaceVehicle.Position = PointList[0].Position;
            float newHeading = PointList[1].Heading;
            RaceVehicle.Heading = newHeading;
            while (DateTime.UtcNow <= (time.Subtract(new TimeSpan(0,0,0,1))) && RaceManager.Status == RaceStatus.Starting)
            {
                // Draw countdown
                countText.Caption = $"Starts in: {Math.Floor((time - DateTime.UtcNow).TotalSeconds)} seconds";
                countText.Draw();

                // Draw marker after start
                World.DrawMarker((MarkerType)PointList[NextPointIndex].Marker, PointList[NextPointIndex].Position, Vector3.Zero, new Vector3(0, 0, PointList[NextPointIndex].Heading), PointList[NextPointIndex].Scale, Color.FromArgb(0, 145, 177));

                //Stop vehicle from moving
                //RaceVehicle.IsHandbrakeForcedOn = true;
                RaceVehicle.IsBurnoutForced = true;

                await Delay(0);
            }
            Audio.StopSound(startSound);
            RaceVehicle.IsBurnoutForced = false;
            countText.Enabled = false;
        }

        public async Task HandleRace()
        {
            while (RaceManager.Status == RaceStatus.Racing)
            {
                World.DrawMarker((MarkerType)PointList[NextPointIndex].Marker, PointList[NextPointIndex].Position, Vector3.Zero, new Vector3(0, 0, PointList[NextPointIndex].Heading), PointList[NextPointIndex].Scale, Color.FromArgb(0, 145, 177));
                if (NextPointIndex < (PointList.Count - 1))
                    World.DrawMarker((MarkerType)PointList[NextPointIndex + 1].Marker, PointList[NextPointIndex + 1].Position, Vector3.Zero, new Vector3(0, 0, PointList[NextPointIndex + 1].Heading), PointList[NextPointIndex + 1].Scale, Color.FromArgb(0, 145, 177));
                // Get distance to next checkpoint
                if (Vector3.Distance(this.RaceVehicle.Position, this.PointList[this.NextPointIndex].Position)
                    < this.PointList[this.NextPointIndex].Scale.X)
                    this.CheckPoint();

                await Delay(0);
            }
        }

        public void CheckPoint()
        {
            var currentTime = DateTime.UtcNow;
            this.Times.Add(currentTime - this.LastTime);
            this.LastTime = currentTime;

            // Set next checkpoint
            if (this.NextPointIndex >= (this.PointList.Count - 1))
            {
                Game.PlaySound("Goal", "DLC_HEIST_HACKING_SNAKE_SOUNDS");
                RaceManager.Status = RaceStatus.Idle;
                this.FinishRace();
            }
            else
            {
                Game.PlaySound("Checkpoint_Hit", "GTAO_FM_Events_Soundset");
                this.NextPointIndex++;
            }
        }

        public void FinishRace()
        {
            distanceFromStart = Single.MaxValue;

            var test = 0.0;

            for (int i = 0; i < Times.Count; i++)
            {
                Debug.WriteLine($"From {i - 1} to {i}: {Times[i].TotalSeconds}");
                test += Times[i].TotalSeconds;
            }

            Debug.WriteLine("Test Time: " + test);
        }

        public void CancelRace()
        {
            RaceManager.Status = RaceStatus.Cancelled;
            distanceFromStart = Single.MaxValue;
        }
    }
}