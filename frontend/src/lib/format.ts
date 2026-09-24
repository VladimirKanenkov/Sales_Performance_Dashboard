const money = new Intl.NumberFormat('ru-RU', {
  style: 'currency',
  currency: 'RUB',
  maximumFractionDigits: 0,
})

const moneyExact = new Intl.NumberFormat('ru-RU', {
  style: 'currency',
  currency: 'RUB',
  maximumFractionDigits: 2,
})

const number = new Intl.NumberFormat('ru-RU')

const percent = new Intl.NumberFormat('ru-RU', {
  style: 'percent',
  maximumFractionDigits: 1,
})

/**
 * Форматирует сумму в рублях.
 *
 * @param value - Сумма.
 * @param exact - Два знака после запятой, иначе округление до рублей.
 * @returns Строка в ru-RU.
 */
export function formatMoney(value: number, exact = false) {
  return (exact ? moneyExact : money).format(value)
}

/**
 * Форматирует целое число с разделителями тысяч.
 *
 * @param value - Число.
 * @returns Строка в ru-RU.
 */
export function formatNumber(value: number) {
  return number.format(value)
}

/**
 * Форматирует маржинальность как процент.
 *
 * @param value - Доля 0..1 или null, если выручки нет.
 * @returns Процент или длинное тире.
 */
export function formatMargin(value: number | null | undefined) {
  if (value == null) return '—'
  return percent.format(value)
}

/**
 * Форматирует процент изменения к предыдущему периоду.
 *
 * @param value - Изменение в процентах или null, если базы нет.
 * @returns Строка со знаком или null.
 */
export function formatChange(value: number | null | undefined) {
  if (value == null) return null
  const sign = value > 0 ? '+' : ''
  return `${sign}${value.toFixed(1)}%`
}

/**
 * Русская подпись статуса продажи.
 *
 * @param status - Paid, Cancelled или Refunded.
 * @returns Подпись для бейджа.
 */
export function statusLabel(status: string) {
  switch (status) {
    case 'Paid':
      return 'Оплачено'
    case 'Cancelled':
      return 'Отменено'
    case 'Refunded':
      return 'Возврат'
    default:
      return status
  }
}
