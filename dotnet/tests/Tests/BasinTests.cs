namespace ObjectBasin.Tests
{
	using ObjectBasin;

	[TestClass]
	public class BasinTests
	{
		[TestMethod]
		public void ExampleTests()
		{
			var basin = new Basin<object>();
			basin.SetCursor(new BasinCursor { JsonPath = "message" });
			Assert.AreEqual("ello", basin.Write("ello"));
			basin.SetCursor(new BasinCursor { JsonPath = "message", Position = -1 });
			Assert.AreEqual("ello World", basin.Write(" World"));
			Assert.AreEqual("ello World!", basin.Write("!"));
			Assert.AreEqual("ello World!", basin.Items["message"]);

			basin.SetCursor(new BasinCursor { JsonPath = "message", Position = 0 });
			Assert.AreEqual("Hello World!", basin.Write("H"));
			Assert.AreEqual("Hello World!", basin.Items["message"]);
		}

		[TestMethod]
		public void IntTests()
		{
			var basin = new Basin<int>();
			const string key = "key";
			basin.SetCursor(new BasinCursor { JsonPath = $"$.['{key}']" });
			Assert.AreEqual(3, basin.Write(3));
			Assert.AreEqual(3, basin.Items[key]);
		}

		[TestMethod]
		public void StringTests()
		{
			var basin = new Basin<string>();
			const string key = "key";
			basin.SetCursor(new BasinCursor { JsonPath = $"$.{key}" });
			Assert.AreEqual("2", basin.Write("2"));
			Assert.AreEqual("2", basin.Items[key]);
			Assert.AreEqual("3", basin.Write("3"));
			Assert.AreEqual("3", basin.Items[key]);

			basin.SetCursor(new BasinCursor { JsonPath = $"$.[o/k]" });
			Assert.AreEqual("4", basin.Write("4"));
			Assert.AreEqual("4", basin.Items["o/k"]);
		}



		[DataRow("/key/", "key")]
		[DataRow("/key/", "$.['key']")]
		[DataRow("/key/", "$.[key]")]
		[DataRow("/key/", "$.key")]
		[DataRow("/key/", "$['key']")]
		[DataRow("/key/", "$[key]")]
		[DataRow("/key/", "$key")]
		[TestMethod]
		public void PathConverionTests(string expected, string input)
		{
			Assert.AreEqual(expected, Basin<object>.ConvertJsonPathToJsonPatchPath(input));
		}
	}
}
