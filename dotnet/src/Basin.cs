namespace ObjectBasin
{
	using System;
	using System.Collections.Generic;
	using Microsoft.AspNetCore.JsonPatch;
	using Newtonsoft.Json.Linq;

	/// <summary>
	/// A container for objects that you can write to using a JSONPath cursor.
	/// </summary>
	/// <typeparam name="ValueType">The type of values (top level) that will be modified.</typeparam>
	public class Basin<ValueType>
	{
		private BasinCursor? cursor;
		private string? currentKey;

		public Basin(IDictionary<string, ValueType>? items = null)
		{
			this.Items = items ?? new Dictionary<string, ValueType>();
		}

		public IDictionary<string, ValueType> Items { get; }

		public void SetCursor(BasinCursor cursor)
		{
			this.cursor = cursor;

#if DEBUG
			if (cursor?.Path == null)
			{
				throw new ArgumentException("The cursor or its JsonPath is null.");
			}
#endif

			int startIndex;
			if (cursor.Path.StartsWith("/", StringComparison.Ordinal))
			{
				startIndex = 1;
			}
			else
			{
				startIndex = 0;
			}

			var endIndex = cursor.Path.IndexOf("/", startIndex + 1);
			if (endIndex == -1)
			{
				endIndex = cursor.Path.Length;
			}

			this.currentKey = cursor.Path[startIndex..endIndex]
				.Replace("~1", "/")
				.Replace("~0", "~");
		}

		public ValueType Write(object value)
		{
#if DEBUG
			if (this.cursor?.Path == null)
			{
				throw new ArgumentException("The cursor or its JsonPath is null.");
			}
#endif
			var path = this.cursor.Path;
			var pos = this.cursor.Position;
			if (pos == null)
			{
				// Set the value.
				// TODO Maybe we should be more efficient and create the operation manually so that the list of operation can remain with size 1 instead of getting resized or starting larger.
				// Maybe we should set this up when setting up the cursor.
				new JsonPatchDocument()
					.Add(path, value)
					.ApplyTo(this.Items);
			}
			else
			{
				// TODO
				var j = new JsonPatchDocument<IDictionary<string, ValueType>>();
			}

			return this.Items[this.currentKey!];
		}
	}
}
