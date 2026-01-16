import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import UpdateChecker from '../UpdateChecker';

const checkForUpdates = vi.fn();
const downloadUpdate = vi.fn();
const installUpdate = vi.fn();
const onUpdateStatus = vi.fn();

vi.mock('@/shared/contexts/useApp', () => ({
	useApp: () => ({ t: (key: string) => key }),
}));

// Mock useJobs with no active jobs by default
const mockGroups: { id: string; status: string; sheets: Record<string, { status: string }> }[] = [];
vi.mock('@/shared/contexts/useJobs', () => ({
	useJobs: () => ({ groups: mockGroups }),
}));

describe('UpdateChecker', () => {
	beforeEach(() => {
		vi.restoreAllMocks();
		checkForUpdates.mockReset();
		downloadUpdate.mockReset();
		installUpdate.mockReset();
		onUpdateStatus.mockReset();
		mockGroups.length = 0; // Clear groups

		// Mock electronAPI
		window.electronAPI = {
			checkForUpdates,
			downloadUpdate,
			installUpdate,
			onUpdateStatus: (handler: (state: unknown) => void) => {
				onUpdateStatus.mockImplementation(handler);
				return () => {};
			},
		} as unknown as typeof window.electronAPI;
	});

	afterEach(() => {
		window.electronAPI = undefined as unknown as typeof window.electronAPI;
	});

	it('renders current version and check button', () => {
		render(<UpdateChecker />);

		expect(screen.getByText(/update.currentVersion/)).toBeInTheDocument();
		expect(screen.getByRole('button', { name: 'update.checkForUpdates' })).toBeInTheDocument();
	});

	it('shows checking status when checking for updates', async () => {
		checkForUpdates.mockImplementation(
			() =>
				new Promise((resolve) => {
					setTimeout(() => resolve({ status: 'not-available' }), 100);
				}),
		);

		const user = userEvent.setup();
		render(<UpdateChecker />);

		await user.click(screen.getByRole('button', { name: 'update.checkForUpdates' }));

		expect(screen.getByText('update.checking')).toBeInTheDocument();
	});

	it('shows not-available status when up to date', async () => {
		checkForUpdates.mockResolvedValue({ status: 'not-available' });

		const user = userEvent.setup();
		render(<UpdateChecker />);

		await user.click(screen.getByRole('button', { name: 'update.checkForUpdates' }));

		await waitFor(() => {
			expect(screen.getByText('update.notAvailable')).toBeInTheDocument();
		});
	});

	it('shows available status with download button when update available', async () => {
		checkForUpdates.mockResolvedValue({
			status: 'available',
			info: { version: '2.0.0' },
		});

		const user = userEvent.setup();
		render(<UpdateChecker />);

		await user.click(screen.getByRole('button', { name: 'update.checkForUpdates' }));

		await waitFor(() => {
			expect(screen.getByText('update.available')).toBeInTheDocument();
			expect(screen.getByText(/2.0.0/)).toBeInTheDocument();
			expect(screen.getByRole('button', { name: 'update.downloadAndInstall' })).toBeInTheDocument();
		});
	});

	it('calls downloadUpdate when download button clicked', async () => {
		checkForUpdates.mockResolvedValue({
			status: 'available',
			info: { version: '2.0.0' },
		});
		downloadUpdate.mockResolvedValue(true);

		const user = userEvent.setup();
		render(<UpdateChecker />);

		await user.click(screen.getByRole('button', { name: 'update.checkForUpdates' }));

		await waitFor(() => {
			expect(screen.getByRole('button', { name: 'update.downloadAndInstall' })).toBeInTheDocument();
		});

		await user.click(screen.getByRole('button', { name: 'update.downloadAndInstall' }));

		expect(downloadUpdate).toHaveBeenCalled();
	});

	it('shows error status when check fails', async () => {
		checkForUpdates.mockRejectedValue(new Error('Network error'));

		const user = userEvent.setup();
		render(<UpdateChecker />);

		await user.click(screen.getByRole('button', { name: 'update.checkForUpdates' }));

		await waitFor(() => {
			expect(screen.getByText('update.error')).toBeInTheDocument();
			expect(screen.getByText('Network error')).toBeInTheDocument();
		});
	});

	it('shows install button when update downloaded', async () => {
		checkForUpdates.mockResolvedValue({ status: 'downloaded' });

		const user = userEvent.setup();
		render(<UpdateChecker />);

		await user.click(screen.getByRole('button', { name: 'update.checkForUpdates' }));

		await waitFor(() => {
			expect(screen.getByText('update.downloaded')).toBeInTheDocument();
			expect(screen.getByRole('button', { name: 'update.installNow' })).toBeInTheDocument();
		});
	});

	it('calls installUpdate when install button clicked', async () => {
		checkForUpdates.mockResolvedValue({ status: 'downloaded' });

		const user = userEvent.setup();
		render(<UpdateChecker />);

		await user.click(screen.getByRole('button', { name: 'update.checkForUpdates' }));

		await waitFor(() => {
			expect(screen.getByRole('button', { name: 'update.installNow' })).toBeInTheDocument();
		});

		await user.click(screen.getByRole('button', { name: 'update.installNow' }));

		expect(installUpdate).toHaveBeenCalled();
	});

	it('shows warning and hides install button when active jobs exist', async () => {
		// Add an active (running) job
		mockGroups.push({
			id: 'group-1',
			status: 'Running',
			sheets: { 'sheet-1': { status: 'Running' } },
		});

		checkForUpdates.mockResolvedValue({ status: 'downloaded' });

		const user = userEvent.setup();
		render(<UpdateChecker />);

		await user.click(screen.getByRole('button', { name: 'update.checkForUpdates' }));

		await waitFor(() => {
			expect(screen.getByText('update.downloaded')).toBeInTheDocument();
			expect(screen.getByText('update.activeJobsWarning')).toBeInTheDocument();
			expect(screen.queryByRole('button', { name: 'update.installNow' })).not.toBeInTheDocument();
		});
	});

	it('shows warning when paused jobs exist', async () => {
		// Add a paused job
		mockGroups.push({
			id: 'group-1',
			status: 'Paused',
			sheets: { 'sheet-1': { status: 'Paused' } },
		});

		checkForUpdates.mockResolvedValue({ status: 'downloaded' });

		const user = userEvent.setup();
		render(<UpdateChecker />);

		await user.click(screen.getByRole('button', { name: 'update.checkForUpdates' }));

		await waitFor(() => {
			expect(screen.getByText('update.activeJobsWarning')).toBeInTheDocument();
		});
	});

	it('allows install when all jobs are completed', async () => {
		// Add a completed job
		mockGroups.push({
			id: 'group-1',
			status: 'Completed',
			sheets: { 'sheet-1': { status: 'Completed' } },
		});

		checkForUpdates.mockResolvedValue({ status: 'downloaded' });

		const user = userEvent.setup();
		render(<UpdateChecker />);

		await user.click(screen.getByRole('button', { name: 'update.checkForUpdates' }));

		await waitFor(() => {
			expect(screen.getByRole('button', { name: 'update.installNow' })).toBeInTheDocument();
			expect(screen.queryByText('update.activeJobsWarning')).not.toBeInTheDocument();
		});
	});
});
