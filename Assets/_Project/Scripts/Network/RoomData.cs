using System;

namespace Gunbound.Network
{
    /// <summary>
    /// Data container representing a match room in the Room Browser directory (RF-6.3.1).
    /// </summary>
    [Serializable]
    public class RoomData
    {
        public string roomId;
        public string roomName;
        public string hostName;
        public string mapName;
        public int currentPlayers;
        public int maxPlayers;
        public string serverIp;
        public ushort serverPort;
        public bool isPrivate;

        public RoomData(string id, string name, string host, string map, int current, int max, string ip, ushort port, bool privateRoom = false)
        {
            roomId = id;
            roomName = name;
            hostName = host;
            mapName = map;
            currentPlayers = current;
            maxPlayers = max;
            serverIp = ip;
            serverPort = port;
            isPrivate = privateRoom;
        }
    }
}
