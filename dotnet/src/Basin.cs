namespace ObjectBasin
{
	using System;
	using System.Collections.Generic;
	using System.Text;
	using Microsoft.AspNetCore.JsonPatch;
	using Microsoft.AspNetCore.JsonPatch.Exceptions;
	using Microsoft.AspNetCore.JsonPatch.Operations;
	using Newtonsoft.Json.Linq;
	using Newtonsoft.Json.Serialization;

	/// <summary>
	/// A container for objects that you can write to using a JSONPath cursor.
	/// </summary>
	/// <typeparam name="ValueType">The type of values (top level) that will be modified.</typeparam>
	/// <remarks>
	/// See <see cref="ConvertJsonPathToJsonPointer"/> for assumptions about JSONPaths.
	/// </remarks>
	public class Basin<ValueType>
	{
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
			// TODO Return the top level object that was modified.
			new JsonPatchDocument(new List<Operation>() { operation }, new DefaultContractResolver())
				.ApplyTo(this.Items);
		}

		public void ApplyPatches(List<Operation> operations)
		{
			// TODO Return the top level objects that were modified without duplicates.
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
			this.currentPointer = ConvertJsonPathToJsonPointer(cursor.JsonPath);

			var endIndex = this.currentPointer.IndexOf('/', 1);
			endIndex = endIndex == -1 ? this.currentPointer.Length : endIndex;
			var currentKeyBuilder = new StringBuilder(this.currentPointer);

			// Remove the first "/".
			currentKeyBuilder
				.Remove(0, 1)
				.Remove(endIndex - 1, currentKeyBuilder.Length - endIndex + 1)
				.Replace("~0", "~")
				.Replace("~1", "/");
			this.currentKey = currentKeyBuilder.ToString();
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
				try
				{
					new JsonPatchDocument()
						.Replace(this.currentPointer, value)
						.ApplyTo(this.Items);
				}
				catch (JsonPatchException exc)
				{
					if (exc.Message.Contains("not found", StringComparison.Ordinal))
					{
						new JsonPatchDocument()
							.Add(this.currentPointer, value)
							.ApplyTo(this.Items);
					} else
					{
						throw;
					}
				}
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

							// Need to replace the string, otherwise we could end up inserting a new entry in a list.
							// There are tests for this.
							this.ApplyPatch(new Operation("replace", this.currentPointer, null, newValue));
							break;
						case JTokenType.Array:
							var pointer = ConvertJsonPathToJsonPointer(token.Path);
							if (pos == -1)
							{
								// Append
								pointer += "/-";
							}
							else
							{
								// Insert
								pointer += $"/{pos.Value}";
							}

							this.ApplyPatch(new Operation("add", pointer, null, value));
							break;
						default:
							throw new Exception($"Token of type  {token.Type} cannot be modified yet.");
					}
				}
				var j = new JsonPatchDocument<IDictionary<string, ValueType>>();
			}

			return this.Items[this.currentKey!];
		}

		/// <summary>
		/// Convert a JSONPath to a JSON Pointer.
		/// </summary>
		/// <param name="path">A JSONPath.</param>
		/// <returns>A JSON Pointer</returns>
		/// <remarks>
		/// Exposed for testing.
		/// ASSUMPTION: Only handles simple cases. E.g., key names cannot have &quot;[&quot; or &quot;]&quot;.
		/// </remarks>
		public static string ConvertJsonPathToJsonPointer(string path)
		{
			// TODO Optimize to only go through the path once.
			var resultBuilder = new StringBuilder(path);
			resultBuilder
				.Replace("~", "~0")
				.Replace("/", "~1")
				.Replace(".", "/");

			if (resultBuilder[0] == '$')
			{
				resultBuilder.Remove(0, 1);
			}

			if (resultBuilder[0] != '/')
			{
				resultBuilder.Insert(0, '/');
			}

			resultBuilder
				.Replace("/['", "/")
				.Replace("']/", "/")
				.Replace("]/", "/")
				.Replace("/[", "/")
				.Replace("[", "/")
				.Replace("']", "")
				.Replace("]", "");

			return resultBuilder.ToString();
		}
	}
}
