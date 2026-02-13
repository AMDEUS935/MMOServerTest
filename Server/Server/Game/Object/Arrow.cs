using Google.Protobuf.Protocol;
using Server.Game.Room;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Game.Object
{
	public class Arrow : Projectile
	{
		public GameObject Owner { get; set; }
		
		long _nextMoveTick = 0;

		public override void Update()
		{
			if(Owner == null || Room == null)
				return;

			if (Environment.TickCount64 < _nextMoveTick)
				return;

			_nextMoveTick = Environment.TickCount64 + 50;

			Vector2Int destPos = GetFrontCellPos();
			
			if (Room.Map.CanGo(destPos))
			{
				CellPos = destPos;

				S_Move movePacket = new S_Move();
				movePacket.ObjectId = id;
				movePacket.PosInfo = PosInfo;
				Room.Broadcast(movePacket);

				Console.WriteLine("Move Arrow!");
			}
			else
			{
				GameObject target = Room.Map.Find(destPos);
				
				if (target != null)
				{
					// 피격 판정
				}

				Room.LeaveGame(id);
			}

		}
	}
}
