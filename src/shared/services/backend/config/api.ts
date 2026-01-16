import { configHub } from '../clients'
import type { ResponseBase } from '../common/types'
import { assertSuccess } from '../common/utils'
import type {
  ConfigGetSuccess,
  ConfigReloadSuccess,
  ConfigResetSuccess,
  ConfigUpdateSuccess,
  ModelStatusSuccess,
  ModelControlSuccess,
} from './types'

/**
 * Retrieves the current backend configuration.
 *
 * @returns The current configuration settings
 */
export async function getConfig(): Promise<unknown> {
  const response = await configHub.sendRequest<ResponseBase>({
    type: 'get',
  })
  return assertSuccess<ConfigGetSuccess>(response)
}

/**
 * Updates backend configuration with new values.
 *
 * @param request - Configuration values to update
 * @returns Updated configuration confirmation
 */
export async function updateConfig(request: Record<string, unknown>): Promise<unknown> {
  const response = await configHub.sendRequest<ResponseBase>({
    type: 'update',
    ...request,
  })
  return assertSuccess<ConfigUpdateSuccess>(response)
}

/**
 * Reloads configuration from the backend config file.
 *
 * @returns Reload confirmation
 */
export async function reloadConfig(): Promise<unknown> {
  const response = await configHub.sendRequest<ResponseBase>({
    type: 'reload',
  })
  return assertSuccess<ConfigReloadSuccess>(response)
}

/**
 * Resets configuration to default values.
 *
 * @returns Reset confirmation
 */
export async function resetConfig(): Promise<unknown> {
  const response = await configHub.sendRequest<ResponseBase>({
    type: 'reset',
  })
  return assertSuccess<ConfigResetSuccess>(response)
}

/**
 * Gets the initialization status of ML models.
 *
 * @returns Model status information
 */
export async function getModelStatus(): Promise<ModelStatusSuccess> {
  const response = await configHub.sendRequest<ResponseBase>({
    type: 'modelstatus',
  })
  return assertSuccess<ModelStatusSuccess>(response)
}

/**
 * Controls ML model initialization/deinitialization.
 *
 * @param model - The model identifier
 * @param action - Action to perform ('init' or 'deinit')
 * @returns Model control confirmation
 */
export async function controlModel(
  model: string,
  action: 'init' | 'deinit',
): Promise<ModelControlSuccess> {
  const response = await configHub.sendRequest<ResponseBase>({
    type: 'modelcontrol',
    Model: model,
    Action: action,
  })
  return assertSuccess<ModelControlSuccess>(response)
}
