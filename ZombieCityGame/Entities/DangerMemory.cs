using Microsoft.Xna.Framework;

namespace ZombieCityGame.Entities
{
    public class DangerMemory
    {
        public Point Tile { get; }
        public double TimeRemainingSeconds { get; set; }

        public DangerMemory(Point tile, double timeRemainingSeconds)
        {
            Tile = tile;
            TimeRemainingSeconds = timeRemainingSeconds;
        }
    }
}
