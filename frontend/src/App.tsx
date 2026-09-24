import { QueryClient, QueryClientProvider, useQuery } from '@tanstack/react-query'
import { useCallback, useMemo, useState } from 'react'
import { CategoriesPanel } from './components/CategoriesPanel'
import { KpiCards } from './components/KpiCards'
import { ManagerRanking } from './components/ManagerRanking'
import { PeriodSelector } from './components/PeriodSelector'
import { RecentSales } from './components/RecentSales'
import { TopProducts } from './components/TopProducts'
import { TrendChart } from './components/TrendChart'
import { api, type RankingSort } from './lib/api'
import { periodFromSearch, periodToSearch, type PeriodState } from './lib/period'

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30_000,
      // F5: браузер может открыться чуть раньше API/Vite — несколько повторов снимают пустой экран.
      retry: 4,
      retryDelay: (attempt) => Math.min(500 * 2 ** attempt, 4_000),
      refetchOnWindowFocus: false,
    },
  },
})

/**
 * Период и сортировка рейтинга, синхронизированные с query string.
 *
 * @returns Текущее состояние и функции обновления.
 */
function useUrlState() {
  const initial = useMemo(() => {
    const period = periodFromSearch(window.location.search)
    const params = new URLSearchParams(window.location.search)
    const sort = (params.get('sort') as RankingSort) || 'grossProfit'
    return { period, sort: sort === 'averageCheck' ? sort : ('grossProfit' as RankingSort) }
  }, [])

  const [period, setPeriod] = useState<PeriodState>(initial.period)
  const [sort, setSort] = useState<RankingSort>(initial.sort)

  const syncUrl = useCallback((nextPeriod: PeriodState, nextSort: RankingSort) => {
    const url = periodToSearch(nextPeriod, nextSort)
    window.history.replaceState(null, '', url)
  }, [])

  const updatePeriod = (next: PeriodState) => {
    setPeriod(next)
    syncUrl(next, sort)
  }

  const updateSort = (next: RankingSort) => {
    setSort(next)
    syncUrl(period, next)
  }

  return { period, sort, updatePeriod, updateSort }
}

function Dashboard() {
  const { period, sort, updatePeriod, updateSort } = useUrlState()
  const { from, to } = period

  const kpis = useQuery({ queryKey: ['kpis', from, to], queryFn: () => api.kpis(from, to) })
  const managers = useQuery({
    queryKey: ['managers', from, to, sort],
    queryFn: () => api.managers(from, to, sort),
  })
  const trend = useQuery({ queryKey: ['trend', from, to], queryFn: () => api.trend(from, to) })
  const categories = useQuery({
    queryKey: ['categories', from, to],
    queryFn: () => api.categories(from, to),
  })
  const products = useQuery({
    queryKey: ['products', from, to],
    queryFn: () => api.products(from, to),
  })
  const recent = useQuery({
    queryKey: ['recent', from, to],
    queryFn: () => api.recentSales(from, to),
  })

  const refreshing =
    kpis.isFetching ||
    managers.isFetching ||
    trend.isFetching ||
    categories.isFetching ||
    products.isFetching ||
    recent.isFetching

  return (
    <div className="mx-auto min-h-screen max-w-[1440px] px-6 py-5">
      <header className="mb-5 flex items-start justify-between gap-4">
        <div>
          <p className="text-xs font-semibold uppercase tracking-[0.16em] text-blue-600">Sales Performance</p>
          <h1 className="mt-1 text-2xl font-semibold tracking-tight text-slate-900">Dashboard продаж</h1>
          <p className="mt-1 text-sm text-slate-500">
            Агрегаты на сервере · период {from} → {to}
          </p>
        </div>
        <PeriodSelector period={period} onChange={updatePeriod} refreshing={refreshing && !kpis.isLoading} />
      </header>

      <div className="mb-4">
        <KpiCards
          data={kpis.data}
          isLoading={kpis.isLoading}
          isError={kpis.isError}
          error={kpis.error as Error | null}
          onRetry={() => void kpis.refetch()}
        />
      </div>

      <div className="mb-4 grid grid-cols-2 gap-4" style={{ minHeight: 380 }}>
        <ManagerRanking
          data={managers.data}
          sort={sort}
          onSortChange={updateSort}
          isLoading={managers.isLoading}
          isError={managers.isError}
          error={managers.error as Error | null}
          onRetry={() => void managers.refetch()}
        />
        <TrendChart
          data={trend.data}
          isLoading={trend.isLoading}
          isError={trend.isError}
          error={trend.error as Error | null}
          onRetry={() => void trend.refetch()}
        />
      </div>

      <div className="mb-4 grid grid-cols-2 gap-4" style={{ minHeight: 300 }}>
        <CategoriesPanel
          data={categories.data}
          isLoading={categories.isLoading}
          isError={categories.isError}
          error={categories.error as Error | null}
          onRetry={() => void categories.refetch()}
        />
        <TopProducts
          data={products.data}
          isLoading={products.isLoading}
          isError={products.isError}
          error={products.error as Error | null}
          onRetry={() => void products.refetch()}
        />
      </div>

      <RecentSales
        data={recent.data}
        isLoading={recent.isLoading}
        isError={recent.isError}
        error={recent.error as Error | null}
        onRetry={() => void recent.refetch()}
      />
    </div>
  )
}

/**
 * Корень dashboard: провайдер запросов и экран аналитики.
 */
export default function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <Dashboard />
    </QueryClientProvider>
  )
}
