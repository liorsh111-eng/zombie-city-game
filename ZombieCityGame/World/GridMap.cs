using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace ZombieCityGame.World
{
    // מחלקה שמייצגת את המפה הלוגית של המשחק
    // זו לא מחלקת ציור - היא רק שומרת מידע על העולם:
    // מה יש בכל תא, איפה יש קירות, ומה חוקי למעבר
    public class GridMap
    {
        // רוחב המפה במספר תאים (tiles)
        public int Width { get; }

        // גובה המפה במספר תאים (tiles)
        public int Height { get; }

        // המערך שמחזיק את כל המידע על המפה
        // בכל תא נשמר TileType (למשל Empty, Wall, Exit)
        // הגישה היא לפי [x, y]
        public TileType[,] Tiles { get; }

        // בנאי המחלקה:
        // יוצר מפה חדשה בגודל הנתון
        public GridMap(int width, int height)
        {
            Width = width;
            Height = height;

            // הקצאת מערך דו-ממדי בגודל המפה
            Tiles = new TileType[Width, Height];

            // כברירת מחדל כל תא יקבל את הערך הראשון ב-enum
            // בדרך כלל זה TileType.Empty
        }

        // בודקת האם קואורדינטה מסוימת נמצאת בתוך תחומי המפה
        public bool IsInside(int x, int y)
        {
            return x >= 0 && y >= 0 && x < Width && y < Height;
        }

        // מחזירה את סוג ה-tile שנמצא במקום מסוים
        public TileType GetTile(int x, int y)
        {
            return Tiles[x, y];
        }

        // משנה את סוג ה-tile במקום מסוים
        public void SetTile(int x, int y, TileType type)
        {
            Tiles[x, y] = type;
        }

        // בודקת האם מותר לעבור על tile מסוים
        // כרגע:
        // - אם הוא מחוץ למפה -> אסור
        // - אם הוא Wall -> אסור
        // - כל דבר אחר -> מותר
        public bool IsWalkable(int x, int y)
        {
            if (!IsInside(x, y))
                return false;

            return Tiles[x, y] != TileType.Wall;
        }

        // מחזירה את כל השכנים של תא מסוים ב-4 כיוונים בלבד:
        // ימינה, שמאלה, למטה, למעלה
        // הפונקציה לא מחזירה שכנים מחוץ למפה
        // הפונקציה שימושית במיוחד ל-BFS
        public IEnumerable<Point> GetNeighbors4(int x, int y)
        {
            // ימין
            if (IsInside(x + 1, y)) yield return new Point(x + 1, y);

            // שמאל
            if (IsInside(x - 1, y)) yield return new Point(x - 1, y);

            // למטה
            if (IsInside(x, y + 1)) yield return new Point(x, y + 1);

            // למעלה
            if (IsInside(x, y - 1)) yield return new Point(x, y - 1);
        }
    }
}