import { RpcChannelClient } from '../rpcClient'
import {
  DEFAULT_CONFIG_RPC_CHANNEL,
  DEFAULT_JOB_RPC_CHANNEL,
  DEFAULT_SHEET_RPC_CHANNEL,
} from '../rpc/constants'

/**
 * RPC client for sheet/workbook operations.
 */
export const sheetClient = new RpcChannelClient(DEFAULT_SHEET_RPC_CHANNEL)

/**
 * RPC client for job management operations.
 */
export const jobClient = new RpcChannelClient(DEFAULT_JOB_RPC_CHANNEL)

/**
 * RPC client for configuration operations.
 */
export const configClient = new RpcChannelClient(DEFAULT_CONFIG_RPC_CHANNEL)
