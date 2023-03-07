namespace ObjectBasin
{
	using System.Text.Json;

	/// <summary>
	/// A container for objects that you can write to using a JSONPath cursor.
	/// </summary>
	/// <typeparam name="T">The type of values (top level) that will be modified.</typeparam>
	public class Basin<T>
	{
		public Basin(JsonElement items)
		{

		}

		public void SetCursor(BasinCursor cursor)
		{
			// TODO
		}

		public T Write(JsonElement? value)
		{
			// TODO
			return default;
		}
	}
}
