import {
  CartesianGrid,
  Legend,
  Line,
  LineChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts'
import type { TrendPoint } from '../lib/api'
import { formatMoney, formatNumber } from '../lib/format'
import { EmptyState, ErrorState, SkeletonBlock } from './States'

type Props = {
  data?: TrendPoint[]
  isLoading: boolean
  isError: boolean
  error?: Error | null
  onRetry: () => void
}

/**
 * График выручки и валовой прибыли за период.
 *
 * @param data - Точки динамики.
 * @param isLoading - Первая загрузка.
 * @param isError - Запрос завершился ошибкой.
 * @param error - Текст ошибки.
 * @param onRetry - Повторный запрос.
 */
export function TrendChart({ data, isLoading, isError, error, onRetry }: Props) {
  return (
    <section className="card flex h-full flex-col">
      <div className="border-b border-slate-100 px-4 py-3">
        <h2 className="text-sm font-semibold">Динамика</h2>
        <p className="text-xs text-slate-500">Выручка, валовая прибыль и количество оплаченных продаж</p>
      </div>
      <div className="min-h-72 flex-1 p-3">
        {isLoading && !data && <SkeletonBlock className="h-64" />}
        {isError && !data && <ErrorState message={error?.message ?? 'Ошибка API'} onRetry={onRetry} />}
        {data && data.every((d) => d.revenue === 0 && d.salesCount === 0) && (
          <EmptyState message="Нет продаж за выбранный период" />
        )}
        {data && data.some((d) => d.revenue !== 0 || d.salesCount !== 0) && (
          <ResponsiveContainer width="100%" height="100%" minHeight={260}>
            <LineChart data={data} margin={{ top: 8, right: 12, left: 0, bottom: 0 }}>
              <CartesianGrid strokeDasharray="3 3" stroke="#e2e8f0" />
              <XAxis dataKey="period" tick={{ fontSize: 11 }} stroke="#94a3b8" />
              <YAxis
                yAxisId="money"
                tickFormatter={(v) => `${Math.round(v / 1000)}к`}
                tick={{ fontSize: 11 }}
                stroke="#94a3b8"
                width={48}
              />
              <YAxis
                yAxisId="count"
                orientation="right"
                tick={{ fontSize: 11 }}
                stroke="#94a3b8"
                width={32}
              />
              <Tooltip
                formatter={(value, name) => {
                  const n = typeof value === 'number' ? value : Number(value)
                  if (name === 'Продажи') return [formatNumber(n), name]
                  return [formatMoney(n), name]
                }}
              />
              <Legend />
              <Line
                yAxisId="money"
                type="monotone"
                dataKey="revenue"
                name="Выручка"
                stroke="#2563eb"
                strokeWidth={2}
                dot={false}
                isAnimationActive
              />
              <Line
                yAxisId="money"
                type="monotone"
                dataKey="grossProfit"
                name="Валовая прибыль"
                stroke="#059669"
                strokeWidth={2}
                dot={false}
                isAnimationActive
              />
              <Line
                yAxisId="count"
                type="monotone"
                dataKey="salesCount"
                name="Продажи"
                stroke="#f59e0b"
                strokeWidth={2}
                dot={false}
                isAnimationActive
              />
            </LineChart>
          </ResponsiveContainer>
        )}
      </div>
    </section>
  )
}
