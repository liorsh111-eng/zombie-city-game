using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using ZombieCityGame.World;

namespace ZombieCityGame.Entities
{
    public class Zombie
    {
        public int Id { get; }
        public Vector2 PositionPx { get; private set; }

        private Vector2 _velocity;
        private readonly float _speedPxPerSec;

        private ZombieState _state = ZombieState.Wander;

        // טווח זיהוי של השחקן
        private const float DetectionRangePx = 220f;

        // BFS פנימי של הזומבי
        private List<Point> _currentPath = new();
        private double _repathTimer = 0;
        private const double RepathCooldown = 0.25;

        // טיימר להחלפת כיוון ב-Wander
        private float _timeToChangeDirSeconds;

        public Zombie(int id, Vector2 startPositionPx, float speedPxPerSec = 120f)
        {
            Id = id;
            PositionPx = startPositionPx;
            _speedPxPerSec = speedPxPerSec;

            _velocity = new Vector2(1, 0);
            _timeToChangeDirSeconds = 0f;
        }

        public void Update(GridMap map, float dtSeconds, Random rng, int tileSize, Vector2 playerPositionPx)
        {
            if (map == null) throw new ArgumentNullException(nameof(map));
            if (rng == null) throw new ArgumentNullException(nameof(rng));

            float distanceToPlayer = Vector2.Distance(PositionPx, playerPositionPx);

            if (distanceToPlayer <= DetectionRangePx)
                _state = ZombieState.Chase;
            else
                _state = ZombieState.Wander;

            switch (_state)
            {
                case ZombieState.Wander:
                    UpdateWander(map, dtSeconds, rng, tileSize);
                    break;

                case ZombieState.Chase:
                    UpdateChaseWithBfs(map, dtSeconds, tileSize, playerPositionPx);
                    break;
            }
        }

        private void UpdateWander(GridMap map, float dtSeconds, Random rng, int tileSize)
        {
            _timeToChangeDirSeconds -= dtSeconds;

            if (_timeToChangeDirSeconds <= 0f)
            {
                PickRandomDirection(rng);
                _timeToChangeDirSeconds = 0.4f + (float)rng.NextDouble() * 1.2f;
            }

            Vector2 delta = _velocity * _speedPxPerSec * dtSeconds;
            PositionPx = MoveWithGridCollision(map, PositionPx, delta, tileSize);

            // כשמשוטטים, ננקה מסלול ישן
            _currentPath.Clear();
            _repathTimer = 0;
        }

        private void UpdateChaseWithBfs(GridMap map, float dtSeconds, int tileSize, Vector2 playerPositionPx)
        {
            _repathTimer += dtSeconds;

            Point zombieTile = PixelToTile(PositionPx, tileSize);
            Point playerTile = PixelToTile(playerPositionPx, tileSize);

            if (_repathTimer >= RepathCooldown || _currentPath.Count == 0)
            {
                _repathTimer = 0;
                _currentPath = FindPathBfs(map, zombieTile, playerTile);
            }

            if (_currentPath.Count >= 2)
            {
                Point nextTile = _currentPath[1];

                Vector2 nextCenterPx = new Vector2(
                    nextTile.X * tileSize + tileSize / 2f,
                    nextTile.Y * tileSize + tileSize / 2f
                );

                MoveToward(nextCenterPx, dtSeconds, map, tileSize);
            }
            else
            {
                // fallback קטן אם אין מסלול
                Vector2 toPlayer = playerPositionPx - PositionPx;
                if (toPlayer.LengthSquared() > 0.001f)
                {
                    _velocity = Vector2.Normalize(toPlayer);
                    Vector2 delta = _velocity * _speedPxPerSec * dtSeconds;
                    PositionPx = MoveWithGridCollision(map, PositionPx, delta, tileSize);
                }
            }
        }

        private void MoveToward(Vector2 targetPx, float dtSeconds, GridMap map, int tileSize)
        {
            Vector2 toTarget = targetPx - PositionPx;
            if (toTarget.LengthSquared() < 4f)
                return;

            _velocity = Vector2.Normalize(toTarget);

            Vector2 delta = _velocity * _speedPxPerSec * dtSeconds;
            PositionPx = MoveWithGridCollision(map, PositionPx, delta, tileSize);
        }

        private void PickRandomDirection(Random rng)
        {
            int choice = rng.Next(4);

            _velocity = choice switch
            {
                0 => new Vector2(1, 0),
                1 => new Vector2(-1, 0),
                2 => new Vector2(0, 1),
                _ => new Vector2(0, -1),
            };
        }

        private static List<Point> FindPathBfs(GridMap map, Point start, Point goal)
        {
            var q = new Queue<Point>();
            var cameFrom = new Dictionary<Point, Point>();

            q.Enqueue(start);
            cameFrom[start] = start;

            while (q.Count > 0)
            {
                Point cur = q.Dequeue();

                if (cur == goal)
                    break;

                foreach (var nb in map.GetNeighbors4(cur.X, cur.Y))
                {
                    if (cameFrom.ContainsKey(nb)) continue;
                    if (!map.IsWalkable(nb.X, nb.Y)) continue;

                    cameFrom[nb] = cur;
                    q.Enqueue(nb);
                }
            }

            if (!cameFrom.ContainsKey(goal))
                return new List<Point>();

            var path = new List<Point>();
            Point p = goal;

            while (p != start)
            {
                path.Add(p);
                p = cameFrom[p];
            }

            path.Add(start);
            path.Reverse();
            return path;
        }

        private static Vector2 MoveWithGridCollision(GridMap map, Vector2 pos, Vector2 delta, int tileSize)
        {
            Vector2 newPos = pos;

            Vector2 tryX = new Vector2(newPos.X + delta.X, newPos.Y);
            if (IsWalkableAtPixel(map, tryX, tileSize))
                newPos = tryX;

            Vector2 tryY = new Vector2(newPos.X, newPos.Y + delta.Y);
            if (IsWalkableAtPixel(map, tryY, tileSize))
                newPos = tryY;

            return newPos;
        }

        private static bool IsWalkableAtPixel(GridMap map, Vector2 posPx, int tileSize)
        {
            int tx = (int)(posPx.X / tileSize);
            int ty = (int)(posPx.Y / tileSize);
            return map.IsWalkable(tx, ty);
        }

        private static Point PixelToTile(Vector2 posPx, int tileSize)
        {
            return new Point(
                (int)(posPx.X / tileSize),
                (int)(posPx.Y / tileSize)
            );
        }

        private enum ZombieState
        {
            Wander,
            Chase
        }
    }
}
