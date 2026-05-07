import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import ResultMenu from '../ResultMenu';

const clearCompleted = vi.fn();
const removeSheet = vi.fn();
const removeGroup = vi.fn();
const exportGroupConfig = vi.fn();

vi.mock('@/shared/contexts/useApp', () => ({
	useApp: () => ({ t: (key: string) => key }),
}));

vi.mock('@/shared/contexts/useJobs', () => ({
	useJobs: () => ({
		groups: [
			{
				id: 'group-2',
				workbookPath: 'C:\\book.xlsx',
				outputFolder: 'C:\\out',
				status: 'Completed',
				progress: 100,
				errorCount: 0,
				sheets: {
					'sheet-2': {
						id: 'sheet-2',
						sheetName: 'Sheet2',
						status: 'Completed',
						currentRow: 2,
						totalRows: 2,
						progress: 100,
						errorCount: 0,
						outputPath: 'C:\\out\\Sheet2.pptx',
						logs: [
							{
								message: 'Row 1 completed (text: 1, images: 1, image errors: 0)',
								level: 'Info',
								row: 1,
								rowStatus: 'completed',
								timestamp: new Date('2025-01-01T10:00:00Z').toISOString(),
							},
						],
					},
				},
				logs: [],
			},
		],
		clearCompleted,
		removeGroup,
		removeSheet,
		loadSheetLogs: vi.fn(),
		exportGroupConfig,
		hasGroupConfig: () => true,
	}),
}));

describe('ResultMenu', () => {
	beforeEach(() => {
		vi.restoreAllMocks();
	});

	it('shows row groups and sheet actions', async () => {
		const user = userEvent.setup();
		render(<ResultMenu />);

		await user.click(screen.getByText('book.xlsx'));
		await user.click(screen.getByText('Sheet2'));

		expect(screen.getByText('Row 1')).toBeInTheDocument();
		expect(screen.getAllByLabelText('results.open').length).toBeGreaterThan(0);
		expect(screen.getByLabelText('results.remove')).toBeInTheDocument();
	});

	it('confirms before clearing all and removing groups', async () => {
		const user = userEvent.setup();
		const confirmSpy = vi.spyOn(window, 'confirm').mockReturnValue(true);
		render(<ResultMenu />);

		await user.click(screen.getByText('results.clearAll'));
		await user.click(screen.getByText('results.removeGroup'));

		expect(confirmSpy).toHaveBeenCalled();
		expect(clearCompleted).toHaveBeenCalled();
		expect(removeGroup).toHaveBeenCalled();
	});
});
