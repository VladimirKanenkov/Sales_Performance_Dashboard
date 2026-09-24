# Sales Performance Dashboard

Full-stack dashboard аналитики продаж менеджеров. Целевой viewport: **1440×900**.

## Быстрый старт

```bash
docker compose -f docker-compose.yml up --build
```

(`-f docker-compose.yml` — без `override`, чтобы не подмешивать настройки Visual Studio.)

Откройте [http://localhost:8080](http://localhost:8080).

Сервисы:
- **web** — React UI (nginx), порт `8080`
- **api** — ASP.NET Core 8, порт `5080` (также через `/api` на `8080`). Swagger: [http://localhost:5080/swagger](http://localhost:5080/swagger) (корень `:5080/` редиректит туда же)
- **db** — PostgreSQL 16, host-порт `15432` → контейнер `5432` (user/password/db: `sales` / `sales` / `sales_dashboard`). Высокий порт, чтобы не конфликтовать с локальным PostgreSQL/KOMPAS на `5432`/`5433`.

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
API слушает `http://localhost:5080` (профиль `http` в `launchSettings.json`), БД — `127.0.0.1:15432`.

Если на машине уже установлены Windows PostgreSQL / KOMPAS на `5432`/`5433`, Docker DB публикуется на **15432** (см. `docker-compose.yml`).

## Запуск в Visual Studio

Для **сдачи и демо** достаточно `docker compose up --build`. Visual Studio нужен только для локальной отладки.

### Требования

- Visual Studio 2026 (или VS 2022 17.11+) с workload’ами ASP.NET и Node.js / JavaScript
- Node.js, Docker Desktop
- Preview (если ещё не GA): `Tools > Options > Environment > Preview Features > Enable Multi Launch Profiles`
- Отладка JS: `Tools > Options > Debugging > General > Enable JavaScript debugging for ASP.NET (Chrome and Edge)`

### Как открыть и запустить

**Отладка (рекомендуется):** Api + SpaProxy

1. Откройте корневой [`SalesDashboard.sln`](SalesDashboard.sln).
2. Перед F5 поднимите только БД: `docker compose -f docker-compose.yml up db -d` (Postgres на host-порту **15432**).
3. Startup project: **SalesDashboard.Api**, профиль **http** (не https — иначе VS может открыть лишние URL).
4. F5: SpaProxy ждёт Vite, затем открывает **одно** окно. Не задавайте вручную `launchUrl` на `:5173` — браузер откроется сам после готовности SPA.
5. UI: `http://127.0.0.1:5173`. Запросы `/api` идут через Vite proxy на backend.

**Полный стек из VS (профиль Docker Compose):**

1. Остановите CLI-стек: `docker compose -f docker-compose.yml down` (иначе конфликт портов).
2. Startup project: **docker-compose**, профиль **Docker Compose** → F5 (браузер на `:8080`).
3. **Не** включайте Container Tools / Fast Mode на Api — VS отрывает `api` от Docker-сети, DNS `Host=db` падает, nginx отвечает **502**. Отладка C#: профиль **http** на `SalesDashboard.Api`.

### CORS и прокси

- Frontend использует относительные `/api/...` и **Vite proxy** → `http://localhost:5080`.
- В Development CORS на API разрешает любой origin (запасной путь, если вызывать API напрямую).

### Что отлаживается

| Компонент | F5 | Breakpoints |
| --- | --- | --- |
| Backend (C#) | Start | Visual Studio |
| Frontend (`.tsx`) | Start via `.esproj` | VS + `frontend/.vscode/launch.json` (Chrome/Edge) |
| PostgreSQL | вручную `docker compose up db -d` | — |
| Полный Docker stack | профиль **Docker Compose** (после `docker compose down`) или CLI | — |

### Fallback без Multi Launch Profiles

```bash
docker compose -f docker-compose.yml up db -d
```

В VS запустите только `SalesDashboard.Api`, в терминале — `cd frontend && npm run dev`. Отладку frontend ведите в браузере (Chrome DevTools).

### Известные ограничения

- Профиль **http** на Api — основная отладка с Vite; профиль **Docker Compose** поднимает стек без отладки внутри контейнера Api.
- CLI-демо: `docker compose -f docker-compose.yml up --build`. Не запускайте CLI и VS Compose одновременно. Containers.Tools / Fast Mode на Api ломают DNS (`Host=db`) и дают 502 на `:8080`.
- Backend-only solution [`backend/SalesDashboard.sln`](backend/SalesDashboard.sln) по-прежнему удобен для `dotnet test`.
- Один браузер при F5: Vite поднимает SpaProxy; `ProjectReference` на `.esproj` убран (иначе VS открывал второе окно через `launch.json` на `localhost`).

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
