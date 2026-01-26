#!/bin/bash
set -e

psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" <<-EOSQL
    CREATE DATABASE keycloakdb;
    GRANT ALL PRIVILEGES ON DATABASE keycloakdb TO spotifyuser;
EOSQL
