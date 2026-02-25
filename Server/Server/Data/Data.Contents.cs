using Google.Protobuf.Protocol;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Data
{
	#region Stat
	[Serializable]
	public class StatData : ILoader<int, StatInfo>
	{
		public List<StatInfo> stats = new List<StatInfo>();

		public Dictionary<int, StatInfo> MakeDict()
		{
			Dictionary<int, StatInfo> dict = new Dictionary<int, StatInfo>();

			foreach (StatInfo stat in stats)
			{
				stat.Hp = stat.MaxHp; // 초기 HP는 MaxHP로 설정
				dict.Add(stat.Level, stat);
			}
			return dict;
		}
	}
	#endregion

	#region Skill
	[Serializable]
	public class Skill
	{
		public int Id;
		public string Name;
		public float Cooldown; 
		public int Damage;
		public SkillType SkillType; 
		public ProjectileInfo Projectile; 

	}

	public class ProjectileInfo
	{
		public string name;
		public float speed;
		public int range;
		public string prefab;
	}

	[Serializable]
	public class SkillData : ILoader<int, Skill>
	{
		public List<Skill> skills = new List<Skill>();

		public Dictionary<int, Skill> MakeDict()
		{
			Dictionary<int, Skill> dict = new Dictionary<int, Skill>();
			foreach (Skill skill in skills)
				dict.Add(skill.Id, skill);
			return dict;
		}
	}
	#endregion
}