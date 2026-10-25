import { FindingKind, RiskLevel } from '../../core/models/api-models';

export type Tone = 'ok' | 'info' | 'warning' | 'danger';

export interface RiskPresentation {
  tone: Tone;
  headline: string;
  advice: string;
}

/**
 * What each risk level means to the person reading it. The words are the
 * product here: "High" tells a user nothing, "Someone may be using your
 * account" tells them to act.
 */
const presentations: Record<RiskLevel, RiskPresentation> = {
  None: {
    tone: 'ok',
    headline: 'No suspicious activity',
    advice: 'Your recent sign-ins all look like you.',
  },
  Low: {
    tone: 'info',
    headline: 'Something new to review',
    advice: 'We noticed a sign-in we have not seen before. If it was you, mark it so we stop asking.',
  },
  Medium: {
    tone: 'warning',
    headline: 'Your account may be at risk',
    advice: 'Review the activity below. If you do not recognise it, change your password now.',
  },
  High: {
    tone: 'danger',
    headline: 'Someone may be using your account',
    advice: 'Change your password now and sign out of all other sessions. Then review the activity below.',
  },
};

export function presentRisk(level: RiskLevel | null | undefined): RiskPresentation {
  // An unexpected value from a newer API is shown as the most cautious
  // known level rather than as "all clear". Fail towards telling the user.
  return presentations[level as RiskLevel] ?? presentations.Medium;
}

const findingTitles: Record<FindingKind, string> = {
  BruteForce: 'Repeated failed sign-ins',
  ImpossibleTravel: 'Sign-ins too far apart to be one person',
  NewCountry: 'Sign-in from a new country',
  NewDevice: 'Sign-in from a new device',
};

export function findingTitle(kind: FindingKind | string): string {
  return findingTitles[kind as FindingKind] ?? 'Unusual activity';
}

export function severityTone(level: RiskLevel | string): Tone {
  return presentRisk(level as RiskLevel).tone;
}
