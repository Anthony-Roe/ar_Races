namespace Races.Client.RaceScript.Modules
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using System.Threading.Tasks;

    using CitizenFX.Core;
    using static CitizenFX.Core.Native.API;
    using CitizenFX.Core.UI;

    using Newtonsoft.Json;

    using Races.Client.RaceScript.Interfaces;
    using Races.Shared;

    using Font = CitizenFX.Core.UI.Font;
    using Point = Shared.Point;

    public class Static_TimeAttack : BaseScript, IRace
    {
        // IRace variables
        public int Id { get; set; }
        public RaceType Type { get; set; }
        public Time BestTime { get; set; }
        public string Name { get; set; }
        public List<Point> PointList { get; set; }

        // Client variables
        private int NextPointIndex { get; set; }
        private Blip NextPointBlip { get; set; }
        private Vehicle RaceVehicle { get; set; }

        private List<DateTime> Times { get; set; }
        private DateTime LastPenalTime { get; set; } = DateTime.UtcNow;
        private float PenalizedTime { get; set; }
        private float TotalPenalizedTime { get; set; }

        private Vector3 PreviousPosition { get; set; }
        private int counter { get; set; }
        public float distanceFromStart { get; set; } = 0.0f;

        private bool RemovingCollisions { get; set; }

        // Gui variables
        private Text DisplayText { get; set; }
        private Sprite DisplaySprite { get; set; }
        private string DisplayFormat { get; set; }
        private Text CheckPointText { get; set; }
        private Text ElapsedText { get; set; }
        private Text LastGapText { get; set; }
        private Text PenalityText { get; set; }

        private int Scale { get; set; }

        public Static_TimeAttack()
        {
            DisplayFormat = "~h~{0}~h~\n\n{1}\n{2}\n{3}\n\nPress E to start";
            DisplayText = new Text(DisplayFormat, PointF.Empty, 1.0f, Color.FromArgb(250, 250, 250), (Font)RegisterFontId("Lulo"), Alignment.Center, true, true);
            SetupScale();
        }

        private async void SetupScale()
        {
            Scale = RequestScaleformMovie("INSTRUCTIONAL_BUTTONS");
            while (!HasScaleformMovieLoaded(Scale))
            {
                await Delay(0);
            }
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

                NextPointBlip = World.CreateBlip(PointList[NextPointIndex].Position);
                NextPointBlip.Name = "Checkpoint: " + NextPointIndex;
                NextPointBlip.Sprite = BlipSprite.Standard;
                NextPointBlip.Color = BlipColor.Yellow;

                counter = 0;
                await RaceReady();

                // Set up hud text
                CheckPointText = new Text($" {this.NextPointIndex - 1}/{PointList.Count - 1}", new PointF(160f, 557f), 0.5f, Color.FromArgb(200, 255, 255, 255), (Font)RegisterFontId("Lulo"), Alignment.Left, true, true);
                ElapsedText = new Text("0s", new PointF(20f, 540f), 1f, Color.FromArgb(200, 255, 255, 255), (Font)RegisterFontId("Lulo"), Alignment.Left, true, true);
                PenalityText = new Text("", new PointF(125f, 557f), 0.5f, Color.FromArgb(200, 255, 255, 255), (Font)RegisterFontId("Lulo"), Alignment.Left, true, true);
                //LastGapText = new Text("Last Gap: 0s", new PointF(100f, 540f), 0.5f, Color.FromArgb(200, 255, 255, 255), (Font)RegisterFontId("Lulo"), Alignment.Center, true, true);

                Times = new List<DateTime>();
                
                await this.HandleRace();

            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }

        public async Task Tick()
        {
            try
            {
                if (distanceFromStart < 100)
                {
                    if (RaceManager.Status != RaceStatus.Starting)
                    {
                        if (RaceManager.Status == RaceStatus.Idle)
                        {
                            World.DrawMarker((MarkerType)PointList[0].Marker, PointList[0].Position, Vector3.Zero, PointList[0].Rotation, PointList[0].Scale, Color.FromArgb((int)PointList[0].MarkerColor.X, (int)PointList[0].MarkerColor.Y, (int)PointList[0].MarkerColor.Z, (int)PointList[0].MarkerColor.W));
                            if(distanceFromStart < 20)
                            {
                                var displayTitle = String.Format(DisplayFormat, Name);
                                if (DisplayText.Caption != displayTitle)
                                    DisplayText.Caption = displayTitle;
                                DrawText3D(DisplayText, 1.5f, new Vector3(PointList[0].Position.X, PointList[0].Position.Y, PointList[0].Position.Z + (PointList[0].Scale.Z + 1f)));

                                var displayText = String.Format(DisplayFormat, Name, BestTime != null && BestTime.Name != null ? BestTime.Name : "", BestTime != null && BestTime.TotalTime != null ? $"{TimeSpan.Parse(BestTime.TotalTime):mm\\:ss\\.ff}" : "", BestTime != null && BestTime.Car != null ? BestTime.Car : "");
                                if (DisplayText.Caption != displayText)
                                    DisplayText.Caption = displayText;
                                DrawText3D(DisplayText, 1f, new Vector3(PointList[0].Position.X, PointList[0].Position.Y, PointList[0].Position.Z + (PointList[0].Scale.Z + 1f)));
                                
                                DisplaySprite = new Sprite("timerbg", "gradient", new SizeF(20, 1), PointF.Empty);
                                DrawSprite(DisplaySprite, new Vector3(PointList[0].Position.X, PointList[0].Position.Y, PointList[0].Position.Z + (PointList[0].Scale.Z + 1f)));
                            }
                        }
                    }

                    if (distanceFromStart < 15)
                    {
                        this.RaceVehicle = this.LocalPlayer.Character.CurrentVehicle;
                        if (this.RaceVehicle != null)
                        {
                            RemoveCollisions();
                        }
                    }

                    if (distanceFromStart < PointList[0].Scale.X)
                    {
                        if (RaceManager.Status == RaceStatus.Idle)
                        {
                            this.PreviousPosition = Game.PlayerPed.CurrentVehicle.Position;
                            if (Game.IsControlJustReleased(2, Control.VehicleHorn) && (RaceManager.Status == RaceStatus.Idle))
                                this.StartRace();
                            if (RaceManager.Status == RaceStatus.Idle)
                                DrawScale("~INPUT_VEH_HORN~", "Start Race");
                        } else if (RaceManager.Status == RaceStatus.Starting)
                        {
                            if (Game.IsControlJustReleased(2, Control.VehicleHorn) && (RaceManager.Status == RaceStatus.Starting || RaceManager.Status == RaceStatus.Racing))
                                this.CancelRace();
                            if (RaceManager.Status == RaceStatus.Starting)
                                DrawScale("~INPUT_VEH_HORN~", "Cancel Race");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }

        private async Task RemoveCollisions()
        {
            if (RemovingCollisions)
                return;

            RemovingCollisions = true;
            while (Vector3.Distance(PointList[0].Position, LocalPlayer.Character.Position) < 15)
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
                        }

                        if (veh.Opacity == 155)
                        {
                            veh.Opacity = 255;
                        }

                    });

            RemovingCollisions = false;
        }

        public async Task RaceReady()
        {
            Text startText = new Text("Start when ready!!!", new PointF(640f, 355f), 0.5f, Color.FromArgb(255, 226, 226, 226), Font.Pricedown, Alignment.Center, true, true);
            startText.Enabled = true;
            RaceVehicle.IsBurnoutForced = true;
            startText.Caption = "";
            //Set start position
            ResetToStart();

            while (RaceManager.Status == RaceStatus.Starting)
            {
                if (!PreviousPosition.Equals(null))
                {
                    if (Vector3.Distance(PreviousPosition, Game.PlayerPed.CurrentVehicle.Position) > 2)
                    {
                        RaceManager.Status = RaceStatus.Cancelled;
                        this.CancelRace();
                    }
                }

                if (!RaceVehicle.Equals(LocalPlayer.Character.CurrentVehicle))
                    this.CancelRace();
                if (!RaceVehicle.IsCollisionEnabled)
                    this.CancelRace();

                // Buttons to cancel race due to exploits
                if (// keyboard
                    Game.CurrentInputMode == InputMode.MouseAndKeyboard &&
                    (Game.IsDisabledControlJustReleased(2, Control.ReplayStartStopRecording) || Game.IsControlJustReleased(2, Control.SaveReplayClip)) ||
                    // gamepad
                    Game.CurrentInputMode == InputMode.GamePad && !Game.IsControlJustReleased(2, Control.VehicleDuck) &&
                    (Game.IsControlJustReleased(2, Control.InteractionMenu) || Game.IsDisabledControlJustReleased(2, Control.VehicleSelectNextWeapon)))
                {
                    RaceManager.Status = RaceStatus.Cancelled;
                    this.CancelRace();
                }

                // Draw countdown

                /* if (Math.Round(RaceVehicle.Speed) < 1)
                     startText.Draw();
                 else*/
                {
                    counter++;
                    await Delay(1);
                    if (counter > 100)
                    {
                        PlaySoundFrontend(-1, "Enter_1st", "GTAO_Magnate_Boss_Modes_Soundset", false);
                        RaceVehicle.IsBurnoutForced = false;
                        World.DrawMarker((MarkerType)PointList[NextPointIndex].Marker, PointList[NextPointIndex].Position, Vector3.Zero, PointList[NextPointIndex].Rotation, PointList[NextPointIndex].Scale, Color.FromArgb((int)PointList[NextPointIndex].MarkerColor.X, (int)PointList[NextPointIndex].MarkerColor.Y, (int)PointList[NextPointIndex].MarkerColor.Z, (int)PointList[NextPointIndex].MarkerColor.W));
                        RaceManager.Status = RaceStatus.Racing;
                        break;
                    }
                }

                // Draw marker after start
                //World.DrawMarker((MarkerType)PointList[NextPointIndex].Marker, PointList[NextPointIndex].Position, Vector3.Zero, PointList[NextPointIndex].Rotation, PointList[NextPointIndex].Scale, Color.FromArgb((int)PointList[NextPointIndex].MarkerColor.X, (int)PointList[NextPointIndex].MarkerColor.Y, (int)PointList[NextPointIndex].MarkerColor.Z, (int)PointList[NextPointIndex].MarkerColor.W));
            }
            RaceVehicle.IsBurnoutForced = false;
            startText.Enabled = false;
        }

        public async Task HandleRace()
        {
            Times.Add(DateTime.UtcNow);
            while (RaceManager.Status == RaceStatus.Racing)
            {
                if (Vector3.Distance(PreviousPosition, Game.PlayerPed.CurrentVehicle.Position) >= 3)
                {
                    RaceManager.Status = RaceStatus.Cancelled;
                    this.CancelRace();
                }

                if (!RaceVehicle.Equals(LocalPlayer.Character.CurrentVehicle))
                    this.CancelRace();
                if (!RaceVehicle.IsCollisionEnabled)
                    this.CancelRace();

                //Debug.WriteLine("[ar_Races]: PenaltyTime = "+(LastPenalTime <= DateTime.UtcNow).ToString());
                if (LastPenalTime <= DateTime.UtcNow && RaceVehicle.HasCollided)
                {
                    this.PenalizeTime();
                } else if (LastPenalTime > DateTime.UtcNow)
                {
                    PenalityText.Color = Color.FromArgb(250, 250, 250);
                }

                DrawScale("~INPUT_HUD_SPECIAL~", "Reset Race");

                PenalityText.Caption = "+"+TotalPenalizedTime+"s";
                PenalityText.Draw();

                CheckPointText.Draw();
                ElapsedText.Caption = $"{(DateTime.UtcNow - this.Times[0]):mm\\:ss\\.ff}";
                ElapsedText.Draw();
                
                // Deactivated until later update
                //LastGapText.Draw();

                World.DrawMarker((MarkerType)PointList[NextPointIndex].Marker, PointList[NextPointIndex].Position, Vector3.Zero, PointList[NextPointIndex].Rotation, PointList[NextPointIndex].Scale, Color.FromArgb((int)PointList[NextPointIndex].MarkerColor.X, (int)PointList[NextPointIndex].MarkerColor.Y, (int)PointList[NextPointIndex].MarkerColor.Z, (int)PointList[NextPointIndex].MarkerColor.W));
                if (NextPointIndex < (PointList.Count - 1))
                    World.DrawMarker((MarkerType)PointList[NextPointIndex + 1].Marker, PointList[NextPointIndex + 1].Position, Vector3.Zero, PointList[NextPointIndex + 1].Rotation, PointList[NextPointIndex + 1].Scale, Color.FromArgb((int)PointList[NextPointIndex + 1].MarkerColor.X, (int)PointList[NextPointIndex + 1].MarkerColor.Y, (int)PointList[NextPointIndex + 1].MarkerColor.Z, (int)PointList[NextPointIndex + 1].MarkerColor.W));
                // Get distance to next checkpoint
                if (Vector3.Distance(this.RaceVehicle.Position, this.PointList[this.NextPointIndex].Position)
                    < (this.PointList[this.NextPointIndex].Scale.X * 0.85))
                    this.CheckPoint();
                if (Game.IsControlJustReleased(2, Control.HUDSpecial))
                {
                    this.CancelRace();
                    this.StartRace();
                }

                // Buttons to cancel race due to exploits
                if (// keyboard
                    Game.CurrentInputMode == InputMode.MouseAndKeyboard &&
                    (Game.IsDisabledControlJustReleased(2, Control.ReplayStartStopRecording) || Game.IsControlJustReleased(2, Control.SaveReplayClip)) ||
                    // gamepad
                    Game.CurrentInputMode == InputMode.GamePad && !Game.IsControlJustReleased(2, Control.VehicleDuck) &&
                    (Game.IsControlJustReleased(2, Control.InteractionMenu) || Game.IsDisabledControlJustReleased(2, Control.VehicleSelectNextWeapon)))
                {
                    RaceManager.Status = RaceStatus.Cancelled;
                    this.CancelRace();
                }

                await Delay(1);
                PreviousPosition = Game.PlayerPed.CurrentVehicle.Position;
            }
        }

        private void PenalizeTime()
        {
            LastPenalTime = DateTime.UtcNow.AddSeconds(0.5f);
            PenalizedTime = 2f;
            TotalPenalizedTime += PenalizedTime;
            PenalityText.Color = Color.FromArgb(250, 0, 0);
            //Screen.ShowNotification($"{TotalPenalizedTime} Seconds have been added to your time for colliding", true);
        }

        public void CheckPoint()
        {
            var currentTime = DateTime.UtcNow.AddSeconds(PenalizedTime);
            PenalizedTime = 0;
            this.Times.Add(currentTime);

            // Set next checkpoint
            if (this.NextPointIndex >= (this.PointList.Count - 1))
            {
                NextPointBlip.Delete();
                NextPointBlip = null;
                Game.PlaySound("CHECKPOINT_PERFECT", "HUD_MINI_GAME_SOUNDSET");
                RaceManager.Status = RaceStatus.Idle;
                this.FinishRace();
            }
            else
            {
                Game.PlaySound("Beep_Red", "DLC_HEIST_HACKING_SNAKE_SOUNDS");
                this.NextPointIndex++;
                CheckPointText.Caption = $"{this.NextPointIndex - 1}/{PointList.Count - 1}";
                NextPointBlip.Position = this.PointList[this.NextPointIndex].Position;
                NextPointBlip.Name = "Checkpoint: " + NextPointIndex;
                //if (NextPointIndex > 1)
                    //LastGapText.Caption = $"Last Gap: {(this.Times[NextPointIndex - 1] - this.Times[NextPointIndex - 2]):mm\\:ss\\.ff}";
            }
        }

        public void FinishRace()
        {
            distanceFromStart = Single.MaxValue;
            if (NextPointBlip != null && NextPointBlip.Exists())
            {
                NextPointBlip.Delete();
                NextPointBlip = null;
            }
            // Get ready to insert new times
            Time time = new Time();

            // add Penal time to end of race
            time.TotalTime = $"{this.Times[this.Times.Count - 1].AddSeconds(TotalPenalizedTime) - this.Times[0]}";
            TotalPenalizedTime = 0;
            time.GapTimesData = JsonConvert.SerializeObject(Times);
            time.Name = LocalPlayer.Name;
            time.Car = GetLabelText(RaceVehicle.DisplayName);
            time.MapId = Id;
            TriggerServerEvent("ar_Races:RegisterTime", JsonConvert.SerializeObject(time));
            Screen.ShowNotification($"You finished ~h~{Name}~h~.\nTime: ~h~{TimeSpan.Parse(time.TotalTime):mm\\:ss\\.ff}~h~, Car: ~h~{time.Car}", true);
        }

        public void CancelRace()
        {
            TotalPenalizedTime = 0;
            RaceManager.Status = RaceStatus.Idle;
            distanceFromStart = Single.MaxValue;
            if (NextPointBlip != null && NextPointBlip.Exists())
            {
                NextPointBlip.Delete();
                NextPointBlip = null;
            }

            List<Vehicle> VehicleList = World.GetAllVehicles().Where(veh => (veh.Handle != LocalPlayer.Character.CurrentVehicle.Handle)).ToList();
            VehicleList.ForEach(
                veh =>
                    {
                        if (veh.IsCollisionEnabled == false)
                        {
                            veh.SetNoCollision(RaceVehicle, false);
                        }

                        if (veh.Opacity == 155)
                        {
                            veh.Opacity = 255;
                        }

                    });

            RemovingCollisions = false;
        }

        private void DrawText3D(Text text, float size, Vector3 coords)
        {
            var camCoords = GameplayCamera.Position;
            var distance = Vector3.Distance(camCoords, coords);
            var scale = (size / distance) * 2;
            var fov = (1 / GameplayCamera.FieldOfView) * 100;
            
            scale = scale * fov;
            
            text.Scale = scale;
            text.Position = Screen.WorldToScreen(coords);

            if (text.Position != new PointF(0, 0))
                text.Draw();

        }

        private void DrawSprite(Sprite sprite, Vector3 coords)
        {
            var camCoords = GameplayCamera.Position;
            var distance = Vector3.Distance(camCoords, coords);
            var fov = (1 / GameplayCamera.FieldOfView) * 100;

            sprite.Position = Screen.WorldToScreen(coords);

            if (sprite.Position != new PointF(0, 0))
                sprite.Draw();
        }

        private void ResetToStart()
        {
            RaceVehicle.Position = PointList[0].Position;
            RaceVehicle.Heading = PointList[0].Rotation.Z;
            RaceVehicle.PlaceOnGround();
            TotalPenalizedTime = 0;
        }

        private void DrawScale(string button, string text)
        {
            if (!IsHudHidden())
            {
                BeginScaleformMovieMethod(Scale, "CLEAR_ALL");
                EndScaleformMovieMethod();

                BeginScaleformMovieMethod(Scale, "SET_DATA_SLOT");
                ScaleformMovieMethodAddParamInt(0);
                PushScaleformMovieMethodParameterString(button);
                PushScaleformMovieMethodParameterString(text);
                EndScaleformMovieMethod();

                BeginScaleformMovieMethod(Scale, "DRAW_INSTRUCTIONAL_BUTTONS");
                ScaleformMovieMethodAddParamInt(0);
                EndScaleformMovieMethod();

                DrawScaleformMovieFullscreen(Scale, 255, 255, 255, 255, 0);
            }
        }
    }
}