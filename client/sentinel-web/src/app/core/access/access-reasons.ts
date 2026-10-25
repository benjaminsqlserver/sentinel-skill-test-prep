import { AccessReason } from '../models/api-models';

/**
 * Plain-English explanations of the evaluator's reasons, for the people who
 * hit them. Written for a non-engineer: no "permission strings", no
 * "parents", just what happened and what to do next.
 */
const explanations: Record<AccessReason | 'Unknown', string> = {
  PublicPage: 'This page is open to everyone.',
  AuthenticatedUser: 'Every signed-in user can open this page.',
  PermissionGranted: 'One of your roles gives you access to this page.',
  NotAuthenticated: 'You need to sign in to see this page.',
  UserSuspended: 'Your account is suspended, so only public pages are available. Contact your administrator.',
  MissingPermission: 'None of your roles includes this page. Ask your administrator if you need it.',
  ExplicitlyDenied: 'Your administrator has specifically removed your access to this page.',
  ParentDenied: 'This page sits inside a section you do not have access to.',
  OrphanedPage: 'This page is misconfigured. Please report it to support.',
  CyclicHierarchy: 'This page is misconfigured. Please report it to support.',
  Unknown: 'This page does not exist, or we could not check your access to it right now.',
};

export function explainAccess(reason: string | null | undefined): string {
  return explanations[(reason ?? 'Unknown') as AccessReason] ?? explanations.Unknown;
}
