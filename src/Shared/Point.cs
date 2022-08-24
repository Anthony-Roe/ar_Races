using System.Drawing;
using CitizenFX.Core;

namespace Races.Shared
{
    public class Point
    {
        public Vector4 MarkerColor { get; set; }
        public Vector3 Position { get; set; }
        public Vector3 Rotation { get; set; }
        public Vector3 Scale { get; set; }
        public int Marker { get; set; }
    }
}