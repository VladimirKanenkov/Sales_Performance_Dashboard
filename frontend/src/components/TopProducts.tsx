import type { ProductStats } from '../lib/api'
import { formatMoney, formatNumber } from '../lib/format'
import { EmptyState, ErrorState, SkeletonBlock } from './States'

type Props = {
  data?: ProductStats[]
  isLoading: boolean
  isError: boolean
  error?: Error | null
  onRetry: () => void
}

/**
 * Топ товаров по валовой прибыли.
 *
 * @param data - Товары.
 * @param isLoading - Первая загрузка.
 * @param isError - Запрос завершился ошибкой.
 * @param error - Текст ошибки.
 * @param onRetry - Повторный запрос.
 */
export function TopProducts({ data, isLoading, isError, error, onRetry }: Props) {
  return (
    <section className="card flex h-full flex-col overflow-hidden">
      <div className="border-b border-slate-100 px-4 py-3">
        <h2 className="text-sm font-semibold">Лучшие продукты</h2>
        <p className="text-xs text-slate-500">Топ по валовой прибыли</p>
      </div>
      <div className="flex-1 overflow-auto p-2">
        {isLoading && !data && (
          <div className="space-y-2 p-2">
            {Array.from({ length: 6 }).map((_, i) => (
              <SkeletonBlock key={i} className="h-9" />
            ))}
          </div>
        )}
        {isError && !data && <ErrorState message={error?.message ?? 'Ошибка API'} onRetry={onRetry} />}
        {data && data.length === 0 && <EmptyState message="Нет данных по продуктам" />}
        {data && data.length > 0 && (
          <ol className="space-y-1">
            {data.map((p, idx) => {
              const max = data[0]?.grossProfit || 1
              const width = Math.max(8, Math.round((p.grossProfit / max) * 100))
              return (
                <li key={p.productId} className="rounded-lg px-2 py-1.5 hover:bg-slate-50">
                  <div className="mb-1 flex items-center justify-between gap-2 text-xs">
                    <span className="font-medium text-slate-800">
                      <span className="mr-2 text-slate-400">{idx + 1}.</span>
                      {p.name}
                    </span>
                    <span className="tabular font-semibold text-emerald-700">{formatMoney(p.grossProfit)}</span>
                  </div>
                  <div className="h-1.5 overflow-hidden rounded-full bg-slate-100">
                    <div className="h-full rounded-full bg-emerald-500 transition-all" style={{ width: `${width}%` }} />
                  </div>
                  <div className="mt-1 text-[10px] text-slate-400">
                    {p.categoryName} · qty {formatNumber(p.quantity)} · {formatMoney(p.revenue)}
                  </div>
                </li>
              )
            })}
          </ol>
        )}
      </div>
    </section>
  )
}
