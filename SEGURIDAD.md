# Resumen de Seguridad: Soporte de Autenticación

## Fecha: 2025-11-01

## Vulnerabilidades Identificadas por CodeQL

### 1. Almacenamiento de Información Sensible en Texto Claro

**Estado:** ⚠️ CONOCIDO Y DOCUMENTADO - Limitación Inherente

**Ubicaciones:**
- `MongoConnectionValidator.cs` (línea 47)
- `ProcessRunner.cs` (línea 28)
- `BackupService.cs` (múltiples ubicaciones)
- `RestoreService.cs` (múltiples ubicaciones)

**Descripción:**
Las contraseñas se pasan como argumentos de línea de comandos a mongodump, mongorestore y mongosh. Esto es una limitación inherente de estas herramientas de MongoDB, que requieren las credenciales como parámetros.

**Mitigaciones Implementadas:**

1. **Sanitización de Logs:**
   - Se sanitizan los argumentos antes de escribirlos en los logs
   - Las contraseñas se reemplazan con "***" en todos los logs
   - Se sanitizan URIs que contienen contraseñas (mongodb://user:password@host)

2. **Validación Temprana:**
   - Se validan las credenciales antes de ejecutar operaciones
   - Esto reduce el número de intentos fallidos con credenciales visibles

3. **Documentación de Riesgos:**
   - Cada ubicación donde se manejan contraseñas tiene comentarios explicando el riesgo
   - Se incluyen recomendaciones de seguridad para entornos de producción

**Recomendaciones para Producción:**

1. **Autenticación Basada en Certificados:**
   - Usar certificados X.509 en lugar de contraseñas
   - Configurar MongoDB con autenticación Kerberos (GSSAPI)

2. **Variables de Entorno:**
   - Usar variables de entorno en lugar de parámetros de línea de comandos
   - Nota: mongodump/mongorestore tienen soporte limitado para esto

3. **Permisos de Proceso:**
   - Ejecutar mongodump/mongorestore con permisos restrictivos
   - En contenedores Docker, el riesgo es menor debido al aislamiento

4. **Rotación de Credenciales:**
   - Implementar rotación automática de credenciales
   - Usar gestores de secretos (HashiCorp Vault, AWS Secrets Manager, etc.)

5. **Autenticación Interactiva:**
   - Para operaciones manuales, ejecutar sin --password
   - mongodump/mongorestore pedirán la contraseña interactivamente

## Vulnerabilidades NO Encontradas

✅ No se encontraron inyecciones de comandos
✅ No se encontraron path traversal vulnerabilities
✅ No se encontraron problemas de validación de entrada
✅ No se encontraron problemas de manejo de excepciones
✅ No se encontraron deadlocks (después de corrección)

## Validaciones de Seguridad Implementadas

### 1. Validación de Caracteres Peligrosos
- Nombres de base de datos
- Nombres de contenedores
- Nombres de usuario
- Hosts

**Caracteres Bloqueados:** `"`, `'`, `;`, `&`, `|`, `` ` ``

### 2. Sanitización de Logs
- Contraseñas en argumentos --password
- Contraseñas en URIs (mongodb://)
- Registro detallado solo en modo verbose

### 3. Validación de Credenciales
- Validación temprana antes de operaciones costosas
- Mensajes de error que no revelan información sensible
- Timeouts apropiados para prevenir DoS

## Notas Adicionales

### Alcance Limitado
Esta herramienta es una CLI para administradores de sistemas, no una aplicación web pública. Los riesgos se asumen con conocimiento del contexto de uso.

### Responsabilidad del Usuario
- Los usuarios deben proteger sus credenciales
- Las variables de entorno deben manejarse con cuidado
- Los logs deben almacenarse con permisos restrictivos

### Actualizaciones Futuras
Considerar agregar soporte para:
- Integración con gestores de secretos
- Soporte de archivos de configuración con permisos restrictivos
- ✅ **Cifrado de backups en reposo** - Implementado con AES-256-CBC
- Auditoría de accesos

## Cifrado de Backups (AES-256)

### Implementación

**Fecha de Implementación:** 2025-11-01

La herramienta ahora incluye cifrado AES-256 opcional para proteger backups en reposo.

**Características de Seguridad:**
- **Algoritmo:** AES-256-CBC (Estándar de Cifrado Avanzado)
- **Autenticación:** HMAC-SHA256 para verificar integridad
- **Derivación de Claves:** PBKDF2 con SHA-256 y 100,000 iteraciones
- **Vector de Inicialización:** Generado aleatoriamente para cada operación
- **Limpieza de Memoria:** Las claves se eliminan de memoria después de uso

### Seguridad del Cifrado

**Fortalezas:**
1. AES-256 es considerado seguro contra ataques de fuerza bruta
2. HMAC-SHA256 previene manipulación de datos cifrados
3. IV aleatorio previene ataques de análisis de patrones
4. PBKDF2 con 100,000 iteraciones hace más lenta la fuerza bruta contra claves

**Limitaciones Conocidas:**
1. La seguridad depende completamente de la fortaleza de la clave proporcionada
2. Si la clave se compromete, todos los backups cifrados con ella están comprometidos
3. No hay gestión automática de rotación de claves

### Mejores Prácticas de Uso

#### 1. Generación de Claves Seguras

**Recomendado:**
```bash
# Generar clave de 32 caracteres (256 bits de entropía)
openssl rand -base64 32

# O usar un gestor de contraseñas para generar claves complejas
```

**NO Usar:**
- Palabras del diccionario
- Claves cortas (<16 caracteres)
- Información predecible (fechas, nombres, etc.)
- La misma clave para múltiples entornos

#### 2. Almacenamiento de Claves

**Entornos de Desarrollo:**
```bash
# Variable de entorno local (en .bashrc o .zshrc)
export MONGO_ENCRYPTION_KEY="$(cat ~/.mongodb_encryption_key)"
```

**Entornos de Producción:**

**AWS:**
```bash
# Usar AWS Secrets Manager
aws secretsmanager get-secret-value \
  --secret-id mongodb/encryption-key \
  --query SecretString --output text
```

**Azure:**
```bash
# Usar Azure Key Vault
az keyvault secret show \
  --vault-name myVault \
  --name mongodb-encryption-key \
  --query value -o tsv
```

**HashiCorp Vault:**
```bash
# Usar Vault
vault kv get -field=key secret/mongodb/encryption-key
```

#### 3. Rotación de Claves

**Proceso Recomendado:**
1. Generar nueva clave
2. Re-cifrar backups importantes con la nueva clave
3. Almacenar la clave antigua de forma segura (para backups históricos)
4. Actualizar la clave en los sistemas que la usan
5. Documentar el cambio con fecha y motivo

**Ejemplo de Script de Rotación:**
```bash
#!/bin/bash
# rotate-encryption-key.sh

OLD_KEY="$MONGO_ENCRYPTION_KEY"
NEW_KEY="$(openssl rand -base64 32)"

# Descifrar y re-cifrar backups importantes
for backup in ./backups/*.encrypted; do
  temp_file="${backup%.encrypted}"
  
  # Descifrar con clave antigua
  mongodb-br decrypt --file "$backup" \
    --encryption-key "$OLD_KEY" \
    --output "$temp_file"
  
  # Cifrar con clave nueva
  mongodb-br encrypt --file "$temp_file" \
    --encryption-key "$NEW_KEY" \
    --output "${backup}.new"
  
  # Reemplazar backup antiguo
  mv "${backup}.new" "$backup"
  rm "$temp_file"
done

# Actualizar clave en gestor de secretos
echo "$NEW_KEY" | save-to-secrets-manager mongodb/encryption-key
```

#### 4. Separación de Claves y Backups

**NUNCA hacer:**
- Almacenar claves en el mismo servidor que los backups
- Incluir claves en scripts de control de versiones
- Enviar claves por email o chat sin cifrar
- Almacenar claves en archivos de texto plano

**SÍ hacer:**
- Usar gestores de secretos dedicados
- Almacenar backups en un servidor y claves en otro
- Cifrar las claves de backup con claves maestras
- Limitar el acceso a las claves mediante RBAC

#### 5. Respaldo de Claves

**Estrategia Recomendada:**
1. **Copias de Seguridad Seguras:**
   - Imprimir la clave y guardarla en caja fuerte física
   - Usar servicios de custodia de claves
   - Dividir la clave usando Shamir's Secret Sharing

2. **Documentación:**
   - Registrar cuándo se creó cada clave
   - Qué backups están cifrados con cada clave
   - Procedimiento de recuperación de claves

3. **Pruebas Periódicas:**
   - Verificar que puedes descifrar backups antiguos
   - Validar el proceso de recuperación de claves
   - Documentar problemas encontrados

### Validación de Seguridad

**Tests Implementados:**
- ✅ Cifrado/descifrado con claves válidas
- ✅ Rechazo de claves demasiado cortas
- ✅ Detección de claves incorrectas (vía HMAC)
- ✅ Detección de archivos corruptos
- ✅ Integridad de archivos grandes (>1 MB)
- ✅ IVs únicos para cada cifrado

**Auditoría de Código:**
- ✅ Limpieza de claves en memoria con `Array.Clear()`
- ✅ Uso de `CryptoStream` con manejo apropiado de excepciones
- ✅ Validación HMAC antes de descifrado
- ✅ Comparación de bytes en tiempo constante (previene timing attacks)

### Consideraciones de Cumplimiento

**GDPR / Protección de Datos:**
- El cifrado AES-256 es considerado adecuado para datos personales
- Asegúrate de documentar el uso de cifrado en tu política de privacidad
- Mantén registros de cuándo se cifraron/descifraron datos

**HIPAA (Salud):**
- AES-256 cumple con los requisitos de cifrado de HIPAA
- Documenta el proceso de gestión de claves
- Implementa auditoría de accesos

**PCI-DSS (Pagos):**
- AES-256 es aceptable para datos de tarjetas
- Requiere gestión robusta de claves
- Necesita rotación periódica de claves

### Incidentes de Seguridad

**En caso de clave comprometida:**
1. **Inmediatamente:**
   - Revocar acceso a la clave comprometida
   - Generar nueva clave
   - Notificar al equipo de seguridad

2. **Evaluación:**
   - Determinar qué backups están afectados
   - Identificar si algún backup fue accedido
   - Revisar logs de acceso

3. **Remediación:**
   - Re-cifrar todos los backups con nueva clave
   - Actualizar sistemas que usan la clave
   - Revisar procesos para prevenir futuros incidentes

4. **Documentación:**
   - Registrar el incidente
   - Documentar lecciones aprendidas
   - Actualizar procedimientos de seguridad

## Conclusión

Las alertas de CodeQL sobre almacenamiento de contraseñas en texto claro son **conocidas, documentadas y mitigadas** en la medida de lo posible. Son una limitación inherente del uso de herramientas de MongoDB (mongodump/mongorestore/mongosh) que requieren credenciales como parámetros de línea de comandos.

Se han implementado todas las mitigaciones razonables dentro de estas limitaciones, y se han documentado recomendaciones de seguridad para entornos de producción.

**Mejoras de Seguridad Implementadas:**
- ✅ Cifrado AES-256-CBC con HMAC-SHA256 para backups en reposo
- ✅ Validación exhaustiva de claves de cifrado
- ✅ Sanitización de logs para prevenir exposición de credenciales
- ✅ Validación de caracteres peligrosos en entradas
- ✅ Tests de seguridad completos para cifrado

**Estado Final:** ✅ ACEPTABLE CON MITIGACIONES DOCUMENTADAS

**Última Actualización:** 2025-11-01 - Añadido cifrado AES-256 de backups
