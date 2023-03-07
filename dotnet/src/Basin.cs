namespace ObjectBasin
{
	using System.Text.Json;
	using Newtonsoft.Json.Linq;

	/// <summary>
	/// A container for objects that you can write to using a JSONPath cursor.
	/// </summary>
	/// <typeparam name="T">The type of values (top level) that will be modified.</typeparam>
	public class Basin<T>
	{
		private BasinCursor? cursor;

		public Basin(object items)
		{
			Items = items;
		}

		public object Items { get; }

		public void SetCursor(BasinCursor cursor)
		{
			this.cursor = cursor;
			// TODO
		}

		public T Write(object value)
		{
			// TODO JObject.FromObject(this.Items).SelectTokens
			return default;
		}
	}
}
