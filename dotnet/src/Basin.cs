namespace ObjectBasin;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.JsonPatch.Exceptions;
using Microsoft.AspNetCore.JsonPatch.Operations;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using ObjectBasin.JsonSerialization;

/// <summary>
/// A container for objects that you can write to using a JSONPath cursor.
/// </summary>
/// <typeparam name="ValueType">The type of values (top level) that will be modified.</typeparam>
/// <remarks>
/// See <see cref="ConvertJsonPathToJsonPointer"/> for assumptions about JSONPaths.
/// </remarks>
public sealed class Basin<ValueType>
{
	private static readonly IContractResolver s_defaultContractResolver = new DefaultContractResolver();
	private static readonly Newtonsoft.Json.JsonSerializer s_defaultJsonSerializer = Newtonsoft.Json.JsonSerializer.CreateDefault();

	static Basin()
	{
		s_defaultJsonSerializer.Converters.Add(new JsonElementConverter());
	}

	private readonly Dictionary<string, BasinCursor> cursors = [];
	private BasinCursor? defaultCursor;

	/// <summary>
	/// The JSON Patch pointer paths.
	/// </summary>
	private readonly Dictionary<string, string> pointers = [];
	private string? defaultPointer;

	/// <summary>
	/// Helps reading objects when performing JSON Patch operations.
	/// </summary>
	public IContractResolver ContractResolver { private get; set; } = s_defaultContractResolver;

	/// <summary>
	/// The serializer to use to get a value from a JSON Path
	/// and to rebuild an object after certain types of modifications.
	/// </summary>
	public Newtonsoft.Json.JsonSerializer JsonSerializer { private get; set; } = s_defaultJsonSerializer;

	/// <summary>
	/// Create a new basin.
	/// </summary>
	/// <param name="items">The items to contain. If not provided, an empty dictionary will be created.</param>
	/// <param name="cursor">The default cursor to use.
	/// If not provided, then it must be provided later by calling <see cref="SetCursor"/></param>
	public Basin(
		IDictionary<string, ValueType?>? items = null,
		BasinCursor? cursor = null)
	{
		this.Items = items ?? new Dictionary<string, ValueType?>();
		if (cursor is not null)
		{
			this.SetCursor(cursor);
		}
	}

	/// <summary>
	/// The items held in this basin.
	/// </summary>
	public IDictionary<string, ValueType?> Items { get; }

	/// <summary>
	/// Apply a JSON Patch operation.
	/// </summary>
	/// <param name="operation">The operation to apply.</param>
	/// <returns>
	/// The current top level item that was modified.
	/// </returns>
	public ValueType? ApplyPatch(Operation operation)
	{
		new JsonPatchDocument([operation], this.ContractResolver)
			.ApplyTo(this.Items);
		var key = GetTopLevelKey(operation.path);
		return this.Items[key];
	}

