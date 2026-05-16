-- Criar a base de dados
CREATE DATABASE IF NOT EXISTS dec_financeira
CHARACTER SET utf8mb4
COLLATE utf8mb4_unicode_ci;

USE dec_financeira;

-- Tabela de lançamentos financeiros
CREATE TABLE IF NOT EXISTS entries (
    id              INT AUTO_INCREMENT PRIMARY KEY,
    scenario_id     INT NOT NULL,
    type            ENUM('rendimento', 'despesa') NOT NULL,
    category        VARCHAR(100) NOT NULL,
    amount          DECIMAL(10, 2) NOT NULL,
    month           TINYINT NOT NULL CHECK (month BETWEEN 1 AND 12),
    year            YEAR NOT NULL,
    recurrence      ENUM('mensal', 'anual', 'unica') NOT NULL DEFAULT 'mensal',
    created_at      TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Tabela de objetivos de poupança
CREATE TABLE IF NOT EXISTS objectives (
    id              INT AUTO_INCREMENT PRIMARY KEY,
    scenario_id     INT NOT NULL,
    description     VARCHAR(255) NOT NULL,
    targetvalue     DECIMAL(10, 2) NOT NULL,
    termmonths      INT NOT NULL,
    current_value   DECIMAL(10, 2) NOT NULL DEFAULT 0.00,
    created_at      TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Tabela de histórico de simulações
CREATE TABLE IF NOT EXISTS simulationshistory (
    id              INT AUTO_INCREMENT PRIMARY KEY,
    scenario_id     INT NOT NULL,
    executiondate   TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    jsonresults     JSON NOT NULL,
    simulation_type ENUM('conservadora', 'moderada', 'agressiva') NOT NULL
);