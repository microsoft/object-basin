namespace ObjectBasin.Tests;

using System.Collections;
using System.Collections.Generic;
using System.Text.Json;
using AdaptiveCards;
using FluentAssertions;
using Newtonsoft.Json;
using ObjectBasin;

[TestClass]
public sealed class BasinTests
{
	[TestMethod]
	public void ApplyPatchTest()
	{
		var basin = new Basin<string>();
		basin.ApplyPatch(new()
		{
			op = "add",
			path = "/weird~0~1~01key",
			value = "value"
		});
		Assert.AreEqual(basin.Items["weird~/~1key"], "value");
	}

	[TestMethod]
	public void ExampleTests()
	{
		// Based on the example in the README.md.
		var basin = new Basin<object>(
			cursor: new BasinCursor { JsonPath = "message" });
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

		expected = new Dictionary<string, object>
		{
			["list"] = new List<object> { "item 1", "item 2 is the best" },
		};
		basin.SetCursor(new BasinCursor { JsonPath = "object.list[1]", Position = -1 });
		AssertAreDeepEqual(expected, basin.Write(" is the best"));

		((List<object>)expected["list"]).Insert(1, "item 1.5");
		basin.SetCursor(new BasinCursor { JsonPath = "object.list", Position = 1 });
		AssertAreDeepEqual(expected, basin.Write("item 1.5"));

		((List<object>)expected["list"])[1] = "item 1.33";
		basin.SetCursor(new BasinCursor { JsonPath = "object.list[1]" });
		AssertAreDeepEqual(expected, basin.Write("item 1.33"));

		((List<object>)expected["list"]).Add("item 3");
		basin.SetCursor(new BasinCursor { JsonPath = "object.list", Position = -1 });
		AssertAreDeepEqual(expected, basin.Write("item 3"));
		((List<object>)expected["list"]).Add("item 4");
		basin.SetCursor(new BasinCursor { JsonPath = "object.list", Position = -1 });
		AssertAreDeepEqual(expected, basin.Write("item 4"));
		((List<object>)expected["list"]).Add("item 5");
		basin.SetCursor(new BasinCursor { JsonPath = "object.list", Position = -1 });
		AssertAreDeepEqual(expected, basin.Write("item 5"));

		((List<object>)expected["list"]).RemoveAt(0);
		basin.SetCursor(new BasinCursor { JsonPath = "object.list", Position = 0, DeleteCount = 1 });
		AssertAreDeepEqual(expected, basin.Write(null));

		((List<object>)expected["list"]).RemoveAt(1);
		((List<object>)expected["list"]).RemoveAt(1);
		basin.SetCursor(new BasinCursor { JsonPath = "object.list", Position = 1, DeleteCount = 2 });
		AssertAreDeepEqual(expected, basin.Write(null));

		((List<object>)expected["list"])[0] = "item 1!";
		basin.SetCursor(new BasinCursor { JsonPath = "object.list[0]", Position = 6, DeleteCount = 3 });
		AssertAreDeepEqual(expected, basin.Write("!"));

		expected = new Dictionary<string, object>
		{
			["message"] = "Hello World! It's nice to stream to you.",
			["object"] = new Dictionary<string, object>
			{
				["list"] = new List<object> { "item 1!", "item 4", "item 5" },
			},
		};
		AssertAreDeepEqual(expected, basin.Items);
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
	public void MultiCursor_Tests()
	{
		var basin = new Basin<MyClass>();
		var instance = new MyClass
		{
			Text = "Hello ",
			TextWithAttribute = "Hi ",
			MyElements = [
				new MyElement {
					Body = [
						new MyBodyElement
						{
							Text = "Hey ",
						},
					],
				},
			],
		};
		basin.Items["key1"] = instance;

		var defaultCursor = new BasinCursor
		{
			JsonPath = "$.['key1'].Text",
			Position = -1,
		};
		basin.SetCursor(defaultCursor);
		var cursorForTextWithAttribute = new BasinCursor
		{
			JsonPath = "$.['key1'].textWithAttribute",
			Position = -1,
		};
		basin.SetCursor(cursorForTextWithAttribute, "1");
		var cursorForTextInMyElements = new BasinCursor
		{
			JsonPath = "$.['key1'].MyElements[0].Body[0].Text",
			Position = -1,
		};
		basin.SetCursor(cursorForTextInMyElements, "2");

		var defaultWriteResult = basin.Write("there");
		Assert.AreSame(instance, defaultWriteResult);
		Assert.AreEqual("Hello there", defaultWriteResult!.Text);

		var textWithAttributeWriteResult = basin.Write("guy", "1");
		Assert.AreSame(instance, textWithAttributeWriteResult);
		Assert.AreEqual("Hi guy", textWithAttributeWriteResult!.TextWithAttribute);

		var textInMyElementsWriteResult = basin.Write("you", "2");
		Assert.AreSame(instance, textInMyElementsWriteResult);
		Assert.AreEqual("Hey you", textInMyElementsWriteResult!.MyElements![0].Body![0].Text);

		defaultWriteResult = basin.Write(".", null);
		Assert.AreSame(instance, defaultWriteResult);

		textWithAttributeWriteResult = basin.Write(". Bye.", "1");
		Assert.AreSame(instance, textWithAttributeWriteResult);

		var expected = new MyClass
		{
			Text = "Hello there.",
			TextWithAttribute = "Hi guy. Bye.",
			MyElements = [
				new MyElement
				{
					Body = [
						new MyBodyElement
						{
							Text = "Hey you",
						},
					],
				},
			],
		};
		textInMyElementsWriteResult.Should().BeEquivalentTo(expected);
	}

	[TestMethod]
	public void ObjectTests()
	{
		var basin = new Basin<object>();
		const string key = "key";
		basin.SetCursor(new BasinCursor { JsonPath = $"$.['{key}']" });
		var expected = new Dictionary<string, object> { ["a"] = 1, };
		CollectionAssert.AreEquivalent(expected, (ICollection?)basin.Write(new Dictionary<string, object> { ["a"] = 1, }));
		CollectionAssert.AreEquivalent(expected, (ICollection?)basin.Items[key]);

		basin.SetCursor(new BasinCursor { JsonPath = $"{key}.b" });
		expected["b"] = new List<object> { new Dictionary<string, object> { ["t"] = "h" } };
		AssertAreDeepEqual(expected, basin.Write(new List<object> { new Dictionary<string, object> { ["t"] = "h" } }));

		basin.SetCursor(new BasinCursor { JsonPath = $"{key}.b.[0].t", Position = -1 });
		var o = basin.Write("el");
		Assert.AreSame(o, basin.Write("lo"));
		expected["b"] = new List<object> { new Dictionary<string, object> { ["t"] = "hello" } };
		AssertAreDeepEqual(expected, basin.Items[key]);
	}

	[TestMethod]
	public void MyClass_Text_Tests()
	{
		var basin = new Basin<MyClass>();
		const string key = "key";
		var myClass = new MyClass()
		{
			Text = "Hello ",
		};
		basin.Items[key] = myClass;
		basin.SetCursor(new BasinCursor
		{
			// Note that it does not work with "text" with a lowercase "t".
			JsonPath = $"$.['{key}'].Text",
			Position = -1,
		});

		var expected = new Dictionary<string, MyClass>
		{
			[key] = new MyClass()
			{
				Text = "Hello ",
			},
		};
		AssertAreDeepEqual(expected, basin.Items);
		var writeResult = basin.Write("World!");
		Assert.IsNotNull(writeResult, "Item not found at the cursor.");
		Assert.AreSame(basin.Items[key], writeResult);
		Assert.AreSame(myClass, writeResult);
		Assert.AreEqual("Hello World!", writeResult.Text);
		expected[key].Text = "Hello World!";
		AssertAreDeepEqual(expected, basin.Items);
	}

	[TestMethod]
	public void MyClass_TextWithAttribute_Tests()
	{
		var basin = new Basin<MyClass>();
		const string key = "key";
		var myClass = new MyClass()
		{
			TextWithAttribute = "Hello ",
		};
		basin.Items[key] = myClass;
		basin.SetCursor(new BasinCursor
		{
			// A lower case first letter works because we have an attribute on the property.
			JsonPath = $"$.['{key}'].textWithAttribute",
			Position = -1,
		});

		var expected = new Dictionary<string, MyClass>
		{
			[key] = new MyClass()
			{
				TextWithAttribute = "Hello ",
			},
		};
		AssertAreDeepEqual(expected, basin.Items);
		var writeResult = basin.Write("World!");
		Assert.IsNotNull(writeResult, "Item not found at the cursor.");
		Assert.AreSame(basin.Items[key], writeResult);
		Assert.AreSame(myClass, writeResult);
		Assert.AreEqual("Hello World!", writeResult.TextWithAttribute);
		expected[key].TextWithAttribute = "Hello World!";
		AssertAreDeepEqual(expected, basin.Items);
	}

	[TestMethod]
	public void MyClass_Elements_Tests()
	{
		var basin = new Basin<MyClass>();
		const string key = "key";
		var ac = new AdaptiveCard("1.5")
		{
			Body = [
				new AdaptiveTextBlock("Hello ")],
		};
		var acJson = JsonConvert.SerializeObject(ac);
		Assert.AreEqual("{\"type\":\"AdaptiveCard\",\"version\":\"1.5\",\"body\":[{\"type\":\"TextBlock\",\"text\":\"Hello \"}]}", acJson);
		var acJsonElement = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(acJson);
		Assert.AreEqual("Hello ", acJsonElement.GetProperty("body")[0].GetProperty("text").GetString());
		basin.Items[key] = new MyClass()
		{
			Elements = [
				acJsonElement,
			],
		};
		basin.SetCursor(new BasinCursor
		{
			JsonPath = $"$.['{key}'].elements[0].body[0].text",
			Position = -1,
		});

		var writeResult = basin.Write("World!");
		Assert.IsNotNull(writeResult, "Item not found at the cursor.");
		Assert.AreSame(basin.Items[key], writeResult);
		Assert.IsNotNull(writeResult.Elements, "Elements is null.");
		Assert.AreEqual("Hello World!", writeResult.Elements![0].GetProperty("body")[0].GetProperty("text").GetString());
		const string expectedAcJson = "{\"type\":\"AdaptiveCard\",\"version\":\"1.5\",\"body\":[{\"type\":\"TextBlock\",\"text\":\"Hello World!\"}]}";
		var expectedAcJsonElement = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(expectedAcJson);
		var expected = new Dictionary<string, MyClass>
		{
			[key] = new MyClass()
			{
				Elements = [
					expectedAcJsonElement,
				],
			},
		};
		AssertAreDeepEqual(expected, basin.Items);

		// Ensure that we can write at the cursor again.
		writeResult = basin.Write(" How are you?");
		Assert.IsNotNull(writeResult, "Item not found at the cursor.");
		Assert.AreSame(basin.Items[key], writeResult);
		Assert.AreEqual("Hello World! How are you?", writeResult.Elements![0].GetProperty("body")[0].GetProperty("text").GetString());
	}

	[TestMethod]
	public void MyClass_Elements_Replace_String_In_JsonElement_Test()
	{
		var basin = new Basin<MyClass>();
		const string key = "key";
		var ac = new AdaptiveCard("1.5")
		{
			Body = [
				new AdaptiveTextBlock("Hello ")],
		};
		var acJson = JsonConvert.SerializeObject(ac);
		Assert.AreEqual("{\"type\":\"AdaptiveCard\",\"version\":\"1.5\",\"body\":[{\"type\":\"TextBlock\",\"text\":\"Hello \"}]}", acJson);
		var acJsonElement = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(acJson);
		Assert.AreEqual("Hello ", acJsonElement.GetProperty("body")[0].GetProperty("text").GetString());
		basin.Items[key] = new MyClass()
		{
			Elements = [
				acJsonElement,
			],
		};

		// Overwrite with no position.
		basin.SetCursor(new BasinCursor()
		{
			JsonPath = $"$.['{key}'].elements[0].body[0].text",
		});
		var writeResult = basin.Write("Replaced!");
		Assert.IsNotNull(writeResult, "Item not found at the cursor.");
		Assert.AreSame(basin.Items[key], writeResult);
		Assert.AreEqual("Replaced!", writeResult.Elements![0].GetProperty("body")[0].GetProperty("text").GetString());

	}

	[TestMethod]
	public void MyClass_Elements_Array_Replacement_Test()
	{
		var basin = new Basin<MyClass>();
		const string key = "key";
		var ac = new AdaptiveCard("1.5")
		{
			Body = [
				new AdaptiveTextBlock("Hello ")],
		};
		var acJson = JsonConvert.SerializeObject(ac);
		Assert.AreEqual("{\"type\":\"AdaptiveCard\",\"version\":\"1.5\",\"body\":[{\"type\":\"TextBlock\",\"text\":\"Hello \"}]}", acJson);
		var acJsonElement = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(acJson);
		Assert.AreEqual("Hello ", acJsonElement.GetProperty("body")[0].GetProperty("text").GetString());
		basin.Items[key] = new MyClass()
		{
			Elements = [
				acJsonElement,
			],
		};

		var newTextBlock = new AdaptiveTextBlock("Replacement");
		var newTextBlockJson = JsonConvert.SerializeObject(newTextBlock);
		Assert.AreEqual("{\"type\":\"TextBlock\",\"text\":\"Replacement\"}", newTextBlockJson);
		var newTextBlockJsonElement = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(newTextBlockJson);

		basin.SetCursor(new BasinCursor
		{
			JsonPath = $"$.['{key}'].elements[0].body[0]",
		});
		var writeResult = basin.Write(newTextBlockJsonElement);
		Assert.IsNotNull(writeResult, "Item not found at the cursor.");
		Assert.AreSame(basin.Items[key], writeResult);
		Assert.IsNotNull(writeResult.Elements, "Elements is null.");
		Assert.AreEqual("Replacement", writeResult.Elements![0].GetProperty("body")[0].GetProperty("text").GetString());
		string expectedAcJson = "{\"type\":\"AdaptiveCard\",\"version\":\"1.5\",\"body\":[{\"type\":\"TextBlock\",\"text\":\"Hello World!\"}]}";
		var expectedAcJsonElement = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(expectedAcJson);
		var expected = new Dictionary<string, MyClass>
		{
			[key] = new MyClass()
			{
				Elements = [
					expectedAcJsonElement,
				],
			},
		};
		AssertAreDeepEqual(expected, basin.Items);
	}

	[Ignore("Applying patches is not supported within `JsonElement`s yet.")]
	[TestMethod]
	public void MyClass_Elements_Array_Patches_Tests()
	{
		var basin = new Basin<MyClass>();
		const string key = "key";
		var ac = new AdaptiveCard("1.5")
		{
			Body = [
				new AdaptiveTextBlock("Hello ")],
		};
		var acJson = JsonConvert.SerializeObject(ac);
		Assert.AreEqual("{\"type\":\"AdaptiveCard\",\"version\":\"1.5\",\"body\":[{\"type\":\"TextBlock\",\"text\":\"Hello \"}]}", acJson);
		var acJsonElement = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(acJson);
		Assert.AreEqual("Hello ", acJsonElement.GetProperty("body")[0].GetProperty("text").GetString());
		basin.Items[key] = new MyClass()
		{
			Elements = [
				acJsonElement,
			],
		};

		var newTextBlock = new AdaptiveTextBlock("World!");
		var newTextBlockJson = JsonConvert.SerializeObject(newTextBlock);
		Assert.AreEqual("{\"type\":\"TextBlock\",\"text\":\"World!\"}", newTextBlockJson);
		var newTextBlockJsonElement = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(newTextBlockJson);

		var patchResult = basin.ApplyPatch(new()
		{
			op = "add",
			path = $"/{key}/elements/0/body/1",
			value = newTextBlockJsonElement,
		});
		Assert.IsNotNull(patchResult, "Item not found at the cursor.");
		Assert.AreSame(basin.Items[key], patchResult);
		Assert.IsNotNull(patchResult.Elements, "Elements is null.");
		Assert.AreEqual("World!", patchResult.Elements![0].GetProperty("body")[1].GetProperty("text").GetString());

		newTextBlock = new AdaptiveTextBlock("Hello to the ");
		newTextBlockJson = JsonConvert.SerializeObject(newTextBlock);
		newTextBlockJsonElement = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(newTextBlockJson);
		basin.SetCursor(new BasinCursor
		{
			JsonPath = $"$.['{key}'].elements[0].body[0]",
		});
		var writeResult = basin.Write(newTextBlockJsonElement);
		Assert.IsNotNull(writeResult, "Item not found at the cursor.");
		Assert.AreSame(basin.Items[key], writeResult);
		Assert.IsNotNull(writeResult.Elements, "Elements is null.");
		Assert.AreEqual("World!", writeResult.Elements![0].GetProperty("body")[0].GetProperty("text").GetString());
		string expectedAcJson = "{\"type\":\"AdaptiveCard\",\"version\":\"1.5\",\"body\":[{\"type\":\"TextBlock\",\"text\":\"Hello to the \"},{\"type\":\"TextBlock\",\"text\":\"World!\"}]}";
		var expectedAcJsonElement = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(expectedAcJson);
		var expected = new Dictionary<string, MyClass>
		{
			[key] = new MyClass()
			{
				Elements = [
					expectedAcJsonElement,
				],
			},
		};
		AssertAreDeepEqual(expected, basin.Items);

		newTextBlock = new AdaptiveTextBlock("World! What's up?");
		newTextBlockJson = JsonConvert.SerializeObject(newTextBlock);
		newTextBlockJsonElement = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(newTextBlockJson);
		basin.SetCursor(new BasinCursor
		{
			JsonPath = $"$.['{key}'].elements[0].body",
			Position = 1,
		});
		writeResult = basin.Write(newTextBlockJsonElement);
		Assert.IsNotNull(writeResult, "Item not found at the cursor.");
		Assert.AreSame(basin.Items[key], writeResult);
		Assert.IsNotNull(writeResult.Elements, "Elements is null.");
		Assert.AreEqual("World! What's up?", writeResult.Elements![0].GetProperty("body")[1].GetProperty("text").GetString());
		expectedAcJson = "{\"type\":\"AdaptiveCard\",\"version\":\"1.5\",\"body\":[{\"type\":\"TextBlock\",\"text\":\"Hello to the \"},{\"type\":\"TextBlock\",\"text\":\"World! What's up?\"}]}";
		expectedAcJsonElement = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(expectedAcJson);
		expected = new Dictionary<string, MyClass>
		{
			[key] = new MyClass()
			{
				Elements = [
					expectedAcJsonElement,
				],
			},
		};
		AssertAreDeepEqual(expected, basin.Items);
	}

	[TestMethod]
	public void MyClass_MyElements_Tests()
	{
		var basin = new Basin<MyClass>();
		const string key = "key";
		var myClass = new MyClass()
		{
			MyElements = [
				new MyElement
				{
					Body = [
						new MyBodyElement
						{
							Text = "Hello ",
						},
					],
				},
			],
		};
		basin.Items[key] = myClass;
		basin.SetCursor(new BasinCursor
		{
			JsonPath = $"$.['{key}'].MyElements[0].Body[0].Text",
			Position = -1,
		});

		var writeResult = basin.Write("World!");
		Assert.IsNotNull(writeResult, "Item not found at the cursor.");
		Assert.AreSame(basin.Items[key], writeResult);
		Assert.AreSame(basin.Items[key], myClass);
		Assert.IsNotNull(writeResult.MyElements, "MyElements is null.");
		Assert.AreEqual("Hello World!", writeResult.MyElements![0].Body![0].Text);

		var expected = new Dictionary<string, MyClass>
		{
			[key] = new MyClass()
			{
				MyElements = [
					new MyElement
					{
						Body = [
							new MyBodyElement
							{
								Text = "Hello World!",
							},
						],
					},
				],
			},
		};
		AssertAreDeepEqual(expected, basin.Items);
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
	[DataRow("/2321839/adaptiveCards/0/body/0/text", $"$['2321839'].adaptiveCards[0].body[0].text")]
	[DataRow("/2321839/adaptiveCards/0/body/0/text", $"$.['2321839'].adaptiveCards[0].body[0].text")]
	[TestMethod]
	public void PathConverion_Tests(string expected, string input)
	{
		Assert.AreEqual(expected, Basin<object>.ConvertJsonPathToJsonPointer(input));
	}

	[DataRow("key", "/key")]
	[DataRow("2321839", "/2321839/adaptiveCards/0/body/0/text")]
	[DataRow("f113fc271999463d851bd4f3052b25f3", "/f113fc271999463d851bd4f3052b25f3/adaptiveCards/0/body/0/text")]
	[TestMethod]
	public void GetTopLevelKey_Tests(string expected, string input)
	{
		Assert.AreEqual(expected, Basin<object>.GetTopLevelKey(input));
	}

	[TestMethod]
	public void String_Tests()
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
		var o = basin.Write(new List<string> { "item 1" });
		AssertAreDeepEqual(new List<string> { "item 1" }, o);
		basin.SetCursor(new BasinCursor { JsonPath = "list[0]" });
		Assert.AreSame(o, basin.Write("1"));
		AssertAreDeepEqual(new List<string> { "1" }, o);
	}

	private static void AssertAreDeepEqual(object? expected, object? actual)
	{
		Assert.AreEqual(JsonConvert.SerializeObject(expected), JsonConvert.SerializeObject(actual));
	}
}
