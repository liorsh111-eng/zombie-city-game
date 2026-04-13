using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using ZombieCityGame.World;
using ZombieCityGame.Entities;
using ZombieCityGame.AI;

namespace ZombieCityGame
{
    public class Game1 : Game
    {
        // מנהל הגרפיקה של המשחק
        private GraphicsDeviceManager _graphics;

        // אובייקט שאחראי על ציור למסך
        private SpriteBatch _spriteBatch;

        // פיקסל לבן בודד שמשמש לציור מלבנים, HUD, מסגרות וכו'
        private Texture2D _pixel;

        // פונט להצגת טקסט על המסך
        private SpriteFont _font;

        // המפה הלוגית של המשחק
        private GridMap _map;

        // מחלקת ציור העיר (כבישים, בניינים, כיכרות וכו')
        private CityMapRenderer _cityRenderer;

        // מחלקת ציור שכבת המשחק העליונה (HUD, ישויות, דיבאג)
        private GameOverlayRenderer _overlayRenderer;

        // רשימת השחקנים
        private List<Agent> _agents;

        // לכל שחקן יש AI משלו
        private List<AgentAI> _agentsAI;

        // רשימת הזומבים
        private List<Zombie> _zombies;

        // מחולל מספרים אקראיים
        private Random _rng;

        // תור ההודעות המרכזי של התקשורת בין השחקנים
        private Queue<AgentMessage> _messageQueue;

        // גודל tile במפה בפיקסלים
        private const int TileSize = 32;

        // מיקום מרכז המצלמה בעולם
        private Vector2 _cameraCenterPx;

        // מיקום היציאה במונחי grid
        private Point _exitTile;

        // משתני מצב משחק
        private bool _isGameOver = false;
        private bool _hasWon = false;
        private bool _hasLost = false;

        // האם להציג שכבת דיבאג
        private bool _debugDraw = true;

        // בנאי המחלקה - הגדרות בסיס של המשחק
        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        // אתחול ראשוני של המשחק
        protected override void Initialize()
        {
            _rng = new Random();
            ResetGame();

            // גודל חלון המשחק
            _graphics.PreferredBackBufferWidth = 900;
            _graphics.PreferredBackBufferHeight = 600;
            _graphics.ApplyChanges();

            base.Initialize();
        }

        // טעינת תוכן גרפי
        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // יצירת פיקסל לבן 1x1
            _pixel = new Texture2D(GraphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });

            // טעינת פונט
            _font = Content.Load<SpriteFont>("DefaultFont");

