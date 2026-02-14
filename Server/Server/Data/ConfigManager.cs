using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Data
{
	[Serializable]
	public class ServerConfig
	{
		public string dataPath { get; set; }
	}

	public class ConfigManager
	{
		public static ServerConfig Config { get; private set; }
		public static void LoadConfig()
		{
			string text = System.IO.File.ReadAllText("config.json");
			Config = JsonConvert.DeserializeObject<ServerConfig>(text);
		}
	}
}
