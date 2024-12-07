namespace ObjectBasin
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
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
		/// <summary>
		/// The JSON Patch pointer path.
		/// </summary>
		private string? currentPointer;

		/// <summary>
		/// Create a new basin.
		/// </summary>
		/// <param name="items">The items to contain. If not provided, an empty dictionary will be created.</param>
		/// <param name="cursor">The cursor to use. If not provided, then it must be provided later by calling <see cref="SetCursor"/></param>
		public Basin(
			IDictionary<string, ValueType>? items = null,
			BasinCursor? cursor = null)
		{
			this.Items = items ?? new Dictionary<string, ValueType>();
			if (cursor != null)
			{
				this.SetCursor(cursor);
			}
		}

		/// <summary>
		/// The items held in this basin.
		/// </summary>
		public IDictionary<string, ValueType> Items { get; }

		/// <summary>
		/// Apply a JSON Patch operation.
		/// </summary>
		/// <param name="operation">The operation to apply.</param>
		/// <returns>
		/// The current top level item that was modified.
		/// </returns>
		public ValueType ApplyPatch(Operation operation)
		{
			var resolver = new DefaultContractResolver();
			// TODO Add converters.
			var key = GetTopLevelKey(operation.path);
			new JsonPatchDocument(new List<Operation>() { operation }, resolver)
				.ApplyTo(this.Items);
			return this.Items[key];
		}

		/// <summary>
		/// Apply JSON Patch operations.
		/// </summary>
		/// <param name="operations">The operations to apply.</param>
		/// <returns>The top level items that were modified.</returns>
		public IEnumerable<ValueType> ApplyPatches(List<Operation> operations)
		{
			new JsonPatchDocument(operations, new DefaultContractResolver())
				.ApplyTo(this.Items);
			return operations
				.Select(o => GetTopLevelKey(o.path))
				.Distinct()
				.Select(k => this.Items[k])
				.ToArray();
		}

		/// <summary>
		/// Set the cursor for future calls to <see cref="Write"/>.
		/// </summary>
		/// <param name="cursor">The cursor to use.</param>
		public void SetCursor(BasinCursor cursor)
		{
			this.cursor = cursor;

#if DEBUG
			if (cursor?.JsonPath == null)
			{
				throw new ArgumentException($"The cursor or its {nameof(BasinCursor.JsonPath)} is null.");
			}
#endif
			this.currentPointer = ConvertJsonPathToJsonPointer(cursor.JsonPath!);
		}

		/// <summary>
		/// Write or set a value.
		/// </summary>
		/// <param name="value">The value to write or insert.
		/// Ignored when deleting items from lists.
		/// </param>
		/// <returns>The current top level item that was modified.</returns>
		public ValueType Write(object? value)
		{
			ValueType result = default;
#if DEBUG
			if (this.cursor?.JsonPath == null)
			{
				throw new ArgumentException($"The cursor or its {nameof(BasinCursor.JsonPath)} is null.");
			}
#endif
			var path = this.cursor!.JsonPath!;
			var pos = this.cursor.Position;
			if (pos == null)
			{
				// Set the value.
				try
				{
					// Need to try to replace first, otherwise a JSONPath like "object.list[1]" would insert a new entry in the list.
					result = this.ApplyPatch(new Operation("replace", this.currentPointer, null, value));
				}
				catch (JsonPatchException exc)
				{
					if (exc.Message.Contains("was not found.", StringComparison.Ordinal))
					{
						// The location did not exist.
						result = this.ApplyPatch(new Operation("add", this.currentPointer, null, value));
					}
					else
					{
						throw;
					}
				}
			}
			else
			{
				// Get the value at the path.
				Newtonsoft.Json.JsonSerializer jsonSerializer = Newtonsoft.Json.JsonSerializer.CreateDefault();
				// Add a converter to read `JsonElement`s.
				jsonSerializer.Converters.Add(new JsonElementConverter());
				var obj = JObject.FromObject(this.Items, jsonSerializer);
				foreach (var token in obj.SelectTokens(path))
				{
					result = token.Type switch
					{
						JTokenType.String => this.HandleStringUpdate(value, pos, token),
						JTokenType.Array => this.HandleArrayUpdate(value, pos, token),
						_ => throw new Exception($"Token of type  {token.Type} cannot be modified yet."),
					};
				}
			}

#pragma warning disable CS8603 // Possible null reference return.
			return result;
#pragma warning restore CS8603 // Possible null reference return.
		}

		/// <summary>
		/// Convert a JSONPath to a JSON Pointer.
		/// </summary>
		/// <param name="path">A JSONPath.</param>
		/// <returns>A JSON Pointer</returns>
		/// <remarks>
		/// ASSUMPTION: Only handles simple cases. E.g., key names within the path cannot have &quot;[&quot; or &quot;]&quot;.
		/// </remarks>
		internal static string ConvertJsonPathToJsonPointer(string path)
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

		internal static string GetTopLevelKey(string pointer)
		{
			// Assume it starts with a "/" and remove it.
			var endIndex = pointer.IndexOf('/', 1);
			endIndex = endIndex == -1 ? pointer.Length : endIndex;
			var result = new StringBuilder(pointer)
				.Remove(0, 1)
				.Remove(endIndex - 1, pointer.Length - endIndex)
				.Replace("~1", "/")
				.Replace("~0", "~")
				.ToString();
			return result;
		}

		private ValueType HandleArrayUpdate(object? value, int? pos, JToken token)
		{
			ValueType result;
			var deleteCount = this.cursor!.DeleteCount;
			var pointer = ConvertJsonPathToJsonPointer(token.Path);
			if (deleteCount != null)
			{
				pointer += $"/{pos!.Value}";
				var ops = new List<Operation>(deleteCount.Value);
				for (int i = 0; i < deleteCount.Value; ++i)
				{
					ops.Add(new Operation("remove", pointer, null));
				}
				var items = this.ApplyPatches(ops);

				// There should only be one item because all of the pointers are the same.
				result = items.First();
			}
			else if (pos == -1)
			{
				// Append
				pointer += "/-";
				result = this.ApplyPatch(new Operation("add", pointer, null, value));
			}
			else
			{
				// Insert
				pointer += $"/{pos!.Value}";
				result = this.ApplyPatch(new Operation("add", pointer, null, value));
			}

			return result;
		}

		private ValueType HandleStringUpdate(object? value, int? pos, JToken token)
		{
			var deleteCount = this.cursor!.DeleteCount;
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
				newValue = currentValue[0..pos!.Value] + value + currentValue[(pos.Value + (deleteCount ?? 0))..];
			}

			// Need to replace the string, otherwise we could end up inserting a new entry in a list.
			// There are tests for this.
			return this.ApplyPatch(new Operation("replace", this.currentPointer, null, newValue));
		}
	}
}
