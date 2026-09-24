/** Значение метрики и процент изменения к предыдущему периоду. */
export type MetricValue = {
  value: number
  changePercent: number | null
}

/** Ответ GET /api/analytics/kpis. */
export type KpiResponse = {
  revenue: MetricValue
  grossProfit: MetricValue
  margin: MetricValue | null
  salesCount: MetricValue
  averageCheck: MetricValue | null
  bestManager: {
    id: number
    fullName: string
    initials: string
    avatarColor: string
    grossProfit: number
  } | null
  period: { from: string; to: string }
  previousPeriod: { from: string; to: string }
}

/** Строка рейтинга менеджеров. */
export type ManagerRankingRow = {
  rank: number
  managerId: number
  fullName: string
  team: string
  title: string
  initials: string
  avatarColor: string
  isActive: boolean
  salesCount: number
  revenue: number
  grossProfit: number
  averageCheck: number | null
  margin: number | null
  changePercent: number | null
}

/** Точка графика динамики. */
export type TrendPoint = {
  period: string
  from: string
  to: string
  revenue: number
  grossProfit: number
  salesCount: number
}

/** Агрегат по категории. */
export type CategoryStats = {
  categoryId: number
  name: string
  revenue: number
  grossProfit: number
  margin: number | null
  share: number
}

/** Товар в топе по валовой прибыли. */
export type ProductStats = {
  productId: number
  name: string
  categoryName: string
  revenue: number
  grossProfit: number
  quantity: number
}

/** Последняя продажа. Суммы уже со знаком статуса. */
export type RecentSale = {
  id: number
  saleDate: string
  status: string
  managerName: string
  managerInitials: string
  managerAvatarColor: string
  customerName: string
  customerCompany: string
  products: string[]
  amount: number
  grossProfit: number
}

/** Режим сортировки рейтинга. */
export type RankingSort = 'grossProfit' | 'averageCheck'

async function getJson<T>(url: string): Promise<T> {
  const res = await fetch(url)
  if (!res.ok) {
    const body = await res.text()
    throw new Error(body || `HTTP ${res.status}`)
  }
  return res.json() as Promise<T>
}

function qs(from: string, to: string, extra?: Record<string, string | number>) {
  const p = new URLSearchParams({ from, to })
  if (extra) {
    for (const [k, v] of Object.entries(extra)) p.set(k, String(v))
  }
  return p.toString()
}

/**
 * Клиент аналитического API. Даты в формате yyyy-MM-dd, обе границы включительны.
 */
export const api = {
  kpis: (from: string, to: string) =>
    getJson<KpiResponse>(`/api/analytics/kpis?${qs(from, to)}`),
  managers: (from: string, to: string, sort: RankingSort) =>
    getJson<ManagerRankingRow[]>(`/api/analytics/managers?${qs(from, to, { sort })}`),
  trend: (from: string, to: string) =>
    getJson<TrendPoint[]>(`/api/analytics/trend?${qs(from, to)}`),
  categories: (from: string, to: string) =>
    getJson<CategoryStats[]>(`/api/analytics/categories?${qs(from, to)}`),
  products: (from: string, to: string) =>
    getJson<ProductStats[]>(`/api/analytics/products?${qs(from, to, { limit: 8 })}`),
  recentSales: (from: string, to: string) =>
    getJson<RecentSale[]>(`/api/sales/recent?${qs(from, to, { limit: 12 })}`),
}
