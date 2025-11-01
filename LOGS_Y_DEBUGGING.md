# Guía de Logs y Debugging

Esta guía proporciona información detallada sobre el sistema de logging de MongoDB Backup & Restore CLI y técnicas de depuración.

## Tabla de Contenidos

- [Sistema de Logging](#sistema-de-logging)
- [Niveles de Log](#niveles-de-log)
- [Configuración de Logs](#configuración-de-logs)
- [Análisis de Logs](#análisis-de-logs)
- [Debugging Avanzado](#debugging-avanzado)
- [Errores Comunes](#errores-comunes)
- [Mejores Prácticas](#mejores-prácticas)

---

## Sistema de Logging

MongoDB Backup & Restore CLI utiliza un sistema de logging estructurado que proporciona:

- **Logs a consola**: Salida en tiempo real durante la ejecución
- **Logs a archivo**: Almacenamiento persistente para auditoría
- **Niveles configurables**: Control fino del detalle de información
- **Formato estructurado**: Facilita el análisis y búsqueda

### Componentes Principales

```
┌─────────────────┐
│   Aplicación    │
└────────┬────────┘
         │
    ┌────▼─────┐
    │  Logger  │
    └────┬─────┘
         │
    ┌────▼──────────────┐
    │   Serilog/ILogger │
    └────┬──────────────┘
         │
    ┌────▼────┐  ┌──────▼──────┐
    │ Consola │  │   Archivo   │
    └─────────┘  └─────────────┘
```

---

## Niveles de Log

### Descripción de Niveles

| Nivel | Código | Descripción | Cuándo Usar |
|-------|--------|-------------|-------------|
| **Trace** | 0 | Información muy detallada | Desarrollo interno |
| **Debug** | 1 | Información de depuración | Troubleshooting |
| **Information** | 2 | Información general | Operación normal |
| **Warning** | 3 | Advertencias no críticas | Problemas potenciales |
| **Error** | 4 | Errores que impiden operación | Fallos operacionales |
| **Critical** | 5 | Errores críticos del sistema | Fallos catastróficos |

### Ejemplos de Mensajes por Nivel

#### Trace
```
[2025-11-01 14:30:52.123 TRC] Iniciando validación de parámetros...
[2025-11-01 14:30:52.125 TRC] Parámetro --db validado: MiBaseDatos
[2025-11-01 14:30:52.127 TRC] Parámetro --out validado: ./backups
```

#### Debug
```
[2025-11-01 14:30:52.200 DBG] Detectando contenedores Docker...
[2025-11-01 14:30:52.350 DBG] Contenedores encontrados: mi-mongodb, mongo-test
[2025-11-01 14:30:52.400 DBG] Ejecutando comando: docker exec mi-mongodb mongodump --version
```

#### Information
```
[2025-11-01 14:30:52.500 INF] Iniciando backup de la base de datos: MiBaseDatos
[2025-11-01 14:30:55.000 INF] Backup completado exitosamente
[2025-11-01 14:30:55.100 INF] Tamaño del backup: 45.3 MB
```

#### Warning
```
[2025-11-01 14:30:53.000 WRN] La base de datos está vacía
[2025-11-01 14:30:53.100 WRN] Espacio en disco bajo: 2 GB disponibles
[2025-11-01 14:30:53.200 WRN] No se encontró archivo de configuración, usando valores por defecto
```

#### Error
```
[2025-11-01 14:30:52.000 ERR] Error de conexión: No se pudo conectar a MongoDB en localhost:27017
[2025-11-01 14:30:52.100 ERR] mongodump no está disponible en el PATH del sistema
[2025-11-01 14:30:52.200 ERR] Clave de cifrado incorrecta o archivo corrupto
```

#### Critical
```
[2025-11-01 14:30:52.000 CRT] Error fatal: Sin espacio en disco
[2025-11-01 14:30:52.100 CRT] Error crítico de sistema: StackOverflowException
```

---

## Configuración de Logs

### Método 1: Parámetro de Línea de Comandos

```bash
# Modo verbose (Debug)
mongodb-br backup --db MiBaseDatos --out ./backups --verbose

# Guardar logs en archivo
mongodb-br backup --db MiBaseDatos --out ./backups --log-file ./logs/backup.log

# Combinar ambos
mongodb-br backup --db MiBaseDatos --out ./backups --verbose --log-file ./logs/backup.log
```

### Método 2: Variables de Entorno

```bash
# Configurar nivel de log
export MONGO_LOG_LEVEL=debug
mongodb-br backup --db MiBaseDatos --out ./backups

# Configurar archivo de log
export MONGO_LOG_FILE=/var/log/mongodb-backup/backup.log
mongodb-br backup --db MiBaseDatos --out ./backups

# Configurar ambos
export MONGO_LOG_LEVEL=debug
export MONGO_LOG_FILE=/var/log/mongodb-backup/backup.log
mongodb-br backup --db MiBaseDatos --out ./backups
```

### Método 3: Archivo de Configuración

```bash
# Crear archivo .mongodb-backup.env
cat > ~/.mongodb-backup.env << 'EOF'
export MONGO_LOG_LEVEL=info
export MONGO_LOG_FILE=/var/log/mongodb-backup/backup.log
EOF

# Cargar y usar
source ~/.mongodb-backup.env
mongodb-br backup --db MiBaseDatos --out ./backups
```

---

## Análisis de Logs

### Ver Logs en Tiempo Real

```bash
# Seguir logs en tiempo real (Linux/Mac)
tail -f /var/log/mongodb-backup/backup.log

# Windows PowerShell
Get-Content -Path "C:\Logs\mongodb-backup.log" -Wait -Tail 50
```

### Filtrar Logs por Nivel

```bash
# Solo errores y críticos
grep -E "\[(ERR|CRT)\]" /var/log/mongodb-backup/backup.log

# Solo warnings y superiores
grep -E "\[(WRN|ERR|CRT)\]" /var/log/mongodb-backup/backup.log

# Contar errores
grep "\[ERR\]" /var/log/mongodb-backup/backup.log | wc -l
```

### Buscar Información Específica

```bash
# Buscar por base de datos
grep "MiBaseDatos" /var/log/mongodb-backup/backup.log

# Buscar por contenedor Docker
grep "mi-mongodb" /var/log/mongodb-backup/backup.log

# Buscar operaciones de cifrado
grep -i "encrypt\|decrypt" /var/log/mongodb-backup/backup.log

# Buscar por fecha/hora específica
grep "2025-11-01 14:30" /var/log/mongodb-backup/backup.log
```

### Analizar Rendimiento

```bash
# Extraer tiempos de ejecución
grep "completado\|Duration\|elapsed" /var/log/mongodb-backup/backup.log

# Analizar tamaños de backup
grep -i "tamaño\|size\|bytes" /var/log/mongodb-backup/backup.log
```

---

## Debugging Avanzado

### Activar Modo Debug Completo

```bash
# Máximo nivel de detalle
export MONGO_LOG_LEVEL=trace
mongodb-br backup \
  --db MiBaseDatos \
  --out ./backups \
  --verbose \
  --log-file ./debug-$(date +%Y%m%d-%H%M%S).log
```

### Depurar Problemas de Conexión

```bash
# 1. Verificar conectividad básica
mongosh --host localhost --port 27017 --eval "db.adminCommand('ping')"

# 2. Ejecutar backup con verbose
mongodb-br backup \
  --db MiBaseDatos \
  --host localhost \
  --port 27017 \
  --out ./backups \
  --verbose

# 3. Analizar logs de conexión
grep -i "connect\|connection\|timeout" ./backup.log
```

### Depurar Problemas con Docker

```bash
# 1. Verificar contenedores
docker ps -a

# 2. Listar contenedores MongoDB específicamente
docker ps --filter "ancestor=mongo"

# 3. Verificar logs del contenedor
docker logs mi-mongodb

# 4. Ejecutar backup con debug
export MONGO_LOG_LEVEL=debug
mongodb-br backup \
  --db MiBaseDatos \
  --in-docker \
  --container-name mi-mongodb \
  --out ./backups \
  --verbose

# 5. Verificar binarios en contenedor
docker exec mi-mongodb which mongodump
docker exec mi-mongodb mongodump --version
```

### Depurar Problemas de Cifrado

```bash
# 1. Verificar longitud de clave
echo -n "$MONGO_ENCRYPTION_KEY" | wc -c
# Debe ser >= 16 caracteres

# 2. Backup con debug de cifrado
mongodb-br backup \
  --db MiBaseDatos \
  --out ./backups \
  --encrypt \
  --encryption-key "TestKey123456789" \
  --verbose \
  --log-file ./encryption-debug.log

# 3. Analizar logs de cifrado
grep -i "encrypt\|decrypt\|aes\|cipher" ./encryption-debug.log

# 4. Intentar descifrar con verbose
mongodb-br restore \
  --db MiBaseDatos_Test \
  --from ./backups/backup.encrypted \
  --encryption-key "TestKey123456789" \
  --verbose
```

### Depurar Problemas de Compresión

```bash
# 1. Verificar herramientas de compresión
which zip
which tar
which gzip

# 2. Backup con debug de compresión
mongodb-br backup \
  --db MiBaseDatos \
  --out ./backups \
  --compress zip \
  --verbose \
  --log-file ./compression-debug.log

# 3. Verificar archivo comprimido
file ./backups/backup.zip
unzip -t ./backups/backup.zip # Verificar integridad
```

---

## Errores Comunes

### Error 1: "mongodump no encontrado"

**Log**:
```
[ERR] mongodump no está disponible en el PATH del sistema
```

**Causa**: MongoDB Database Tools no instaladas o no en PATH

**Solución**:
```bash
# Verificar instalación
which mongodump

# Instalar MongoDB Database Tools
# Ver: https://www.mongodb.com/try/download/database-tools

# Agregar al PATH (Linux/Mac)
export PATH=$PATH:/path/to/mongodb-database-tools/bin

# Verificar
mongodump --version
```

---

### Error 2: "Error de conexión"

**Log**:
```
[ERR] Error de conexión: No se pudo conectar al servidor MongoDB en localhost:27017
```

**Debugging**:
```bash
# 1. Verificar que MongoDB está corriendo
systemctl status mongod  # Linux
# o
docker ps | grep mongo   # Docker

# 2. Verificar puerto
netstat -tuln | grep 27017
# o
lsof -i :27017

# 3. Probar conexión manual
mongosh --host localhost --port 27017

# 4. Verificar logs de MongoDB
tail -f /var/log/mongodb/mongod.log  # Linux
docker logs mi-mongodb               # Docker
```

---

### Error 3: "Contenedor Docker no encontrado"

**Log**:
```
[ERR] El contenedor 'mi-mongodb' no existe
```

**Debugging**:
```bash
# 1. Listar todos los contenedores
docker ps -a

# 2. Listar solo contenedores MongoDB
docker ps --filter "ancestor=mongo"

# 3. Ver logs del contenedor
docker logs <container-name>

# 4. Iniciar contenedor si está detenido
docker start <container-name>

# 5. Verificar con auto-detección
mongodb-br backup --db MiBaseDatos --in-docker --out ./backups --verbose
```

---

### Error 4: "Clave de cifrado incorrecta"

**Log**:
```
[ERR] Error al descifrar: la clave de cifrado es incorrecta o el archivo está corrupto
```

**Debugging**:
```bash
# 1. Verificar longitud de clave
echo -n "$MONGO_ENCRYPTION_KEY" | wc -c

# 2. Verificar caracteres especiales
echo "$MONGO_ENCRYPTION_KEY" | od -c

# 3. Intentar con clave en archivo
echo "MiClaveSegura123456" > /tmp/key.txt
export MONGO_ENCRYPTION_KEY=$(cat /tmp/key.txt)

# 4. Verificar encabezado del archivo
head -c 100 backup.encrypted | xxd
# Debe comenzar con "MONGOBR-AES256"
```

---

### Error 5: "Sin espacio en disco"

**Log**:
```
[ERR] Error al escribir backup: Sin espacio disponible en disco
```

**Debugging**:
```bash
# 1. Verificar espacio disponible
df -h /backups

# 2. Verificar inodos (Linux)
df -i /backups

# 3. Limpiar espacio
# Eliminar backups antiguos
find /backups -name "backup-*" -mtime +30 -delete

# 4. Usar compresión
mongodb-br backup --db MiBaseDatos --out ./backups --compress zip

# 5. Cambiar ubicación
mongodb-br backup --db MiBaseDatos --out /mnt/large-disk/backups
```

---

## Mejores Prácticas

### 1. Logging en Producción

```bash
# Configuración recomendada para producción
export MONGO_LOG_LEVEL=info
export MONGO_LOG_FILE=/var/log/mongodb-backup/backup-$(date +%Y%m%d).log

# Rotación de logs
# Crear /etc/logrotate.d/mongodb-backup
cat > /etc/logrotate.d/mongodb-backup << 'EOF'
/var/log/mongodb-backup/*.log {
    daily
    rotate 30
    compress
    delaycompress
    notifempty
    create 0640 backup backup
    sharedscripts
    postrotate
        # Opcional: notificar a la aplicación
    endscript
}
EOF
```

### 2. Logging en Desarrollo

```bash
# Máximo detalle para desarrollo
export MONGO_LOG_LEVEL=debug
export MONGO_LOG_FILE=./dev-logs/backup-$(date +%Y%m%d-%H%M%S).log

mongodb-br backup --db DevDB --out ./backups --verbose
```

### 3. Análisis de Logs Programático

```bash
# Script para analizar logs y generar reporte
#!/bin/bash

LOG_FILE="/var/log/mongodb-backup/backup.log"
REPORT_FILE="/tmp/backup-report-$(date +%Y%m%d).txt"

cat > "$REPORT_FILE" << EOF
=== Reporte de Logs MongoDB Backup ===
Generado: $(date)
Archivo: $LOG_FILE

=== Resumen ===
Total de líneas: $(wc -l < "$LOG_FILE")
Errores: $(grep -c "\[ERR\]" "$LOG_FILE")
Warnings: $(grep -c "\[WRN\]" "$LOG_FILE")
Info: $(grep -c "\[INF\]" "$LOG_FILE")

=== Últimos 10 Errores ===
$(grep "\[ERR\]" "$LOG_FILE" | tail -10)

=== Últimos 10 Warnings ===
$(grep "\[WRN\]" "$LOG_FILE" | tail -10)

=== Backups Exitosos (últimos 5) ===
$(grep "completado exitosamente" "$LOG_FILE" | tail -5)

EOF

echo "Reporte generado: $REPORT_FILE"
cat "$REPORT_FILE"
```

### 4. Monitoreo con Alertas

```bash
# Script para monitorear logs y alertar
#!/bin/bash

LOG_FILE="/var/log/mongodb-backup/backup.log"
SLACK_WEBHOOK="https://hooks.slack.com/services/YOUR/WEBHOOK"

# Buscar errores en los últimos 5 minutos
RECENT_ERRORS=$(find "$LOG_FILE" -mmin -5 -exec grep "\[ERR\]" {} \; | wc -l)

if [ "$RECENT_ERRORS" -gt 0 ]; then
    MESSAGE="⚠️ $RECENT_ERRORS error(es) detectado(s) en MongoDB Backup"
    curl -X POST "$SLACK_WEBHOOK" \
        -H 'Content-Type: application/json' \
        -d "{\"text\":\"$MESSAGE\"}"
fi
```

### 5. Debugging Sistemático

```bash
# Checklist de debugging
#!/bin/bash

echo "=== MongoDB Backup Debugging Checklist ==="

# 1. Verificar instalación
echo "1. Verificando instalación de CLI..."
mongodb-br --version || echo "❌ CLI no instalada"

# 2. Verificar MongoDB
echo "2. Verificando MongoDB..."
mongosh --eval "db.adminCommand('ping')" || echo "❌ MongoDB no disponible"

# 3. Verificar mongodump
echo "3. Verificando mongodump..."
mongodump --version || echo "❌ mongodump no disponible"

# 4. Verificar Docker (si aplica)
echo "4. Verificando Docker..."
docker ps || echo "❌ Docker no disponible"

# 5. Verificar espacio en disco
echo "5. Verificando espacio en disco..."
df -h /backups

# 6. Verificar permisos
echo "6. Verificando permisos..."
ls -ld /backups

# 7. Test de backup básico
echo "7. Test de backup básico..."
mongodb-br backup --db test --out /tmp/test-backup --verbose
```

---

## Recursos Adicionales

- [Documentación Principal](./README.md)
- [Ejemplos End-to-End](./EJEMPLOS_END_TO_END.md)
- [Variables de Entorno](./VARIABLES_ENTORNO.md)
- [Integración CI/CD](./INTEGRACION_CI_CD.md)

## Soporte

Para ayuda adicional:
1. Revisa los [logs con modo verbose](#activar-modo-debug-completo)
2. Consulta la [documentación de errores comunes](#errores-comunes)
3. Abre un [issue en GitHub](https://github.com/JoseRGWeb/mongodb-backup-restore-cli/issues) con los logs relevantes
