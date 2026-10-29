import type { HouseholdRole } from './api'

// PrimeVue Tag severities for the household role badges.
export const roleSeverity: Record<HouseholdRole, string> = {
  Guest: 'warn',
  Member: 'secondary',
  Admin: 'info',
  Owner: 'contrast',
}
