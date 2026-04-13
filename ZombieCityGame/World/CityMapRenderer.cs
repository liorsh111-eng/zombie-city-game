using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace ZombieCityGame.World
{
    public class CityMapRenderer
    {
        private readonly GridMap _map;
        private readonly Texture2D _pixel;
        private readonly int _tileSize;

        public CityMapRenderer(GridMap map, Texture2D pixel, int tileSize)
        {
            _map = map;
            _pixel = pixel;
            _tileSize = tileSize;
        }

        public void Draw(SpriteBatch spriteBatch, Vector2 viewTopLeft, int screenWidth, int screenHeight)
        {
            int minTileX = Math.Max(0, (int)(viewTopLeft.X / _tileSize) - 2);
            int minTileY = Math.Max(0, (int)(viewTopLeft.Y / _tileSize) - 2);
            int maxTileX = Math.Min(_map.Width - 1, (int)((viewTopLeft.X + screenWidth) / _tileSize) + 2);
            int maxTileY = Math.Min(_map.Height - 1, (int)((viewTopLeft.Y + screenHeight) / _tileSize) + 2);

            spriteBatch.Draw(
                _pixel,
                new Rectangle(0, 0, screenWidth, screenHeight),
                new Color(14, 14, 18)
            );

            for (int y = minTileY; y <= maxTileY; y++)
            {
                for (int x = minTileX; x <= maxTileX; x++)
                {
                    TileType tile = _map.GetTile(x, y);

                    float screenX = x * _tileSize - viewTopLeft.X;
                    float screenY = y * _tileSize - viewTopLeft.Y;
                    Rectangle rect = new Rectangle((int)screenX, (int)screenY, _tileSize, _tileSize);

                    bool isBorder = (x == 0 || y == 0 || x == _map.Width - 1 || y == _map.Height - 1);

                    if (tile == TileType.Wall)
                    {
                        DrawBuildingTile(spriteBatch, rect, x, y, isBorder);
                    }
                    else if (tile == TileType.Exit)
                    {
                        DrawGroundTile(spriteBatch, rect, x, y);
                        DrawExitTile(spriteBatch, rect);
                    }
                    else
                    {
                        DrawGroundTile(spriteBatch, rect, x, y);
                        DrawDecoration(spriteBatch, rect, x, y);
                    }
                }
            }
        }

        private void DrawBuildingTile(SpriteBatch spriteBatch, Rectangle rect, int x, int y, bool isBorder)
        {
            spriteBatch.Draw(_pixel, rect, new Color(42, 42, 50));

            spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, rect.Width, 2), new Color(62, 62, 72));
            spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, 2, rect.Height), new Color(62, 62, 72));

            spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Bottom - 2, rect.Width, 2), new Color(24, 24, 30));
            spriteBatch.Draw(_pixel, new Rectangle(rect.Right - 2, rect.Y, 2, rect.Height), new Color(24, 24, 30));

            if (HasAdjacentEmpty(x, y))
            {
                Rectangle inner = new Rectangle(rect.X + 2, rect.Y + 2, rect.Width - 4, rect.Height - 4);
                spriteBatch.Draw(_pixel, inner, new Color(52, 52, 60));
            }

            if (!isBorder)
            {
                int seed = (x * 73856093) ^ (y * 19349663);
                bool lightOn = (seed & 3) != 0;

                if (lightOn)
                {
                    Rectangle win1 = new Rectangle(rect.X + 6, rect.Y + 6, 5, 5);
                    Rectangle win2 = new Rectangle(rect.X + 19, rect.Y + 8, 5, 5);
                    Rectangle win3 = new Rectangle(rect.X + 11, rect.Y + 18, 5, 5);

                    spriteBatch.Draw(_pixel, win1, new Color(215, 195, 95));
                    spriteBatch.Draw(_pixel, win2, new Color(200, 180, 80));
                    spriteBatch.Draw(_pixel, win3, new Color(185, 165, 70));
                }
            }
        }

        private void DrawGroundTile(SpriteBatch spriteBatch, Rectangle rect, int x, int y)
        {
            bool isPlaza = IsPlazaLikeTile(x, y);
            bool isRoad = IsRoadLikeTile(x, y);

            if (isPlaza)
                DrawPlazaTile(spriteBatch, rect, x, y);
            else if (isRoad)
                DrawRoadTile(spriteBatch, rect, x, y);
            else
                DrawOpenGroundTile(spriteBatch, rect, x, y);
        }

        private void DrawRoadTile(SpriteBatch spriteBatch, Rectangle rect, int x, int y)
        {
            spriteBatch.Draw(_pixel, rect, new Color(26, 26, 30));

            if (((x + y) & 1) == 0)
            {
                Rectangle speck = new Rectangle(rect.X + 8, rect.Y + 8, 2, 2);
                spriteBatch.Draw(_pixel, speck, new Color(36, 36, 42));
            }

            bool horizontal = HasEmptyLeft(x, y) && HasEmptyRight(x, y) && !HasEmptyUp(x, y) && !HasEmptyDown(x, y);
            bool vertical = HasEmptyUp(x, y) && HasEmptyDown(x, y) && !HasEmptyLeft(x, y) && !HasEmptyRight(x, y);

            if (horizontal)
            {
                Rectangle line = new Rectangle(rect.X + 10, rect.Y + rect.Height / 2 - 1, rect.Width - 20, 2);
                spriteBatch.Draw(_pixel, line, new Color(180, 180, 120));
            }

            if (vertical)
            {
                Rectangle line = new Rectangle(rect.X + rect.Width / 2 - 1, rect.Y + 10, 2, rect.Height - 20);
                spriteBatch.Draw(_pixel, line, new Color(180, 180, 120));
            }

            if (HasEmptyLeft(x, y) && HasEmptyRight(x, y) && HasEmptyUp(x, y) && HasEmptyDown(x, y))
            {
                DrawCrosswalk(spriteBatch, rect, x, y);
            }
        }

        private void DrawPlazaTile(SpriteBatch spriteBatch, Rectangle rect, int x, int y)
        {
            spriteBatch.Draw(_pixel, rect, new Color(72, 72, 78));

            spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, rect.Width, 1), new Color(90, 90, 96));
            spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, 1, rect.Height), new Color(90, 90, 96));

            if (((x + y) & 1) == 0)
            {
                Rectangle stone = new Rectangle(rect.X + 6, rect.Y + 6, rect.Width - 12, rect.Height - 12);
                spriteBatch.Draw(_pixel, stone, new Color(82, 82, 88));
            }
        }

        private void DrawOpenGroundTile(SpriteBatch spriteBatch, Rectangle rect, int x, int y)
        {
            spriteBatch.Draw(_pixel, rect, new Color(34, 34, 38));

            if (((x * 17 + y * 31) & 3) == 0)
            {
                Rectangle mark = new Rectangle(rect.X + 12, rect.Y + 14, 6, 3);
                spriteBatch.Draw(_pixel, mark, new Color(52, 52, 58));
            }
        }

        private void DrawExitTile(SpriteBatch spriteBatch, Rectangle rect)
        {
            spriteBatch.Draw(_pixel, rect, new Color(90, 70, 10));

            Rectangle inner = new Rectangle(rect.X + 6, rect.Y + 6, rect.Width - 12, rect.Height - 12);
            spriteBatch.Draw(_pixel, inner, Color.Gold);

            DrawRectOutline(spriteBatch, rect, new Color(255, 245, 180));
        }

        private void DrawDecoration(SpriteBatch spriteBatch, Rectangle rect, int x, int y)
        {
            if (IsPlazaLikeTile(x, y))
            {
                if ((x % 5 == 0) && (y % 5 == 0))
                    DrawFountain(spriteBatch, rect);
                else if ((x + y) % 7 == 0)
                    DrawTree(spriteBatch, rect);
                return;
            }

            if (IsRoadLikeTile(x, y))
            {
                if ((x * 13 + y * 7) % 9 == 0)
                    DrawStreetLamp(spriteBatch, rect, x, y);
                return;
            }

            if ((x * 17 + y * 11) % 19 == 0)
                DrawTree(spriteBatch, rect);
        }

        private void DrawFountain(SpriteBatch spriteBatch, Rectangle rect)
        {
            Rectangle basin = new Rectangle(rect.X + 6, rect.Y + 6, rect.Width - 12, rect.Height - 12);
            spriteBatch.Draw(_pixel, basin, new Color(95, 95, 105));

            Rectangle water = new Rectangle(rect.X + 10, rect.Y + 10, rect.Width - 20, rect.Height - 20);
            spriteBatch.Draw(_pixel, water, new Color(70, 130, 180));

            Rectangle center = new Rectangle(rect.X + rect.Width / 2 - 2, rect.Y + rect.Height / 2 - 6, 4, 12);
            spriteBatch.Draw(_pixel, center, new Color(190, 190, 210));
        }

        private void DrawTree(SpriteBatch spriteBatch, Rectangle rect)
        {
            Rectangle trunk = new Rectangle(rect.X + rect.Width / 2 - 2, rect.Y + rect.Height / 2 + 2, 4, 8);
            spriteBatch.Draw(_pixel, trunk, new Color(100, 70, 40));

            Rectangle leaves = new Rectangle(rect.X + 8, rect.Y + 6, rect.Width - 16, rect.Height - 16);
            spriteBatch.Draw(_pixel, leaves, new Color(40, 120, 60));
        }

        private void DrawStreetLamp(SpriteBatch spriteBatch, Rectangle rect, int x, int y)
        {
            Rectangle pole = new Rectangle(rect.X + rect.Width / 2 - 1, rect.Y + 8, 2, rect.Height - 12);
            spriteBatch.Draw(_pixel, pole, new Color(120, 120, 130));

            Rectangle light = new Rectangle(rect.X + rect.Width / 2 - 3, rect.Y + 5, 6, 4);
            spriteBatch.Draw(_pixel, light, new Color(230, 220, 140));
        }

        private void DrawCrosswalk(SpriteBatch spriteBatch, Rectangle rect, int x, int y)
        {
            for (int i = 0; i < 3; i++)
            {
                Rectangle stripeH = new Rectangle(rect.X + 4, rect.Y + 6 + i * 8, rect.Width - 8, 2);
                Rectangle stripeV = new Rectangle(rect.X + 6 + i * 8, rect.Y + 4, 2, rect.Height - 8);
                spriteBatch.Draw(_pixel, stripeH, new Color(220, 220, 220, 140));
                spriteBatch.Draw(_pixel, stripeV, new Color(220, 220, 220, 140));
            }
        }

        private void DrawRectOutline(SpriteBatch spriteBatch, Rectangle rect, Color color)
        {
            spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, rect.Width, 2), color);
            spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Bottom - 2, rect.Width, 2), color);
            spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, 2, rect.Height), color);
            spriteBatch.Draw(_pixel, new Rectangle(rect.Right - 2, rect.Y, 2, rect.Height), color);
        }

        private bool IsPlazaLikeTile(int x, int y)
        {
            if (!_map.IsInside(x, y) || _map.GetTile(x, y) != TileType.Empty)
                return false;

            int emptyCount = 0;

            for (int yy = y - 1; yy <= y + 1; yy++)
            {
                for (int xx = x - 1; xx <= x + 1; xx++)
                {
                    if (_map.IsInside(xx, yy) && _map.GetTile(xx, yy) == TileType.Empty)
                        emptyCount++;
                }
            }

            return emptyCount >= 7;
        }

        private bool IsRoadLikeTile(int x, int y)
        {
            if (!_map.IsInside(x, y) || _map.GetTile(x, y) != TileType.Empty)
                return false;

            bool horizontal = HasEmptyLeft(x, y) && HasEmptyRight(x, y);
            bool vertical = HasEmptyUp(x, y) && HasEmptyDown(x, y);

            return horizontal || vertical;
        }

        private bool HasEmptyLeft(int x, int y)
        {
            return _map.IsInside(x - 1, y) && _map.GetTile(x - 1, y) == TileType.Empty;
        }

        private bool HasEmptyRight(int x, int y)
        {
            return _map.IsInside(x + 1, y) && _map.GetTile(x + 1, y) == TileType.Empty;
        }

        private bool HasEmptyUp(int x, int y)
        {
            return _map.IsInside(x, y - 1) && _map.GetTile(x, y - 1) == TileType.Empty;
        }

        private bool HasEmptyDown(int x, int y)
        {
            return _map.IsInside(x, y + 1) && _map.GetTile(x, y + 1) == TileType.Empty;
        }

        private bool HasAdjacentEmpty(int x, int y)
        {
            return HasEmptyLeft(x, y) || HasEmptyRight(x, y) || HasEmptyUp(x, y) || HasEmptyDown(x, y);
        }
    }
}
