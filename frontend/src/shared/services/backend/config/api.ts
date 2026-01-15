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

export async function getConfig(): Promise<unknown> {
  const response = await configHub.sendRequest<ResponseBase>({
    type: 'get',
  })
  return assertSuccess<ConfigGetSuccess>(response)
}

export async function updateConfig(request: Record<string, unknown>): Promise<unknown> {
  const response = await configHub.sendRequest<ResponseBase>({
    type: 'update',
    ...request,
  })
  return assertSuccess<ConfigUpdateSuccess>(response)
}

export async function reloadConfig(): Promise<unknown> {
  const response = await configHub.sendRequest<ResponseBase>({
    type: 'reload',
  })
  return assertSuccess<ConfigReloadSuccess>(response)
}

export async function resetConfig(): Promise<unknown> {
  const response = await configHub.sendRequest<ResponseBase>({
    type: 'reset',
  })
  return assertSuccess<ConfigResetSuccess>(response)
}

export async function getModelStatus(): Promise<ModelStatusSuccess> {
  const response = await configHub.sendRequest<ResponseBase>({
    type: 'modelstatus',
  })
  return assertSuccess<ModelStatusSuccess>(response)
}

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
