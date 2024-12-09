namespace ObjectBasin.Tests;

using System.Text.Json;
using Newtonsoft.Json;

// TODO Seal classes.
public class MyBodyElement
{
	public string? Text { get; set; }
}

public class MyElement
{
	public List<MyBodyElement>? Body { get; set; }
}

public class MyClass
{
	// The Newtonsoft.Json property is required to support a JSONPath with "textWithAttribute" instead of "TextWithAttribute".
	[JsonProperty("elements")]
	public List<JsonElement>? Elements { get; set; }

	public List<MyElement>? MyElements { get; set; }

	public string? Text { get; set; }

	// The Newtonsoft.Json property is required to support a JSONPath with "textWithAttribute" instead of "TextWithAttribute".
	[JsonProperty("textWithAttribute")]
	public string? TextWithAttribute { get; set; }
}
