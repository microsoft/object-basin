import { expect } from 'chai'
import { Basin } from '..'

it('Basin', () => {
	const container = new Basin<string>()
	const key = 'key'
	container.setCursor({ jsonPath: '$[\'key\']' })
	let o = container.write('2')
	expect(container.items[key]).to.equal('2')
	expect(o).to.equal('2')

	container.setCursor({ jsonPath: 'key' })
	o = container.write('4')
	expect(container.items[key]).to.equal('4')

	container.setCursor({ jsonPath: '$.key', position: -1 })
	container.write('3')
	expect(container.items[key]).to.equal('43')
	o = container.write('21')
	expect(container.items[key]).to.equal('4321')
	expect(o).to.equal('4321')

	container.setCursor({ jsonPath: 'key', position: 0 })
	container.write('76')
	expect(container.items[key]).to.equal('764321')
	container.write('5')
	expect(container.items[key]).to.equal('7654321')

	delete container.items[key]
	expect(container.items[key]).undefined

	container.setCursor({ jsonPath: '$[\'key\']' })
	container.write({ a: 1 })
	expect(container.items[key]).to.deep.equal({ a: 1 })
	container.setCursor({ jsonPath: '$[\'key\'].b' })
	container.write([{ t: 'h' }])
	expect(container.items[key]).to.deep.equal({ a: 1, b: [{ t: 'h' }] })
	container.setCursor({ jsonPath: '$[\'key\'].b[0].t', position: -1 })
	container.write('el')
	o = container.write('lo')
	expect(container.items[key]).to.deep.equal({ a: 1, b: [{ t: 'hello' }] })
	expect(o).to.deep.equal({ a: 1, b: [{ t: 'hello' }] })
})