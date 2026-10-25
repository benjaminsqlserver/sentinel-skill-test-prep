import { RiskLevel } from '../../core/models/api-models';
import { findingTitle, presentRisk } from './risk';

describe('presentRisk', () => {
  it.each<[RiskLevel, string]>([
    ['None', 'ok'],
    ['Low', 'info'],
    ['Medium', 'warning'],
    ['High', 'danger'],
  ])('%s is shown as %s', (level, tone) => {
    expect(presentRisk(level).tone).toBe(tone);
  });

  it('treats an unknown level as a warning, never as all clear', () => {
    expect(presentRisk('Catastrophic' as RiskLevel).tone).toBe('warning');
    expect(presentRisk(undefined).tone).toBe('warning');
  });
});

describe('findingTitle', () => {
  it('names every known kind and copes with new ones', () => {
    expect(findingTitle('ImpossibleTravel')).toBe('Sign-ins too far apart to be one person');
    expect(findingTitle('SomethingNew')).toBe('Unusual activity');
  });
});
