import { motion } from 'framer-motion'
import type { KpiResponse } from '../lib/api'
import { formatChange, formatMargin, formatMoney, formatNumber } from '../lib/format'
import { AnimatedNumber } from './AnimatedNumber'
import { EmptyState, ErrorState, SkeletonBlock } from './States'

type Props = {
  data?: KpiResponse
  isLoading: boolean
  isError: boolean
  error?: Error | null
  onRetry: () => void
}

function ChangeBadge({ value }: { value: number | null | undefined }) {
  const label = formatChange(value)
  if (!label) return <span className="text-xs text-slate-400">нет базы</span>
  const positive = (value ?? 0) >= 0
  return (
    <span className={`text-xs font-medium ${positive ? 'text-emerald-600' : 'text-red-600'}`}>
      {label} к пред. периоду
    </span>
  )
}

/**
 * Карточки KPI и сравнение с предыдущим периодом.
 *
 * @param data - Ответ /api/analytics/kpis.
 * @param isLoading - Первая загрузка.
 * @param isError - Запрос завершился ошибкой.
 * @param error - Текст ошибки.
 * @param onRetry - Повторный запрос.
 */
export function KpiCards({ data, isLoading, isError, error, onRetry }: Props) {
  if (isLoading && !data) {
    return (
      <div className="grid grid-cols-6 gap-3">
        {Array.from({ length: 6 }).map((_, i) => (
          <SkeletonBlock key={i} className="h-28" />
        ))}
      </div>
    )
  }

  if (isError && !data) {
    return <ErrorState message={error?.message ?? 'Ошибка API'} onRetry={onRetry} />
  }

  if (!data) {
    return <EmptyState message="Нет данных за выбранный период" />
  }

  const cards = [
    {
      key: 'revenue',
      label: 'Выручка',
      node: <AnimatedNumber value={data.revenue.value} format={formatMoney} className="tabular text-xl font-semibold" />,
      change: data.revenue.changePercent,
    },
    {
      key: 'gp',
      label: 'Валовая прибыль',
      node: <AnimatedNumber value={data.grossProfit.value} format={formatMoney} className="tabular text-xl font-semibold" />,
      change: data.grossProfit.changePercent,
    },
    {
      key: 'margin',
      label: 'Маржинальность',
      node: <span className="tabular text-xl font-semibold">{formatMargin(data.margin?.value ?? null)}</span>,
      change: data.margin?.changePercent ?? null,
    },
    {
      key: 'count',
      label: 'Продажи',
      node: <AnimatedNumber value={data.salesCount.value} format={formatNumber} className="tabular text-xl font-semibold" />,
      change: data.salesCount.changePercent,
    },
    {
      key: 'avg',
      label: 'Средний чек',
      node: (
        <span className="tabular text-xl font-semibold">
          {data.averageCheck ? formatMoney(data.averageCheck.value) : '—'}
        </span>
      ),
      change: data.averageCheck?.changePercent ?? null,
    },
    {
      key: 'best',
      label: 'Лучший менеджер',
      node: data.bestManager ? (
        <div className="flex items-center gap-2">
          <span
            className="flex h-8 w-8 items-center justify-center rounded-full text-xs font-bold text-white"
            style={{ background: data.bestManager.avatarColor }}
          >
            {data.bestManager.initials}
          </span>
          <div>
            <div className="text-sm font-semibold leading-tight">{data.bestManager.fullName}</div>
            <div className="tabular text-xs text-slate-500">{formatMoney(data.bestManager.grossProfit)}</div>
          </div>
        </div>
      ) : (
        <span className="text-sm text-slate-400">Нет оплаченных продаж</span>
      ),
      change: null as number | null,
    },
  ]

  return (
    <div className="grid grid-cols-6 gap-3">
      {cards.map((card, index) => (
        <motion.div
          key={card.key}
          initial={{ opacity: 0, y: 12 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ delay: index * 0.05, duration: 0.35 }}
          className="card p-4"
        >
          <div className="mb-2 text-xs font-medium uppercase tracking-wide text-slate-500">{card.label}</div>
          <div className="mb-2 min-h-10">{card.node}</div>
          {card.key !== 'best' ? <ChangeBadge value={card.change} /> : <span className="text-xs text-slate-400">по валовой прибыли</span>}
        </motion.div>
      ))}
    </div>
  )
}
