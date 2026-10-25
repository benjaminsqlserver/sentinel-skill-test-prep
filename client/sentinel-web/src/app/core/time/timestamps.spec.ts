import { formatInZone, formatRelative, formatUtc, isValidTimeZone, parseApiTimestamp } from './timestamps';

describe('parseApiTimestamp', () => {
  it('parses UTC with a Z', () => {
    expect(parseApiTimestamp('2026-10-24T08:00:00Z')?.toISOString()).toBe('2026-10-24T08:00:00.000Z');
  });

  it('parses an explicit offset', () => {
    expect(parseApiTimestamp('2026-10-24T09:00:00+01:00')?.toISOString()).toBe('2026-10-24T08:00:00.000Z');
  });

  it('parses fractional seconds, which .NET emits', () => {
    expect(parseApiTimestamp('2026-10-24T08:00:00.1234567Z')).not.toBeNull();
  });

  it.each([null, undefined, '', 'yesterday', '24/10/2026 08:00', '2026-13-45T99:99:99Z'])('returns null for %s', (value) => {
    expect(parseApiTimestamp(value as string | null | undefined)).toBeNull();
  });
});

describe('formatInZone', () => {
  const eightUtc = '2026-10-24T08:00:00Z';

  it('shows the same instant in different zones', () => {
    expect(formatInZone(eightUtc, 'Africa/Lagos', { showZone: false })).toBe('24 Oct 2026, 09:00');
    expect(formatInZone(eightUtc, 'Asia/Singapore', { showZone: false })).toBe('24 Oct 2026, 16:00');
    expect(formatInZone(eightUtc, 'America/New_York', { showZone: false })).toBe('24 Oct 2026, 04:00');
  });

  it('crosses the date line correctly', () => {
    expect(formatInZone('2026-10-24T23:30:00Z', 'Asia/Singapore', { showZone: false })).toBe('25 Oct 2026, 07:30');
  });

  it('applies daylight saving for the date being shown, not for today', () => {
    // London is UTC+1 in July and UTC+0 in January.
    expect(formatInZone('2026-07-01T12:00:00Z', 'Europe/London', { showZone: false })).toBe('1 Jul 2026, 13:00');
    expect(formatInZone('2026-01-01T12:00:00Z', 'Europe/London', { showZone: false })).toBe('1 Jan 2026, 12:00');
  });

  it('includes a zone name by default', () => {
    // Engines disagree on the abbreviation (WAT vs GMT+1), so only check one is there.
    expect(formatInZone(eightUtc, 'Africa/Lagos')).toMatch(/09:00 \S+/);
  });

  it('falls back to UTC for an unknown zone instead of throwing', () => {
    expect(formatInZone(eightUtc, 'Mars/Olympus_Mons', { showZone: false })).toBe('24 Oct 2026, 08:00');
  });

  it('says so when there is no usable time', () => {
    expect(formatInZone(null, 'Africa/Lagos')).toBe('time unavailable');
  });

  it('supports a short numeric style', () => {
    expect(formatInZone(eightUtc, 'Africa/Lagos', { style: 'short', showZone: false })).toBe('24/10/2026, 09:00');
  });
});

describe('formatUtc', () => {
  it('drops the milliseconds and says UTC', () => {
    expect(formatUtc('2026-10-24T08:00:00.123Z')).toBe('2026-10-24 08:00:00 UTC');
  });
});

describe('formatRelative', () => {
  const now = new Date('2026-10-25T12:00:00Z');

  it.each([
    ['2026-10-25T11:59:30Z', 'just now'],
    ['2026-10-25T12:01:00Z', 'just now'], // slightly in the future: clock skew
    ['2026-10-25T11:55:00Z', '5 minutes ago'],
    ['2026-10-25T09:00:00Z', '3 hours ago'],
    ['2026-10-24T10:00:00Z', 'yesterday'],
    ['2026-10-20T12:00:00Z', '5 days ago'],
    ['2026-10-11T12:00:00Z', '2 weeks ago'],
    ['2026-07-25T12:00:00Z', '3 months ago'],
  ])('%s is "%s"', (value, expected) => {
    expect(formatRelative(value, now)).toBe(expected);
  });
});

describe('isValidTimeZone', () => {
  it.each([['Africa/Lagos', true], ['UTC', true], ['Lagos', false], ['', false], [null, false]])('%s -> %s', (zone, expected) => {
    expect(isValidTimeZone(zone)).toBe(expected);
  });
});
