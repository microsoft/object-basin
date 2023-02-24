import jp from 'jsonpath'

/**
 * Indicates where updates should be made in a {@link Basin}.
 */
export class BasinCursor {
	/**
	 * @param jsonPath The path to the object to be updated.
	 * @param position The position to insert the value.
	 * If `undefined`, the value will be set.
	 * If -1, the value will be appended.
	 * Otherwise, the value will be inserted at the specified position.
	 */
	constructor(
		public jsonPath: string,
		public position?: number) {
	}
}

/**
 * A container for objects that you can write to using a JSONPath cursor.
 * @typeparam T The type of values (top level) that will be modified.
 */
export class Basin<T> {
	private _currentKey?: T

	/**
	 * @param items The items to contain. If not provided, an empty object will be used.
	 * @param _cursor The cursor to use. If not provided, then it must be provided later by calling {@link setCursor}.
	 */
	public constructor(
		public items: any = {},
		private _cursor?: BasinCursor) {
		if (_cursor !== undefined) {
			this.setCursor(_cursor)
		}
	}

	/**
	 * @param cursor The cursor to use.
	 */
	public setCursor(cursor: BasinCursor): void {
		this._cursor = cursor
		const expressions = jp.parse(cursor.jsonPath)
		for (const expression of expressions) {
			if (expression.expression.type === 'root') {
				continue
			}
			this._currentKey = expression.expression.value
			break
		}
	}

	/**
	 * Write or set a value.
	 * @param value The value to write or insert. Assumed to be a string, but other values might work and will be allowed in the future.
	 * @returns The current top level item that was modified.
	 */
	public write(value: any): T {
		// For efficiency, assume the cursor is set.
		const position = this._cursor!.position
		if (typeof position !== 'number') {
			// Set the value.
			jp.value(this.items, this._cursor!.jsonPath, value)
		} else {
			if (typeof value !== 'string') {
				throw new Error('Cannot update a non-string value.')
			}

			jp.apply(this.items, this._cursor!.jsonPath, (currentValue: string) => {
				if (position === -1) {
					// Append.
					return currentValue + value
				} else {
					// Insert.
					this._cursor!.position! += value.length
					return currentValue.slice(0, position) + value + currentValue.slice(position)
				}
			})
		}

		return this.items[this._currentKey]
	}
}