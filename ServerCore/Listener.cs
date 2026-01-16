using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ServerCore
{
	public class Listener
	{
		Socket _listenSocket;
		Func<Session> _sessionFactory;

		public void init(IPEndPoint endPoint, Func<Session> sessionFactory) //  서버 초기화
		{
			_listenSocket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
			_sessionFactory += sessionFactory;

			_listenSocket.Bind(endPoint); // 특정 IP 주소와 포트에서 클라이언트의 연결 요청을 수신

			_listenSocket.Listen(10); // 클라이언트의 연결 요청을 수신하기 위한 대기열을 설정

			SocketAsyncEventArgs args = new SocketAsyncEventArgs();
			args.Completed += new EventHandler<SocketAsyncEventArgs>(OnAcceptCompleted);
			RegisterAccept(args);
		}

		void RegisterAccept(SocketAsyncEventArgs args) // 연결 수락 - 비동기적으로 호출
		{
			args.AcceptSocket = null; // accept 소켓 초기화

			bool pending = _listenSocket.AcceptAsync(args);

			if (pending == false)
				OnAcceptCompleted(null, args);
		}

		void OnAcceptCompleted(object sender, SocketAsyncEventArgs args) // 연결 수락 완료 , 클라이언트 소켓 처리
		{
			if (args.SocketError == SocketError.Success)
			{
				Session session = _sessionFactory.Invoke();

				session.Start(args.AcceptSocket);

				session.OnConnected(args.AcceptSocket.RemoteEndPoint);
			}
			else
				Console.WriteLine(args.SocketError.ToString());

			RegisterAccept(args);
		}
	}
}
