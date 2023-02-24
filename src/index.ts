import jp from 'jsonpath'

/**
 * Indicates where updates should be made in a {@link Basin}.
 */
export class BasinCursor {
	constructor(
		public jsonPath: string,
		public position?: number) {
	}
}

/**
 * A container for objects that you can write to using a JSONPath cursor.
 */
export class Basin<T> {
	private _currentKey?: T

	public constructor(
		public items: any = {},
		private cursor?: BasinCursor) {
		if (cursor !== undefined) {
			this.setCursor(cursor)
		}
	}

	public setCursor(cursor: BasinCursor): void {
		this.cursor = cursor
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
	public write(value: unknown): T {
		// For efficiency, assume the cursor is set.
		const position = this.cursor!.position
		if (typeof position !== 'number') {
			// Set the value.
			jp.value(this.items, this.cursor!.jsonPath, value)
		} else {
			if (typeof value !== 'string') {
				throw new Error('Cannot insert a non-string value.')
			}

			jp.apply(this.items, this.cursor!.jsonPath, (currentValue: string) => {
				if (position === -1) {
					// Append.
					return currentValue + value
				} else {
					// Insert.
					this.cursor!.position! += value.length
					return currentValue.slice(0, position) + value + currentValue.slice(position)
				}
			})
		}

		return this.items[this._currentKey]
	}
}
