param containerAppsEnvName string

resource cappsEnv 'Microsoft.App/managedEnvironments@2023-05-01' existing = {
  name: containerAppsEnvName
}

resource daprComponent 'Microsoft.App/managedEnvironments/daprComponents@2023-05-01' = {
  name: 'orders'
  parent: cappsEnv
  properties: {
    componentType: 'bindings.cron'
    version: 'v1'
    metadata: [
      {
        name: 'schedule'
        value: '@every 15s'
      }
    ]
    scopes: [
      'virtual-worker'
    ]
  }
}
