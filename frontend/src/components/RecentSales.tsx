import type { RecentSale } from '../lib/api'
import { formatMoney, statusLabel } from '../lib/format'
import { EmptyState, ErrorState, SkeletonBlock } from './States'

type Props = {
  data?: RecentSale[]
  isLoading: boolean
  isError: boolean
  error?: Error | null
  onRetry: () => void
}

function statusClass(status: string) {
  switch (status) {
    case 'Paid':
      return 'bg-emerald-50 text-emerald-700'
    case 'Refunded':
      return 'bg-amber-50 text-amber-700'
    case 'Cancelled':
      return 'bg-slate-100 text-slate-600'
    default:
      return 'bg-slate-100 text-slate-600'
  }
}

/**
 * Таблица последних продаж, включая отмены и возвраты.
 *
 * @param data - Продажи периода.
 * @param isLoading - Первая загрузка.
 * @param isError - Запрос завершился ошибкой.
 * @param error - Текст ошибки.
 * @param onRetry - Повторный запрос.
 */
export function RecentSales({ data, isLoading, isError, error, onRetry }: Props) {
  return (
    <section className="card overflow-hidden">
      <div className="border-b border-slate-100 px-4 py-3">
        <h2 className="text-sm font-semibold">Последние продажи</h2>
      </div>
      <div className="overflow-auto">
        {isLoading && !data && (
          <div className="space-y-2 p-4">
            {Array.from({ length: 6 }).map((_, i) => (
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
            <EmptyState message="В этом периоде продаж нет" />
          </div>
        )}
        {data && data.length > 0 && (
          <table className="w-full text-left text-xs">
            <thead className="bg-slate-50 text-slate-500">
              <tr>
                <th className="px-3 py-2 font-medium">Дата</th>
                <th className="px-3 py-2 font-medium">Менеджер</th>
                <th className="px-3 py-2 font-medium">Клиент</th>
                <th className="px-3 py-2 font-medium">Товары</th>
                <th className="px-3 py-2 font-medium">Статус</th>
                <th className="px-3 py-2 font-medium text-right">Сумма</th>
                <th className="px-3 py-2 font-medium text-right">Прибыль</th>
              </tr>
            </thead>
            <tbody>
              {data.map((s) => (
                <tr key={s.id} className="border-t border-slate-50 hover:bg-slate-50/70">
                  <td className="px-3 py-2 tabular whitespace-nowrap">{s.saleDate}</td>
                  <td className="px-3 py-2">
                    <div className="flex items-center gap-2">
                      <span
                        className="flex h-6 w-6 items-center justify-center rounded-full text-[10px] font-bold text-white"
                        style={{ background: s.managerAvatarColor }}
                      >
                        {s.managerInitials}
                      </span>
                      {s.managerName}
                    </div>
                  </td>
                  <td className="px-3 py-2">
                    <div>{s.customerName}</div>
                    <div className="text-[10px] text-slate-400">{s.customerCompany}</div>
                  </td>
                  <td className="max-w-56 truncate px-3 py-2 text-slate-600" title={s.products.join(', ')}>
                    {s.products.join(', ')}
                  </td>
                  <td className="px-3 py-2">
                    <span className={`rounded-full px-2 py-0.5 text-[10px] font-medium ${statusClass(s.status)}`}>
                      {statusLabel(s.status)}
                    </span>
                  </td>
                  <td className="px-3 py-2 text-right tabular font-medium">{formatMoney(s.amount, true)}</td>
                  <td className="px-3 py-2 text-right tabular">{formatMoney(s.grossProfit, true)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
    </section>
  )
}
