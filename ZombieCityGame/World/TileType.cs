using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZombieCityGame.World
{
    // Represents the type of a single cell (tile) in the grid map
    // This enum describes what the tile is, not how it behaves or how it is drawn
    public enum TileType
    {
        Empty,
        Wall,
        Exit
    }
}
