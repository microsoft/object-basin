# Basin
JavaScript/TypeScript library to stream updates to an object using JSONPaths.
It is called Basin because streams flow into a basin.

See in [npmjs.com](https://www.npmjs.com/package/object-basin).

This library supports various ways to update objects or their contents:
* appending to the end of a string
* inserting anywhere in a string
* removing characters from a string
* appending to the end of a list
* inserting anywhere in a list
* overwriting an item in a list
* deleting items in a list
* setting a value in an object

Learn more at https://github.com/microsoft/object-basin.

# Install
```bash
npm install object-basin
```

# Examples
```TypeScript
import { Basin } from 'object-basin'

const basin = new Basin<any>()

// Stream a string.
// `$.` is needed when the key does not exist yet.
basin.setCursor({ jsonPath: '$.message' })
basin.write("ello") // "ello"

// Append to the end of the string.
basin.setCursor({ jsonPath: 'message', position: -1 })
basin.write(" World") // "ello World"
basin.write("!") // "ello World!"
basin.items // { message: "ello World!" }

// Insert at the beginning of the string.
// `j` and `p` can be used to be more concise.
basin.setCursor({ j: 'message', p: 0 })
basin.write("H") // "Hello World!"
basin.items // { message: "Hello World!" }

// Stream a string.
basin.setCursor({ jsonPath: 'message', p: -1 })
basin.write(" It's") // "Hello World! It's"
basin.write(" nice ") // "Hello World! It's nice "
basin.write("to stream") // "Hello World! It's nice to stream"
basin.write(" to you.") // "Hello World! It's nice to stream to you."

// Stream parts of an object.
basin.setCursor({ jsonPath: '$.object' })
basin.write({ list: ["item 1"] }) // { list: ["item 1"] }

// Append to the end of a list.
basin.setCursor({ jsonPath: '$.object.list', position: -1 })
basin.write("item 2") // { list: ["item 1", "item 2"] }
basin.items // { message: "Hello World! It's nice to stream to you.", object: { list: [ 'item 1', 'item 2' ] } }

// Append to the end of a string in a list.
basin.setCursor({ jsonPath: 'object.list[1]', position: -1 })
basin.write(" is the best") // { list: [ 'item 1', 'item 2 is the best' ] }
basin.items
// {
//   message: "Hello World! It's nice to stream to you.",
//   object: { list: [ 'item 1', 'item 2 is the best' ] }
// }

// Insert in a list.
basin.setCursor({ jsonPath: 'object.list', position: 1 })
basin.write("item 1.5") // { list: ['item 1', 'item 1.5', 'item 2 is the best'] }

// Overwrite an item in a list.
basin.setCursor({ jsonPath: 'object.list[1]'})
basin.write("item 1.33") // { list: ['item 1', 'item 1.33', 'item 2 is the best'] }

// Add a few more items to the list.
basin.setCursor({ jsonPath: 'object.list', p: -1 })
basin.write('item 3') // { list: ['item 1', 'item 1.33', 'item 2 is the best', 'item 3'] }
basin.write('item 4') // { list: ['item 1', 'item 1.33', 'item 2 is the best', 'item 3', 'item 4'] }
basin.write('item 5') // { list: ['item 1', 'item 1.33', 'item 2 is the best', 'item 3', 'item 4', 'item 5'] }

// Delete the first item in the list.
basin.setCursor({ jsonPath: 'object.list', p: 0, deleteCount: 1 })
// The value given to `write` is ignored when deleting items from lists.
basin.write(undefined) // { list: ['item 1.33', 'item 2 is the best', 'item 3', 'item 4', 'item 5'] }

// Delete 2 items in the list starting at index 1.
// `d` can be used to be more concise.
basin.setCursor({ jsonPath: 'object.list', p: 1, d: 2 })
basin.write() // { list: ['item 1.33', 'item 4', 'item 5'] }

// Insert into a string and then delete the next 3 characters.
basin.setCursor({ jsonPath: 'object.list[0]', p: 6, d: 3 })
basin.write('!') // { list: ['item 1!', 'item 4', 'item 5'] }
```

See more examples in the [tests](src/__tests__/index.test.ts).
