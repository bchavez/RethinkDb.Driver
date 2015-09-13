using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Templates.UnitTests
{
	public class TestGroup
	{
		public string Desc { get; set; }
		[JsonProperty("table_variable_name")]
		public string TableVariableName { get; set; }
		public List<JObject> Tests { get; set; }
		public string File { get; set; }
	}

	public class UnitTestDb
	{
		public static List<TestGroup> LoadAll()
		{
			var json = File.ReadAllText("../../UnitTests/default.json");
			var tg = JsonConvert.DeserializeObject<TestGroup>(json);
			return new List<TestGroup> {tg};
		}
	}
}