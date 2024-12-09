import jsonpatch, { Operation } from 'fast-json-patch'
import jp from 'jsonpath'

// Export to help dependencies because this is used in our interface.
export { Operation } from 'fast-json-patch'

/**
 * Indicates where updates should be made in a {@link Basin}.
 */
export class BasinCursor {
	/**
	 * A more concise way to specify the {@link jsonPath}.
	 */
	public j?: string
	/**
	 * A more concise way to specify the {@link position}.
	 */
	public p?: number
	/**
	 * A more concise way to specify the {@link deleteCount}.
	 */
	public d?: number

	/**
	 * @param jsonPath The path to the value to be updated.
	 * @param position The position to insert the value.
	 * If `undefined`, the value will be set.
	 * If -1, the value will be appended.
	 * Otherwise, the value will be inserted at the specified position.
	 * @param deleteCount If the {@link jsonPath} points a list or string and this is not `undefined`, then this number of items will be removed from the list or string starting from {@link position}.
	 * {@link position} must not be `undefined` to delete items.
	 */
	constructor(
		public jsonPath?: string,
		public position?: number,
		public deleteCount?: number,
	) {
	}
}

/**
 * A container for objects that you can write to using a JSONPath cursor.
 * @typeparam T The type of values (top level) that will be modified.
 */
export class Basin<T> {
	private _currentKey?: T

	/**
	 * @param items The items to contain. If not provided, an empty object will be created.
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
	 * Apply JSON Patch operations.
	 * @param operations The operations to apply.
	 * @returns The top level items that were modified.
	 */
	applyPatches(operations: Operation[]): T[] {
		jsonpatch.applyPatch(this.items, operations)
		const topLevelKeys = new Set<string>()
		const result: T[] = []
		for (const operation of operations) {
			let endIndex = operation.path.indexOf('/', 1);
			endIndex = endIndex == -1 ? operation.path.length : endIndex;
			const key = operation.path.slice(1, endIndex)
				.replace(/~1/g, '/')
				.replace(/~0/g, '~')
			if (topLevelKeys.has(key)) {
				continue
			}
			result.push(this.items[key])
			topLevelKeys.add(key)
		}

		return result
	}

	/**
	 * @param cursor The cursor to use.
	 */
	public setCursor(cursor: BasinCursor, label?: string): void {
		this._cursor = cursor
		if (cursor.j !== undefined) {
			cursor.jsonPath = cursor.j
			delete cursor.j
		}
		if (cursor.p !== undefined) {
			cursor.position = cursor.p
			delete cursor.p
		}
		if (cursor.d !== undefined) {
			cursor.deleteCount = cursor.d
			delete cursor.d
		}

		const expressions = jp.parse(cursor.jsonPath!)
		for (const expression of expressions) {
			if (expression.expression.type !== 'root') {
				this._currentKey = expression.expression.value
				break
			}
		}
	}

	/**
	 * Write or set a value.
	 * @param value The value to write or insert.
	 * Ignored when deleting items from lists.
	 * @returns The current top level item that was modified.
	 */
	public write(value?: any, cursorLabel?: string): T {
		// For efficiency, assume the cursor is set.
		const cursor = this._cursor!
		const position = cursor.position
		const jsonPath = cursor.jsonPath!
		if (typeof position !== 'number') {
			// Set the value.
			jp.value(this.items, jsonPath, value)
		} else {
			jp.apply(this.items, jsonPath, (currentValue: string) => {
				if (Array.isArray(currentValue)) {
					if (cursor.deleteCount !== undefined) {
						// Delete
						currentValue.splice(position, cursor.deleteCount)
					} else if (position === -1) {
						// Append
						currentValue.push(value)
					} else {
						// Insert
						currentValue.splice(position, 0, value)
					}
					return currentValue
				} else {
					// Assume the value is a number, string, or something that works `+` and `slice`.
					if (position === -1) {
						// Append
						return currentValue + value
					} else {
						// Insert
						cursor.position! += value.length
						const deleteCount = cursor.deleteCount || 0
						return currentValue.slice(0, position) + value + currentValue.slice(position + deleteCount)
					}
				}
			})
		}

		return this.items[this._currentKey]
	}
}