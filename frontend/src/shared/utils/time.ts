type FormatterBundle = {
	dateTime: Intl.DateTimeFormat;
	time: Intl.DateTimeFormat;
};

const formatters = new Map<string, FormatterBundle>();

const getFormatters = (locale?: string | null): FormatterBundle => {
	const key = locale && locale.trim().length > 0 ? locale : 'default';
	const cached = formatters.get(key);
	if (cached) return cached;

	const localeArg = key === 'default' ? undefined : (locale ?? undefined);
	const created = {
		dateTime: new Intl.DateTimeFormat(localeArg, {
			day: '2-digit',
			month: '2-digit',
			year: 'numeric',
			hour: '2-digit',
			minute: '2-digit',
			second: '2-digit',
		}),
		time: new Intl.DateTimeFormat(localeArg, {
			timeStyle: 'medium',
		}),
	};
	formatters.set(key, created);
	return created;
};

const toDate = (value?: string | number | Date) => {
	if (!value) return null;
	const date = value instanceof Date ? value : new Date(value);
	if (Number.isNaN(date.getTime())) return null;
	return date;
};

export const formatUserDateTime = (value?: string | number | Date, locale?: string | null) => {
	const date = toDate(value);
	if (!date) return '';
	return getFormatters(locale).dateTime.format(date);
};

export const formatUserTime = (value?: string | number | Date, locale?: string | null) => {
	const date = toDate(value);
	if (!date) return '';
	return getFormatters(locale).time.format(date);
};
