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
