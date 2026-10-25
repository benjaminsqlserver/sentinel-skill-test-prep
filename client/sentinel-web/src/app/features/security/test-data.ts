import { AccountSecurityStatus, SecurityFinding, SignInAttempt } from '../../core/models/api-models';

// Builders for specs. Each takes overrides, so a test states only what
// matters to it: `aFinding({ severity: 'High' })`.

export const NOW = new Date('2026-10-25T12:00:00Z');

export function aFinding(overrides: Partial<SecurityFinding> = {}): SecurityFinding {
  return {
    key: 'new-country:abc',
    kind: 'NewCountry',
    severity: 'Medium',
    occurredAtUtc: '2026-10-25T09:00:00Z',
    summary: 'First sign-in from London, GB.',
    attemptIds: ['a1'],
    ...overrides,
  };
}

export function aSignIn(overrides: Partial<SignInAttempt> = {}): SignInAttempt {
  return {
    id: 'a1',
    occurredAtUtc: '2026-10-25T09:00:00Z',
    ipAddress: '81.2.69.17',
    countryCode: 'GB',
    city: 'London',
    device: 'Safari on iPhone',
    succeeded: true,
    failureReason: null,
    isFlagged: false,
    ...overrides,
  };
}

export function aStatus(overrides: Partial<AccountSecurityStatus> = {}): AccountSecurityStatus {
  return {
    riskLevel: 'Medium',
    isCompromiseSuspected: true,
    assessedAtUtc: '2026-10-25T11:58:00Z',
    timeZoneId: 'Africa/Lagos',
    findings: [aFinding()],
    recentSignIns: [aSignIn({ isFlagged: true })],
    ...overrides,
  };
}
