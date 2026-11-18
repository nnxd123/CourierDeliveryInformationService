-- =====================================================================
-- Информационная система службы курьерской доставки
-- Скрипт создания базы данных CourierDelivery (Microsoft SQL Server)
-- =====================================================================
IF DB_ID(N'CourierDelivery') IS NULL
    CREATE DATABASE CourierDelivery COLLATE Cyrillic_General_CI_AS;
GO
USE CourierDelivery;
GO

-- 1. Создание таблицы Ролей
CREATE TABLE Roles
(
    role_id   INT IDENTITY(1,1) PRIMARY KEY,
    role_name NVARCHAR(50) NOT NULL UNIQUE
);
GO

-- 2. Создание таблицы Пользователей (пароли в учебной версии хранятся открытым текстом)
CREATE TABLE Users
(
    user_id    INT IDENTITY(1,1) PRIMARY KEY,
    login      NVARCHAR(50)  NOT NULL UNIQUE,
    password   NVARCHAR(50)  NOT NULL,
    last_name  NVARCHAR(100) NOT NULL,
    first_name NVARCHAR(100) NOT NULL,
    phone      NVARCHAR(20)  NOT NULL,
    role_id    INT NOT NULL REFERENCES Roles(role_id)
);
GO

-- 3. Создание таблицы Курьеров (профиль сотрудника с ролью «Курьер»)
CREATE TABLE Couriers
(
    courier_id INT IDENTITY(1,1) PRIMARY KEY,
    user_id    INT NOT NULL UNIQUE REFERENCES Users(user_id),
    transport  NVARCHAR(50) NOT NULL DEFAULT N'Пешком',
    is_active  BIT NOT NULL DEFAULT 1
);
GO

-- 4. Создание таблицы Зон доставки (тариф за 1 кг груза)
CREATE TABLE Zones
(
    zone_id     INT IDENTITY(1,1) PRIMARY KEY,
    name        NVARCHAR(100) NOT NULL UNIQUE,
    rate_per_kg DECIMAL(10,2) NOT NULL CHECK (rate_per_kg > 0),
    description NVARCHAR(255) NULL
);
GO

-- 5. Создание таблицы Статусов заказа
CREATE TABLE Statuses
(
    status_id INT IDENTITY(1,1) PRIMARY KEY,
    name      NVARCHAR(50) NOT NULL UNIQUE
);
GO

-- 6. Создание таблицы Заказов
CREATE TABLE Orders
(
    order_id         INT IDENTITY(1,1) PRIMARY KEY,
    order_date       DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    client_id        INT NOT NULL REFERENCES Users(user_id),
    zone_id          INT NOT NULL REFERENCES Zones(zone_id),
    status_id        INT NOT NULL REFERENCES Statuses(status_id),
    courier_id       INT NULL REFERENCES Couriers(courier_id),
    weight           DECIMAL(5,2) NOT NULL CHECK (weight > 0 AND weight <= 30),
    is_urgent        BIT NOT NULL DEFAULT 0,
    discount_percent INT NOT NULL DEFAULT 0 CHECK (discount_percent BETWEEN 0 AND 100),
    total_cost       DECIMAL(10,2) NOT NULL CHECK (total_cost >= 0),
    comment          NVARCHAR(500) NULL
);
GO
CREATE INDEX ix_orders_date ON Orders(order_date);
CREATE INDEX ix_orders_courier ON Orders(courier_id, status_id);
GO

-- 7. Создание таблицы Адресов заказа (отправление и получение)
CREATE TABLE OrderAddresses
(
    address_id    INT IDENTITY(1,1) PRIMARY KEY,
    order_id      INT NOT NULL REFERENCES Orders(order_id) ON DELETE CASCADE,
    address_type  NVARCHAR(20) NOT NULL
                  CHECK (address_type IN (N'Отправление', N'Получение')),
    address       NVARCHAR(255) NOT NULL,
    contact_name  NVARCHAR(150) NOT NULL,
    contact_phone NVARCHAR(20)  NOT NULL,
    CONSTRAINT uq_order_address UNIQUE (order_id, address_type)
);
GO

