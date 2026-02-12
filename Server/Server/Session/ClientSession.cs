using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using ServerCore;
using System.Net;
using Google.Protobuf.Protocol;
using Google.Protobuf;
using Server.Game;

namespace Server
{
	public class ClientSession : PacketSession
	{
		public Player MyPlayer { get; set; }
        public int SessionId { get; set; }

		public void Send(IMessage packet)
		{
			string msgName = packet.Descriptor.Name.Replace("_", string.Empty);
			MsgId msgId = (MsgId)Enum.Parse(typeof(MsgId), msgName);

            ushort size = (ushort)packet.CalculateSize();

            byte[] sendBuffer = new byte[size + 4];

            Array.Copy(BitConverter.GetBytes(size + 4), 0, sendBuffer, 0, sizeof(ushort));
            Array.Copy(BitConverter.GetBytes((ushort)msgId), 0, sendBuffer, 2, sizeof(ushort));
            Array.Copy(packet.ToByteArray(), 0, sendBuffer, 4, size);
			Send(new ArraySegment<byte>(sendBuffer));
        }

		public override void OnConnected(EndPoint endPoint)
		{
			Console.ForegroundColor = ConsoleColor.Green;
			Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [Server] Player {SessionId} Connected : {endPoint}");
			Console.ResetColor();

			MyPlayer = PlayerManager.Instance.Add();
			{
				MyPlayer.Info.Name = $"Player{MyPlayer.Info.ObjectId}";	
				MyPlayer.Info.PosInfo.State = CreatureState.Idle;
				MyPlayer.Info.PosInfo.MoveDir = MoveDir.Down;
				MyPlayer.Info.PosInfo.PosX = 0;
				MyPlayer.Info.PosInfo.PosY = 0;
				MyPlayer.Session = this;
            }

            RoomManager.Instance.Find(1).EnterGame(MyPlayer);
        }

        public override void OnRecvPacket(ArraySegment<byte> buffer)
		{
			PacketManager.Instance.OnRecvPacket(this, buffer);
		}

		public override void OnDisconnected(EndPoint endPoint)
		{
			RoomManager.Instance.Find(1).LeaveGame(MyPlayer.Info.ObjectId);

            SessionManager.Instance.Remove(this);
			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [Server] Player {SessionId} Disconnected : {endPoint}");
			Console.ResetColor();
		}

		public override void OnSend(int numOfBytes)
		{
			//Console.WriteLine($"Transferred bytes: {numOfBytes}");
		}
	}
}
