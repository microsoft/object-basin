﻿namespace ObjectBasin.JsonSerialization;

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

/// <summary>
/// Converts <see cref="System.Text.Json.JsonElement"/> to and from Newtonsoft.Json types.
/// </summary>
/// <remarks>
/// We could make this `<tt>public</tt> eventually, but for now it's pretty simple and only handles typical cases that we expect.
/// </remarks>
internal sealed class JsonElementConverter : JsonConverter
{
	public override bool CanConvert(Type objectType)
	{
		return objectType == typeof(System.Text.Json.JsonElement);
	}

	public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
	{
		object obj = reader.TokenType switch
		{
			JsonToken.StartObject => JObject.Load(reader),
			JsonToken.StartArray => JArray.Load(reader),
			_ => JToken.Load(reader),
		};
		return System.Text.Json.JsonSerializer.Deserialize(obj.ToString(), objectType);
	}

	public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
	{
		if (value is null)
		{
			writer.WriteNull();
			return;
		}

		// We know it is a `JsonElement` because that is the only type that `CanConvert` allows.
		var element = (System.Text.Json.JsonElement)value;
		switch (element.ValueKind)
		{
			case System.Text.Json.JsonValueKind.Object:
				var obj = JObject.Parse(element.GetRawText());
				obj.WriteTo(writer);
				break;


			case System.Text.Json.JsonValueKind.Array:
				var array = JArray.Parse(element.GetRawText());
				array.WriteTo(writer);
				break;

			default:
				writer.WriteValue(element.ToString());
				break;
		}
	}
}