-- ============================ ДАННЫЕ ============================
INSERT INTO Roles (role_name) VALUES
(N'Администратор'), (N'Оператор'), (N'Курьер'), (N'Клиент');
GO

-- Пароль у всех тестовых учётных записей: 1234
INSERT INTO Users (login, password, last_name, first_name, phone, role_id) VALUES
('admin',    '1234', N'Колосков',  N'Андрей', '79110000001', 1),
('operator', '1234', N'Смирнова',  N'Ольга',  '79110000002', 2),
('kuzmin',   '1234', N'Кузьмин',   N'Денис',  '79110000003', 3),
('orlov',    '1234', N'Орлов',     N'Максим', '79110000004', 3),
('sidorov',  '1234', N'Сидоров',   N'Павел',  '79110000005', 3),
('ivanova',  '1234', N'Иванова',   N'Мария',  '79210000006', 4),
('petrov',   '1234', N'Петров',    N'Игорь',  '79210000007', 4),
('volkova',  '1234', N'Волкова',   N'Анна',   '79210000008', 4),
('sokolov',  '1234', N'Соколов',   N'Артём',  '79210000009', 4);
GO

INSERT INTO Couriers (user_id, transport) VALUES
(3, N'Автомобиль'), (4, N'Велосипед'), (5, N'Пешком');
GO

INSERT INTO Zones (name, rate_per_kg, description) VALUES
(N'Центральный район', 120.00, N'Адмиралтейский, Центральный и Петроградский районы'),
(N'Спальные районы',   160.00, N'Приморский, Калининский, Невский, Московский районы'),
(N'Окраины города',    210.00, N'Красносельский, Выборгский, Кировский районы'),
(N'Пригород',          300.00, N'Пушкин, Колпино, Мурино, Кудрово (до 30 км от КАД)');
GO

INSERT INTO Statuses (name) VALUES
(N'Новый'), (N'В работе'), (N'Выполнен'), (N'Отменён');
GO


-- Заказы. Даты считаются от текущей даты, чтобы демонстрационные данные
-- всегда содержали «прошлый месяц» (для правила скидки) и «сегодня».
DECLARE @pm DATETIME2 = DATEADD(MONTH, -1, DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1));
DECLARE @now DATETIME2 = SYSDATETIME();

INSERT INTO Orders (order_date, client_id, zone_id, status_id, courier_id,
                    weight, is_urgent, discount_percent, total_cost, comment) VALUES
