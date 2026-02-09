using Google.Protobuf;
using Google.Protobuf.Protocol;
using ServerCore;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Server.Game
{
	public class GameRoom
	{
		object _lock = new object();
		public int RoomId { get; set; }

		List<Player> _players = new List<Player>();

		public void EnterGame(Player newPlayer)
		{
			if (newPlayer == null)
				return;

			lock (_lock)
			{
				_players.Add(newPlayer);
				newPlayer.Room = this;

				Console.ForegroundColor = ConsoleColor.Cyan;
				Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [Room] Player {newPlayer.Info.PlayerId} Entered GameRoom. (Total: {_players.Count})");
				Console.ResetColor();

				// 본인한테 정보 전송
				{
					S_EnterGame enterPacket = new S_EnterGame();
					enterPacket.Player = newPlayer.Info;
					newPlayer.Session.Send(enterPacket);

					S_Spawn spawnPacket = new S_Spawn();
					foreach (Player p in _players)
					{
						if (newPlayer != p)
							spawnPacket.Players.Add(p.Info);
					}
					newPlayer.Session.Send(spawnPacket);
				}

				// 타인한테 정보 전송
				{
					S_Spawn spawnPacket = new S_Spawn();
					spawnPacket.Players.Add(newPlayer.Info);
					foreach (Player p in _players)
					{
						if (newPlayer != p)
							p.Session.Send(spawnPacket);
					}
				}
			}
		}

		public void LeaveGame(int playerId)
		{
			lock (_lock)
			{
				Player player = _players.Find(p => p.Info.PlayerId == playerId);
				if (player == null)
					return;

				_players.Remove(player);
				player.Room = null;

				// 본인한테 정보 전송
				{
					S_LeaveGame leavePacket = new S_LeaveGame();
					player.Session.Send(leavePacket);
				}

				// 타인한테 정보 전송
				{
					S_Despawn despawnPacket = new S_Despawn();
					despawnPacket.PlayerIds.Add(player.Info.PlayerId);
					foreach (Player p in _players)
					{
						if (player != p)
							p.Session.Send(despawnPacket);
					}
				}
				Console.ForegroundColor = ConsoleColor.Cyan;
				Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [Room] Player {player.Info.PlayerId} Left GameRoom. (Total: {_players.Count})");
				Console.ResetColor();
			}
		}

		public void HandleMove(Player player, C_Move movePacket)
		{
			if (player == null)
				return;

			lock (_lock)
			{
				// TODO : 검증

				// 서버에 있는 내 위치 정보 갱신
				PlayerInfo info = player.Info;
				info.PosInfo = movePacket.PosInfo;

				// 다른 플레이어한테도 알려줌
				S_Move resMovePacket = new S_Move();
				resMovePacket.PlayerId = player.Info.PlayerId;
				resMovePacket.PosInfo = movePacket.PosInfo;

				Broadcast(resMovePacket);
			}

			// 많이 뜨면 주석 처리
			//Console.ForegroundColor = ConsoleColor.DarkGray;
			//Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [Move] ID({clientSession.SessionId}) -> ({movePacket.PosInfo.PosX:F2}, {movePacket.PosInfo.PosY:F2})");
			//Console.ResetColor();
		}

		public void Broadcast(IMessage packet)
		{
			lock (_lock)
			{
				foreach (Player p in _players)
				{
					p.Session.Send(packet);
				}
			}
		}
	}
}
