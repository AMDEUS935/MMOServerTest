using Google.Protobuf;
using Google.Protobuf.Protocol;
using Server.Data;
using Server.Game.Object;
using ServerCore;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Threading;

namespace Server.Game.Room
{
	public class GameRoom
	{
		object _lock = new object();
		public int RoomId { get; set; }

		Dictionary<int, Player> _players = new Dictionary<int, Player>();
		Dictionary<int, Monster> _monster = new Dictionary<int, Monster>();
		Dictionary<int, Projectile> _projectiles = new Dictionary<int, Projectile>();

		public Map Map{ get; private set; } = new Map();

		public void Init(int mapId)
		{
			Map.LoadMap(mapId);
		}

		public void Update()
		{
			lock (_lock)
			{
				foreach (var projectile in _projectiles.Values)
				{
					projectile.Update();
				}
			}
		}

		public void EnterGame(GameObject gameObject)
		{
			if (gameObject == null)
				return;

			GameObjectType type = ObjectManager.GetObjectTypeById(gameObject.id);

			lock (_lock)
			{
				if (type == GameObjectType.Player)
				{
					Player player = gameObject as Player;
					_players.Add(gameObject.id, player);
					player.Room = this;

					Console.ForegroundColor = ConsoleColor.Cyan;
					Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [Room] Player {player.Info.ObjectId} Entered GameRoom. (Total: {_players.Count})");
					Console.ResetColor();

					// 본인한테 정보 전송
					{
						S_EnterGame enterPacket = new S_EnterGame();
						enterPacket.Player = player.Info;
						player.Session.Send(enterPacket);

						S_Spawn spawnPacket = new S_Spawn();
						foreach (Player p in _players.Values)
						{
							if (player != p)
								spawnPacket.Objects.Add(p.Info);
						}
						player.Session.Send(spawnPacket);
					}
				}

				else if (type == GameObjectType.Monster)
				{
					Monster monster = gameObject as Monster;
					_monster.Add(gameObject.id, monster);
					monster.Room = this;
				}

				else if (type == GameObjectType.Projectile)
				{
					Projectile projectile = gameObject as Projectile;
					_projectiles.Add(gameObject.id, projectile);
					projectile.Room = this;
				}

				// 타인한테 정보 전송
				{
					S_Spawn spawnPacket = new S_Spawn();
					spawnPacket.Objects.Add(gameObject.Info);
					foreach (Player p in _players.Values)
					{
						if (p.id != gameObject.id)
							p.Session.Send(spawnPacket);
					}
				}
			}
		}

		public void LeaveGame(int objectId)
		{
			GameObjectType type = ObjectManager.GetObjectTypeById(objectId);

			lock (_lock)
			{
				if (type == GameObjectType.Player)
				{
					Player player = null;
					if (_players.Remove(objectId, out player) == false)
						return;

					player.Room = null;
					Map.ApplyLeave(player);

					// 본인한테 정보 전송
					{
						S_LeaveGame leavePacket = new S_LeaveGame();
						player.Session.Send(leavePacket);
					}
				}
				else if (type == GameObjectType.Monster)
				{
					Monster monster = null;

					if (_monster.Remove(objectId, out monster) == false)
						return;

					monster.Room = null;
					Map.ApplyLeave(monster);
				}
				else if (type == GameObjectType.Projectile)
				{
					Projectile projectile = null;

					if (_projectiles.Remove(objectId, out projectile) == false)
						return;

					projectile.Room = null;
				}

				// 타인한테 정보 전송
				{
					S_Despawn despawnPacket = new S_Despawn();
					despawnPacket.ObjectIds.Add(objectId);
					foreach (Player p in _players.Values)
					{
						if (p.id != objectId)
							p.Session.Send(despawnPacket);
					}
				}
			}
		}

		public void HandleMove(Player player, C_Move movePacket)
		{
			if (player == null)
				return;

			lock (_lock)
			{
				PositionInfo movePosInfo = movePacket.PosInfo;
				ObjectInfo info = player.Info;

				// 좌표 이동시, 이동 가능 여부 체크
				if (movePosInfo.PosX != info.PosInfo.PosX || movePosInfo.PosY != info.PosInfo.PosY)
				{
					if (Map.CanGo(new Vector2Int(movePosInfo.PosX, movePosInfo.PosY)) == false)
						return;
				}

				info.PosInfo.State = movePosInfo.State;
				info.PosInfo.MoveDir = movePosInfo.MoveDir;
				Map.ApplyMove(player, new Vector2Int(movePosInfo.PosX, movePosInfo.PosY));


				// 다른 플레이어한테도 알려줌
				S_Move resMovePacket = new S_Move();
				resMovePacket.ObjectId = player.Info.ObjectId;
				resMovePacket.PosInfo = movePacket.PosInfo;

				Broadcast(resMovePacket);
			}
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

				info.PosInfo.State = CreatureState.Skill;

				S_Skill skill = new S_Skill() { Info = new Skill_Info() };

				skill.ObjectId = info.ObjectId;
				skill.Info.SkillId = skillPacket.Info.SkillId;

				Broadcast(skill);

				Data.Skill skillData = null;
				if (DataManager.SkillDict.TryGetValue(skillPacket.Info.SkillId, out skillData) == false)
					return;

				switch (skillData.SkillType)
				{
					case SkillType.SkillAuto:
					{
						Vector2Int skillPos = player.GetFrontCellPos(info.PosInfo.MoveDir);
						GameObject target = Map.Find(skillPos);
						if (target != null)
						{
							Console.WriteLine("Hit GameObject!");
						}
					}
						break;
					case SkillType.SkillProjectile:
					{
						Arrow arrow = ObjectManager.Instance.Add<Arrow>();

						if (arrow == null)
							return;

						arrow.Owner = player;
						arrow.Data = skillData;

						arrow.PosInfo.State = CreatureState.Moving;
						arrow.PosInfo.MoveDir = info.PosInfo.MoveDir;

						arrow.PosInfo.PosX = info.PosInfo.PosX;
						arrow.PosInfo.PosY = info.PosInfo.PosY;

						EnterGame(arrow);
					}
						break;
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