(DATEADD(MINUTE, 1110, DATEADD(DAY, 2, @pm)), 6, 1, 3, 1, 1.50, 0, 0, 180.00, NULL),
(DATEADD(MINUTE, 1035, DATEADD(DAY, 5, @pm)), 6, 2, 3, 2, 3.00, 0, 0, 480.00, NULL),
(DATEADD(MINUTE, 615, DATEADD(DAY, 9, @pm)), 6, 1, 3, 1, 0.80, 0, 0, 96.00, NULL),
(DATEADD(MINUTE, 840, DATEADD(DAY, 13, @pm)), 6, 3, 3, 2, 2.20, 0, 0, 462.00, NULL),
(DATEADD(MINUTE, 1020, DATEADD(DAY, 18, @pm)), 6, 1, 3, 1, 4.00, 0, 0, 480.00, NULL),
(DATEADD(MINUTE, 1080, DATEADD(DAY, 24, @pm)), 6, 2, 3, 2, 1.20, 0, 0, 192.00, NULL),
(DATEADD(MINUTE, 1095, DATEADD(DAY, 7, @pm)), 7, 2, 3, 2, 5.50, 1, 0, 1320.00, NULL),
(DATEADD(MINUTE, 1005, DATEADD(DAY, 15, @pm)), 8, 4, 4, NULL, 12.00, 0, 0, 3600.00, N'Клиент отменил заявку'),
(DATEADD(MINUTE, -262, DATEADD(DAY, -2, @now)), 6, 2, 2, 3, 4.20, 1, 0, 1008.00, NULL),
(DATEADD(MINUTE, -157, DATEADD(DAY, 0, @now)), 7, 1, 2, 3, 1.00, 0, 0, 120.00, NULL),
(DATEADD(MINUTE, -71, DATEADD(DAY, -2, @now)), 8, 1, 2, 3, 2.00, 0, 0, 240.00, NULL),
(DATEADD(MINUTE, -298, DATEADD(DAY, 0, @now)), 9, 1, 2, 3, 4.20, 1, 0, 756.00, NULL),
(DATEADD(MINUTE, -259, DATEADD(DAY, -3, @now)), 6, 1, 2, 3, 2.00, 0, 0, 240.00, NULL),
(DATEADD(MINUTE, -90, DATEADD(DAY, 0, @now)), 7, 4, 2, 3, 0.50, 0, 0, 150.00, NULL),
(DATEADD(MINUTE, -114, DATEADD(DAY, 0, @now)), 8, 2, 2, 3, 0.50, 1, 0, 120.00, NULL),
(DATEADD(MINUTE, -280, DATEADD(DAY, -3, @now)), 9, 1, 2, 3, 2.00, 0, 0, 240.00, NULL),
(DATEADD(MINUTE, -69, DATEADD(DAY, 0, @now)), 6, 2, 2, 3, 2.50, 0, 0, 400.00, NULL),
(DATEADD(MINUTE, -204, DATEADD(DAY, -3, @now)), 7, 2, 2, 3, 1.00, 1, 0, 240.00, NULL),
(DATEADD(MINUTE, -263, DATEADD(DAY, -1, @now)), 7, 1, 2, 1, 2.00, 0, 0, 240.00, N'Хрупкое: стекло'),
(DATEADD(MINUTE, -168, DATEADD(DAY, -2, @now)), 9, 3, 2, 2, 7.50, 0, 0, 1575.00, NULL),
(DATEADD(MINUTE, -61, DATEADD(DAY, -1, @now)), 6, 2, 2, 2, 1.80, 0, 0, 288.00, N'Домофон не работает, звонить'),
(DATEADD(MINUTE, -175, DATEADD(DAY, 0, @now)), 8, 2, 1, NULL, 1.00, 1, 0, 240.00, N'Позвонить за час до приезда'),
(DATEADD(MINUTE, -41, DATEADD(DAY, 0, @now)), 9, 4, 1, NULL, 15.00, 0, 0, 4500.00, NULL);
GO

