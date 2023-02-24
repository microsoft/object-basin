import { expect } from 'chai'
import { Basin } from '..'

describe('Basin', () => {
	it('string', () => {
		const basin = new Basin<any>()
		const key = 'key'
		basin.setCursor({ jsonPath: '$[\'key\']' })
		let o = basin.write('2')
		expect(basin.items[key]).to.equal('2')
		expect(o).to.equal('2')

		basin.setCursor({ jsonPath: 'key' })
		o = basin.write('4')
		expect(basin.items[key]).to.equal('4')

		basin.setCursor({ jsonPath: '$.key', position: -1 })
		basin.write('3')
		expect(basin.items[key]).to.equal('43')
		o = basin.write('21')
		expect(basin.items[key]).to.equal('4321')
		expect(o).to.equal('4321')

		basin.setCursor({ jsonPath: 'key', position: 0 })
		basin.write('76')
		expect(basin.items[key]).to.equal('764321')
		basin.write('5')
		expect(basin.items[key]).to.equal('7654321')

		delete basin.items[key]
		expect(basin.items[key]).undefined
	})

	it('object', () => {
		const basin = new Basin<any>()
		const key = 'key'
		basin.setCursor({ jsonPath: '$[\'key\']' })
		basin.write({ a: 1 })
		expect(basin.items[key]).to.deep.equal({ a: 1 })
		basin.setCursor({ jsonPath: 'key.b' })
		basin.write([{ t: 'h' }])
		expect(basin.items[key]).to.deep.equal({ a: 1, b: [{ t: 'h' }] })
		basin.setCursor({ jsonPath: '$[\'key\'].b[0].t', position: -1 })
		basin.write('el')
		let o = basin.write('lo')
		expect(basin.items[key]).to.deep.equal({ a: 1, b: [{ t: 'hello' }] })
		expect(o).to.deep.equal({ a: 1, b: [{ t: 'hello' }] })
	})

	it('example', () => {
		const basin = new Basin<any>()
		basin.setCursor({ jsonPath: '$.message' })
		expect(basin.write("ello")).to.equal("ello")
		basin.setCursor({ jsonPath: 'message', position: -1 })
		expect(basin.write(" World")).to.equal("ello World")
		expect(basin.items).to.deep.equal({ message: 'ello World' })

		basin.setCursor({ jsonPath: 'message', position: 0 })
		expect(basin.write("H")).to.equal("Hello World")
		expect(basin.items).to.deep.equal({ message: 'Hello World' })

		basin.setCursor({ jsonPath: 'message', position: -1 })
		expect(basin.write("!")).to.equal("Hello World!")
		expect(basin.items).to.deep.equal({ message: 'Hello World!' })

		basin.setCursor({ jsonPath: '$.object' })
		expect(basin.write({ list: ["item 1", "item 2"] })).to.deep.equal({ list: ["item 1", "item 2"] })
		expect(basin.items).to.deep.equal({ message: 'Hello World!', object: { list: ['item 1', 'item 2'] } })

		basin.setCursor({ jsonPath: 'object.list[1]', position: -1 })
		expect(basin.write(" is the best")).to.deep.equal({ list: ['item 1', 'item 2 is the best'] })
		expect(basin.items).to.deep.equal({
			message: 'Hello World!',
			object: { list: ['item 1', 'item 2 is the best'] },
		})
	})
})