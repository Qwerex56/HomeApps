import type { AccountStatus, ServerRole } from './api'

// PrimeVue Tag severities for the role and status badges.
export const roleSeverity: Record<ServerRole, string> = { User: 'secondary', Admin: 'info', Owner: 'contrast' }
export const statusSeverity: Record<AccountStatus, string> = { Active: 'success', Disabled: 'danger' }
