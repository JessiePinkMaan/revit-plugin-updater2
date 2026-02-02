-- Инициализация базы данных для локальной разработки
-- Этот файл выполняется при первом запуске PostgreSQL контейнера

-- Создаем расширения если нужно
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Создаем пользователя для приложения (если не существует)
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_catalog.pg_roles WHERE rolname = 'revit_user') THEN
        CREATE USER revit_user WITH PASSWORD 'revit_password';
    END IF;
END
$$;

-- Даем права пользователю
GRANT ALL PRIVILEGES ON DATABASE revit_updater TO revit_user;
GRANT ALL ON SCHEMA public TO revit_user;

-- Создаем таблицы будут созданы автоматически через Entity Framework миграции