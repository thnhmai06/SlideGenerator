import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import ProcessMenu from '../ProcessMenu'

const groupControl = vi.fn()
const jobControl = vi.fn()
const globalControl = vi.fn()
const exportGroupConfig = vi.fn()

vi.mock('../../contexts/AppContext', () => ({
  useApp: () => ({ t: (key: string) => key }),
}))

vi.mock('../../contexts/JobContext', () => ({
  useJobs: () => ({
    groups: [
      {
        id: 'group-1',
        workbookPath: 'C:\\book.xlsx',
        status: 'Running',
        progress: 35,
        errorCount: 0,
        sheets: {
          'sheet-1': {
            id: 'sheet-1',
            sheetName: 'Sheet1',
            status: 'Running',
            currentRow: 1,
            totalRows: 3,
            progress: 33,
            errorCount: 0,
            logs: [
              {
                message: 'Processing row 1',
                level: 'Info',
                row: 1,
                rowStatus: 'processing',
                timestamp: new Date('2025-01-01T10:00:00Z').toISOString(),
              },
              {
                message: 'Row 1 completed (text: 1, images: 1, image errors: 0)',
                level: 'Info',
                row: 1,
                rowStatus: 'completed',
                timestamp: new Date('2025-01-01T10:00:02Z').toISOString(),
              },
            ],
          },
        },
        logs: [],
      },
    ],
    groupControl,
    jobControl,
    globalControl,
    loadSheetLogs: vi.fn(),
    exportGroupConfig,
    hasGroupConfig: () => true,
  }),
}))

vi.mock('../../services/signalrClient', () => ({
  getBackendBaseUrl: () => 'http://localhost:5000',
}))

describe('ProcessMenu', () => {
  it('groups logs by row and shows per-sheet stop action', async () => {
    const user = userEvent.setup()
    render(<ProcessMenu />)

    await user.click(screen.getByText('book.xlsx'))
    await user.click(screen.getByText('Sheet1'))

    expect(screen.getByText('Row 1')).toBeInTheDocument()
    expect(document.querySelector('.file-action-btn-danger')).not.toBeNull()
  })
})
