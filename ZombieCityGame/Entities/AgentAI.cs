using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using ZombieCityGame.World;
using ZombieCityGame.Entities;

namespace ZombieCityGame.AI
{
    public class AgentAI
    {
        private readonly GridMap _map;
        private readonly int _tileSize;

        private List<Point> _currentPath = new();
        private HashSet<Point> _dangerTiles = new();

        private double _repathTimer = 0;
        private const double RepathCooldown = 0.2;

        public IReadOnlyList<Point> CurrentPath => _currentPath;
        public IReadOnlyCollection<Point> DangerTiles => _dangerTiles;

        public AgentAI(GridMap map, int tileSize)
        {
            _map = map;
            _tileSize = tileSize;
        }

        public void Update(
            GameTime gameTime,
            Agent agent,
            List<Zombie> zombies,
            Point exitTile,
            HashSet<Point> occupiedTiles,
            HashSet<Point> knownDangerTiles)
        {
            _repathTimer += gameTime.ElapsedGameTime.TotalSeconds;

            Point agentTile = PixelToTile(agent.PositionPx);

            if (_repathTimer >= RepathCooldown || _currentPath.Count == 0)
            {
                _repathTimer = 0;

                _dangerTiles = GetDangerTiles(zombies, agent.CautionLevel);

                foreach (var tile in knownDangerTiles)
                    _dangerTiles.Add(tile);

                _currentPath = FindPathBfs(
                    agentTile,
                    exitTile,
                    p => _dangerTiles.Contains(p) || occupiedTiles.Contains(p)
                );
            }
        }

        public Vector2? GetNextTarget(Agent agent)
        {
            if (_currentPath.Count < 2)
                return null;

            Point nextTile = _currentPath[1];

            return new Vector2(
                nextTile.X * _tileSize + _tileSize / 2f,
                nextTile.Y * _tileSize + _tileSize / 2f
            );
        }

        private HashSet<Point> GetDangerTiles(List<Zombie> zombies, int cautionLevel)
        {
            var danger = new HashSet<Point>();

            foreach (var z in zombies)
            {
                Point zt = PixelToTile(z.PositionPx);
                danger.Add(zt);

                foreach (var nb in _map.GetNeighbors4(zt.X, zt.Y))
                    danger.Add(nb);

                if (cautionLevel >= 2)
                {
                    foreach (var nb in _map.GetNeighbors4(zt.X, zt.Y))
                    {
                        foreach (var nb2 in _map.GetNeighbors4(nb.X, nb.Y))
                            danger.Add(nb2);
                    }
                }

                if (cautionLevel >= 3)
                {
                    var extra = new List<Point>(danger);
                    foreach (var d in extra)
                    {
                        foreach (var nb in _map.GetNeighbors4(d.X, d.Y))
                            danger.Add(nb);
                    }
                }
            }

            return danger;
        }

        private List<Point> FindPathBfs(Point start, Point goal, Func<Point, bool> blocked)
        {
            var q = new Queue<Point>();
            var cameFrom = new Dictionary<Point, Point>();

            q.Enqueue(start);
            cameFrom[start] = start;

            while (q.Count > 0)
            {
                var cur = q.Dequeue();
                if (cur == goal)
                    break;

                foreach (var nb in _map.GetNeighbors4(cur.X, cur.Y))
                {
                    if (cameFrom.ContainsKey(nb)) continue;
                    if (!_map.IsWalkable(nb.X, nb.Y)) continue;
                    if (blocked(nb)) continue;

                    cameFrom[nb] = cur;
                    q.Enqueue(nb);
                }
            }

            if (!cameFrom.ContainsKey(goal))
                return new List<Point>();

            var path = new List<Point>();
            var p = goal;

            while (p != start)
            {
                path.Add(p);
                p = cameFrom[p];
            }

            path.Add(start);
            path.Reverse();
            return path;
        }

        private Point PixelToTile(Vector2 posPx)
            => new Point((int)(posPx.X / _tileSize), (int)(posPx.Y / _tileSize));
    }
}