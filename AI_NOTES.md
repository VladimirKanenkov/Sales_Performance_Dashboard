# AI Notes — рефлексия

## Модели и инструменты

Cursor Agent (Composer) + локальные CLI: .NET 8 SDK, Docker, Node 24, Git Bash. Хук `beforeSubmitPrompt` пишет промпты в `AI_PROMPTS.md` через Git Bash + Python (без `jq`).

## Что делегировано AI

Каркас solution/API, seed-генератор, аналитические агрегаты, React dashboard (KPI, рейтинг, графики), Docker Compose, тесты KPI/рейтинга/периода, документация README.

## Что спроектировано самостоятельно

Бизнес-правила Paid/Refunded/Cancelled, формула предыдущего периода, выбор стека (TanStack Query + Tailwind + Recharts + Framer Motion без глобального store), контракт REST, индексы PostgreSQL, отказ от Clean Architecture/MediatR для одного bounded context. Правила коммитов (Conventional Commits на русском, атомарные коммиты по плану) и комментариев (XML-doc / JSDoc на русском) заданы самостоятельно и сохранены в `.cursor/rules/commits-and-comments.mdc`. Переписывание сообщений существующих коммитов и XML-doc/JSDoc в коде сделаны по запросу; тексты комментариев предложил AI по этим правилам.

## Где AI ускорил работу

Быстрый каркас EF + seed на ~3500 продаж, UI-блоки со skeleton/error/empty, compose с healthcheck и автомиграцией.

## Где AI ошибся / что отклонили

Сломанный `bash` в PATH (WSL stub) — хук явно вызывает Git Bash. Черновик KPI-теста с двойным Paid ломал ожидания Refund — исправлено фикстурой. `.slnx` от нового SDK заменён на классический `.sln`.

## Как проверяли итоговый код

`dotnet test` (KPI, период, рейтинг/ничья, DateRange), `npm test` (пресеты, сортировка рейтинга, error/empty), `npm run build`, `docker compose up --build` и проверка `/api/health` + UI на `:8080`.

## Решение по отладке в Visual Studio

Выбран **подход A + SpaProxy**: корневой `SalesDashboard.sln`, `frontend.esproj`, F5 на `SalesDashboard.Api` поднимает Vite через `Microsoft.AspNetCore.SpaProxy` (Multi Launch Start для `.esproj` на этой машине не стартовал frontend). Порт API `5080`, Vite `host: 127.0.0.1:5173`. Host-порт Docker Postgres **15432** — на `5432`/`5433` уже слушали локальный PostgreSQL 16/12 и KOMPAS. CLI-демо: `docker compose -f docker-compose.yml up --build` (без override).

Swagger в Docker был недоступен: `UseSwagger` только при `IsDevelopment()`, а compose задаёт `Production`; корень `:5080/` давал 404. Включены Swagger/SwaggerUI всегда + редирект `/` → `/swagger`. Проверка: `:5080/swagger` → 200.

### Ошибка VS «Не удается найти контейнер для службы api»

Воспроизводилось при F5 профиля **Docker Compose**, пока в терминале уже был CLI-стек на тех же портах. Runtime: порты заняты `sales_performance_dashboard-*`, контейнеров VS-проекта (`dockercompose*`) нет; повторный bind на `5080` зависал. Дополнительно не хватало Containers.Tools / `DockerComposeProjectPath` / `docker-compose.override.yml`. Исправление: связать Api с dcproj, override с метками VS, Build.0 в `.sln`; перед F5 Compose — `docker compose down`.

После освобождения портов VS поднял Fast Mode (`vs.debug.g.yml`): у `api` пустые `Networks`, DNS `Host=db` → Npgsql/`System.Net.Dns`, nginx **502**. `ContainerDevelopmentMode=Regular` не помог (кэш остался Fast). Откат: убраны Containers.Tools / `DockerComposeProjectPath` / override с метками VS — полный стек без debuggee в контейнере; отладка C# — профиль http. Переменный `proxy_pass` в nginx давал 404 на `/api/*` — возвращён статический `http://api:8080/api/`. CLI-стек после фикса: `/api/health` → 200.
