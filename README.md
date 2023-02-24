# Basin
JavaScript/TypeScript library to stream updates to an object.
See in [npmjs.com](https://www.npmjs.com/package/object-basin).

# Install
```bash
npm install object-basin
```

# Examples
For now, this library mostly works well with updating strings, but we will support more complex updates in the future such as adding and removing items from arrays.

```TypeScript
import { Basin } from 'object-basin'

const basin = new Basin<any>()
basin.setCursor({ jsonPath: '$.message' })
basin.write("ello") // "ello"
basin.setCursor({ jsonPath: 'message', position: -1 })
basin.write(" World") // "ello World"
basin.items // { message: 'ello World' }

basin.setCursor({ jsonPath: 'message', position: 0 })
basin.write("H") // "Hello World"
basin.items // { message: 'Hello World' }

basin.setCursor({ jsonPath: 'message', position: -1 })
basin.write("!") // "Hello World!"
basin.items // { message: 'Hello World!' }

basin.setCursor({ jsonPath: '$.object' })
basin.write({ list: ["item 1", "item 2"] })
basin.items // { message: 'Hello World!', object: { list: [ 'item 1', 'item 2' ] } }

basin.setCursor({ jsonPath: 'object.list[1]', position: -1 })
basin.write(" is the best") // { list: [ 'item 1', 'item 2 is the best' ] }
basin.items
// {
//   message: 'Hello World!',
//   object: { list: [ 'item 1', 'item 2 is the best' ] }
// }
```

See more examples in the [tests](src/__tests__/index.test.ts).

## Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.opensource.microsoft.com.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

## Trademarks

This project may contain trademarks or logos for projects, products, or services. Authorized use of Microsoft 
trademarks or logos is subject to and must follow 
[Microsoft's Trademark & Brand Guidelines](https://www.microsoft.com/en-us/legal/intellectualproperty/trademarks/usage/general).
Use of Microsoft trademarks or logos in modified versions of this project must not cause confusion or imply Microsoft sponsorship.
Any use of third-party trademarks or logos are subject to those third-party's policies.
