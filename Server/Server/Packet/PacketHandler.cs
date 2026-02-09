using Google.Protobuf;
using Google.Protobuf.Protocol;
using Server;
using ServerCore;
using System;
using System.Collections.Generic;
using System.Text;

class PacketHandler
{
	public static void C_MoveHandler(PacketSession session, IMessage packet)
	{
		C_Move movePacket = packet as C_Move;
		ClientSession clientSession = session as ClientSession;

		if (clientSession.MyPlayer == null)
			return;
		if (clientSession.MyPlayer.Room == null)
			return;

		// TODO : 검증

		PlayerInfo info = clientSession.MyPlayer.Info;
		info.PosInfo = movePacket.PosInfo;

		// 다른 플레이어한테도 알려줌
		S_Move resMovePacket = new S_Move();
		resMovePacket.PlayerId = clientSession.MyPlayer.Info.PlayerId;
		resMovePacket.PosInfo = movePacket.PosInfo;

		clientSession.MyPlayer.Room.Broadcast(resMovePacket);

		// 많이 뜨면 주석 처리
		Console.ForegroundColor = ConsoleColor.DarkGray; 
		Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [Move] ID({clientSession.SessionId}) -> ({movePacket.PosInfo.PosX:F2}, {movePacket.PosInfo.PosY:F2})");
		Console.ResetColor();
	}
}
