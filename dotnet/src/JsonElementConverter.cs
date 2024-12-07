namespace ObjectBasin
{
	using System;
	using Newtonsoft.Json;
	using Newtonsoft.Json.Linq;

	internal class JsonElementConverter : JsonConverter
	{
		public override bool CanConvert(Type objectType)
		{
			return objectType == typeof(System.Text.Json.JsonElement);
		}
		public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, Newtonsoft.Json.JsonSerializer serializer)
		{
			object obj;
			if (reader.TokenType == JsonToken.StartArray)
			{
				obj = JArray.Load(reader);
			}
			else
			{
				obj = JObject.Load(reader);
			}

			return System.Text.Json.JsonSerializer.Deserialize<object>(obj.ToString());
		}
		public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
		{
			if (value is null)
			{
				writer.WriteNull();
				return;
			}

			var element = (System.Text.Json.JsonElement)value;
			switch (element.ValueKind)
			{
				case System.Text.Json.JsonValueKind.Object:
					{
						JObject obj = JObject.Parse(element.GetRawText());
						obj.WriteTo(writer);
						break;
					}

				case System.Text.Json.JsonValueKind.Array:
					{
						JArray obj = JArray.Parse(element.GetRawText());
						obj.WriteTo(writer);
						break;
					}

				default:
					writer.WriteValue(element.ToString());
					break;
			}
		}
	}
}
