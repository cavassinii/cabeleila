-- =============================================================================
-- Cabeleila - Sistema de agendamento para salao de beleza
-- Banco de dados: PostgreSQL
-- Script de criacao das tabelas (DDL)
-- =============================================================================

-- -----------------------------------------------------------------------------
-- Funcao utilitaria: mantem updated_at sempre atualizado em qualquer UPDATE
-- -----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION set_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = now();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- =============================================================================
-- customers: clientes do salao (login para agendar online)
-- =============================================================================
CREATE TABLE customers (
    id            BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    full_name     VARCHAR(150)    NOT NULL,
    email         VARCHAR(150)    NOT NULL,
    phone         VARCHAR(20)     NOT NULL,
    password_hash VARCHAR(255)    NOT NULL,
    active        BOOLEAN         NOT NULL DEFAULT TRUE,
    created_at    TIMESTAMPTZ     NOT NULL DEFAULT now(),
    updated_at    TIMESTAMPTZ     NOT NULL DEFAULT now(),
    CONSTRAINT uq_customers_email UNIQUE (email)
);

CREATE INDEX ix_customers_phone ON customers (phone);

CREATE TRIGGER trg_customers_updated_at
    BEFORE UPDATE ON customers
    FOR EACH ROW
    EXECUTE FUNCTION set_updated_at();

-- =============================================================================
-- staff_users: usuarios internos (Leila e futuros atendentes) - painel admin
-- =============================================================================
CREATE TABLE staff_users (
    id            BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    full_name     VARCHAR(150)    NOT NULL,
    email         VARCHAR(150)    NOT NULL,
    password_hash VARCHAR(255)    NOT NULL,
    role          VARCHAR(20)     NOT NULL DEFAULT 'OWNER',
    active        BOOLEAN         NOT NULL DEFAULT TRUE,
    created_at    TIMESTAMPTZ     NOT NULL DEFAULT now(),
    updated_at    TIMESTAMPTZ     NOT NULL DEFAULT now(),
    CONSTRAINT uq_staff_users_email UNIQUE (email),
    CONSTRAINT ck_staff_users_role CHECK (role IN ('OWNER', 'ATTENDANT'))
);

CREATE TRIGGER trg_staff_users_updated_at
    BEFORE UPDATE ON staff_users
    FOR EACH ROW
    EXECUTE FUNCTION set_updated_at();

-- =============================================================================
-- services: catalogo de servicos oferecidos pelo salao
-- =============================================================================
CREATE TABLE services (
    id               BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    name             VARCHAR(100)    NOT NULL,
    description      VARCHAR(500),
    duration_minutes INTEGER         NOT NULL,
    price            NUMERIC(10, 2)  NOT NULL,
    active           BOOLEAN         NOT NULL DEFAULT TRUE,
    created_at       TIMESTAMPTZ     NOT NULL DEFAULT now(),
    updated_at       TIMESTAMPTZ     NOT NULL DEFAULT now(),
    -- Duracao sempre em multiplos de 30 min, para a Leila conseguir encaixar
    -- os atendimentos sem deixar buracos na agenda.
    CONSTRAINT ck_services_duration_multiple_30 CHECK (duration_minutes > 0 AND duration_minutes % 30 = 0),
    CONSTRAINT ck_services_price_non_negative CHECK (price >= 0)
);

CREATE TRIGGER trg_services_updated_at
    BEFORE UPDATE ON services
    FOR EACH ROW
    EXECUTE FUNCTION set_updated_at();