INSERT INTO OrderAddresses (order_id, address_type, address, contact_name, contact_phone) VALUES
(1, N'Отправление', N'Пулковское ш., д. 25', N'Иванова Мария', '79210000006'),
(1, N'Получение',   N'пр. Энгельса, д. 154', N'Никитина Дарья', '79312964541'),
(2, N'Отправление', N'г. Пушкин, Октябрьский б-р, д. 9', N'Иванова Мария', '79210000006'),
(2, N'Получение',   N'ул. Рубинштейна, д. 15', N'Фролов Сергей', '79315822307'),
(3, N'Отправление', N'Комендантский пр., д. 13', N'Иванова Мария', '79210000006'),
(3, N'Получение',   N'ул. Савушкина, д. 36', N'Лебедев Илья', '79317559047'),
(4, N'Отправление', N'г. Пушкин, Октябрьский б-р, д. 9', N'Иванова Мария', '79210000006'),
(4, N'Получение',   N'Лиговский пр., д. 50', N'Никитина Дарья', '79318536114'),
(5, N'Отправление', N'Садовая ул., д. 42', N'Иванова Мария', '79210000006'),
(5, N'Получение',   N'г. Кудрово, Европейский пр., д. 21', N'Ковалёва Ирина', '79313297239'),
(6, N'Отправление', N'ул. Дыбенко, д. 23', N'Иванова Мария', '79210000006'),
(6, N'Получение',   N'г. Кудрово, Европейский пр., д. 21', N'Ковалёва Ирина', '79317967519'),
(7, N'Отправление', N'пр. Энгельса, д. 154', N'Петров Игорь', '79210000007'),
(7, N'Получение',   N'ул. Дыбенко, д. 23', N'Фролов Сергей', '79313532032'),
(8, N'Отправление', N'Лиговский пр., д. 50', N'Волкова Анна', '79210000008'),
(8, N'Получение',   N'Московский пр., д. 171', N'Никитина Дарья', '79314891590'),
(9, N'Отправление', N'пр. Ветеранов, д. 101', N'Иванова Мария', '79210000006'),
(9, N'Получение',   N'Невский пр., д. 28', N'Ершова Вера', '79314059205'),
(10, N'Отправление', N'ул. Савушкина, д. 36', N'Петров Игорь', '79210000007'),
(10, N'Получение',   N'ул. Восстания, д. 6', N'Белова Елена', '79313444044'),
(11, N'Отправление', N'ул. Дыбенко, д. 23', N'Волкова Анна', '79210000008'),
(11, N'Получение',   N'г. Кудрово, Европейский пр., д. 21', N'Морозов Глеб', '79316345416'),
(12, N'Отправление', N'Комендантский пр., д. 13', N'Соколов Артём', '79210000009'),
(12, N'Получение',   N'г. Мурино, Охтинская аллея, д. 14', N'Белова Елена', '79318661210'),
(13, N'Отправление', N'г. Мурино, Охтинская аллея, д. 14', N'Иванова Мария', '79210000006'),
(13, N'Получение',   N'Садовая ул., д. 42', N'Лебедев Илья', '79317693754'),
(14, N'Отправление', N'Садовая ул., д. 42', N'Петров Игорь', '79210000007'),
(14, N'Получение',   N'пр. Просвещения, д. 87', N'Ершова Вера', '79317718312'),
(15, N'Отправление', N'ул. Рубинштейна, д. 15', N'Волкова Анна', '79210000008'),
(15, N'Получение',   N'пр. Ветеранов, д. 101', N'Захаров Олег', '79314502465'),
(16, N'Отправление', N'Пулковское ш., д. 25', N'Соколов Артём', '79210000009'),
(16, N'Получение',   N'ул. Типанова, д. 4', N'Захаров Олег', '79316705153'),
(17, N'Отправление', N'Гражданский пр., д. 111', N'Иванова Мария', '79210000006'),
(17, N'Получение',   N'ул. Рубинштейна, д. 15', N'Захаров Олег', '79311003913'),
(18, N'Отправление', N'г. Кудрово, Европейский пр., д. 21', N'Петров Игорь', '79210000007'),
(18, N'Получение',   N'Комендантский пр., д. 13', N'Захаров Олег', '79317100362'),
(19, N'Отправление', N'Гражданский пр., д. 111', N'Петров Игорь', '79210000007'),
(19, N'Получение',   N'Невский пр., д. 28', N'Захаров Олег', '79314488867'),
(20, N'Отправление', N'Гражданский пр., д. 111', N'Соколов Артём', '79210000009'),
(20, N'Получение',   N'Садовая ул., д. 42', N'Никитина Дарья', '79315232182'),
(21, N'Отправление', N'пр. Энгельса, д. 154', N'Иванова Мария', '79210000006'),
(21, N'Получение',   N'Садовая ул., д. 42', N'Ершова Вера', '79313060950'),
(22, N'Отправление', N'пр. Просвещения, д. 87', N'Волкова Анна', '79210000008'),
(22, N'Получение',   N'г. Колпино, ул. Труда, д. 3', N'Ершова Вера', '79319059692'),
(23, N'Отправление', N'г. Пушкин, Октябрьский б-р, д. 9', N'Соколов Артём', '79210000009'),
(23, N'Получение',   N'Большой пр. П.С., д. 64', N'Захаров Олег', '79313417890');
GO
