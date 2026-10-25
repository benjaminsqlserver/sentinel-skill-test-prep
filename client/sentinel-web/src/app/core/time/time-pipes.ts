import { Pipe, PipeTransform } from '@angular/core';
import { formatInZone, formatRelative, formatUtc, ZonedFormatOptions } from './timestamps';

/**
 * `{{ signIn.occurredAtUtc | zonedDate: timeZone }}` -> "24 Oct 2026, 09:00 WAT"
 *
 * Why not Angular's built-in DatePipe? DatePipe's timezone argument takes an
 * offset ("+0100"), not a zone name, so it cannot know that London is +0000
 * in January and +0100 in July. Intl.DateTimeFormat takes the IANA name and
 * gets daylight saving right for every date, past or future.
 */
@Pipe({ name: 'zonedDate' })
export class ZonedDatePipe implements PipeTransform {
  transform(value: string | null | undefined, timeZone: string, options?: ZonedFormatOptions): string {
    return formatInZone(value, timeZone, options);
  }
}

/**
 * `{{ finding.occurredAtUtc | relativeTime: now }}` -> "3 hours ago"
 *
 * A pure pipe only re-runs when its inputs change, so "now" is an argument:
 * the page passes a signal that ticks once a minute, and the text stays
 * current without the pipe being impure (impure pipes run on every change
 * detection, which is wasteful for a table of fifty rows).
 */
@Pipe({ name: 'relativeTime' })
export class RelativeTimePipe implements PipeTransform {
  transform(value: string | null | undefined, now: Date): string {
    return formatRelative(value, now);
  }
}

/** `{{ value | utcTime }}` -> "2026-10-24 08:00:00 UTC". For tooltips. */
@Pipe({ name: 'utcTime' })
export class UtcTimePipe implements PipeTransform {
  transform(value: string | null | undefined): string {
    return formatUtc(value);
  }
}
