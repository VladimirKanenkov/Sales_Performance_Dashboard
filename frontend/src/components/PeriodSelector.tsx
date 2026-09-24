import type { PeriodPreset, PeriodState } from '../lib/period'
import { PRESET_LABELS, resolvePreset } from '../lib/period'

const presets: PeriodPreset[] = ['today', '7d', '30d', 'thisMonth', 'lastMonth']

type Props = {
  period: PeriodState
  onChange: (next: PeriodState) => void
  refreshing?: boolean
}

/**
 * Пресеты периода и произвольный диапазон дат.
 *
 * @param period - Текущий период.
 * @param onChange - Новый период.
 * @param refreshing - Идёт повторный запрос без сброса карточек.
 */
export function PeriodSelector({ period, onChange, refreshing }: Props) {
  return (
    <div className="flex flex-wrap items-center gap-3">
      <div className="flex flex-wrap gap-1.5 rounded-xl bg-slate-100/80 p-1">
        {presets.map((p) => {
          const active = period.preset === p
          return (
            <button
              key={p}
              type="button"
              onClick={() => onChange(resolvePreset(p))}
              className={`rounded-lg px-3 py-1.5 text-sm font-medium transition ${
                active
                  ? 'bg-white text-slate-900 shadow-sm'
                  : 'text-slate-600 hover:text-slate-900'
              }`}
            >
              {PRESET_LABELS[p]}
            </button>
          )
        })}
      </div>

      <div className="flex items-center gap-2 rounded-xl border border-slate-200 bg-white px-3 py-1.5">
        <label className="text-xs text-slate-500">с</label>
        <input
          type="date"
          value={period.from}
          onChange={(e) =>
            onChange({
              preset: 'custom',
              from: e.target.value,
              to: period.to < e.target.value ? e.target.value : period.to,
            })
          }
          className="border-0 bg-transparent text-sm outline-none"
        />
        <label className="text-xs text-slate-500">по</label>
        <input
          type="date"
          value={period.to}
          onChange={(e) =>
            onChange({
              preset: 'custom',
              from: period.from > e.target.value ? e.target.value : period.from,
              to: e.target.value,
            })
          }
          className="border-0 bg-transparent text-sm outline-none"
        />
      </div>

      {refreshing && (
        <span className="text-xs font-medium text-blue-600 animate-pulse">Обновление…</span>
      )}
    </div>
  )
}
