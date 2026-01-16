import {
	assertSuccess,
	getResponseErrorMessage,
	getResponseType,
} from './utils';

describe('getResponseType', () => {
	it('reads Type and normalizes case', () => {
		expect(getResponseType({ Type: 'Error' })).toBe('error');
		expect(getResponseType({ type: 'SUCCESS' })).toBe('success');
	});
});

describe('getResponseErrorMessage', () => {
	it('includes kind and message with file path', () => {
		const message = getResponseErrorMessage({
			Type: 'error',
			Kind: 'BadRequest',
			Message: 'Invalid data',
			FilePath: 'data.xlsx',
		});
		expect(message).toBe('[data.xlsx] BadRequest: Invalid data');
	});

	it('returns message without repeating kind', () => {
		const message = getResponseErrorMessage({
			Type: 'error',
			Kind: 'BadRequest',
			Message: 'BadRequest: Invalid data',
		});
		expect(message).toBe('BadRequest: Invalid data');
	});
});

describe('assertSuccess', () => {
	it('returns response when not error', () => {
		const response = { Type: 'ok', Value: 1 };
		expect(assertSuccess<typeof response>(response)).toBe(response);
	});

	it('throws when response is error', () => {
		expect(() =>
			assertSuccess({
				Type: 'error',
				Message: 'Boom',
			}),
		).toThrow('Boom');
	});
});
