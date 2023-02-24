import { expect } from 'chai'
import { Basin } from '..'

it('Basin', () => {
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

	basin.setCursor({ jsonPath: '$[\'key\']' })
	basin.write({ a: 1 })
	expect(basin.items[key]).to.deep.equal({ a: 1 })
	basin.setCursor({ jsonPath: '$[\'key\'].b' })
	basin.write([{ t: 'h' }])
	expect(basin.items[key]).to.deep.equal({ a: 1, b: [{ t: 'h' }] })
	basin.setCursor({ jsonPath: '$[\'key\'].b[0].t', position: -1 })
	basin.write('el')
	o = basin.write('lo')
	expect(basin.items[key]).to.deep.equal({ a: 1, b: [{ t: 'hello' }] })
	expect(o).to.deep.equal({ a: 1, b: [{ t: 'hello' }] })
})