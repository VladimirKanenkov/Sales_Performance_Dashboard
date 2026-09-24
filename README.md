# Sales Performance Dashboard

Full-stack dashboard аналитики продаж менеджеров. Целевой viewport: **1440×900**.

## Быстрый старт

```bash
docker compose up --build
```

Откройте [http://localhost:8080](http://localhost:8080).

Сервисы:
- **web** — React UI (nginx), порт `8080`
- **api** — ASP.NET Core 8, порт `5080` (также через `/api` на `8080`)
- **db** — PostgreSQL 16, порт `5432` (user/password/db: `sales` / `sales` / `sales_dashboard`)

При старте API автоматически:
1. ждёт готовности Postgres;
2. применяет EF Core миграции;
3. наполняет БД seed-данными, если таблица менеджеров пуста (`Random(42)`, якорь — сегодня в `Europe/Moscow`, ~12 месяцев, ~20 менеджеров, ~80 клиентов, ~50 товаров, ~3500 продаж).

## Локальная разработка

```bash
docker compose up db -d
dotnet run --project backend/src/SalesDashboard.Api
cd frontend && npm install && npm run dev
```

UI: [http://localhost:5173](http://localhost:5173) (proxy `/api` → `:5080`).

## Стек

| Слой | Технологии |
| --- | --- |
| Backend | .NET 8, Minimal APIs, EF Core 8, Npgsql |
| Frontend | React 19, TypeScript, Vite, TanStack Query, Tailwind 4, Recharts, Framer Motion |
| DB | PostgreSQL 16 |
| Infra | Docker Compose |

## Business rules

| Статус | Revenue / Cost | Количество продаж / Average Check |
| --- | --- | --- |
| **Paid** | сумма строк входит в агрегаты | учитывается |
| **Refunded** | сумма и себестоимость **вычитаются** | не учитывается |
| **Cancelled** | не участвует | не участвует |

- **Revenue** = Σ Paid − Σ Refunded  
- **Gross Profit** = Revenue − Cost  
- **Margin** = Gross Profit / Revenue (`null`, если Revenue = 0)  
- **Average Check** = Revenue / количество Paid (`null`, если Paid = 0)  
- **Предыдущий период** — интервал той же длины сразу перед текущим (`from`/`to` включительно, календарные даты).  
- **Ничья в рейтинге** — одинаковый `rank`, дальше сортировка по выручке, затем по `Id`.  
- Менеджер без продаж за период остаётся в рейтинге с нулями.

## API

| Метод | Путь | Параметры |
| --- | --- | --- |
| GET | `/api/health` | — |
| GET | `/api/analytics/kpis` | `from`, `to` |
| GET | `/api/analytics/managers` | `from`, `to`, `sort=grossProfit\|averageCheck` |
| GET | `/api/analytics/trend` | `from`, `to` (день если ≤62 дней, иначе месяц) |
| GET | `/api/analytics/categories` | `from`, `to` |
| GET | `/api/analytics/products` | `from`, `to`, `limit` |
| GET | `/api/sales/recent` | `from`, `to`, `limit` |

Пустой период → `200` и нули/пустые массивы. Невалидные даты → `400`.

## Индексы

`Sale(SaleDate)`, `Sale(ManagerId, SaleDate)`, `Sale(Status, SaleDate)`, `SaleItem(SaleId)`, `SaleItem(ProductId)`, `Product(CategoryId)`.

## Тесты

```bash
dotnet test backend/SalesDashboard.sln
cd frontend && npm test
```

## Хук AI_PROMPTS.md

`.cursor/hooks.json` → `beforeSubmitPrompt` → Git Bash `.cursor/hooks/save-prompt.sh`.  
Проверка: Hooks output в Cursor или отправка промпта >15 символов и новая запись в `AI_PROMPTS.md`.

## Что улучшили бы в production

- Материализованные дневные агрегаты / кэш Redis для KPI  
- Аутентификация и роли  
- Пагинация рейтинга и recent sales  
- Наблюдаемость (OpenTelemetry, structured logs)  
- CI с Testcontainers против реального Postgres  

## Что сознательно не делали за 8 часов

Сравнение двух менеджеров side-by-side, scatter plot, mobile layout, auth, Kubernetes.
