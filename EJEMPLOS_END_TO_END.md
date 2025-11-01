# Ejemplos End-to-End de MongoDB Backup & Restore CLI

Este documento proporciona ejemplos prácticos completos de uso de MongoDB Backup & Restore CLI en diferentes escenarios del mundo real.

## Tabla de Contenidos

- [Escenario 1: Backup Local en Windows](#escenario-1-backup-local-en-windows)
- [Escenario 2: Backup y Restore en Docker Local](#escenario-2-backup-y-restore-en-docker-local)
- [Escenario 3: Backup Remoto con Autenticación](#escenario-3-backup-remoto-con-autenticación)
- [Escenario 4: Backup con Compresión y Cifrado](#escenario-4-backup-con-compresión-y-cifrado)
- [Escenario 5: Backup Automatizado con Retención](#escenario-5-backup-automatizado-con-retención)
- [Escenario 6: Migración de Base de Datos entre Ambientes](#escenario-6-migración-de-base-de-datos-entre-ambientes)
- [Escenario 7: Backup de Múltiples Bases de Datos](#escenario-7-backup-de-múltiples-bases-de-datos)
- [Escenario 8: Recuperación ante Desastres](#escenario-8-recuperación-ante-desastres)
- [Escenario 9: Backup en Entorno de Desarrollo](#escenario-9-backup-en-entorno-de-desarrollo)
- [Escenario 10: Backup en Producción con Máxima Seguridad](#escenario-10-backup-en-producción-con-máxima-seguridad)

---

## Escenario 1: Backup Local en Windows

### Contexto
Tienes MongoDB instalado localmente en tu máquina Windows y necesitas hacer un backup manual de tu base de datos de desarrollo.

### Requisitos Previos
- MongoDB instalado y en ejecución en `localhost:27017`
- MongoDB Database Tools instalados
- CLI instalada globalmente: `dotnet tool install --global MongoBackupRestore.Cli`

### Paso 1: Verificar la Instalación

```bash
# Verificar que MongoDB está en ejecución
mongosh --eval "db.adminCommand('ping')"

# Verificar que la CLI está instalada
mongodb-br --version
```

### Paso 2: Crear Directorio para Backups

```bash
# Crear directorio para almacenar backups
mkdir C:\Backups\MongoDB
cd C:\Backups\MongoDB
```

### Paso 3: Realizar el Backup

```bash
# Backup simple de la base de datos "MiProyecto"
mongodb-br backup --db MiProyecto --out C:\Backups\MongoDB\backup-$(Get-Date -Format "yyyy-MM-dd")

# Resultado esperado:
# ✓ Backup completado exitosamente para la base de datos 'MiProyecto'
# Backup guardado en: C:\Backups\MongoDB\backup-2025-11-01
```

### Paso 4: Verificar el Backup

```bash
# Listar archivos del backup
dir C:\Backups\MongoDB\backup-2025-11-01

# Deberías ver archivos BSON y metadatos de colecciones
```

### Paso 5: Restaurar el Backup (Si es Necesario)

```bash
# Restaurar a la misma base de datos (crea una copia)
mongodb-br restore --db MiProyecto_Restaurado --from C:\Backups\MongoDB\backup-2025-11-01

# O reemplazar la base de datos existente
mongodb-br restore --db MiProyecto --from C:\Backups\MongoDB\backup-2025-11-01 --drop
```

### Resultado
Backup y restore completados exitosamente en un entorno local Windows sin necesidad de Docker.

---

## Escenario 2: Backup y Restore en Docker Local

### Contexto
Estás desarrollando una aplicación con MongoDB en un contenedor Docker y necesitas hacer backups periódicos.

### Requisitos Previos
- Docker Desktop instalado y en ejecución
- Contenedor MongoDB en ejecución

### Paso 1: Iniciar Contenedor MongoDB

```bash
# Crear y ejecutar contenedor MongoDB
docker run -d \
  --name mi-mongodb \
  -p 27017:27017 \
  -e MONGO_INITDB_ROOT_USERNAME=admin \
  -e MONGO_INITDB_ROOT_PASSWORD=password123 \
  mongo:latest

# Verificar que el contenedor está en ejecución
docker ps | grep mi-mongodb
```

### Paso 2: Crear Base de Datos de Prueba

```bash
# Conectar al contenedor y crear datos de prueba
docker exec -it mi-mongodb mongosh -u admin -p password123 --authenticationDatabase admin

# Dentro de mongosh:
use TiendaOnline
db.productos.insertMany([
  { nombre: "Laptop", precio: 1200, stock: 10 },
  { nombre: "Mouse", precio: 25, stock: 50 },
  { nombre: "Teclado", precio: 75, stock: 30 }
])
db.clientes.insertMany([
  { nombre: "Juan Pérez", email: "juan@example.com" },
  { nombre: "María García", email: "maria@example.com" }
])
exit
```

### Paso 3: Backup con Auto-detección de Contenedor

```bash
# La CLI detectará automáticamente el contenedor MongoDB
mongodb-br backup \
  --db TiendaOnline \
  --in-docker \
  --user admin \
  --password password123 \
  --auth-db admin \
  --out ./backups/tienda-$(date +%Y%m%d)

# Salida esperada:
# Auto-detectando contenedores Docker con MongoDB...
# Contenedor detectado automáticamente: mi-mongodb
# Contenedor Docker validado: mi-mongodb
# ✓ Backup completado exitosamente para la base de datos 'TiendaOnline' desde el contenedor 'mi-mongodb'
```

### Paso 4: Simular Pérdida de Datos

```bash
# Eliminar datos para probar el restore
docker exec -it mi-mongodb mongosh -u admin -p password123 --authenticationDatabase admin --eval "
  use TiendaOnline
  db.productos.deleteMany({})
  db.clientes.deleteMany({})
  db.productos.countDocuments()
"
# Resultado: 0 (datos eliminados)
```

### Paso 5: Restaurar desde Backup

```bash
# Restaurar la base de datos
mongodb-br restore \
  --db TiendaOnline \
  --in-docker \
  --user admin \
  --password password123 \
  --auth-db admin \
  --from ./backups/tienda-20251101
```

### Paso 6: Verificar la Restauración

```bash
# Verificar que los datos fueron restaurados
docker exec -it mi-mongodb mongosh -u admin -p password123 --authenticationDatabase admin --eval "
  use TiendaOnline
  print('Productos:', db.productos.countDocuments())
  print('Clientes:', db.clientes.countDocuments())
"
# Resultado:
# Productos: 3
# Clientes: 2
```

### Resultado
Backup y restore exitosos en un contenedor Docker con auto-detección y autenticación.

---

## Escenario 3: Backup Remoto con Autenticación

### Contexto
Necesitas hacer backup de una base de datos MongoDB que está en un servidor remoto con autenticación habilitada.

### Requisitos Previos
- Acceso de red al servidor MongoDB remoto
- Credenciales de autenticación
- MongoDB Database Tools instalados localmente

### Paso 1: Verificar Conectividad

```bash
# Probar conexión con mongosh
mongosh "mongodb://usuario:password@db.ejemplo.com:27017/admin"
```

### Paso 2: Configurar Variables de Entorno (Recomendado)

```bash
# Linux/Mac
export MONGO_HOST=db.ejemplo.com
export MONGO_PORT=27017
export MONGO_USER=backup_user
export MONGO_PASSWORD='P@ssw0rd!Segur0'
export MONGO_AUTH_DB=admin

# Windows PowerShell
$env:MONGO_HOST="db.ejemplo.com"
$env:MONGO_PORT="27017"
$env:MONGO_USER="backup_user"
$env:MONGO_PASSWORD="P@ssw0rd!Segur0"
$env:MONGO_AUTH_DB="admin"
```

### Paso 3: Realizar Backup Remoto

```bash
# Backup usando variables de entorno
mongodb-br backup \
  --db ProductionDB \
  --out ./backups/prod-$(date +%Y%m%d-%H%M%S) \
  --verbose

# O especificando parámetros directamente
mongodb-br backup \
  --db ProductionDB \
  --host db.ejemplo.com \
  --port 27017 \
  --user backup_user \
  --password 'P@ssw0rd!Segur0' \
  --auth-db admin \
  --out ./backups/prod-$(date +%Y%m%d-%H%M%S)
```

### Paso 4: Usar Cadena de Conexión URI (Alternativa)

```bash
# Configurar URI completa
export MONGO_URI="mongodb://backup_user:P%40ssw0rd%21Segur0@db.ejemplo.com:27017/admin"

# Hacer backup con URI
mongodb-br backup \
  --db ProductionDB \
  --uri "$MONGO_URI" \
  --out ./backups/prod-$(date +%Y%m%d-%H%M%S)
```

### Paso 5: Restaurar en Ambiente Local para Pruebas

```bash
# Restaurar a MongoDB local para pruebas
mongodb-br restore \
  --db ProductionDB_Test \
  --host localhost \
  --port 27017 \
  --from ./backups/prod-20251101-143052
```

### Resultado
Backup exitoso de base de datos remota con autenticación segura usando variables de entorno.

---

## Escenario 4: Backup con Compresión y Cifrado

### Contexto
Necesitas hacer backups de una base de datos sensible con máxima seguridad y optimización de espacio.

### Requisitos Previos
- CLI instalada
- MongoDB en ejecución (local o remoto)

### Paso 1: Generar Clave de Cifrado Segura

```bash
# Linux/Mac: Generar clave aleatoria de 32 caracteres
export MONGO_ENCRYPTION_KEY=$(openssl rand -base64 32)
echo "IMPORTANTE: Guardar esta clave de forma segura:"
echo $MONGO_ENCRYPTION_KEY

# Windows PowerShell: Generar clave aleatoria
$bytes = New-Object Byte[] 32
[Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
$env:MONGO_ENCRYPTION_KEY = [Convert]::ToBase64String($bytes)
Write-Host "IMPORTANTE: Guardar esta clave de forma segura:"
Write-Host $env:MONGO_ENCRYPTION_KEY
```

### Paso 2: Backup con Compresión ZIP y Cifrado

```bash
# Backup comprimido y cifrado
mongodb-br backup \
  --db DatosSensibles \
  --out ./backups/seguro-$(date +%Y%m%d) \
  --compress zip \
  --encrypt \
  --verbose \
  --log-file ./logs/backup-$(date +%Y%m%d).log

# Resultado esperado:
# ✓ Backup comprimido: DatosSensibles_20251101_143052.zip
# ✓ Backup cifrado: DatosSensibles_20251101_143052.zip.encrypted
# Archivo final: ./backups/seguro-20251101/DatosSensibles_20251101_143052.zip.encrypted
```

### Paso 3: Verificar el Archivo Cifrado

```bash
# Verificar que el archivo no es legible sin descifrar
file ./backups/seguro-20251101/DatosSensibles_20251101_143052.zip.encrypted
# Resultado: data (contenido binario cifrado)

# Verificar tamaño del archivo
ls -lh ./backups/seguro-20251101/
```

### Paso 4: Restaurar Backup Cifrado

```bash
# La CLI detecta automáticamente que el backup está cifrado
mongodb-br restore \
  --db DatosSensibles_Recuperado \
  --from ./backups/seguro-20251101/DatosSensibles_20251101_143052.zip.encrypted \
  --verbose

# La clave se toma de la variable de entorno MONGO_ENCRYPTION_KEY
# Si no está definida, pedirá la clave:
# ✗ El backup está cifrado pero no se proporcionó la clave de cifrado

# O especificar la clave directamente (menos seguro)
mongodb-br restore \
  --db DatosSensibles_Recuperado \
  --from ./backups/seguro-20251101/DatosSensibles_20251101_143052.zip.encrypted \
  --encryption-key "$MONGO_ENCRYPTION_KEY"
```

### Paso 5: Probar con Clave Incorrecta

```bash
# Intentar restaurar con clave incorrecta
mongodb-br restore \
  --db DatosSensibles_Test \
  --from ./backups/seguro-20251101/DatosSensibles_20251101_143052.zip.encrypted \
  --encryption-key "ClaveIncorrecta12345"

# Resultado esperado:
# ✗ Error al descifrar: la clave de cifrado es incorrecta o el archivo está corrupto
```

### Resultado
Backup seguro con compresión ZIP y cifrado AES-256, con clave almacenada de forma segura.

---

## Mejores Prácticas Generales

### 1. Seguridad
- ✓ Siempre use cifrado para backups de producción
- ✓ Almacene claves de cifrado en gestores de secretos (AWS Secrets Manager, Azure Key Vault, HashiCorp Vault)
- ✓ Use variables de entorno en lugar de parámetros de línea de comandos para credenciales
- ✓ Implemente control de acceso basado en roles para scripts de backup
- ✓ Rote claves de cifrado periódicamente

### 2. Confiabilidad
- ✓ Pruebe las restauraciones regularmente (al menos mensualmente)
- ✓ Mantenga backups en múltiples ubicaciones (local + remoto)
- ✓ Verifique la integridad de los backups con checksums
- ✓ Documente procedimientos de recuperación ante desastres
- ✓ Implemente retención apropiada según política de la empresa

### 3. Performance
- ✓ Use compresión para reducir espacio de almacenamiento
- ✓ Programe backups durante ventanas de bajo tráfico
- ✓ Considere backups incrementales para bases de datos grandes
- ✓ Monitore el tiempo de ejecución de backups

### 4. Monitoreo
- ✓ Configure alertas para backups fallidos
- ✓ Monitore el espacio de disco disponible
- ✓ Revise logs regularmente
- ✓ Verifique que los backups automáticos se ejecutan según lo programado

### 5. Documentación
- ✓ Documente la ubicación de backups
- ✓ Mantenga un registro de cambios importantes
- ✓ Documente el proceso de restauración
- ✓ Capacite al equipo en procedimientos de backup/restore

## Recursos Adicionales

- [Documentación Principal](./README.md)
- [Guía de Instalación](./INSTALACION.md)
- [Modo Docker](./DOCKER_MODE.md)
- [Variables de Entorno](./VARIABLES_ENTORNO.md)
- [Integración CI/CD](./INTEGRACION_CI_CD.md)
- [Logs y Debugging](./LOGS_Y_DEBUGGING.md)

## Soporte

Si necesitas ayuda adicional:
1. Consulta la [documentación completa](./README.md)
2. Revisa los [issues existentes](https://github.com/JoseRGWeb/mongodb-backup-restore-cli/issues)
3. Abre un [nuevo issue](https://github.com/JoseRGWeb/mongodb-backup-restore-cli/issues/new) con detalles