-- =============================================================================
-- business_hours: horario de atendimento por dia da semana (1 linha por dia).
-- day_of_week segue a convencao do Postgres EXTRACT(DOW) e do C# DayOfWeek:
-- 0 = domingo ... 6 = sabado. Usada para calcular os horarios disponiveis
-- para agendamento e impedir que a Leila fique com dois clientes ao mesmo tempo.
-- =============================================================================
CREATE TABLE business_hours (
    day_of_week SMALLINT      PRIMARY KEY,
    opens_at    TIME          NOT NULL,
    closes_at   TIME          NOT NULL,
    is_closed   BOOLEAN       NOT NULL DEFAULT FALSE,
    updated_at  TIMESTAMPTZ   NOT NULL DEFAULT now(),
    CONSTRAINT ck_business_hours_day_of_week CHECK (day_of_week BETWEEN 0 AND 6),
    CONSTRAINT ck_business_hours_valid_range CHECK (is_closed OR closes_at > opens_at)
);

CREATE TRIGGER trg_business_hours_updated_at
    BEFORE UPDATE ON business_hours
    FOR EACH ROW
    EXECUTE FUNCTION set_updated_at();

-- Seed: seg-sex 09h-19h, sabado 09h-13h, domingo fechado (padrao comum de salao,
-- ajustavel depois pela propria Leila na tela de horarios).
INSERT INTO business_hours (day_of_week, opens_at, closes_at, is_closed) VALUES
    (0, '09:00', '09:00', TRUE),   -- domingo
    (1, '09:00', '19:00', FALSE),  -- segunda
    (2, '09:00', '19:00', FALSE),  -- terca
    (3, '09:00', '19:00', FALSE),  -- quarta
    (4, '09:00', '19:00', FALSE),  -- quinta
    (5, '09:00', '19:00', FALSE),  -- sexta
    (6, '09:00', '13:00', FALSE)   -- sabado
ON CONFLICT (day_of_week) DO NOTHING;

-- =============================================================================
-- appointment_status: tabela de apoio com os status possiveis
-- Reaproveitada tanto pelo agendamento (appointments) quanto pelo item (appointment_items)
-- =============================================================================
CREATE TABLE appointment_status (
    id          SMALLINT      PRIMARY KEY,
    code        VARCHAR(20)   NOT NULL,
    description VARCHAR(50)   NOT NULL,
    CONSTRAINT uq_appointment_status_code UNIQUE (code)
);

INSERT INTO appointment_status (id, code, description) VALUES
    (1, 'PENDING',     'Pendente'),
    (2, 'CONFIRMED',   'Confirmado'),
    (3, 'IN_PROGRESS', 'Em andamento'),
    (4, 'COMPLETED',   'Concluido'),
    (5, 'CANCELLED',   'Cancelado')
ON CONFLICT (id) DO NOTHING;

-- =============================================================================
-- appointments: cabecalho do agendamento (1 cliente, 1 data/hora, N servicos)
-- =============================================================================
CREATE TABLE appointments (
    id                  BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    customer_id         BIGINT        NOT NULL,
    appointment_date    DATE          NOT NULL,
    appointment_time    TIME          NOT NULL,
    status_id           SMALLINT      NOT NULL DEFAULT 1,
    notes               VARCHAR(500),
    confirmed_at        TIMESTAMPTZ,
    confirmed_by        BIGINT,
    cancelled_at        TIMESTAMPTZ,
    created_by_staff_id BIGINT,
    created_at          TIMESTAMPTZ   NOT NULL DEFAULT now(),
    updated_at          TIMESTAMPTZ   NOT NULL DEFAULT now(),
    CONSTRAINT fk_appointments_customer
        FOREIGN KEY (customer_id) REFERENCES customers (id),
    CONSTRAINT fk_appointments_status
        FOREIGN KEY (status_id) REFERENCES appointment_status (id),
    CONSTRAINT fk_appointments_confirmed_by
        FOREIGN KEY (confirmed_by) REFERENCES staff_users (id),
    CONSTRAINT fk_appointments_created_by_staff
        FOREIGN KEY (created_by_staff_id) REFERENCES staff_users (id)
);

CREATE INDEX ix_appointments_customer_id ON appointments (customer_id);
CREATE INDEX ix_appointments_date ON appointments (appointment_date);
CREATE INDEX ix_appointments_status_id ON appointments (status_id);
-- Usado para checar conflito de horario (dois agendamentos no mesmo dia/hora) rapidamente.
CREATE INDEX ix_appointments_date_time ON appointments (appointment_date, appointment_time);

