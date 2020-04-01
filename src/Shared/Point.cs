namespace Races.Shared
{
    using CitizenFX.Core;

    public class Point
    {
        public Vector3 Position { get; set; }
        public float Heading { get; set; }
        public Vector3 Scale { get; set; }
        public int Marker { get; set; }
    }
}