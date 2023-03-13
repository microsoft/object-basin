namespace ObjectBasin.Tests
{
	using System.Collections;
	using System.Collections.Generic;
	using Newtonsoft.Json;
	using ObjectBasin;

	[TestClass]
	public class BasinTests
	{
		[TestMethod]
		public void ExampleTests()
		{
			// Based on the example in the README.md.
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

			basin.SetCursor(new BasinCursor { JsonPath = "message", Position = -1 });
			Assert.AreEqual("Hello World! It's", basin.Write(" It's"));
			Assert.AreEqual("Hello World! It's nice ", basin.Write(" nice "));
			Assert.AreEqual("Hello World! It's nice to stream", basin.Write("to stream"));
			Assert.AreEqual("Hello World! It's nice to stream to you.", basin.Write(" to you."));

			basin.SetCursor(new BasinCursor { JsonPath = "$.object" });
			var expected = new Dictionary<string, object>
			{
				["list"] = new List<object> { "item 1" },
			};
			AssertAreDeepEqual(expected, basin.Write(new Dictionary<string, object>
			{
				["list"] = new List<object> { "item 1" },
			}));
			basin.SetCursor(new BasinCursor { JsonPath = "$.object.list", Position = -1 });
			expected["list"] = new List<object> { "item 1", "item 2" };
			AssertAreDeepEqual(expected, basin.Write("item 2"));
			expected = new Dictionary<string, object>
			{
				["message"] = "Hello World! It's nice to stream to you.",
				["object"] = expected,
			};
			AssertAreDeepEqual(expected, basin.Items);

			expected = (Dictionary<string, object>)expected["object"];
			expected["list"] = new List<object> { "item 1", "item 2 is the best" };
			basin.SetCursor(new BasinCursor { JsonPath = "object.list[1]", Position = -1 });
			AssertAreDeepEqual(expected, basin.Write(" is the best"));


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
		public void ObjectTests()
		{
			var basin = new Basin<object>();
			const string key = "key";
			basin.SetCursor(new BasinCursor { JsonPath = $"$.['{key}']" });
			var expected = new Dictionary<string, object> { ["a"] = 1, };
			CollectionAssert.AreEquivalent(expected, (ICollection)basin.Write(new Dictionary<string, object> { ["a"] = 1, }));
			CollectionAssert.AreEquivalent(expected, (ICollection)basin.Items[key]);

			basin.SetCursor(new BasinCursor { JsonPath = $"{key}.b" });
			expected["b"] = new List<object> { new Dictionary<string, object> { ["t"] = "h" } };
			AssertAreDeepEqual(expected, basin.Write(new List<object> { new Dictionary<string, object> { ["t"] = "h" } }));

			basin.SetCursor(new BasinCursor { JsonPath = $"{key}.b.[0].t", Position = -1 });
			basin.Write("el");
			basin.Write("lo");
			expected["b"] = new List<object> { new Dictionary<string, object> { ["t"] = "hello" } };
			AssertAreDeepEqual(expected, basin.Items[key]);
		}

		[DataRow("/key", "key")]
		[DataRow("/key", "$.['key']")]
		[DataRow("/key", "$.[key]")]
		[DataRow("/key", "$.key")]
		[DataRow("/key", "$['key']")]
		[DataRow("/key", "$[key]")]
		[DataRow("/key", "$key")]
		[DataRow("/key/b/0/t", "key.b.[0].t")]
		[DataRow("/key/b/0/t", "key.b[0].t")]
		[DataRow("/key/b/0/t/1/s", "key.b[0].t[1].s")]
		[TestMethod]
		public void PathConverionTests(string expected, string input)
		{
			Assert.AreEqual(expected, Basin<object>.ConvertJsonPathToJsonPointer(input));
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

			basin.SetCursor(new BasinCursor { JsonPath = "$.[o/k]" });
			Assert.AreEqual("4", basin.Write("4"));
			Assert.AreEqual("4", basin.Items["o/k"]);
		}

		[TestMethod]
		public void StringInListTests()
		{
			var basin = new Basin<object>();
			basin.SetCursor(new BasinCursor { JsonPath = "$.list" });
			basin.Write(new List<string> { "item 1" });
			basin.SetCursor(new BasinCursor { JsonPath = "list[0]" });
			basin.Write("1");
		}

		private static void AssertAreDeepEqual(object expected, object actual)
		{
			Assert.AreEqual(JsonConvert.SerializeObject(expected), JsonConvert.SerializeObject(actual));
		}
	}
}
