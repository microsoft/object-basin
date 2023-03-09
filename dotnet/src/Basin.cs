namespace ObjectBasin
{
	using System;
	using System.Collections.Generic;
	using Microsoft.AspNetCore.JsonPatch;
	using Microsoft.AspNetCore.JsonPatch.Operations;
	using Newtonsoft.Json.Linq;
	using Newtonsoft.Json.Serialization;

	/// <summary>
	/// A container for objects that you can write to using a JSONPath cursor.
	/// </summary>
	/// <typeparam name="ValueType">The type of values (top level) that will be modified.</typeparam>
	public class Basin<ValueType>
	{
		private static readonly IEnumerable<(string, string)> KeyMarkers = new List<(string, string)>
		{
			("$.['", "']" ),
			("$['", "']" ),
			("$.[", "]" ),
			("$[", "]" ),
			("$.", "<end>" ),
			(".", "<end>" ),
		};
		private BasinCursor? cursor;
		private string? currentKey;
		/// <summary>
		/// The JSON Patch pointer path.
		/// </summary>
		private string? currentPointer;

		public Basin(IDictionary<string, ValueType>? items = null)
		{
			this.Items = items ?? new Dictionary<string, ValueType>();
		}

		public IDictionary<string, ValueType> Items { get; }

		public void ApplyPatch(Operation operation)
		{
			new JsonPatchDocument(new List<Operation>() { operation }, new DefaultContractResolver())
				.ApplyTo(this.Items);
		}

		public void ApplyPatches(List<Operation> operations)
		{
			new JsonPatchDocument(operations, new DefaultContractResolver())
				.ApplyTo(this.Items);
		}

		public void SetCursor(BasinCursor cursor)
		{
			this.cursor = cursor;

#if DEBUG
			if (cursor?.JsonPath == null)
			{
				throw new ArgumentException($"The cursor or its {nameof(BasinCursor.JsonPath)} is null.");
			}
#endif
			this.currentKey = null;
			foreach (var pair in KeyMarkers)
			{
				if (cursor.JsonPath.StartsWith(pair.Item1, StringComparison.Ordinal))
				{
					var startIndex = pair.Item1.Length;
					var endIndex = cursor.JsonPath.IndexOf(pair.Item2, startIndex + 1);
					if (endIndex == -1)
					{
						endIndex = cursor.JsonPath.Length;
					}

					this.currentKey = cursor.JsonPath[startIndex..endIndex];
					break;
				}
			}

			if (this.currentKey == null)
			{
				this.currentKey = cursor.JsonPath;
			}

			this.currentPointer = ConvertJsonPathToJsonPatchPath(cursor.JsonPath);
		}

		public ValueType Write(object value)
		{
#if DEBUG
			if (this.cursor?.JsonPath == null)
			{
				throw new ArgumentException($"The cursor or its {nameof(BasinCursor.JsonPath)} is null.");
			}
#endif
			var path = this.cursor.JsonPath;
			var pos = this.cursor.Position;
			if (pos == null)
			{
				// Set the value.
				// TODO Maybe we should be more efficient and create the operation manually so that the list of operation can remain with size 1 instead of getting resized or starting larger.
				// Maybe we should set this up when setting up the cursor.
				new JsonPatchDocument()
					.Add(this.currentPointer, value)
					.ApplyTo(this.Items);
			}
			else
			{
				// Get the value at the path.
				var obj = JObject.FromObject(this.Items);
				foreach (var token in obj.SelectTokens(path))
				{
					switch (token.Type)
					{
						case JTokenType.String:
							string newValue;
							var currentValue = token.ToString();
							if (pos == -1)
							{
								// Append
								newValue = currentValue + value;
							}
							else
							{
								// Insert
								this.cursor.Position += (value as string)!.Length;
								var deleteCount = this.cursor.DeleteCount ?? 0;
								newValue = currentValue[0..pos.Value] + value + currentValue[(pos.Value + deleteCount)..];
							}

							this.ApplyPatch(new Operation("add", this.currentPointer, null, newValue));
							break;
						default:
							throw new Exception("Not handled.");
					}
				}
				var j = new JsonPatchDocument<IDictionary<string, ValueType>>();
			}

			return this.Items[this.currentKey!];
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		/// <remarks>
		/// Exposed for testing.
		/// </remarks>
		public static string ConvertJsonPathToJsonPatchPath(string path)
		{
			// Only handle some simple cases.
			// Requires dots.
			string result = path
				.Replace("~", "~0")
				.Replace("/", "~1")
				.Replace(".", "/");

			if (result.StartsWith("$", StringComparison.Ordinal))
			{
				result = result[1..];
			}

			if (!result.StartsWith("/", StringComparison.Ordinal))
			{
				result = "/" + result;
			}

			if (!result.EndsWith("/", StringComparison.Ordinal))
			{
				result += "/";
			}

			result = result
				.Replace("/['", "/")
				.Replace("']/", "/")
				.Replace("]/", "/")
				.Replace("/[", "/");

			return result;
		}
	}
}
