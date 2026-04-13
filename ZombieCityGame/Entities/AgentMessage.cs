using Microsoft.Xna.Framework;

namespace ZombieCityGame.Entities
{
    public class AgentMessage
    {
        public int SenderId { get; }
        public int ReceiverId { get; }
        public MessageType Type { get; }
        public Point DangerTile { get; }
        public double TimeToLiveSeconds { get; }

        public AgentMessage(
            int senderId,
            int receiverId,
            MessageType type,
            Point dangerTile,
            double timeToLiveSeconds = 2.0)
        {
            SenderId = senderId;
            ReceiverId = receiverId;
            Type = type;
            DangerTile = dangerTile;
            TimeToLiveSeconds = timeToLiveSeconds;
        }
    }
}