            // יצירת מחלקות ציור
            _cityRenderer = new CityMapRenderer(_map, _pixel, TileSize);
            _overlayRenderer = new GameOverlayRenderer(_pixel, _font, TileSize);
        }

        // איפוס כל מצב המשחק ויצירה מחדש של העולם
        private void ResetGame()
        {
            _isGameOver = false;
            _hasWon = false;
            _hasLost = false;

            // יצירת מפה חדשה
            _map = MapFactory.CreateTestMap(40, 25);

            // חיפוש מיקום היציאה במפה
            _exitTile = FindExitTile(_map);

            // יצירת 4 שחקנים עם תכונות שונות
            _agents = new List<Agent>
            {
                new Agent(1, TileCenter(2, 2), speedPxPerSec: 185f, cautionLevel: 1, roleName: "Fast"),
                new Agent(2, TileCenter(3, 2), speedPxPerSec: 160f, cautionLevel: 2, roleName: "Careful"),
                new Agent(3, TileCenter(2, 3), speedPxPerSec: 170f, cautionLevel: 2, roleName: "Balanced"),
                new Agent(4, TileCenter(3, 3), speedPxPerSec: 165f, cautionLevel: 2, roleName: "Smart"),
            };

            // לכל שחקן יש AI משלו
            _agentsAI = new List<AgentAI>
            {
                new AgentAI(_map, TileSize),
                new AgentAI(_map, TileSize),
                new AgentAI(_map, TileSize),
                new AgentAI(_map, TileSize),
            };

            // יצירת רשימת זומבים ותור הודעות
            _zombies = new List<Zombie>();
            _messageQueue = new Queue<AgentMessage>();

            int desiredZombieCount = 6;

            // יצירת זומבים במקומות חוקיים במפה, לא קרוב מדי לשחקנים
            for (int y = 0; y < _map.Height && _zombies.Count < desiredZombieCount; y++)
            {
                for (int x = 0; x < _map.Width && _zombies.Count < desiredZombieCount; x++)
                {
                    if (!_map.IsWalkable(x, y))
                        continue;

                    Vector2 zPos = TileCenter(x, y);

                    bool tooCloseToAnyAgent = false;
                    foreach (var a in _agents)
                    {
                        if (Vector2.Distance(zPos, a.PositionPx) <= TileSize * 6)
                        {
                            tooCloseToAnyAgent = true;
                            break;
                        }
                    }

                    if (!tooCloseToAnyAgent)
                        _zombies.Add(new Zombie(_zombies.Count + 1, zPos, 120f));
                }
            }

            // המצלמה תתחיל על מרכז הקבוצה
            _cameraCenterPx = GetActiveAgentsCenter();

            // אם הטקסטורות כבר נטענו, מעדכנים גם את מחלקות הציור
            if (_pixel != null)
            {
                _cityRenderer = new CityMapRenderer(_map, _pixel, TileSize);
                _overlayRenderer = new GameOverlayRenderer(_pixel, _font, TileSize);
            }
        }

        // לוגיקת המשחק שמתבצעת כל frame
        protected override void Update(GameTime gameTime)
        {
            // יציאה מהמשחק עם Escape או Back
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
                Keyboard.GetState().IsKeyDown(Keys.Escape))
            {
                Exit();
                return;
            }

            KeyboardState kb = Keyboard.GetState();

            // אם המשחק נגמר ולוחצים R - מתחילים מחדש
            if (_isGameOver && kb.IsKeyDown(Keys.R))
            {
                ResetGame();
                base.Update(gameTime);
                return;
            }

            // אם המשחק נגמר לא ממשיכים לעדכן לוגיקה
            if (_isGameOver)
            {
                base.Update(gameTime);
                return;
            }

            // זמן שחלף מאז הפריים הקודם
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // עדכון כל הזומבים - כל זומבי רודף אחרי השחקן הפעיל הקרוב ביותר
            foreach (var z in _zombies)
            {
                Vector2 targetPlayerPos = GetClosestActiveAgentPosition(z.PositionPx);
                z.Update(_map, dt, _rng, TileSize, targetPlayerPos);
            }

            // עדכון זיכרון סכנות אצל כל שחקן - דעיכה של אזהרות ישנות
            foreach (var agent in _agents)
            {
                if (!agent.HasEscaped)
                    agent.UpdateKnownDangers(dt);
            }

            // יצירת הודעות אזהרה חדשות בין שחקנים
            GenerateDangerMessages();

            // עיבוד תור ההודעות
            ProcessMessageQueue();

            // יצירת אוסף של התאים שבהם שחקנים עומדים כרגע
            HashSet<Point> occupiedTiles = GetOccupiedAgentTiles();

            // עדכון כל שחקן בנפרד
            for (int i = 0; i < _agents.Count; i++)
            {
                Agent agent = _agents[i];
                AgentAI ai = _agentsAI[i];

                // שחקן שכבר escaped לא צריך להתעדכן
                if (agent.HasEscaped)
                    continue;

                // תאים תפוסים על ידי שחקנים אחרים
                HashSet<Point> occupiedByOthers = new HashSet<Point>(occupiedTiles);
                occupiedByOthers.Remove(PixelToTile(agent.PositionPx));

                // תאי סכנה ידועים שהשחקן קיבל דרך תקשורת
                HashSet<Point> knownDangerTiles = GetKnownDangerTiles(agent);

                // עדכון ה-AI של השחקן
                ai.Update(gameTime, agent, _zombies, _exitTile, occupiedByOthers, knownDangerTiles);

                // קבלת היעד הבא במסלול
                Vector2? target = ai.GetNextTarget(agent);

                if (target.HasValue)
                {
                    // חישוב ה-tile הבא במסלול
                    Point nextTile = new Point(
                        (int)(target.Value.X / TileSize),
                        (int)(target.Value.Y / TileSize)
                    );

                    // אם ה-tile הבא לא תפוס על ידי שחקן אחר - נזוז
                    if (!IsNextTileOccupiedByAnotherAgent(i, nextTile))
                    {
                        occupiedTiles.Remove(PixelToTile(agent.PositionPx));
                        agent.MoveToward(target.Value, dt, _map, TileSize);
                        occupiedTiles.Add(PixelToTile(agent.PositionPx));
                    }
                }

                // בדיקה אם השחקן הגיע ליציאה
                Point agentTile = PixelToTile(agent.PositionPx);
                if (_map.IsInside(agentTile.X, agentTile.Y) &&
                    _map.GetTile(agentTile.X, agentTile.Y) == TileType.Exit)
                {
                    agent.MarkEscaped();
                    occupiedTiles.Remove(agentTile);
                }
            }

            // בדיקת הפסד - אם זומבי תופס אחד מהשחקנים הפעילים
            foreach (var z in _zombies)
            {
                foreach (var a in _agents)
                {
                    if (a.HasEscaped)
                        continue;

                    if (Vector2.Distance(z.PositionPx, a.PositionPx) < TileSize * 0.45f)
                    {
                        _isGameOver = true;
                        _hasLost = true;
                        _hasWon = false;
                        break;
                    }
                }

                if (_isGameOver)
                    break;
            }

            // בדיקת ניצחון - אם כל השחקנים escaped
            if (!_isGameOver)
            {
                bool allEscaped = true;
                foreach (var a in _agents)
                {
                    if (!a.HasEscaped)
                    {
                        allEscaped = false;
                        break;
                    }
                }

                if (allEscaped)
                {
                    _isGameOver = true;
                    _hasWon = true;
                    _hasLost = false;
                }
            }

            // עדכון מיקום המצלמה למרכז הקבוצה
            _cameraCenterPx = GetActiveAgentsCenter();

            base.Update(gameTime);
        }
        // יצירת הודעות אזהרה חדשות בין שחקנים
        private void GenerateDangerMessages()
        {
            const float warningRangePx = 160f;
            const float dangerToReceiverPx = 130f;

            // כל שחקן יכול להיות שולח אזהרה
            foreach (var sender in _agents)
            {
                if (sender.HasEscaped)
                    continue;

                // כל זומבי נבדק מול השחקן השולח
                foreach (var zombie in _zombies)
                {
                    float senderToZombie = Vector2.Distance(sender.PositionPx, zombie.PositionPx);
                    if (senderToZombie > warningRangePx)
                        continue;

                    Point dangerTile = PixelToTile(zombie.PositionPx);

                    // אם הזומבי גם מסוכן לשחקן אחר - שולחים הודעה
                    foreach (var receiver in _agents)
                    {
                        if (receiver.HasEscaped)
                            continue;

                        if (receiver.Id == sender.Id)
                            continue;

                        float zombieToReceiver = Vector2.Distance(zombie.PositionPx, receiver.PositionPx);
                        if (zombieToReceiver > dangerToReceiverPx)
                            continue;

                        _messageQueue.Enqueue(new AgentMessage(
                            senderId: sender.Id,
                            receiverId: receiver.Id,
                            type: MessageType.DangerWarning,
                            dangerTile: dangerTile,
                            timeToLiveSeconds: 2.0
                        ));
                    }
                }
            }
        }

        // עיבוד תור ההודעות
        private void ProcessMessageQueue()
        {
            while (_messageQueue.Count > 0)
            {
                AgentMessage msg = _messageQueue.Dequeue();

                Agent receiver = FindAgentById(msg.ReceiverId);
                if (receiver == null || receiver.HasEscaped)
                    continue;

                // כרגע יש סוג הודעה אחד: DangerWarning
                if (msg.Type == MessageType.DangerWarning)
                {
                    receiver.AddOrRefreshDanger(msg.DangerTile, msg.TimeToLiveSeconds);
                }
            }
        }

        // חיפוש שחקן לפי מזהה
        private Agent FindAgentById(int id)
        {
            foreach (var a in _agents)
            {
                if (a.Id == id)
                    return a;
            }
            return null;
        }

        // אוסף כל ה-danger tiles הידועים של שחקן
        private HashSet<Point> GetKnownDangerTiles(Agent agent)
        {
            var result = new HashSet<Point>();
            foreach (var memory in agent.KnownDangers)
                result.Add(memory.Tile);

            return result;
        }

        // ציור כל פריים
        protected override void Draw(GameTime gameTime)
        {
            // צבע רקע לפי מצב המשחק
            Color backgroundColor = Color.Black;
            if (_isGameOver && _hasWon) backgroundColor = Color.DarkGreen;
            else if (_isGameOver && _hasLost) backgroundColor = Color.DarkRed;

            GraphicsDevice.Clear(backgroundColor);

            _spriteBatch.Begin();

            Vector2 viewTopLeft = GetViewTopLeft();

            // ציור העיר
            _cityRenderer.Draw(_spriteBatch, viewTopLeft, ScreenW(), ScreenH());

            // ציור שכבת דיבאג אם מופעלת
            if (_debugDraw)
            {
                _overlayRenderer.DrawDebugOverlay(
                    _spriteBatch,
                    _agents,
                    _agentsAI,
                    _exitTile,
                    viewTopLeft
                );
            }

            // ציור הישויות
            _overlayRenderer.DrawEntities(
                _spriteBatch,
                _agents,
                _zombies,
                viewTopLeft,
                _isGameOver,
                _hasWon,
                _hasLost
            );

            // ציור HUD
            _overlayRenderer.DrawHud(
                _spriteBatch,
                GetEscapedCount(),
                GetActiveCount(),
                _agents.Count,
                _zombies.Count,
                _isGameOver,
                _hasWon,
                _hasLost
            );

            // אם המשחק נגמר - מציגים גם רמז לריסט
            if (_isGameOver)
                _overlayRenderer.DrawRestartHint(_spriteBatch);

            _spriteBatch.End();

            base.Draw(gameTime);
        }

        // חישוב הפינה השמאלית-עליונה של המסך לפי מרכז המצלמה
        private Vector2 GetViewTopLeft()
            => _cameraCenterPx - new Vector2(ScreenW() / 2f, ScreenH() / 2f);

        // רוחב המסך
        private int ScreenW() => _graphics.PreferredBackBufferWidth;

        // גובה המסך
        private int ScreenH() => _graphics.PreferredBackBufferHeight;

        // חיפוש היציאה במפה
        private static Point FindExitTile(GridMap map)
        {
            for (int y = 0; y < map.Height; y++)
                for (int x = 0; x < map.Width; x++)
                    if (map.GetTile(x, y) == TileType.Exit)
                        return new Point(x, y);

            return new Point(1, 1);
        }

        // המרה ממיקום בפיקסלים ל-tile במפה
        private Point PixelToTile(Vector2 posPx)
            => new Point((int)(posPx.X / TileSize), (int)(posPx.Y / TileSize));

        // המרה מ-tile למרכז התא בפיקסלים
        private Vector2 TileCenter(int tileX, int tileY)
            => new Vector2(tileX * TileSize + TileSize / 2f, tileY * TileSize + TileSize / 2f);

        // חישוב מרכז כל השחקנים הפעילים עבור המצלמה
        private Vector2 GetActiveAgentsCenter()
        {
            Vector2 sum = Vector2.Zero;
            int count = 0;

            foreach (var a in _agents)
            {
                if (a.HasEscaped)
                    continue;

                sum += a.PositionPx;
                count++;
            }

            if (count == 0)
                return TileCenter(_exitTile.X, _exitTile.Y);

            return sum / count;
        }

        // מחזיר את מיקום השחקן הפעיל הקרוב ביותר לזומבי
        private Vector2 GetClosestActiveAgentPosition(Vector2 zombiePositionPx)
        {
            Vector2 best = Vector2.Zero;
            float bestDistSq = float.MaxValue;
            bool found = false;

            foreach (var a in _agents)
            {
                if (a.HasEscaped)
                    continue;

                float d = Vector2.DistanceSquared(zombiePositionPx, a.PositionPx);
                if (d < bestDistSq)
                {
                    bestDistSq = d;
                    best = a.PositionPx;
                    found = true;
                }
            }

            return found ? best : TileCenter(_exitTile.X, _exitTile.Y);
        }

        // אוסף כל ה-tiles שתפוסים כרגע על ידי שחקנים
        private HashSet<Point> GetOccupiedAgentTiles()
        {
            var occupied = new HashSet<Point>();

            foreach (var a in _agents)
            {
                if (a.HasEscaped)
                    continue;

                occupied.Add(PixelToTile(a.PositionPx));
            }

            return occupied;
        }

        // בדיקה אם ה-tile הבא תפוס על ידי שחקן אחר
        private bool IsNextTileOccupiedByAnotherAgent(int currentAgentIndex, Point nextTile)
        {
            for (int i = 0; i < _agents.Count; i++)
            {
                if (i == currentAgentIndex)
                    continue;

                if (_agents[i].HasEscaped)
                    continue;

                Point otherTile = PixelToTile(_agents[i].PositionPx);
                if (otherTile == nextTile)
                    return true;
            }

            return false;
        }

        // ספירת שחקנים שכבר ברחו
        private int GetEscapedCount()
        {
            int count = 0;
            foreach (var a in _agents)
            {
                if (a.HasEscaped)
                    count++;
            }
            return count;
        }

        // ספירת שחקנים שעדיין פעילים
        private int GetActiveCount()
        {
            int count = 0;
            foreach (var a in _agents)
            {
                if (!a.HasEscaped)
                    count++;
            }
            return count;
        }
    }
}