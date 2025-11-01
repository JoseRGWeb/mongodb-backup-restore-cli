#!/bin/bash

# Script de demostración del comando backup
# Este script muestra los diferentes escenarios de uso de la CLI

echo "=== Demo de MongoDB Backup & Restore CLI ==="
echo ""

# Comando base
CLI_CMD="dotnet run --project src/MongoBackupRestore.Cli/MongoBackupRestore.Cli.csproj --"

echo "1. Mostrar ayuda general:"
$CLI_CMD --help
echo ""
echo "---"
echo ""

echo "2. Mostrar ayuda del comando backup:"
$CLI_CMD backup --help
echo ""
echo "---"
echo ""

echo "3. Intentar backup sin mongodump (debe fallar con mensaje amigable):"
$CLI_CMD backup --db testdb --out /tmp/backup-test
echo ""
echo "Código de salida: $?"
echo ""
echo "---"
echo ""

echo "4. Validar opciones requeridas (debe fallar):"
$CLI_CMD backup --db testdb
echo ""
echo "Código de salida: $?"
echo ""
echo "---"
echo ""

echo "5. Modo Docker sin nombre de contenedor (debe fallar):"
$CLI_CMD backup --db testdb --out /tmp/backup-test --in-docker
echo ""
echo "Código de salida: $?"
echo ""
echo "---"
echo ""

echo "=== Demo completado ==="
echo ""
echo "Para usar la CLI en un entorno real:"
echo "1. Instale MongoDB Database Tools desde: https://www.mongodb.com/try/download/database-tools"
echo "2. Ejecute: dotnet run --project src/MongoBackupRestore.Cli/MongoBackupRestore.Cli.csproj -- backup --db MiBaseDeDatos --out ./backup"
