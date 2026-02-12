using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Game.Object
{
	public class Arrow : Projectile
	{
		public GameObject Owner { get; set; }

		public void Update()
		{
			// 화살 이동 로직 구현
		}
	}
}
