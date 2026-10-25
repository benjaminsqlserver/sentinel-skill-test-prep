// Everything about turning the API's timestamps into words on a screen.
//
// Three rules, each of which is a bug report if broken:
//   1. A timestamp is converted to a zone exactly once: at display time.
//   2. The zone is the user's profile zone (an IANA name such as
//      "Africa/Lagos"), not whatever the browser happens to be set to.
//
// These are plain functions, not an Angular service, so they can be tested
// without TestBed and reused by pipes, components and specs alike.

/** "Z", "+01:00", "-0500" at the end of an ISO string. */
const ZONE_DESIGNATOR = /(Z|[+-]\d{2}:?\d{2})$/i;
const ISO_DATE_TIME = /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}/;

/** Parses an API timestamp. */
export function parseApiTimestamp(value: string | null | undefined): Date | null {
  if (!value || !ISO_DATE_TIME.test(value)) {
    return null;
  }

  const parsed = new Date(value);
  return Number.isNaN(parsed.getTime()) ? null : parsed;
}

/** True if `zone` is an IANA time zone this browser knows. */
export function isValidTimeZone(zone: string | null | undefined): boolean {
  if (!zone) {
    return false;
  }
  try {
    new Intl.DateTimeFormat('en-GB', { timeZone: zone });
    return true;
  } catch {
    return false;
  }
}

export interface ZonedFormatOptions {
  /** BCP 47 locale for the words and the order of the parts. Defaults to en-GB (day before month). */
  locale?: string;
  /** Include the zone abbreviation ("WAT", "GMT+1"). Defaults to true. */
  showZone?: boolean;
  /** "medium": 24 Oct 2026, 09:00 WAT. "short": 24/10/2026, 09:00. */
  style?: 'medium' | 'short';
}

/**
 * Formats an API timestamp for a person in `timeZone`.
 *
 *   formatInZone('2026-10-24T08:00:00Z', 'Africa/Lagos')  -> "24 Oct 2026, 09:00 WAT"
 *   formatInZone('2026-10-24T08:00:00Z', 'Asia/Singapore') -> "24 Oct 2026, 16:00 GMT+8"
 *
 * Abbreviations come from the browser's ICU data and vary: Lagos gets
 * "WAT" in some engines and "GMT+1" in others. Tests should therefore
 * assert on the time, not the abbreviation.
 *
 * An unknown zone falls back to UTC (and says so) rather than throwing,
 * because one bad profile value must not blank the whole page.
 */
export function formatInZone(value: string | null | undefined, timeZone: string, options: ZonedFormatOptions = {}): string {
  const date = parseApiTimestamp(value);
  if (!date) {
    return 'time unavailable';
  }

  const zone = isValidTimeZone(timeZone) ? timeZone : 'UTC';
  const { locale = 'en-GB', showZone = true, style = 'medium' } = options;

  const formatter = new Intl.DateTimeFormat(locale, {
    timeZone: zone,
    day: style === 'medium' ? 'numeric' : '2-digit',
    month: style === 'medium' ? 'short' : '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
    hourCycle: 'h23',
    ...(showZone ? { timeZoneName: 'short' as const } : {}),
  });

  return formatter.format(date);
}

/** The UTC form of a timestamp, for tooltips and support conversations: "2026-10-24 08:00:00 UTC". */
export function formatUtc(value: string | null | undefined): string {
  const date = parseApiTimestamp(value);
  return date ? `${date.toISOString().slice(0, 19).replace('T', ' ')} UTC` : 'time unavailable';
}

const RELATIVE_UNITS: [Intl.RelativeTimeFormatUnit, number][] = [
  ['year', 365 * 24 * 60 * 60],
  ['month', 30 * 24 * 60 * 60],
  ['week', 7 * 24 * 60 * 60],
  ['day', 24 * 60 * 60],
  ['hour', 60 * 60],
  ['minute', 60],
];

/**
 * "3 hours ago", "yesterday", "just now". Relative to `now`, which is a
 * parameter rather than `Date.now()` so the function is deterministic and
 * a test can pin the clock.
 *
 * Times slightly in the future (clock skew between server and browser)
 * read as "just now" instead of the confusing "in 2 minutes".
 */
export function formatRelative(value: string | null | undefined, now: Date, locale = 'en-GB'): string {
  const date = parseApiTimestamp(value);
  if (!date) {
    return 'time unavailable';
  }

  const secondsAgo = Math.round((now.getTime() - date.getTime()) / 1000);
  if (secondsAgo < 60) {
    return 'just now';
  }

  const formatter = new Intl.RelativeTimeFormat(locale, { numeric: 'auto' });
  for (const [unit, secondsInUnit] of RELATIVE_UNITS) {
    if (secondsAgo >= secondsInUnit) {
      return formatter.format(-Math.floor(secondsAgo / secondsInUnit), unit);
    }
  }

  return 'just now'; // unreachable: the loop ends at minutes
}
