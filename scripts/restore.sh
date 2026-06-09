#!/bin/sh
# Restauro de backup para uma das bases de dados MySQL do DEC.
# Uso: ./restore.sh <ficheiro.sql> <base_de_dados>
# Exemplo: ./restore.sh backups/dec_educativa_2026-06-09.sql dec_educativa

set -e

FICHEIRO="$1"
BASE="$2"

if [ -z "$FICHEIRO" ] || [ -z "$BASE" ]; then
  echo "Uso: $0 <ficheiro.sql> <base_de_dados>"
  echo "  ex: $0 backups/dec_educativa_2026-06-09.sql dec_educativa"
  exit 1
fi

if [ ! -f "$FICHEIRO" ]; then
  echo "Erro: ficheiro '$FICHEIRO' não encontrado."
  exit 1
fi

# Determinar host MySQL consoante a base de dados
if [ "$BASE" = "dec_financeira" ]; then
  HOST="mysql-financeira"
else
  HOST="mysql"
fi

echo "A restaurar '$FICHEIRO' em '$BASE' (host: $HOST)..."

mysql -h "$HOST" -u root -p"${MYSQL_ROOT_PASSWORD}" "$BASE" < "$FICHEIRO"

echo "Restauro concluído."
