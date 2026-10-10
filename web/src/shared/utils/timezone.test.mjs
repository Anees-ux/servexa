import test from 'node:test';
import assert from 'node:assert/strict';

function isValidIanaTimeZone(timeZone) {
  if (!timeZone || typeof timeZone !== 'string') return false;
  try {
    new Intl.DateTimeFormat(undefined, { timeZone });
    return true;
  } catch {
    return false;
  }
}

function getTimeZoneOffsetMs(instantUtc, timeZone) {
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

function localDateTimeToUtcIso(dateStr, timeStr, timeZone = 'UTC') {
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

  const initialOffset = getTimeZoneOffsetMs(new Date(targetLocalMs), cleanTz);
  let candidateMs = targetLocalMs - initialOffset;

  const refinedOffset = getTimeZoneOffsetMs(new Date(candidateMs), cleanTz);
  candidateMs = targetLocalMs - refinedOffset;

  return new Date(candidateMs).toISOString();
}

function utcToLocalDateTime(utcInstant, timeZone = 'UTC') {
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

function formatSiteDateTime(utcInstant, timeZone, options) {
  if (!utcInstant) return '—';

  const dateObj = typeof utcInstant === 'string' ? new Date(utcInstant) : utcInstant;
  if (isNaN(dateObj.getTime())) return '—';

  const cleanTz = isValidIanaTimeZone(timeZone) ? timeZone : 'UTC';

  const defaultOptions = {
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

test('Timezone validation works for valid and invalid timezones', () => {
  assert.equal(isValidIanaTimeZone('UTC'), true);
  assert.equal(isValidIanaTimeZone('Asia/Karachi'), true);
  assert.equal(isValidIanaTimeZone('Europe/Helsinki'), true);
  assert.equal(isValidIanaTimeZone('America/New_York'), true);
  assert.equal(isValidIanaTimeZone('Invalid/Timezone_Name'), false);
  assert.equal(isValidIanaTimeZone(''), false);
  assert.equal(isValidIanaTimeZone(null), false);
});

test('UTC site: Local 09:00 - 11:00 produces identical UTC instant', () => {
  const startUtc = localDateTimeToUtcIso('2026-06-01', '09:00', 'UTC');
  const endUtc = localDateTimeToUtcIso('2026-06-01', '11:00', 'UTC');

  assert.equal(startUtc, '2026-06-01T09:00:00.000Z');
  assert.equal(endUtc, '2026-06-01T11:00:00.000Z');

  const roundTrip = utcToLocalDateTime(startUtc, 'UTC');
  assert.equal(roundTrip.date, '2026-06-01');
  assert.equal(roundTrip.time, '09:00');
});

test('Asia/Karachi site: UTC+5 converts correctly without DST drift', () => {
  const startUtc = localDateTimeToUtcIso('2026-06-01', '09:00', 'Asia/Karachi');
  const endUtc = localDateTimeToUtcIso('2026-06-01', '11:00', 'Asia/Karachi');

  assert.equal(startUtc, '2026-06-01T04:00:00.000Z');
  assert.equal(endUtc, '2026-06-01T06:00:00.000Z');

  const roundTrip = utcToLocalDateTime(startUtc, 'Asia/Karachi');
  assert.equal(roundTrip.date, '2026-06-01');
  assert.equal(roundTrip.time, '09:00');
});

test('Europe/Helsinki site: Winter (UTC+2) and Summer (UTC+3) produce exact UTC instants', () => {
  const summerUtc = localDateTimeToUtcIso('2026-06-01', '09:00', 'Europe/Helsinki');
  assert.equal(summerUtc, '2026-06-01T06:00:00.000Z');

  const summerLocal = utcToLocalDateTime(summerUtc, 'Europe/Helsinki');
  assert.equal(summerLocal.date, '2026-06-01');
  assert.equal(summerLocal.time, '09:00');

  const winterUtc = localDateTimeToUtcIso('2026-01-15', '09:00', 'Europe/Helsinki');
  assert.equal(winterUtc, '2026-01-15T07:00:00.000Z');

  const winterLocal = utcToLocalDateTime(winterUtc, 'Europe/Helsinki');
  assert.equal(winterLocal.date, '2026-01-15');
  assert.equal(winterLocal.time, '09:00');
});

test('Europe/Helsinki site: DST spring-forward gap (nonexistent local time) resolves to valid UTC instant', () => {
  const gapUtc = localDateTimeToUtcIso('2026-03-29', '03:30', 'Europe/Helsinki');
  assert.ok(gapUtc.endsWith('Z'));
  assert.equal(gapUtc, '2026-03-29T01:30:00.000Z');

  const localAfterGap = utcToLocalDateTime(gapUtc, 'Europe/Helsinki');
  assert.equal(localAfterGap.date, '2026-03-29');
  assert.equal(localAfterGap.time, '04:30');
});

test('Europe/Helsinki site: DST autumn overlap (ambiguous local time) resolves deterministically', () => {
  const overlapUtc = localDateTimeToUtcIso('2026-10-25', '03:30', 'Europe/Helsinki');
  assert.ok(overlapUtc.endsWith('Z'));
  assert.equal(overlapUtc, '2026-10-25T01:30:00.000Z');

  const roundTrip = utcToLocalDateTime(overlapUtc, 'Europe/Helsinki');
  assert.equal(roundTrip.date, '2026-10-25');
  assert.equal(roundTrip.time, '03:30');
});

test('Reschedule round-trip preserves instant fidelity', () => {
  const initialUtc = localDateTimeToUtcIso('2026-05-10', '10:00', 'Europe/Helsinki');
  assert.equal(initialUtc, '2026-05-10T07:00:00.000Z');

  const rescheduledUtc = localDateTimeToUtcIso('2026-05-10', '14:30', 'Europe/Helsinki');
  assert.equal(rescheduledUtc, '2026-05-10T11:30:00.000Z');

  const formatted = formatSiteDateTime(rescheduledUtc, 'Europe/Helsinki');
  assert.ok(formatted.includes('2:30 PM') || formatted.includes('14:30'));
  assert.ok(formatted.includes('Europe/Helsinki'));
});

test('formatSiteDateTime formats in Site timezone with clear timezone label', () => {
  const instant = '2026-06-01T04:00:00.000Z';

  const karachiStr = formatSiteDateTime(instant, 'Asia/Karachi');
  assert.ok(karachiStr.includes('Asia/Karachi'));
  assert.ok(karachiStr.includes('9:00'));

  const helsinkiStr = formatSiteDateTime(instant, 'Europe/Helsinki');
  assert.ok(helsinkiStr.includes('Europe/Helsinki'));
  assert.ok(helsinkiStr.includes('7:00'));

  const utcStr = formatSiteDateTime(instant, 'UTC');
  assert.ok(utcStr.includes('4:00'));
});
