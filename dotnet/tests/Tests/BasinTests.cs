namespace Tests
{
	using ObjectBasin;

	[TestClass]
	public class BasinTests
	{
		[TestMethod]
		public void StringTests()
		{
			var basin = new Basin<string>();
			const string key = "key";
			basin.SetCursor(new BasinCursor { JsonPath = $"$['{key}']" });
			Assert.AreEqual("2", basin.Write("2"));
			Assert.AreEqual("2", basin.Items[key]);
		}
	}
}