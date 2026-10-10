/**
 * Enterprise Timezone Utility for Servexa
 * Follows Servexa Architecture Baseline §24 and PRD REV-NFR-001.
 * Authoritative canonical storage is always UTC ISO 8601 instants.
 * Presentation and input conversion respects Site-local IANA timezones.
 * Never performs custom offset arithmetic; delegates timezone rules to standard Intl.DateTimeFormat.
 */

export function isValidIanaTimeZone(timeZone?: string | null): boolean {
  if (!timeZone || typeof timeZone !== 'string') return false;
  try {
    new Intl.DateTimeFormat(undefined, { timeZone });
    return true;
  } catch {
    return false;
  }
}

/**
 * Returns the exact signed offset in milliseconds between the given instant in the specified
 * IANA timezone and UTC (i.e. localInstantAsUtcMs - instantUtcMs).
 */
export function getTimeZoneOffsetMs(instantUtc: Date, timeZone: string): number {
  if (!isValidIanaTimeZone(timeZone) || timeZone.toUpperCase() === 'UTC') {
    return 0;
  }

  const formatter = new Intl.DateTimeFormat('en-US', {
    timeZone,
    hourCycle: 'h23',
    year: 'numeric',
    month: 'numeric',
    day: 'numeric',
    hour: 'numeric',
    minute: 'numeric',
    second: 'numeric',
  });

  const parts = Object.fromEntries(
    formatter.formatToParts(instantUtc).map((p) => [p.type, p.value])
  );

  const localAsUtc = Date.UTC(
    parseInt(parts.year, 10),
    parseInt(parts.month, 10) - 1,
    parseInt(parts.day, 10),
    parseInt(parts.hour, 10),
    parseInt(parts.minute, 10),
    parseInt(parts.second, 10)
  );

  return localAsUtc - instantUtc.getTime();
}

/**
 * Converts a Site-local date (YYYY-MM-DD) and time (HH:mm or HH:mm:ss) in the authoritative
 * IANA timezone to a canonical UTC ISO-8601 string.
 *
 * Correctly resolves standard time, daylight saving time (DST), spring-forward gaps,
 * and autumn overlaps without manual offset arithmetic.
 */
export function localDateTimeToUtcIso(
  dateStr: string,
  timeStr: string,
  timeZone: string = 'UTC'
): string {
  const cleanTz = isValidIanaTimeZone(timeZone) ? timeZone : 'UTC';

  const [yearStr, monthStr, dayStr] = dateStr.split('-');
  const [hourStr, minuteStr, secStr] = timeStr.split(':');

  const y = parseInt(yearStr, 10);
  const m = parseInt(monthStr, 10);
  const d = parseInt(dayStr, 10);
  const h = parseInt(hourStr, 10);
  const min = parseInt(minuteStr, 10);
  const s = secStr ? parseInt(secStr, 10) : 0;

  if (isNaN(y) || isNaN(m) || isNaN(d) || isNaN(h) || isNaN(min)) {
    throw new Error(`Invalid local date or time: date='${dateStr}', time='${timeStr}'`);
  }

  const targetLocalMs = Date.UTC(y, m - 1, d, h, min, s);

  if (cleanTz.toUpperCase() === 'UTC') {
    return new Date(targetLocalMs).toISOString();
  }

  // 1. Initial offset estimate using target wall-clock instant
  const initialOffset = getTimeZoneOffsetMs(new Date(targetLocalMs), cleanTz);
  let candidateMs = targetLocalMs - initialOffset;

  // 2. Refined offset at candidate UTC instant (handles DST boundary accurately)
  const refinedOffset = getTimeZoneOffsetMs(new Date(candidateMs), cleanTz);
  candidateMs = targetLocalMs - refinedOffset;

  return new Date(candidateMs).toISOString();
}

/**
 * Converts a canonical UTC instant to Site-local date (YYYY-MM-DD) and time (HH:mm).
 */
export function utcToLocalDateTime(
  utcInstant: string | Date,
  timeZone: string = 'UTC'
): { date: string; time: string; timeZoneName?: string } {
  const cleanTz = isValidIanaTimeZone(timeZone) ? timeZone : 'UTC';
  const dateObj = typeof utcInstant === 'string' ? new Date(utcInstant) : utcInstant;

  if (isNaN(dateObj.getTime())) {
    return { date: '', time: '' };
  }

  const formatter = new Intl.DateTimeFormat('en-US', {
    timeZone: cleanTz,
    hourCycle: 'h23',
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
    timeZoneName: 'short',
  });

  const parts = Object.fromEntries(
    formatter.formatToParts(dateObj).map((p) => [p.type, p.value])
  );

  return {
    date: `${parts.year}-${parts.month}-${parts.day}`,
    time: `${parts.hour}:${parts.minute}`,
    timeZoneName: parts.timeZoneName,
  };
}

/**
 * Formats a UTC instant for display in the Site's authoritative IANA timezone.
 * Clearly displays the timezone abbreviation/context to prevent confusion with browser local time.
 */
export function formatSiteDateTime(
  utcInstant: string | Date | null | undefined,
  timeZone?: string | null,
  options?: Intl.DateTimeFormatOptions
): string {
  if (!utcInstant) return '—';

  const dateObj = typeof utcInstant === 'string' ? new Date(utcInstant) : utcInstant;
  if (isNaN(dateObj.getTime())) return '—';

  const cleanTz = isValidIanaTimeZone(timeZone) ? (timeZone as string) : 'UTC';

  const defaultOptions: Intl.DateTimeFormatOptions = {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
    timeZone: cleanTz,
    timeZoneName: 'short',
    ...options,
  };

  try {
    const formatted = new Intl.DateTimeFormat('en-US', defaultOptions).format(dateObj);
    return cleanTz !== 'UTC' ? `${formatted} (${cleanTz})` : formatted;
  } catch {
    return dateObj.toUTCString();
  }
}
