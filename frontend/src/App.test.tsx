import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { PeriodSelector } from '../src/components/PeriodSelector'
import { ManagerRanking } from '../src/components/ManagerRanking'
import { ErrorState, EmptyState } from '../src/components/States'
import { resolvePreset } from '../src/lib/period'
import type { ManagerRankingRow } from '../src/lib/api'

describe('period presets', () => {
  it('resolves 7d as inclusive 7-day window', () => {
    const p = resolvePreset('7d')
    const from = new Date(p.from)
    const to = new Date(p.to)
    const days = Math.round((to.getTime() - from.getTime()) / 86400000) + 1
    expect(days).toBe(7)
    expect(p.preset).toBe('7d')
  })
})

describe('PeriodSelector', () => {
  it('switches preset on click', async () => {
    const user = userEvent.setup()
    const onChange = vi.fn()
    render(<PeriodSelector period={resolvePreset('30d')} onChange={onChange} />)
    await user.click(screen.getByRole('button', { name: '7 дней' }))
    expect(onChange).toHaveBeenCalled()
    expect(onChange.mock.calls[0][0].preset).toBe('7d')
  })
})

describe('ManagerRanking', () => {
  const rows: ManagerRankingRow[] = [
    {
      rank: 1,
      managerId: 1,
      fullName: 'Иван Тестов',
      team: 'Север',
      title: 'Менеджер',
      initials: 'ИТ',
      avatarColor: '#2563eb',
      isActive: true,
      salesCount: 10,
      revenue: 100000,
      grossProfit: 30000,
      averageCheck: 10000,
      margin: 0.3,
      changePercent: 12.5,
    },
  ]

  it('toggles sort mode', async () => {
    const user = userEvent.setup()
    const onSort = vi.fn()
    render(
      <ManagerRanking
        data={rows}
        sort="grossProfit"
        onSortChange={onSort}
        isLoading={false}
        isError={false}
        onRetry={() => undefined}
      />,
    )
    await user.click(screen.getByRole('button', { name: 'Средний чек' }))
    expect(onSort).toHaveBeenCalledWith('averageCheck')
  })
})

describe('states', () => {
  it('renders error with retry', async () => {
    const user = userEvent.setup()
    const onRetry = vi.fn()
    render(<ErrorState message="boom" onRetry={onRetry} />)
    expect(screen.getByText('boom')).toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: 'Повторить' }))
    expect(onRetry).toHaveBeenCalled()
  })

  it('renders empty state', () => {
    render(<EmptyState message="пусто" />)
    expect(screen.getByText('пусто')).toBeInTheDocument()
  })
})
