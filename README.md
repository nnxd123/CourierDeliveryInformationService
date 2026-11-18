# Информационная система службы курьерской доставки «КурьерЭкспресс»

Курсовой проект по МДК.01.01 «Разработка программных модулей».
Тема: **Разработка информационной системы для службы курьерской доставки**.

## Описание

Десктопное приложение для учёта заказов курьерской службы: расчёт стоимости по тарифу зоны и весу,
скидка постоянным клиентам, назначение курьеров с контролем загрузки, статусы заказов и разграничение прав.

### Основные функции
1. **Вход по логину и паролю**, регистрация клиента. Роли: Администратор, Оператор, Курьер, Клиент, Гость.
2. **Тарифы и калькулятор** — стоимость = тариф зоны × вес × k (срочно — 1,5).
3. **Скидка 10 %**, если у клиента за прошлый месяц выполнено более 5 заказов.
4. **Заказы** — совместный поиск по номеру и ФИО клиента, фильтр по статусу, сортировка по дате, цветные статусы.
5. **Назначение курьера** — в списке только курьеры с загрузкой менее 10 активных заказов (транзакция Serializable).
6. **Администрирование** — зоны и тарифы, создание сотрудников, удаление заказов.

## Технологии
* C# (.NET 10), Avalonia UI 12
* Microsoft SQL Server, Entity Framework Core (Database First)
* Git & GitHub

## Запуск
1. Выполните в SSMS скрипт `database/database_script.sql` (создаст базу `CourierDelivery`).
2. Укажите свой сервер в `src/CourierDelivery/connection.txt`.
3. Соберите и запустите `src/CourierDelivery/CourierDelivery.csproj`.

Тестовые учётные записи (пароль у всех `1234`, без хеширования):
`admin` (администратор), `operator` (оператор), `kuzmin`, `orlov`, `sidorov` (курьеры),
`ivanova`, `petrov`, `volkova`, `sokolov` (клиенты).

## Структура
```text
src/CourierDelivery/
├── Assets/          # logo.png, courier.ico
├── Data/            # Db.cs (фабрика подключений), DeliveryContext.cs (EF Core)
├── Models/          # Role, User, Courier, Zone, Status, Order, OrderAddress + ModelExtras
├── Services/        # Session, PriceRules, OrderService, StaffService, ZoneService, AppException
├── Views/           # MainWindow и 9 страниц: Tariffs, Login, Registration, Orders, OrderCreate,
│                    # OrderDetails, Couriers, Zones, Staff; UiSupport
├── App.axaml        # Цветовая схема и стили
└── connection.txt   # Строка подключения
database/database_script.sql   # Структура БД и тестовые данные
docs/                          # Пояснительная записка и презентация
```
