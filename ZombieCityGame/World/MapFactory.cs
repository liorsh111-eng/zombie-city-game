using System;

namespace ZombieCityGame.World
{
    public static class MapFactory
    {
        public static GridMap CreateTestMap(int width, int height)
        {
            var map = new GridMap(width, height);
            var rng = new Random();

            // ---------------------------------
            // 1) קודם כל נמלא את כל המפה בבניינים
            // ---------------------------------
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    map.SetTile(x, y, TileType.Wall);
                }
            }

            // ---------------------------------
            // 2) מסגרת חיצונית נשארת קיר
            // ---------------------------------
            for (int x = 0; x < width; x++)
            {
                map.SetTile(x, 0, TileType.Wall);
                map.SetTile(x, height - 1, TileType.Wall);
            }

            for (int y = 0; y < height; y++)
            {
                map.SetTile(0, y, TileType.Wall);
                map.SetTile(width - 1, y, TileType.Wall);
            }

            // ---------------------------------
            // 3) כבישים ראשיים אנכיים
            // ---------------------------------
            CreateVerticalRoad(map, 3, 2);
            CreateVerticalRoad(map, width / 2, 2);
            CreateVerticalRoad(map, width - 5, 2);

            // ---------------------------------
            // 4) כבישים ראשיים אופקיים
            // ---------------------------------
            CreateHorizontalRoad(map, 3, 2);
            CreateHorizontalRoad(map, height / 2, 2);
            CreateHorizontalRoad(map, height - 5, 2);

            // ---------------------------------
            // 5) כיכר מרכזית
            // ---------------------------------
            int plazaW = 8;
            int plazaH = 6;
            int plazaX = width / 2 - plazaW / 2;
            int plazaY = height / 2 - plazaH / 2;
            CarveEmptyRect(map, plazaX, plazaY, plazaW, plazaH);

            // ---------------------------------
            // 6) כיכרות קטנות נוספות
            // ---------------------------------
            CarveEmptyRect(map, 6, 6, 4, 4);
            CarveEmptyRect(map, width - 10, 6, 4, 4);
            CarveEmptyRect(map, 6, height - 10, 4, 4);
            CarveEmptyRect(map, width - 10, height - 10, 4, 4);

            // ---------------------------------
            // 7) בלוקים של בניינים
            //    אנחנו "מארגנים" את העיר לבלוקים,
            //    אבל משאירים את הכבישים והכיכרות ריקים
            // ---------------------------------
            BuildCityBlocks(map, rng);

            // ---------------------------------
            // 8) פארקים / שטחים פתוחים קטנים
            // ---------------------------------
            CarveEmptyRect(map, width / 2 - 2, 5, 3, 3);
            CarveEmptyRect(map, width / 2 + 3, height - 8, 3, 3);

            // ---------------------------------
            // 9) אזור התחלה בטוח ל-4 שחקנים
            // ---------------------------------
            CarveEmptyRect(map, 2, 2, 5, 5);

            // ---------------------------------
            // 10) יציאה - ממקמים אותה במקום ריק ורחוק מההתחלה
            // ---------------------------------
            PlaceExitFarFromStart(map, rng, startX: 2, startY: 2);

            return map;
        }

        // יוצר כביש אנכי בעובי נתון
        private static void CreateVerticalRoad(GridMap map, int centerX, int halfWidth)
        {
            for (int x = centerX - halfWidth; x <= centerX + halfWidth; x++)
            {
                if (x <= 0 || x >= map.Width - 1)
                    continue;

                for (int y = 1; y < map.Height - 1; y++)
                {
                    map.SetTile(x, y, TileType.Empty);
                }
            }
        }

        // יוצר כביש אופקי בעובי נתון
        private static void CreateHorizontalRoad(GridMap map, int centerY, int halfHeight)
        {
            for (int y = centerY - halfHeight; y <= centerY + halfHeight; y++)
            {
                if (y <= 0 || y >= map.Height - 1)
                    continue;

                for (int x = 1; x < map.Width - 1; x++)
                {
                    map.SetTile(x, y, TileType.Empty);
                }
            }
        }

        // הופך אזור מלבני לריק
        private static void CarveEmptyRect(GridMap map, int startX, int startY, int w, int h)
        {
            for (int y = startY; y < startY + h && y < map.Height - 1; y++)
            {
                for (int x = startX; x < startX + w && x < map.Width - 1; x++)
                {
                    if (x <= 0 || y <= 0)
                        continue;

                    map.SetTile(x, y, TileType.Empty);
                }
            }
        }

        // בונה בלוקים עירוניים מסודרים יחסית
        private static void BuildCityBlocks(GridMap map, Random rng)
        {
            // אנחנו עוברים על המפה ב"בלוקים"
            // ובכל אזור שאין בו כביש - יוצרים מבנים מלבניים
            for (int blockY = 1; blockY < map.Height - 1; blockY += 6)
            {
                for (int blockX = 1; blockX < map.Width - 1; blockX += 8)
                {
                    // גודל מבנה אקראי אך סביר
                    int buildingW = 3 + rng.Next(0, 3); // 3..5
                    int buildingH = 2 + rng.Next(0, 3); // 2..4

                    // מיקום פנימי בתוך הבלוק
                    int startX = blockX + 1;
                    int startY = blockY + 1;

                    // אם חורג מהמפה - דלג
                    if (startX + buildingW >= map.Width - 1 || startY + buildingH >= map.Height - 1)
                        continue;

                    // בונים רק אם האזור לא כביש/כיכר
                    bool canBuild = true;
                    for (int y = startY; y < startY + buildingH && canBuild; y++)
                    {
                        for (int x = startX; x < startX + buildingW; x++)
                        {
                            if (map.GetTile(x, y) == TileType.Empty || map.GetTile(x, y) == TileType.Exit)
                            {
                                canBuild = false;
                                break;
                            }
                        }
                    }

                    if (!canBuild)
                        continue;

                    // יוצרים את הבניין
                    for (int y = startY; y < startY + buildingH; y++)
                    {
                        for (int x = startX; x < startX + buildingW; x++)
                        {
                            map.SetTile(x, y, TileType.Wall);
                        }
                    }

                    // לפעמים משאירים "חצר" קטנה ליד המבנה
                    if (rng.NextDouble() < 0.25)
                    {
                        int yardX = startX + buildingW;
                        int yardY = startY;

                        if (yardX < map.Width - 1)
                            CarveEmptyRect(map, yardX, yardY, 2, 2);
                    }
                }
            }
        }

        // ממקם יציאה רחוק מאזור ההתחלה
        private static void PlaceExitFarFromStart(GridMap map, Random rng, int startX, int startY)
        {
            Point bestPoint = new Point(1, 1);
            double bestDist = -1;

            for (int y = 1; y < map.Height - 1; y++)
            {
                for (int x = 1; x < map.Width - 1; x++)
                {
                    if (map.GetTile(x, y) != TileType.Empty)
                        continue;

                    double dx = x - startX;
                    double dy = y - startY;
                    double dist = dx * dx + dy * dy;

                    // קצת רנדומליות כדי לא לבחור תמיד בדיוק אותה פינה
                    dist += rng.NextDouble() * 5.0;

                    if (dist > bestDist)
                    {
                        bestDist = dist;
                        bestPoint = new Point(x, y);
                    }
                }
            }

            map.SetTile(bestPoint.X, bestPoint.Y, TileType.Exit);
        }

        private struct Point
        {
            public int X;
            public int Y;

            public Point(int x, int y)
            {
                X = x;
                Y = y;
            }
        }
    }
}