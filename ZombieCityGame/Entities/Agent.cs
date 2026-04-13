using System.Collections.Generic;
using Microsoft.Xna.Framework;
using ZombieCityGame.World;

namespace ZombieCityGame.Entities
{
    public class Agent
    {
        public int Id { get; }
        public Vector2 PositionPx { get; private set; }
        public bool HasEscaped { get; private set; }

        public string RoleName { get; }
        public int CautionLevel { get; }

        // זיכרון סכנות שהשחקן קיבל בהודעות
        private readonly List<DangerMemory> _knownDangers = new();

        public IReadOnlyList<DangerMemory> KnownDangers => _knownDangers;

        private readonly float _speedPxPerSec;

        public Agent(
            int id,
            Vector2 startPositionPx,
            float speedPxPerSec = 170f,
            int cautionLevel = 2,
            string roleName = "Balanced")
        {
            Id = id;
            PositionPx = startPositionPx;
            _speedPxPerSec = speedPxPerSec;
            CautionLevel = cautionLevel;
            RoleName = roleName;
            HasEscaped = false;
        }

        public void MoveToward(Vector2 targetPx, float dt, GridMap map, int tileSize)
        {
            if (HasEscaped)
                return;

            Vector2 toTarget = targetPx - PositionPx;
            if (toTarget.LengthSquared() < 4f)
                return;

            Vector2 dir = Vector2.Normalize(toTarget);
            Vector2 delta = dir * _speedPxPerSec * dt;

            Vector2 pos = PositionPx;

            Vector2 tryX = new Vector2(pos.X + delta.X, pos.Y);
            if (map.IsWalkable((int)(tryX.X / tileSize), (int)(tryX.Y / tileSize)))
                pos = tryX;

            Vector2 tryY = new Vector2(pos.X, pos.Y + delta.Y);
            if (map.IsWalkable((int)(tryY.X / tileSize), (int)(tryY.Y / tileSize)))
                pos = tryY;

            PositionPx = pos;
        }

        public void MarkEscaped()
        {
            HasEscaped = true;
        }

        public void AddOrRefreshDanger(Point tile, double ttlSeconds)
        {
            for (int i = 0; i < _knownDangers.Count; i++)
            {
                if (_knownDangers[i].Tile == tile)
                {
                    _knownDangers[i].TimeRemainingSeconds = ttlSeconds;
                    return;
                }
            }

            _knownDangers.Add(new DangerMemory(tile, ttlSeconds));
        }

        public void UpdateKnownDangers(double dtSeconds)
        {
            for (int i = _knownDangers.Count - 1; i >= 0; i--)
            {
                _knownDangers[i].TimeRemainingSeconds -= dtSeconds;
                if (_knownDangers[i].TimeRemainingSeconds <= 0)
                    _knownDangers.RemoveAt(i);
            }
        }
    }
}


