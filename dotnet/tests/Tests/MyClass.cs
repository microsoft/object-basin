namespace ObjectBasin.Tests;

using System.Text.Json;
using Newtonsoft.Json;

internal sealed class MyBodyElement
{
	[JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
	public string? Text { get; set; }
}

internal sealed class MyElement
{
	[JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
	public List<MyBodyElement>? Body { get; set; }
}

internal sealed class MyClass
{
	// The Newtonsoft.Json property is required to support a JSONPath with "elements" instead of "Elements".
	[JsonProperty("elements", DefaultValueHandling = DefaultValueHandling.Ignore)]
	public List<JsonElement>? Elements { get; set; }

	[JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
	public List<MyElement>? MyElements { get; set; }

	[JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
	public string? Text { get; set; }

	// The Newtonsoft.Json property is required to support a JSONPath with "textWithAttribute" instead of "TextWithAttribute".
	[JsonProperty("textWithAttribute", DefaultValueHandling = DefaultValueHandling.Ignore)]
	public string? TextWithAttribute { get; set; }
}
