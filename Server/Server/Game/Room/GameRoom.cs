using Google.Protobuf;
using Google.Protobuf.Protocol;
using Server.Game.Object;
using ServerCore;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Server.Game.Room
{
	public class GameRoom
	{
		object _lock = new object();
		public int RoomId { get; set; }

		Dictionary<int, Player> _players = new Dictionary<int, Player>();

		Map _map = new Map();

		public void Init(int mapId)
		{
			_map.LoadMap(mapId);
		}

		public void EnterGame(Player newPlayer)
		{
			if (newPlayer == null)
				return;

			lock (_lock)
			{
				_players.Add(newPlayer.Info.ObjectId, newPlayer);
				newPlayer.Room = this;

				Console.ForegroundColor = ConsoleColor.Cyan;
				Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [Room] Player {newPlayer.Info.ObjectId} Entered GameRoom. (Total: {_players.Count})");
				Console.ResetColor();

				// 본인한테 정보 전송
				{
					S_EnterGame enterPacket = new S_EnterGame();
					enterPacket.Player = newPlayer.Info;
					newPlayer.Session.Send(enterPacket);

					S_Spawn spawnPacket = new S_Spawn();
					foreach (Player p in _players.Values)
					{
						if (newPlayer != p)
							spawnPacket.Objects.Add(p.Info);
					}
					newPlayer.Session.Send(spawnPacket);
				}

				// 타인한테 정보 전송
				{
					S_Spawn spawnPacket = new S_Spawn();
					spawnPacket.Objects.Add(newPlayer.Info);
					foreach (Player p in _players.Values)
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
				Player player = null;
				if (_players.Remove(playerId, out player) == false)
					return;

				player.Room = null;

				// 본인한테 정보 전송
				{
					S_LeaveGame leavePacket = new S_LeaveGame();
					player.Session.Send(leavePacket);
				}

				// 타인한테 정보 전송
				{
					S_Despawn despawnPacket = new S_Despawn();
					despawnPacket.PlayerIds.Add(player.Info.ObjectId);
					foreach (Player p in _players.Values)
					{
						if (player != p)
							p.Session.Send(despawnPacket);
					}
				}
				Console.ForegroundColor = ConsoleColor.Cyan;
				Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [Room] Player {player.Info.ObjectId} Left GameRoom. (Total: {_players.Count})");
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
				PositionInfo movePosInfo = movePacket.PosInfo;
				ObjectInfo info = player.Info;

				// 좌표 이동시, 이동 가능 여부 체크
				if (movePosInfo.PosX != info.PosInfo.PosX || movePosInfo.PosY != info.PosInfo.PosY)
				{
					if (_map.CanGo(new Vector2Int(movePosInfo.PosX, movePosInfo.PosY)) == false)
						return;
				}

				info.PosInfo.State = movePosInfo.State;
				info.PosInfo.MoveDir = movePosInfo.MoveDir;
				_map.ApplyMove(player, new Vector2Int(movePosInfo.PosX, movePosInfo.PosY));


				// 다른 플레이어한테도 알려줌
				S_Move resMovePacket = new S_Move();
				resMovePacket.PlayerId = player.Info.ObjectId;
				resMovePacket.PosInfo = movePacket.PosInfo;

				Broadcast(resMovePacket);
			}

			// 많이 뜨면 주석 처리
			//Console.ForegroundColor = ConsoleColor.DarkGray;
			//Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [Move] ID({clientSession.SessionId}) -> ({movePacket.PosInfo.PosX:F2}, {movePacket.PosInfo.PosY:F2})");
			//Console.ResetColor();
		}

		public void HandleSkill(Player player, C_Skill skillPacket)
		{
			if (player == null)
				return;

			lock (_lock)
			{
				ObjectInfo info = player.Info;

				if (info.PosInfo.State != CreatureState.Idle)
					return;

				// TODO : 스킬 가능여부 체크

				info.PosInfo.State = CreatureState.Skill;

				S_Skill skill = new S_Skill() { Info = new Skill_Info() };

				skill.PlayerId = info.ObjectId;
				skill.Info.SkillId = skillPacket.Info.SkillId;

				Broadcast(skill);

				if (skillPacket.Info.SkillId == 1)
				{
					// TODO : 데미지 판정
					Vector2Int skillPos = player.GetFrontCellPos(info.PosInfo.MoveDir);
					Player target = _map.Find(skillPos);
					if (target != null)
					{
						Console.WriteLine("Hit Player!");
					}
				}
				else if (skillPacket.Info.SkillId == 2)
				{
					// TODO : Arrow
					
				}
			}
		}

		public void Broadcast(IMessage packet)
		{
			lock (_lock)
			{
				foreach (Player p in _players.Values)
				{
					p.Session.Send(packet);
				}
			}
		}
	}
}
