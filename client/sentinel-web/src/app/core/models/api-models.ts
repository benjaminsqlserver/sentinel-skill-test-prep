// Hand-written mirrors of src/Sentinel.Contracts. Keep the two in step:
// a property renamed in C# and not here compiles fine on both sides and
// fails silently at run time, as `undefined`.
//
// Timestamps arrive as ISO 8601 strings in UTC ("2026-10-24T08:00:00Z").
// They are deliberately typed `string`, not `Date`: JSON has no date type,
// and pretending otherwise hides the moment where parsing happens. Parse
// them with parseApiTimestamp() in core/time, never with `new Date()`.

/** An ISO 8601 UTC timestamp exactly as the API sent it. */
export type UtcTimestamp = string;

export type RiskLevel = 'None' | 'Low' | 'Medium' | 'High';
export type FindingKind = 'BruteForce' | 'ImpossibleTravel' | 'NewCountry' | 'NewDevice';

export interface CurrentUser {
  id: string;
  email: string;
  displayName: string;
  timeZoneId: string;
  permissions: string[];
}

export interface DemoUser {
  email: string;
  displayName: string;
  description: string;
}

export interface NavigationNode {
  path: string;
  title: string;
  children: NavigationNode[];
}

export type AccessReason =
  | 'PublicPage'
  | 'AuthenticatedUser'
  | 'PermissionGranted'
  | 'NotAuthenticated'
  | 'UserSuspended'
  | 'MissingPermission'
  | 'ExplicitlyDenied'
  | 'ParentDenied'
  | 'OrphanedPage'
  | 'CyclicHierarchy';

export interface PageAccess {
  path: string;
  title: string;
  parentPath: string | null;
  requiredPermission: string | null;
  isAllowed: boolean;
  reason: AccessReason;
}

export interface SignInAttempt {
  id: string;
  occurredAtUtc: UtcTimestamp;
  ipAddress: string;
  countryCode: string;
  city: string;
  device: string;
  succeeded: boolean;
  failureReason: string | null;
  isFlagged: boolean;
}

export interface SecurityFinding {
  key: string;
  kind: FindingKind;
  severity: RiskLevel;
  occurredAtUtc: UtcTimestamp;
  summary: string;
  attemptIds: string[];
}

export interface AccountSecurityStatus {
  riskLevel: RiskLevel;
  isCompromiseSuspected: boolean;
  assessedAtUtc: UtcTimestamp;
  timeZoneId: string;
  findings: SecurityFinding[];
  recentSignIns: SignInAttempt[];
}

export interface PagedResponse<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface UserSummary {
  id: string;
  email: string;
  displayName: string;
  status: 'Active' | 'Suspended';
  roles: string[];
}

export interface Role {
  id: string;
  name: string;
  description: string;
  permissions: string[];
  userCount: number;
}
