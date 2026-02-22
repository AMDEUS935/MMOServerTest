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
			if(Data == null || Data.Projectile == null || Owner == null || Room == null)
				return;

			if (Environment.TickCount64 < _nextMoveTick)
				return;

			long tick = (long)(1000 / Data.Projectile.speed);
			_nextMoveTick = Environment.TickCount64 + tick;

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
					target.OnDamaged(Owner, Data.Damage);
				}

				Room.LeaveGame(id);
			}
		}
	}
}
