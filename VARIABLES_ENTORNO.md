# Guía Completa de Variables de Entorno

Esta guía describe todas las variables de entorno soportadas por MongoDB Backup & Restore CLI, su uso y mejores prácticas.

## Tabla de Contenidos

- [Introducción](#introducción)
- [Variables de Conexión](#variables-de-conexión)
- [Variables de Autenticación](#variables-de-autenticación)
- [Variables de Compresión y Seguridad](#variables-de-compresión-y-seguridad)
- [Variables de Logging](#variables-de-logging)
- [Variables de Configuración](#variables-de-configuración)
- [Ejemplos de Uso](#ejemplos-de-uso)
- [Prioridad de Configuración](#prioridad-de-configuración)
- [Mejores Prácticas](#mejores-prácticas)

---

## Introducción

Las variables de entorno permiten configurar la CLI sin necesidad de pasar parámetros en cada ejecución. Esto es especialmente útil para:

- **Scripts automatizados**: Evitar credenciales hardcoded
- **Entornos CI/CD**: Configuración centralizada
- **Seguridad**: No exponer credenciales en la línea de comandos
- **Consistencia**: Misma configuración en múltiples ejecuciones

---

## Variables de Conexión

### `MONGO_URI`

**Descripción**: Cadena de conexión completa de MongoDB en formato URI.

**Formato**: `mongodb://[usuario:contraseña@]host[:puerto]/[base-datos][?opciones]`

**Ejemplo**:
```bash
# Linux/Mac
export MONGO_URI="mongodb://admin:password@localhost:27017/admin"

# Windows PowerShell
$env:MONGO_URI="mongodb://admin:password@localhost:27017/admin"

# Windows CMD
set MONGO_URI=mongodb://admin:password@localhost:27017/admin
```

**Uso con la CLI**:
```bash
mongodb-br backup --db MiBaseDatos --uri "$MONGO_URI" --out ./backups
```

**Notas**:
- Tiene prioridad sobre `MONGO_HOST`, `MONGO_PORT`, etc. si todas están definidas
- Los caracteres especiales en contraseñas deben codificarse en formato URL (p. ej., `@` = `%40`, `!` = `%21`)
- Soporta opciones adicionales como `?authSource=admin&ssl=true`

---

### `MONGO_HOST`

**Descripción**: Dirección del servidor MongoDB (hostname o IP).

**Valor por defecto**: `localhost`

**Ejemplo**:
```bash
# Servidor local
export MONGO_HOST=localhost

# Servidor remoto
export MONGO_HOST=db.ejemplo.com

# Dirección IP
export MONGO_HOST=192.168.1.100
```

**Uso con la CLI**:
```bash
# El host se toma automáticamente de la variable de entorno
mongodb-br backup --db MiBaseDatos --out ./backups

# O se puede sobrescribir
mongodb-br backup --db MiBaseDatos --host otro-servidor.com --out ./backups
```

---

### `MONGO_PORT`

**Descripción**: Puerto del servidor MongoDB.

**Valor por defecto**: `27017`

**Ejemplo**:
```bash
# Puerto estándar (no es necesario definirlo)
export MONGO_PORT=27017

# Puerto personalizado
export MONGO_PORT=27018
```

---

## Variables de Autenticación

### `MONGO_USER`

**Descripción**: Nombre de usuario para autenticación en MongoDB.

**Ejemplo**:
```bash
export MONGO_USER=backup_admin
```

**Notas**:
- Requerido si MongoDB tiene autenticación habilitada
- Debe combinarse con `MONGO_PASSWORD` y `MONGO_AUTH_DB`

---

### `MONGO_PASSWORD`

**Descripción**: Contraseña del usuario de MongoDB.

**Ejemplo**:
```bash
# Contraseña simple
export MONGO_PASSWORD='miPassword123'

# Contraseña con caracteres especiales (usar comillas simples)
export MONGO_PASSWORD='P@ssw0rd!2024#Segur0'
```

**Mejores prácticas**:
- **Siempre use comillas** para evitar interpretación de caracteres especiales
- **No incluya en control de versiones** (archivos `.env`, `.bashrc`, etc.)
- **Use gestores de secretos** en producción (AWS Secrets Manager, Azure Key Vault, HashiCorp Vault)
- **Rote contraseñas periódicamente**

---

### `MONGO_AUTH_DB`

**Descripción**: Base de datos de autenticación.

**Valor por defecto**: `admin`

**Ejemplo**:
```bash
# Autenticación contra admin (más común)
export MONGO_AUTH_DB=admin

# Autenticación contra base de datos específica
export MONGO_AUTH_DB=MiBaseDatos
```

**Notas**:
- Para la mayoría de instalaciones, use `admin`
- Solo cambie si tiene configuración personalizada de autenticación

---

## Variables de Compresión y Seguridad

### `MONGO_COMPRESSION`

**Descripción**: Formato de compresión para los backups.

**Valores válidos**: `none`, `zip`, `targz`

**Valor por defecto**: `none`

**Ejemplo**:
```bash
# Sin compresión
export MONGO_COMPRESSION=none

# Compresión ZIP (recomendado para Windows)
export MONGO_COMPRESSION=zip

# Compresión TAR.GZ (recomendado para Linux/Mac)
export MONGO_COMPRESSION=targz
```

**Uso**:
```bash
# La compresión se aplica automáticamente
mongodb-br backup --db MiBaseDatos --out ./backups

# O sobrescribir la configuración
mongodb-br backup --db MiBaseDatos --out ./backups --compress zip
```

**Comparación de formatos**:

| Formato | Plataforma | Ratio Compresión | Velocidad | Tamaño Típico |
|---------|-----------|------------------|-----------|---------------|
| `none`  | Todas     | 1:1              | Rápida    | 100%          |
| `zip`   | Windows   | ~3:1             | Media     | ~33%          |
| `targz` | Linux/Mac | ~3.5:1           | Lenta     | ~28%          |

---

### `MONGO_ENCRYPTION_KEY`

**Descripción**: Clave para cifrado/descifrado AES-256 de backups.

**Requisitos**:
- **Longitud mínima**: 16 caracteres
- **Recomendado**: 32+ caracteres con alta entropía
- **Formato**: Texto o Base64

**Generar clave segura**:
```bash
# Linux/Mac
export MONGO_ENCRYPTION_KEY=$(openssl rand -base64 32)
echo "Guardar esta clave: $MONGO_ENCRYPTION_KEY"

# Windows PowerShell
$bytes = New-Object Byte[] 32
[Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
$env:MONGO_ENCRYPTION_KEY = [Convert]::ToBase64String($bytes)
Write-Host "Guardar esta clave: $env:MONGO_ENCRYPTION_KEY"
```

**Uso para cifrado**:
```bash
# Cifrar automáticamente usando la variable de entorno
export MONGO_ENCRYPTION_KEY="MiClaveSegura123456789012345678"
mongodb-br backup --db MiBaseDatos --out ./backups --encrypt
```

**Uso para descifrado**:
```bash
# Descifrar automáticamente usando la variable de entorno
export MONGO_ENCRYPTION_KEY="MiClaveSegura123456789012345678"
mongodb-br restore --db MiBaseDatos --from ./backups/backup.encrypted
```

**Almacenamiento seguro**:

1. **AWS Secrets Manager**:
```bash
# Guardar
aws secretsmanager create-secret \
  --name mongodb-backup-key \
  --secret-string "MiClaveSegura123456789012345678"

# Recuperar
export MONGO_ENCRYPTION_KEY=$(aws secretsmanager get-secret-value \
  --secret-id mongodb-backup-key \
  --query SecretString \
  --output text)
```

2. **Azure Key Vault**:
```bash
# Guardar
az keyvault secret set \
  --vault-name mi-vault \
  --name mongodb-backup-key \
  --value "MiClaveSegura123456789012345678"

# Recuperar
export MONGO_ENCRYPTION_KEY=$(az keyvault secret show \
  --vault-name mi-vault \
  --name mongodb-backup-key \
  --query value -o tsv)
```

3. **HashiCorp Vault**:
```bash
# Guardar
vault kv put secret/mongodb backup-key="MiClaveSegura123456789012345678"

# Recuperar
export MONGO_ENCRYPTION_KEY=$(vault kv get -field=backup-key secret/mongodb)
```

---

### `MONGO_RETENTION_DAYS`

**Descripción**: Número de días para retener backups. Los backups más antiguos se eliminan automáticamente.

**Valor por defecto**: No definido (sin limpieza automática)

**Rango válido**: 1 - 365 días

**Ejemplo**:
```bash
# Retener 7 días
export MONGO_RETENTION_DAYS=7

# Retener 30 días (recomendado para producción)
export MONGO_RETENTION_DAYS=30

# Retener 90 días (archivado extendido)
export MONGO_RETENTION_DAYS=90
```

**Uso**:
```bash
# La limpieza se ejecuta automáticamente después del backup
mongodb-br backup --db MiBaseDatos --out ./backups/backup-$(date +%Y%m%d)

# O sobrescribir con parámetro
mongodb-br backup --db MiBaseDatos --out ./backups --retention-days 14
```

**Notas**:
- La limpieza solo ocurre **después de un backup exitoso**
- Se basa en la **fecha de creación** del archivo/directorio
- Ignora directorios ocultos (`.`) y temporales (`temp`)
- Registra logs detallados de la limpieza

---

## Variables de Logging

### `MONGO_LOG_LEVEL`

**Descripción**: Nivel de detalle de los logs.

**Valores válidos**: `trace`, `debug`, `info`, `warning`, `error`, `critical`

**Valor por defecto**: `info`

**Ejemplo**:
```bash
# Modo información (estándar)
export MONGO_LOG_LEVEL=info

# Modo debug (depuración detallada)
export MONGO_LOG_LEVEL=debug

# Solo errores críticos
export MONGO_LOG_LEVEL=error
```

**Descripción de niveles**:

| Nivel | Descripción | Cuándo usar |
|-------|-------------|-------------|
| `trace` | Muy detallado, cada operación | Desarrollo profundo |
| `debug` | Información de depuración | Troubleshooting |
| `info` | Información general | Operación normal |
| `warning` | Advertencias no críticas | Producción |
| `error` | Solo errores | Monitoreo mínimo |
| `critical` | Solo errores críticos | Alertas urgentes |

**Nota**: La opción `--verbose` sobrescribe esta variable y establece el nivel en `debug`.

---

### `MONGO_LOG_FILE`

**Descripción**: Ruta del archivo donde guardar los logs.

**Valor por defecto**: No definido (solo salida a consola)

**Ejemplo**:
```bash
# Linux/Mac
export MONGO_LOG_FILE=/var/log/mongodb-backup/backup.log

# Windows
$env:MONGO_LOG_FILE="C:\Logs\mongodb-backup.log"

# Archivo con timestamp
export MONGO_LOG_FILE="/var/log/mongodb-backup/backup-$(date +%Y%m%d).log"
```

**Uso**:
```bash
# Los logs se escriben automáticamente al archivo
mongodb-br backup --db MiBaseDatos --out ./backups

# Ver logs en tiempo real
tail -f /var/log/mongodb-backup/backup.log
```

**Configuración de rotación de logs** (Linux):
```bash
# Crear configuración de logrotate
sudo cat > /etc/logrotate.d/mongodb-backup << 'EOF'
/var/log/mongodb-backup/*.log {
    daily
    rotate 30
    compress
    delaycompress
    notifempty
    create 0640 backup backup
    sharedscripts
}
EOF
```

---

## Variables de Configuración

### `DOCKER_CONTEXT`

**Descripción**: Contexto de Docker para operaciones remotas (roadmap - no implementado aún).

**Ejemplo**:
```bash
export DOCKER_CONTEXT=remote-server
```

---

### `MONGOBR_OUT_DIR`

**Descripción**: Directorio por defecto para almacenar backups.

**Valor por defecto**: No definido (debe especificarse con `--out`)

**Ejemplo**:
```bash
# Linux/Mac
export MONGOBR_OUT_DIR=/var/backups/mongodb

# Windows
$env:MONGOBR_OUT_DIR="C:\Backups\MongoDB"

# Con esta variable, ya no es necesario especificar --out
mongodb-br backup --db MiBaseDatos
```

---

## Ejemplos de Uso

### Ejemplo 1: Archivo de Configuración Básico

Crear archivo `.env` para desarrollo:

```bash
# .mongodb-backup.env
MONGO_HOST=localhost
MONGO_PORT=27017
MONGO_USER=backup_user
MONGO_PASSWORD=dev_password123
MONGO_AUTH_DB=admin
MONGO_COMPRESSION=zip
MONGO_LOG_LEVEL=info
```

Cargar y usar:
```bash
# Linux/Mac
source .mongodb-backup.env
mongodb-br backup --db DevDB --out ./backups

# Windows PowerShell
Get-Content .mongodb-backup.env | ForEach-Object {
    if ($_ -match '^([^=]+)=(.+)$') {
        Set-Item -Path "env:$($matches[1])" -Value $matches[2]
    }
}
mongodb-br backup --db DevDB --out ./backups
```

---

### Ejemplo 2: Configuración de Producción Segura

```bash
#!/bin/bash
# /etc/mongodb-backup/production.env

# NO GUARDAR CONTRASEÑAS AQUÍ - Usar gestores de secretos

# Conexión
export MONGO_HOST=prod-mongo-01.ejemplo.com
export MONGO_PORT=27017
export MONGO_AUTH_DB=admin

# Recuperar secretos de AWS Secrets Manager
export MONGO_USER=$(aws secretsmanager get-secret-value \
  --secret-id prod/mongodb/backup-user \
  --query SecretString \
  --output text)

export MONGO_PASSWORD=$(aws secretsmanager get-secret-value \
  --secret-id prod/mongodb/backup-password \
  --query SecretString \
  --output text)

export MONGO_ENCRYPTION_KEY=$(aws secretsmanager get-secret-value \
  --secret-id prod/mongodb/encryption-key \
  --query SecretString \
  --output text)

# Configuración
export MONGO_COMPRESSION=zip
export MONGO_RETENTION_DAYS=30
export MONGO_LOG_LEVEL=info
export MONGO_LOG_FILE=/var/log/mongodb-backup/production.log
```

Usar:
```bash
source /etc/mongodb-backup/production.env
mongodb-br backup --db ProductionDB --out /backups/prod --encrypt
```

---

### Ejemplo 3: Script de Backup Automatizado

```bash
#!/bin/bash
# backup-automatico.sh

set -euo pipefail

# Cargar configuración
if [ -f "$HOME/.mongodb-backup.env" ]; then
    source "$HOME/.mongodb-backup.env"
else
    echo "Error: Archivo de configuración no encontrado"
    exit 1
fi

# Configuración específica del script
TIMESTAMP=$(date +%Y%m%d-%H%M%S)
BACKUP_DIR="/backups/mongodb/auto-$TIMESTAMP"

# Ejecutar backup
mongodb-br backup \
  --db "$DB_NAME" \
  --out "$BACKUP_DIR" \
  --compress zip \
  --encrypt \
  --retention-days 30 \
  --verbose

# Verificar resultado
if [ $? -eq 0 ]; then
    echo "Backup exitoso: $BACKUP_DIR"
    # Notificar éxito (Slack, email, etc.)
else
    echo "Error en backup"
    # Notificar fallo
    exit 1
fi
```

---

### Ejemplo 4: Configuración por Ambiente

```bash
# config/dev.env
export MONGO_HOST=localhost
export MONGO_PORT=27017
export MONGO_COMPRESSION=none
export MONGO_LOG_LEVEL=debug

# config/staging.env
export MONGO_HOST=staging-db.ejemplo.com
export MONGO_PORT=27017
export MONGO_COMPRESSION=zip
export MONGO_LOG_LEVEL=info
export MONGO_RETENTION_DAYS=7

# config/production.env
export MONGO_HOST=prod-db.ejemplo.com
export MONGO_PORT=27017
export MONGO_COMPRESSION=zip
export MONGO_LOG_LEVEL=warning
export MONGO_RETENTION_DAYS=30
```

Uso:
```bash
# Seleccionar ambiente
ENV=${1:-dev}
source config/$ENV.env

# Ejecutar backup
mongodb-br backup --db MiApp --out ./backups/$ENV
```

---

## Prioridad de Configuración

Cuando la misma opción está configurada en múltiples lugares, la prioridad es:

1. **Parámetros de línea de comandos** (máxima prioridad)
2. **Variables de entorno**
3. **Valores por defecto** (mínima prioridad)

**Ejemplo**:
```bash
# Variable de entorno
export MONGO_HOST=servidor1.com

# El parámetro --host sobrescribe la variable
mongodb-br backup --db MiDB --host servidor2.com --out ./backups
# Usará: servidor2.com

# Sin parámetro --host, usa la variable
mongodb-br backup --db MiDB --out ./backups
# Usará: servidor1.com
```

---

## Mejores Prácticas

### 1. Seguridad

✓ **Nunca incluir credenciales en control de versiones**
```bash
# Agregar a .gitignore
echo ".mongodb-backup.env" >> .gitignore
echo "*.env" >> .gitignore
```

✓ **Usar gestores de secretos en producción**
```bash
# Malo - credenciales en texto plano
export MONGO_PASSWORD=password123

# Bueno - recuperar de gestor de secretos
export MONGO_PASSWORD=$(aws secretsmanager get-secret-value --secret-id ...)
```

✓ **Proteger archivos de configuración**
```bash
chmod 600 ~/.mongodb-backup.env
```

---

### 2. Organización

✓ **Separar configuración por ambiente**
```
config/
├── dev.env
├── staging.env
└── production.env
```

✓ **Documentar variables requeridas**
```bash
# .env.example (versionado en Git)
MONGO_HOST=localhost
MONGO_PORT=27017
MONGO_USER=backup_user
MONGO_PASSWORD=<requerido>
MONGO_COMPRESSION=zip
```

---

### 3. Validación

✓ **Verificar variables antes de ejecutar**
```bash
#!/bin/bash
required_vars=("MONGO_HOST" "MONGO_USER" "MONGO_PASSWORD")

for var in "${required_vars[@]}"; do
    if [ -z "${!var}" ]; then
        echo "Error: Variable $var no está definida"
        exit 1
    fi
done

# Continuar con backup
mongodb-br backup ...
```

---

### 4. Logging

✓ **Rotar logs para evitar llenado de disco**
```bash
# Configurar rotación de logs
export MONGO_LOG_FILE="/var/log/mongodb-backup/backup-$(date +%Y%m%d).log"
```

✓ **Usar nivel apropiado según ambiente**
```bash
# Desarrollo: debug
export MONGO_LOG_LEVEL=debug

# Producción: info o warning
export MONGO_LOG_LEVEL=info
```

---

## Recursos Adicionales

- [Documentación Principal](./README.md)
- [Guía de Instalación](./INSTALACION.md)
- [Ejemplos End-to-End](./EJEMPLOS_END_TO_END.md)
- [Seguridad](./SEGURIDAD.md)

## Soporte

Para preguntas o problemas:
1. Revisa la [documentación completa](./README.md)
2. Consulta los [issues existentes](https://github.com/JoseRGWeb/mongodb-backup-restore-cli/issues)
3. Abre un [nuevo issue](https://github.com/JoseRGWeb/mongodb-backup-restore-cli/issues/new)
