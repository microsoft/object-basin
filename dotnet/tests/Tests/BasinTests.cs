namespace ObjectBasin.Tests
{
	using ObjectBasin;

	[TestClass]
	public class BasinTests
	{
		[TestMethod]
		public void IntTests()
		{
			var basin = new Basin<int>();
			const string key = "key";
			basin.SetCursor(new BasinCursor { JsonPath = $"/{key}" });
			Assert.AreEqual(3, basin.Write(3));
			Assert.AreEqual(3, basin.Items[key]);
		}

		[TestMethod]
		public void StringTests()
		{
			var basin = new Basin<string>();
			const string key = "key";
			basin.SetCursor(new BasinCursor { JsonPath = $"/{key}/" });
			Assert.AreEqual("2", basin.Write("2"));
			Assert.AreEqual("2", basin.Items[key]);

			basin.SetCursor(new BasinCursor { JsonPath = $"/o~1k/" });
			Assert.AreEqual("4", basin.Write("4"));
			Assert.AreEqual("4", basin.Items["o/k"]);
		}
	}
}
