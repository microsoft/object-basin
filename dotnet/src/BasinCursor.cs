namespace ObjectBasin;

using System.Runtime.Serialization;
using System.Text.Json.Serialization;

/// <summary>
/// Indicates where updates should be made in a <see cref="Basin{T}"/>.
/// </summary>
/// <remarks>
/// A cursor for <a href="https://www.npmjs.com/package/object-basin">object-basin</a>.
/// </remarks>
public class BasinCursor
{
	/// <summary>
	/// The JSONPath to the value to be updated.
	/// </summary>
	/// <remarks>
	/// Note that that concise name &quot;j&quot; is used for serialization.
	/// </remarks>
	[DataMember(Name = "j", EmitDefaultValue = false)]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	[JsonPropertyName("j")]
	public string? JsonPath { get; set; }

	/// <summary>
	/// The position at <see cref="JsonPath"/> to update.
	/// <tt>null</tt> means set the value.
	/// <tt>-1</tt> means append to the end.
	/// Otherwise use an index to insert at that position.
	/// </summary>
	/// <remarks>
	/// Note that that concise name &quot;p&quot; is used for serialization.
	/// </remarks>
	[DataMember(Name = "p", EmitDefaultValue = false)]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	[JsonPropertyName("p")]
	public int? Position { get; set; }

	/// <summary>
	/// The number of items to delete starting from <see cref="Position"/>.
	/// </summary>
	/// <remarks>
	/// Note that that concise name &quot;d&quot; is used for serialization.
	/// </remarks>
	[DataMember(Name = "d", EmitDefaultValue = false)]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	[JsonPropertyName("d")]
	public int? DeleteCount { get; set; }
}
