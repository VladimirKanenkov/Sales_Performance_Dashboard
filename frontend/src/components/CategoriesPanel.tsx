import { Bar, BarChart, Cell, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts'
import type { CategoryStats } from '../lib/api'
import { formatMargin, formatMoney } from '../lib/format'
import { EmptyState, ErrorState, SkeletonBlock } from './States'

const COLORS = ['#2563eb', '#7c3aed', '#db2777', '#ea580c', '#16a34a', '#0891b2', '#ca8a04', '#4f46e5']

type Props = {
  data?: CategoryStats[]
  isLoading: boolean
  isError: boolean
  error?: Error | null
  onRetry: () => void
}

/**
 * Столбцы выручки по категориям.
 *
 * @param data - Агрегаты категорий.
 * @param isLoading - Первая загрузка.
 * @param isError - Запрос завершился ошибкой.
 * @param error - Текст ошибки.
 * @param onRetry - Повторный запрос.
 */
export function CategoriesPanel({ data, isLoading, isError, error, onRetry }: Props) {
  return (
    <section className="card flex h-full flex-col">
      <div className="border-b border-slate-100 px-4 py-3">
        <h2 className="text-sm font-semibold">Категории</h2>
        <p className="text-xs text-slate-500">Выручка и доля в периоде</p>
      </div>
      <div className="flex-1 p-3">
        {isLoading && !data && <SkeletonBlock className="h-56" />}
        {isError && !data && <ErrorState message={error?.message ?? 'Ошибка API'} onRetry={onRetry} />}
        {data && data.length === 0 && <EmptyState message="Нет данных по категориям" />}
        {data && data.length > 0 && (
          <div className="grid h-full grid-cols-2 gap-3">
            <ResponsiveContainer width="100%" height={220}>
              <BarChart data={data} layout="vertical" margin={{ left: 8, right: 8 }}>
                <XAxis type="number" hide />
                <YAxis type="category" dataKey="name" width={100} tick={{ fontSize: 10 }} />
                <Tooltip formatter={(v) => formatMoney(Number(v))} />
                <Bar dataKey="revenue" name="Выручка" radius={[0, 6, 6, 0]}>
                  {data.map((_, i) => (
                    <Cell key={i} fill={COLORS[i % COLORS.length]} />
                  ))}
                </Bar>
              </BarChart>
            </ResponsiveContainer>
            <ul className="space-y-2 overflow-auto pr-1 text-xs">
              {data.map((c, i) => (
                <li key={c.categoryId} className="flex items-center justify-between gap-2 rounded-lg bg-slate-50 px-2 py-1.5">
                  <div className="flex items-center gap-2">
                    <span className="h-2.5 w-2.5 rounded-full" style={{ background: COLORS[i % COLORS.length] }} />
                    <span className="font-medium">{c.name}</span>
                  </div>
                  <div className="text-right tabular text-slate-600">
                    <div>{formatMoney(c.revenue)}</div>
                    <div className="text-[10px] text-slate-400">
                      доля {(c.share * 100).toFixed(1)}% · маржа {formatMargin(c.margin)}
                    </div>
                  </div>
                </li>
              ))}
            </ul>
          </div>
        )}
      </div>
    </section>
  )
}
