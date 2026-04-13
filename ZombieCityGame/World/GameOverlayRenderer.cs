using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ZombieCityGame.Entities;
using ZombieCityGame.AI;

namespace ZombieCityGame.World
{
    public class GameOverlayRenderer
    {
        private readonly Texture2D _pixel;
        private readonly SpriteFont _font;
        private readonly int _tileSize;

        public GameOverlayRenderer(Texture2D pixel, SpriteFont font, int tileSize)
        {
            _pixel = pixel;
            _font = font;
            _tileSize = tileSize;
        }

        public void DrawEntities(
            SpriteBatch spriteBatch,
            List<Agent> agents,
            List<Zombie> zombies,
            Vector2 viewTopLeft,
            bool isGameOver,
            bool hasWon,
            bool hasLost)
        {
            foreach (var z in zombies)
                DrawEntityRect(spriteBatch, z.PositionPx, viewTopLeft, Color.Red);

            Color[] agentColors = { Color.Blue, Color.Cyan, Color.Orange, Color.MediumPurple };

            for (int i = 0; i < agents.Count; i++)
            {
                if (agents[i].HasEscaped)
                    continue;

                Color c = agentColors[i % agentColors.Length];
                if (isGameOver && hasWon) c = Color.LimeGreen;
                else if (isGameOver && hasLost) c = Color.Black;

                DrawEntityRect(spriteBatch, agents[i].PositionPx, viewTopLeft, c);
                DrawAgentLabel(spriteBatch, agents[i], viewTopLeft, c);
            }
        }

        public void DrawDebugOverlay(
            SpriteBatch spriteBatch,
            List<Agent> agents,
            List<AgentAI> agentsAI,
            Point exitTile,
            Vector2 viewTopLeft)
        {
            for (int i = 0; i < agents.Count; i++)
            {
                if (agents[i].HasEscaped)
                    continue;

                foreach (var tile in agentsAI[i].CurrentPath)
                {
                    float screenX = tile.X * _tileSize - viewTopLeft.X;
                    float screenY = tile.Y * _tileSize - viewTopLeft.Y;

                    Rectangle rect = new Rectangle((int)screenX + 6, (int)screenY + 6, _tileSize - 12, _tileSize - 12);
                    spriteBatch.Draw(_pixel, rect, new Color(0, 255, 0, 90));
                }

                if (agentsAI[i].CurrentPath.Count >= 2)
                {
                    Point next = agentsAI[i].CurrentPath[1];
                    float screenX = next.X * _tileSize - viewTopLeft.X;
                    float screenY = next.Y * _tileSize - viewTopLeft.Y;

                    Rectangle rect = new Rectangle((int)screenX + 10, (int)screenY + 10, _tileSize - 20, _tileSize - 20);
                    spriteBatch.Draw(_pixel, rect, new Color(0, 120, 255, 160));
                }
            }

            float exitScreenX = exitTile.X * _tileSize - viewTopLeft.X;
            float exitScreenY = exitTile.Y * _tileSize - viewTopLeft.Y;
            DrawRectOutline(
                spriteBatch,
                new Rectangle((int)exitScreenX, (int)exitScreenY, _tileSize, _tileSize),
                Color.White
            );
        }

        public void DrawHud(
            SpriteBatch spriteBatch,
            int escapedCount,
            int activeCount,
            int totalCount,
            int zombieCount,
            bool isGameOver,
            bool hasWon,
            bool hasLost)
        {
            string statusText = "RUNNING";
            if (isGameOver && hasWon) statusText = "WON";
            else if (isGameOver && hasLost) statusText = "LOST";

            string escapedLine = $"Escaped: {escapedCount} / {totalCount}";
            string activeLine = $"Active: {activeCount} / {totalCount}";
            string zombiesLine = $"Zombies: {zombieCount}";
            string statusLine = $"Status: {statusText}";

            Vector2 panelPos = new Vector2(10, 10);
            int panelWidth = 240;
            int panelHeight = 105;

            spriteBatch.Draw(
                _pixel,
                new Rectangle((int)panelPos.X, (int)panelPos.Y, panelWidth, panelHeight),
                new Color(0, 0, 0, 160)
            );

            DrawRectOutline(
                spriteBatch,
                new Rectangle((int)panelPos.X, (int)panelPos.Y, panelWidth, panelHeight),
                Color.White
            );

            float left = panelPos.X + 10;
            float top = panelPos.Y + 8;
            float lineSpacing = 20f;

            spriteBatch.DrawString(_font, escapedLine, new Vector2(left, top + lineSpacing * 0), Color.White);
            spriteBatch.DrawString(_font, activeLine, new Vector2(left, top + lineSpacing * 1), Color.White);
            spriteBatch.DrawString(_font, zombiesLine, new Vector2(left, top + lineSpacing * 2), Color.White);
            spriteBatch.DrawString(_font, statusLine, new Vector2(left, top + lineSpacing * 3), Color.White);
        }

        public void DrawRestartHint(SpriteBatch spriteBatch)
        {
            spriteBatch.DrawString(_font, "Press R to restart", new Vector2(20, 120), Color.White);
        }

        private void DrawEntityRect(SpriteBatch spriteBatch, Vector2 worldPosPx, Vector2 viewTopLeft, Color color)
        {
            float screenX = worldPosPx.X - viewTopLeft.X;
            float screenY = worldPosPx.Y - viewTopLeft.Y;

            Rectangle rect = new Rectangle(
                (int)(screenX - _tileSize / 2f),
                (int)(screenY - _tileSize / 2f),
                _tileSize,
                _tileSize
            );

            spriteBatch.Draw(_pixel, rect, color);
        }

        private void DrawAgentLabel(SpriteBatch spriteBatch, Agent agent, Vector2 viewTopLeft, Color color)
        {
            string text = agent.RoleName;

            float screenX = agent.PositionPx.X - viewTopLeft.X;
            float screenY = agent.PositionPx.Y - viewTopLeft.Y;

            Vector2 textSize = _font.MeasureString(text);

            Vector2 textPos = new Vector2(
                screenX - textSize.X / 2f,
                screenY - _tileSize / 2f - textSize.Y - 6
            );

            spriteBatch.DrawString(_font, text, textPos + new Vector2(1, 1), Color.Black);
            spriteBatch.DrawString(_font, text, textPos, color);
        }

        private void DrawRectOutline(SpriteBatch spriteBatch, Rectangle rect, Color color)
        {
            spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, rect.Width, 2), color);
            spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Bottom - 2, rect.Width, 2), color);
            spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, 2, rect.Height), color);
            spriteBatch.Draw(_pixel, new Rectangle(rect.Right - 2, rect.Y, 2, rect.Height), color);
        }
    }
}
