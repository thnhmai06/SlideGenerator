import {
	assertSuccess,
	getResponseErrorMessage,
	getResponseType,
} from './utils';

describe('getResponseType', () => {
	it('reads type and normalizes case', () => {
		expect(getResponseType({ type: 'Error' })).toBe('error');
		expect(getResponseType({ type: 'SUCCESS' })).toBe('success');
	});
});

describe('getResponseErrorMessage', () => {
	it('includes kind and message with file path', () => {
		const message = getResponseErrorMessage({
			type: 'error',
			kind: 'BadRequest',
			message: 'Invalid data',
			filePath: 'data.xlsx',
		});
		expect(message).toBe('[data.xlsx] BadRequest: Invalid data');
	});

	it('returns message without repeating kind', () => {
		const message = getResponseErrorMessage({
			type: 'error',
			kind: 'BadRequest',
			message: 'BadRequest: Invalid data',
		});
		expect(message).toBe('BadRequest: Invalid data');
	});
});

describe('assertSuccess', () => {
	it('returns response when not error', () => {
		const response = { type: 'ok', value: 1 };
		expect(assertSuccess<typeof response>(response)).toBe(response);
	});

	it('throws when response is error', () => {
		expect(() =>
			assertSuccess({
				type: 'error',
				message: 'Boom',
			}),
		).toThrow('Boom');
	});
});
