-- =============================================================================
-- Cabeleila - Dados iniciais (rodar depois de schema.sql)
-- Cria o login da Leila (equipe) e um cliente de teste, ja com senha no formato
-- esperado pela API (PBKDF2-HMACSHA256, gerado por Cabeleila.Security.PasswordHasher).
-- Script idempotente: rodar de novo nao duplica nem sobrescreve senha existente.
-- =============================================================================

-- Login da equipe: leila@cabeleila.com.br / senha: Leila@123456
INSERT INTO staff_users (full_name, email, password_hash, role, active, created_at, updated_at)
VALUES (
    'Leila',
    'leila@cabeleila.com.br',
    '100000.VA6NG1ZPVbCC62dAJgM2Qg==.0t0Dm7SQdwyq+SE77twyXIurshCr/198FDrqhbj/oRI=',
    'OWNER',
    TRUE,
    now(),
    now()
)
ON CONFLICT (email) DO NOTHING;

-- Cliente de teste: cliente.teste@example.com / senha: 123456
INSERT INTO customers (full_name, email, phone, password_hash, active, created_at, updated_at)
VALUES (
    'Cliente Teste',
    'cliente.teste@example.com',
    '14999990000',
    '100000.kXCyhRotpwa7H8/NbMjw7Q==.KEUBbPDusj7SwCpOJOXmt+BciDsHygmf1vsKjutT6Kk=',
    TRUE,
    now(),
    now()
)
ON CONFLICT (email) DO NOTHING;
