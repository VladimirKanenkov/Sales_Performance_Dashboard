import {
  endOfMonth,
  format,
  startOfMonth,
  subDays,
  subMonths,
} from 'date-fns'

/** Пресет периода или произвольный диапазон. */
export type PeriodPreset =
  | 'today'
  | '7d'
  | '30d'
  | 'thisMonth'
  | 'lastMonth'
  | 'custom'

/** Выбранный период: пресет и границы в формате yyyy-MM-dd. */
export type PeriodState = {
  preset: PeriodPreset
  from: string
  to: string
}

const fmt = (d: Date) => format(d, 'yyyy-MM-dd')

/**
 * Календарная дата «сегодня» в Europe/Moscow.
 *
 * @returns Локальный Date без времени, соответствующий московскому дню.
 */
export function todayMoscow(): Date {
  const parts = new Intl.DateTimeFormat('en-CA', {
    timeZone: 'Europe/Moscow',
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
  }).formatToParts(new Date())
  const y = Number(parts.find((p) => p.type === 'year')!.value)
  const m = Number(parts.find((p) => p.type === 'month')!.value)
  const d = Number(parts.find((p) => p.type === 'day')!.value)
  return new Date(y, m - 1, d)
}

/**
 * Считает границы периода по пресету. Окна 7 и 30 дней включительны.
 *
 * @param preset - Пресет или custom.
 * @param customFrom - Начало произвольного диапазона.
 * @param customTo - Конец произвольного диапазона.
 * @returns Период с датами yyyy-MM-dd.
 */
export function resolvePreset(preset: PeriodPreset, customFrom?: string, customTo?: string): PeriodState {
  const today = todayMoscow()
  switch (preset) {
    case 'today':
      return { preset, from: fmt(today), to: fmt(today) }
    case '7d':
      return { preset, from: fmt(subDays(today, 6)), to: fmt(today) }
    case '30d':
      return { preset, from: fmt(subDays(today, 29)), to: fmt(today) }
    case 'thisMonth':
      return { preset, from: fmt(startOfMonth(today)), to: fmt(today) }
    case 'lastMonth': {
      const last = subMonths(today, 1)
      return { preset, from: fmt(startOfMonth(last)), to: fmt(endOfMonth(last)) }
    }
    case 'custom':
      return {
        preset,
        from: customFrom ?? fmt(subDays(today, 29)),
        to: customTo ?? fmt(today),
      }
  }
}

/**
 * Восстанавливает период из query string.
 *
 * @param search - window.location.search.
 * @returns Период. Неизвестный пресет заменяется на 30 дней.
 */
export function periodFromSearch(search: string): PeriodState {
  const params = new URLSearchParams(search)
  const preset = (params.get('preset') as PeriodPreset) || '30d'
  const from = params.get('from') ?? undefined
  const to = params.get('to') ?? undefined
  if (preset === 'custom' && from && to) {
    return { preset, from, to }
  }
  return resolvePreset(preset === 'custom' ? '30d' : preset)
}

/**
 * Собирает query string периода и сортировки рейтинга.
 *
 * @param period - Выбранный период.
 * @param sort - grossProfit или averageCheck.
 * @returns Строка, начинающаяся с ?.
 */
export function periodToSearch(period: PeriodState, sort: string): string {
  const p = new URLSearchParams({
    preset: period.preset,
    from: period.from,
    to: period.to,
    sort,
  })
  return `?${p.toString()}`
}

/** Подписи пресетов на русском. */
export const PRESET_LABELS: Record<PeriodPreset, string> = {
  today: 'Сегодня',
  '7d': '7 дней',
  '30d': '30 дней',
  thisMonth: 'Этот месяц',
  lastMonth: 'Прошлый месяц',
  custom: 'Период',
}