CREATE TRIGGER trg_appointments_updated_at
    BEFORE UPDATE ON appointments
    FOR EACH ROW
    EXECUTE FUNCTION set_updated_at();

-- =============================================================================
-- appointment_items: servicos que compoem um agendamento (N:1 com appointments)
-- Preco e duracao sao "congelados" no momento do agendamento (snapshot),
-- para nao distorcer historico e faturamento caso o servico mude de preco depois.
-- =============================================================================
CREATE TABLE appointment_items (
    id                          BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    appointment_id              BIGINT         NOT NULL,
    service_id                  BIGINT         NOT NULL,
    status_id                   SMALLINT       NOT NULL DEFAULT 1,
    price_at_booking            NUMERIC(10, 2) NOT NULL,
    duration_minutes_at_booking INTEGER        NOT NULL,
    created_at                  TIMESTAMPTZ    NOT NULL DEFAULT now(),
    updated_at                  TIMESTAMPTZ    NOT NULL DEFAULT now(),
    CONSTRAINT fk_appointment_items_appointment
        FOREIGN KEY (appointment_id) REFERENCES appointments (id) ON DELETE CASCADE,
    CONSTRAINT fk_appointment_items_service
        FOREIGN KEY (service_id) REFERENCES services (id),
    CONSTRAINT fk_appointment_items_status
        FOREIGN KEY (status_id) REFERENCES appointment_status (id),
    CONSTRAINT ck_appointment_items_price_non_negative CHECK (price_at_booking >= 0),
    CONSTRAINT ck_appointment_items_duration_multiple_30 CHECK (duration_minutes_at_booking > 0 AND duration_minutes_at_booking % 30 = 0)
);

CREATE INDEX ix_appointment_items_appointment_id ON appointment_items (appointment_id);
CREATE INDEX ix_appointment_items_service_id ON appointment_items (service_id);
CREATE INDEX ix_appointment_items_status_id ON appointment_items (status_id);

CREATE TRIGGER trg_appointment_items_updated_at
    BEFORE UPDATE ON appointment_items
    FOR EACH ROW
    EXECUTE FUNCTION set_updated_at();

-- =============================================================================
-- appointment_history: auditoria de alteracoes (rastreabilidade)
-- Registra quem alterou (cliente pelo site ou funcionario pelo telefone),
-- o que mudou e quando - essencial para validar a regra dos 2 dias.
-- =============================================================================
CREATE TABLE appointment_history (
    id                      BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    appointment_id          BIGINT       NOT NULL,
    changed_by_customer_id  BIGINT,
    changed_by_staff_id     BIGINT,
    change_type             VARCHAR(20)  NOT NULL,
    old_value                JSONB,
    new_value                JSONB,
    changed_at              TIMESTAMPTZ  NOT NULL DEFAULT now(),
    CONSTRAINT fk_appointment_history_appointment
        FOREIGN KEY (appointment_id) REFERENCES appointments (id) ON DELETE CASCADE,
    CONSTRAINT fk_appointment_history_customer
        FOREIGN KEY (changed_by_customer_id) REFERENCES customers (id),
    CONSTRAINT fk_appointment_history_staff
        FOREIGN KEY (changed_by_staff_id) REFERENCES staff_users (id),
    CONSTRAINT ck_appointment_history_change_type
        CHECK (change_type IN ('CREATED', 'RESCHEDULE', 'STATUS_CHANGE', 'CANCELLATION', 'ITEMS_CHANGED')),
    CONSTRAINT ck_appointment_history_changed_by
        CHECK (changed_by_customer_id IS NOT NULL OR changed_by_staff_id IS NOT NULL)
);

CREATE INDEX ix_appointment_history_appointment_id ON appointment_history (appointment_id);
