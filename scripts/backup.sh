#!/bin/sh
# Backup automático das duas bases de dados MySQL do projeto DEC.
# Executado diariamente às 02:00 pelo container mysql-backup.
# Mantém os últimos 7 dias de backups.

set -e

BACKUP_DIR="/backups"
DATE=$(date +"%Y-%m-%d_%H-%M-%S")

mkdir -p "$BACKUP_DIR"

echo "[$(date)] A iniciar backup..."

# Backup da base de dados do portal (dec_educativa)
mysqldump \
  -h mysql \
  -u root \
  -p"${MYSQL_ROOT_PASSWORD}" \
  --single-transaction \
  --routines \
  --triggers \
  "${MYSQL_DB}" \
  > "${BACKUP_DIR}/dec_educativa_${DATE}.sql"

echo "[$(date)] dec_educativa guardado."

# Backup da base de dados da API Django (dec_financeira)
mysqldump \
  -h mysql-financeira \
  -u root \
  -p"${MYSQL_ROOT_PASSWORD}" \
  --single-transaction \
  --routines \
  --triggers \
  dec_financeira \
  > "${BACKUP_DIR}/dec_financeira_${DATE}.sql"

echo "[$(date)] dec_financeira guardado."

# Apagar backups com mais de 7 dias
find "$BACKUP_DIR" -name "*.sql" -mtime +7 -delete

echo "[$(date)] Backup concluído. Ficheiros em ${BACKUP_DIR}:"
ls -lh "$BACKUP_DIR"/*.sql 2>/dev/null || echo "(sem ficheiros)"
