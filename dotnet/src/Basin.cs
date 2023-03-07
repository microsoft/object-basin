namespace ObjectBasin
{
	using System;
	using System.Collections.Generic;
	using Newtonsoft.Json.Linq;

	/// <summary>
	/// A container for objects that you can write to using a JSONPath cursor.
	/// </summary>
	/// <typeparam name="ValueType">The type of values (top level) that will be modified.</typeparam>
	public class Basin<ValueType>
		where ValueType : class
	{
		private BasinCursor? cursor;
		private string currentKey;

		public Basin(IDictionary<string, ValueType>? items = null)
		{
			this.Items = items ?? new Dictionary<string, ValueType>();
		}

		public IDictionary<string, ValueType> Items { get; }

		public void SetCursor(BasinCursor cursor)
		{
			this.cursor = cursor;

#if DEBUG
			if (cursor?.JsonPath == null)
			{
				throw new ArgumentException("The cursor or its JsonPath is null.");
			}
#endif

			// TODO Handle more cases, clean-up, and make a more efficient.
			if (cursor.JsonPath.StartsWith("$['", StringComparison.Ordinal))
			{
				var startIndex = 3;
				var endIndex = cursor.JsonPath.IndexOf("']", startIndex + 1);
				this.currentKey = cursor.JsonPath[startIndex..endIndex];
			}
		}

		public ValueType Write(object value)
		{
#if DEBUG
			if (this.cursor?.JsonPath == null)
			{
				throw new ArgumentException("The cursor or its JsonPath is null.");
			}
#endif
			var path = this.cursor.JsonPath;
			var pos = this.cursor.Position;
			if (pos == null)
			{
				// Set the value.
				var obj = JToken.FromObject(this.Items);
				var found = false;
				foreach (var token in obj.SelectTokens(path))
				{
					found = true;
					token.Replace(JToken.FromObject(value));
				}
				// JToken.Parse("dd")
				if (!found)
				{
					obj.Sel
				}
				
			}

			// TODO JObject.FromObject(this.Items).SelectTokens
			return this.Items[this.currentKey];
		}
	}
}
