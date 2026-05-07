import { formatUserDateTime, formatUserTime } from './time';

describe('time formatters', () => {
	it('returns empty string for invalid values', () => {
		expect(formatUserDateTime()).toBe('');
		expect(formatUserTime('')).toBe('');
	});

	it('formats valid dates', () => {
		const date = new Date(2025, 0, 15, 13, 45, 30);
		const dateTime = formatUserDateTime(date, 'en-US');
		const time = formatUserTime(date, 'en-US');

		expect(dateTime).toContain('2025');
		expect(time.length).toBeGreaterThan(0);
	});
});
