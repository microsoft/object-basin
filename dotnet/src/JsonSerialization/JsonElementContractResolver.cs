namespace ObjectBasin.JsonSerialization;

using System;
using Newtonsoft.Json;

internal sealed class JsonElementContractResolver : Newtonsoft.Json.Serialization.DefaultContractResolver
{
	private static readonly JsonConverter s_jsonElementConverter = new JsonElementConverter();

	protected override JsonConverter? ResolveContractConverter(Type objectType)
	{
		if (objectType != typeof(System.Text.Json.JsonElement))
		{
			return base.ResolveContractConverter(objectType);
		}

		return s_jsonElementConverter;
	}
}