	/// <summary>
	/// Apply JSON Patch operations.
	/// </summary>
	/// <param name="operations">The operations to apply.</param>
	/// <returns>The top level items that were modified.</returns>
	public IEnumerable<ValueType?> ApplyPatches(List<Operation> operations)
	{
		new JsonPatchDocument(operations, this.ContractResolver)
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
	/// <param name="label">
	/// The label to use for the cursor.
	/// This can be used to support having multiple cursors to point to different objects in the basin.
	/// </param>
	public void SetCursor(BasinCursor cursor, string? label = null)
	{
#if DEBUG
		if (cursor?.JsonPath == null)
		{
			throw new ArgumentException($"The cursor or its {nameof(BasinCursor.JsonPath)} is null.");
		}
#endif

		if (label is null)
		{
			this.defaultCursor = cursor;
			this.defaultPointer = ConvertJsonPathToJsonPointer(cursor.JsonPath!);
		}
		else
		{
			this.cursors[label] = cursor;
			this.pointers[label] = ConvertJsonPathToJsonPointer(cursor.JsonPath!);
		}
	}

	/// <summary>
	/// Write or set a value.
	/// </summary>
	/// <param name="value">The value to write or insert.
	/// Ignored when deleting items from lists.
	/// </param>
	/// <param name="cursorLabel">
	/// The label of the cursor to use.
	/// Defaults to the unlabeled cursor.
	/// </param>
	/// <returns>
	/// The current top level item that was modified.
	/// This reference should be used as a source of truth for the item.
	/// </returns>
	public ValueType? Write(object? value, string? cursorLabel = null)
	{
		ValueType? result = default;
		BasinCursor cursor;
		string pointer;
		if (cursorLabel is null)
		{
			cursor = this.defaultCursor!;
			pointer = this.defaultPointer!;
		}
		else
		{
			cursor = this.cursors[cursorLabel];
			pointer = this.pointers[cursorLabel];
		}
#if DEBUG
		if (cursor?.JsonPath is null)
		{
			throw new ArgumentException($"The cursor or its `{nameof(BasinCursor.JsonPath)}` is null.");
		}
#endif
		var path = cursor.JsonPath!;
		var pos = cursor.Position;
		if (pos is null)
		{
			result = this.SetValue(value, result, path, pointer);
		}
		else
		{
			// Find the value(s) at the path, then handle updating them.
			var obj = JObject.FromObject(this.Items, this.JsonSerializer);
			foreach (var token in obj.SelectTokens(path))
			{
				result = token.Type switch
				{
					JTokenType.String => this.HandleStringUpdate(value, pos.Value, obj, token, cursor, pointer),
					JTokenType.Array => this.HandleArrayUpdate(value, pos.Value, cursor, pointer),
					_ => throw new Exception($"Token of type  {token.Type} cannot be modified yet."),
				};
			}
		}

		return result;
	}

	private ValueType? SetValue(object? value, ValueType? result, string path, string pointer)
	{
		try
		{
			// Need to try to replace first, otherwise a JSONPath like "object.list[1]" would insert a new entry in the list.
			result = this.ApplyPatch(new Operation("replace", pointer, null, value));
		}
		catch (JsonPatchException exc)
		{
			if (exc.Message.EndsWith("' was not found.", StringComparison.Ordinal))
			{
				// The location did not exist or there was another issue finding it, perhaps it was a complex object such as a `JsonElement`.
				try
				{
					// Try to add the value.
					result = this.ApplyPatch(new Operation("add", pointer, null, value));
				}
				catch (JsonPatchException)
				{
					// Fallback to modifying the token directly.
					// This is mainly to handle modifications inside of `JsonElement`s.
					var obj = JObject.FromObject(this.Items, this.JsonSerializer);
					var replacementValue = value is not null ?
						JToken.FromObject(value, this.JsonSerializer)
						: JValue.CreateNull();
					foreach (var token in obj.SelectTokens(path))
					{
						result = this.HandleJTokenReplace(obj, token, replacementValue, pointer);
					}
				}
			}
			else
			{
				throw;
			}
		}

		return result;
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

	private ValueType? HandleArrayUpdate(object? value, int pos, BasinCursor cursor, string pointer)
	{
		ValueType? result;
		var deleteCount = cursor.DeleteCount;
		if (deleteCount != null)
		{
			pointer += $"/{pos}";
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
			pointer += $"/{pos}";
			result = this.ApplyPatch(new Operation("add", pointer, null, value));
		}

		return result;
	}

	private ValueType? HandleStringUpdate(object? value, int pos, JObject obj, JToken token, BasinCursor cursor, string pointer)
	{
		var deleteCount = cursor.DeleteCount;
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
			cursor.Position += (value as string)!.Length;
			newValue = currentValue[0..pos] + value + currentValue[(pos + (deleteCount ?? 0))..];
		}

		try
		{
			// Need to replace the string, otherwise we could end up inserting a new entry in a list.
			// There are tests for this.
			return this.ApplyPatch(new Operation("replace", pointer, null, newValue));
		}
		catch (JsonPatchException)
		{
			return this.HandleJTokenReplace(obj, token, newValue, pointer);
		}
	}

	private ValueType? HandleJTokenReplace(JObject obj, JToken token, JToken newValue, string pointer)
	{
		// Fallback to modifying the token directly.
		// This is mainly to handle `JsonElement`s.
		token.Replace(newValue);
		// It's wasteful to reparse the entire object here and force users of the library to make existing references not match the basin any more,
		// but this seems to be the most robust way to handle changing a string in certain kinds of objects such as `JsonElement`s.
		var key = GetTopLevelKey(pointer);
		var result = obj[key]!.ToObject<ValueType?>(this.JsonSerializer);
		return this.Items[key] = result;
	}
}
