import { expect } from 'chai'
import { Basin } from '..'

describe('Basin', () => {
	it('patch', () => {
		const basin = new Basin<any>({
			key: 'value',
		})
		expect(basin.applyPatches([
			{ op: 'add', path: '/key', value: 'new value' },
		])).to.deep.equal(['new value'])

		expect(basin.applyPatches([
			{ op: 'add', path: '/key', value: 'newer value' },
			{ op: 'add', path: '/object', value: { list: [1] } },
			{ op: 'add', path: '/object/list/1', value: 2 },
		])).to.deep.equal(['newer value', { list: [1, 2] }])

		expect(basin.applyPatches([
			{ op: 'add', path: '/object/id', value: 'object id' },
		])).to.deep.equal([{ id: 'object id', list: [1, 2] }])

		expect(basin.applyPatches([
			{ op: 'replace', path: '/key', value: 'newest value' },
			{ op: 'replace', path: '/object/id', value: 'object id 2' },
		])).to.deep.equal(['newest value', { id: 'object id 2', list: [1, 2] }])

		expect(basin.items).to.deep
			.equal({
				key: 'newest value',
				object: { id: 'object id 2', list: [1, 2] },
			})

		expect(basin.applyPatches([
			{ op: 'add', path: '/weird~0~1~01key', value: 'val' },
		])).to.deep.equal(['val'])
		expect(basin.items['weird~/~1key']).to.equal('val')
	})

	it('string', () => {
		const basin = new Basin<any>()
		const key = 'key'
		basin.setCursor({ jsonPath: '$[\'key\']' })
		expect(basin.write('2')).to.equal('2')
		expect(basin.items[key]).to.equal('2')

		basin.setCursor({ jsonPath: 'key' })
		basin.write('4')
		expect(basin.items[key]).to.equal('4')

		basin.setCursor({ jsonPath: '$.key', position: -1 })
		basin.write('3')
		expect(basin.items[key]).to.equal('43')
		expect(basin.write('21')).to.equal('4321')
		expect(basin.items[key]).to.equal('4321')

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
		expect(basin.write('lo')).to.deep.equal({ a: 1, b: [{ t: 'hello' }] })
		expect(basin.items[key]).to.deep.equal({ a: 1, b: [{ t: 'hello' }] })
	})

	it('example', () => {
		const basin = new Basin<any>()
		basin.setCursor({ jsonPath: '$.message' })
		expect(basin.write("ello")).to.equal("ello")
		basin.setCursor({ jsonPath: 'message', position: -1 })
		expect(basin.write(" World")).to.equal("ello World")
		expect(basin.write("!")).to.equal("ello World!")
		expect(basin.items).to.deep.equal({ message: "ello World!" })

		basin.setCursor({ j: 'message', p: 0 })
		expect(basin.write("H")).to.equal("Hello World!")
		expect(basin.items).to.deep.equal({ message: "Hello World!" })

		basin.setCursor({ jsonPath: 'message', p: -1 })
		expect(basin.write(" It's")).to.equal("Hello World! It's")
		expect(basin.write(" nice ")).to.equal("Hello World! It's nice ")
		expect(basin.write("to stream")).to.equal("Hello World! It's nice to stream")
		expect(basin.write(" to you.")).to.equal("Hello World! It's nice to stream to you.")

		basin.setCursor({ jsonPath: '$.object' })
		expect(basin.write({ list: ["item 1"] })).to.deep.equal({ list: ["item 1"] })
		basin.setCursor({ jsonPath: '$.object.list', position: -1 })
		expect(basin.write("item 2")).to.deep.equal({ list: ["item 1", "item 2"] })
		expect(basin.items).to.deep.equal({ message: "Hello World! It's nice to stream to you.", object: { list: ['item 1', 'item 2'] } })

		basin.setCursor({ jsonPath: 'object.list[1]', position: -1 })
		expect(basin.write(" is the best")).to.deep.equal({ list: ['item 1', 'item 2 is the best'] })
		expect(basin.items).to.deep.equal({
			message: "Hello World! It's nice to stream to you.",
			object: { list: ['item 1', 'item 2 is the best'] },
		})

		basin.setCursor({ jsonPath: 'object.list', position: 1 })
		expect(basin.write("item 1.5")).to.deep.equal({ list: ['item 1', 'item 1.5', 'item 2 is the best'] })

		basin.setCursor({ jsonPath: 'object.list[1]' })
		expect(basin.write("item 1.33")).to.deep.equal({ list: ['item 1', 'item 1.33', 'item 2 is the best'] })

		basin.setCursor({ jsonPath: 'object.list', p: -1 })
		expect(basin.write('item 3')).to.deep.equal({ list: ['item 1', 'item 1.33', 'item 2 is the best', 'item 3'] })
		expect(basin.write('item 4')).to.deep.equal({ list: ['item 1', 'item 1.33', 'item 2 is the best', 'item 3', 'item 4'] })
		expect(basin.write('item 5')).to.deep.equal({ list: ['item 1', 'item 1.33', 'item 2 is the best', 'item 3', 'item 4', 'item 5'] })

		basin.setCursor({ jsonPath: 'object.list', p: 0, deleteCount: 1 })
		expect(basin.write(undefined)).to.deep.equal({ list: ['item 1.33', 'item 2 is the best', 'item 3', 'item 4', 'item 5'] })

		basin.setCursor({ jsonPath: 'object.list', p: 1, d: 2 })
		expect(basin.write()).to.deep.equal({ list: ['item 1.33', 'item 4', 'item 5'] })

		basin.setCursor({ jsonPath: 'object.list[0]', p: 6, d: 3 })
		expect(basin.write('!')).to.deep.equal({ list: ['item 1!', 'item 4', 'item 5'] })
	})

	it('list insert', () => {
		const basin = new Basin<any>({ list: [] })
		basin.setCursor({ jsonPath: 'list[0]' })
		expect(basin.write('a')).to.deep.equal(['a'])
		basin.setCursor({ jsonPath: 'list[1]' })
		expect(basin.write('b')).to.deep.equal(['a', 'b'])
		basin.setCursor({ jsonPath: 'list', position: -1 })
		expect(basin.write('c')).to.deep.equal(['a', 'b', 'c'])
		basin.setCursor({ jsonPath: '$.holder.list2' })
		expect(basin.write([1])).to.deep.equal({ list2: [1] })
		basin.setCursor({ jsonPath: 'holder.list2[1]' })
		expect(basin.write(2)).to.deep.equal({ list2: [1, 2] })
		basin.setCursor({ jsonPath: 'holder.list2', position: -1 })
		expect(basin.write(3)).to.deep.equal({ list2: [1, 2, 3] })
		basin.setCursor({ jsonPath: 'holder.list2[1]', position: -1 })
		expect(basin.write(4)).to.deep.equal({ list2: [1, 2 + 4, 3] })
	})

	it('multi-cursor', () => {
		const basin = new Basin<any>({ list: [
			"Hello ",
			"Hi ",
			"Hey ",
		] })
		basin.setCursor({ jsonPath: 'list[0]', p: -1 })
		basin.setCursor({ jsonPath: 'list[1]', p: -1 }, '1')
		basin.setCursor({ jsonPath: 'list[2]', p: -1 }, '2')
		expect(basin.write("there")).to.deep.equal(["Hello there", "Hi ", "Hey "])
		expect(basin.write("guy", '1')).to.deep.equal(["Hello there", "Hi guy", "Hey "])
		expect(basin.write("you", '2')).to.deep.equal(["Hello there", "Hi guy", "Hey you"])
		expect(basin.write(".")).to.deep.equal(["Hello there.", "Hi guy", "Hey you"] )
		expect(basin.write(".", '1')).to.deep.equal(["Hello there.", "Hi guy.", "Hey you"] )
		expect(basin.write("!", '2')).to.deep.equal(["Hello there.", "Hi guy.", "Hey you!"])
		expect(basin.items).to.deep.equal({ list: ["Hello there.", "Hi guy.", "Hey you!"] })
	})

	it('multi-cursor deeper', () => {
		const items = {
			key1: {
				list: [
					{
						text: "Hello ",
						wtv: 3,
					},
				],
			},
			key2: {
				list: [
					{
						text: "Hi ",
						wtv: 2,
					},
				],
			},
		}
		const basin = new Basin<any>(items)
		basin.setCursor({ jsonPath: '$[\'key1\'].list[0].text', p: -1 })
		basin.setCursor({ jsonPath: '$[\'key2\'].list[0].text', p: -1 }, 'other')
		expect(basin.write("there")).to.deep.equal({ list: [{ text: "Hello there", wtv: 3 }] })
		expect(basin.write("guy", 'other')).to.deep.equal({ list: [{ text: "Hi guy", wtv: 2 }] })
		expect(basin.items).to.deep.equal({
			key1: { list: [{ text: "Hello there", wtv: 3 }] },
			key2: { list: [{ text: "Hi guy", wtv: 2 }] },
		})
	})
})
