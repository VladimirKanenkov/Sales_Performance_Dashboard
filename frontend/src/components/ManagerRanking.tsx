import { AnimatePresence, motion } from 'framer-motion'
import type { ManagerRankingRow, RankingSort } from '../lib/api'
import { formatChange, formatMargin, formatMoney, formatNumber } from '../lib/format'
import { EmptyState, ErrorState, SkeletonBlock } from './States'

type Props = {
  data?: ManagerRankingRow[]
  sort: RankingSort
  onSortChange: (sort: RankingSort) => void
  isLoading: boolean
  isError: boolean
  error?: Error | null
  onRetry: () => void
}

/**
 * Таблица рейтинга менеджеров с переключением сортировки.
 *
 * @param data - Строки рейтинга.
 * @param sort - grossProfit или averageCheck.
 * @param onSortChange - Смена сортировки.
 * @param isLoading - Первая загрузка.
 * @param isError - Запрос завершился ошибкой.
 * @param error - Текст ошибки.
 * @param onRetry - Повторный запрос.
 */
export function ManagerRanking({ data, sort, onSortChange, isLoading, isError, error, onRetry }: Props) {
  return (
    <section className="card flex h-full flex-col overflow-hidden">
      <div className="flex items-center justify-between border-b border-slate-100 px-4 py-3">
        <h2 className="text-sm font-semibold text-slate-900">Рейтинг менеджеров</h2>
        <div className="flex gap-1 rounded-lg bg-slate-100 p-1">
          {(
            [
              ['grossProfit', 'Валовая прибыль'],
              ['averageCheck', 'Средний чек'],
            ] as const
          ).map(([value, label]) => (
            <button
              key={value}
              type="button"
              onClick={() => onSortChange(value)}
              className={`rounded-md px-2.5 py-1 text-xs font-medium transition ${
                sort === value ? 'bg-white text-slate-900 shadow-sm' : 'text-slate-600'
              }`}
            >
              {label}
            </button>
          ))}
        </div>
      </div>

      <div className="flex-1 overflow-auto">
        {isLoading && !data && (
          <div className="space-y-2 p-4">
            {Array.from({ length: 8 }).map((_, i) => (
              <SkeletonBlock key={i} className="h-10" />
            ))}
          </div>
        )}
        {isError && !data && (
          <div className="p-4">
            <ErrorState message={error?.message ?? 'Ошибка API'} onRetry={onRetry} />
          </div>
        )}
        {data && data.length === 0 && (
          <div className="p-4">
            <EmptyState message="Нет менеджеров" />
          </div>
        )}
        {data && data.length > 0 && (
          <table className="w-full text-left text-xs">
            <thead className="sticky top-0 bg-slate-50 text-slate-500">
              <tr>
                <th className="px-3 py-2 font-medium">#</th>
                <th className="px-3 py-2 font-medium">Менеджер</th>
                <th className="px-3 py-2 font-medium text-right">Продажи</th>
                <th className="px-3 py-2 font-medium text-right">Выручка</th>
                <th className="px-3 py-2 font-medium text-right">Прибыль</th>
                <th className="px-3 py-2 font-medium text-right">Ср. чек</th>
                <th className="px-3 py-2 font-medium text-right">Маржа</th>
                <th className="px-3 py-2 font-medium text-right">Δ</th>
              </tr>
            </thead>
            <tbody>
              <AnimatePresence initial={false}>
                {data.map((row) => (
                  <motion.tr
                    key={row.managerId}
                    layout
                    initial={{ opacity: 0 }}
                    animate={{ opacity: 1 }}
                    exit={{ opacity: 0 }}
                    className="border-t border-slate-50 hover:bg-slate-50/80"
                  >
                    <td className="px-3 py-2 tabular font-semibold text-slate-500">{row.rank}</td>
                    <td className="px-3 py-2">
                      <div className="flex items-center gap-2">
                        <span
                          className="flex h-7 w-7 shrink-0 items-center justify-center rounded-full text-[10px] font-bold text-white"
                          style={{ background: row.avatarColor }}
                        >
                          {row.initials}
                        </span>
                        <div>
                          <div className="font-medium text-slate-900">{row.fullName}</div>
                          <div className="text-[10px] text-slate-400">
                            {row.team} · {row.title}
                            {!row.isActive && ' · неактивен'}
                          </div>
                        </div>
                      </div>
                    </td>
                    <td className="px-3 py-2 text-right tabular">{formatNumber(row.salesCount)}</td>
                    <td className="px-3 py-2 text-right tabular">{formatMoney(row.revenue)}</td>
                    <td className="px-3 py-2 text-right tabular">{formatMoney(row.grossProfit)}</td>
                    <td className="px-3 py-2 text-right tabular">
                      {row.averageCheck == null ? '—' : formatMoney(row.averageCheck)}
                    </td>
                    <td className="px-3 py-2 text-right tabular">{formatMargin(row.margin)}</td>
                    <td
                      className={`px-3 py-2 text-right tabular font-medium ${
                        (row.changePercent ?? 0) >= 0 ? 'text-emerald-600' : 'text-red-600'
                      }`}
                    >
                      {formatChange(row.changePercent) ?? '—'}
                    </td>
                  </motion.tr>
                ))}
              </AnimatePresence>
            </tbody>
          </table>
        )}
      </div>
    </section>
  )
}
