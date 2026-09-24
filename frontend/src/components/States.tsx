type Props = {
  title?: string
  message: string
  onRetry?: () => void
}

/**
 * Блок ошибки запроса с кнопкой повтора.
 *
 * @param title - Заголовок.
 * @param message - Текст ошибки.
 * @param onRetry - Повторный запрос.
 */
export function ErrorState({ title = 'Не удалось загрузить', message, onRetry }: Props) {
  return (
    <div className="flex h-full min-h-28 flex-col items-center justify-center gap-2 rounded-xl border border-red-100 bg-red-50/60 px-4 py-6 text-center">
      <div className="text-sm font-semibold text-red-700">{title}</div>
      <div className="max-w-md text-xs text-red-600/90">{message}</div>
      {onRetry && (
        <button
          type="button"
          onClick={onRetry}
          className="mt-1 rounded-lg bg-red-600 px-3 py-1.5 text-xs font-medium text-white hover:bg-red-700"
        >
          Повторить
        </button>
      )}
    </div>
  )
}

/**
 * Пустое состояние, когда за период нет данных.
 *
 * @param message - Пояснение для пользователя.
 */
export function EmptyState({ message }: { message: string }) {
  return (
    <div className="flex h-full min-h-28 items-center justify-center rounded-xl border border-dashed border-slate-200 bg-slate-50 px-4 py-8 text-center text-sm text-slate-500">
      {message}
    </div>
  )
}

/**
 * Плейсхолдер на время первой загрузки.
 *
 * @param className - Высота и дополнительные классы.
 */
export function SkeletonBlock({ className = 'h-24' }: { className?: string }) {
  return <div className={`skeleton ${className}`} />
}
