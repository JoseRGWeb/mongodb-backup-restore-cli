# Guía de Integración CI/CD

Esta guía proporciona ejemplos completos de integración de MongoDB Backup & Restore CLI en pipelines de CI/CD con diferentes plataformas.

## Tabla de Contenidos

- [GitHub Actions](#github-actions)
- [GitLab CI/CD](#gitlab-cicd)
- [Azure DevOps](#azure-devops)
- [Jenkins](#jenkins)
- [CircleCI](#circleci)
- [Mejores Prácticas](#mejores-prácticas)
- [Troubleshooting](#troubleshooting)

---

## GitHub Actions

### Ejemplo 1: Backup Automático Diario

```yaml
# .github/workflows/mongodb-backup.yml
name: MongoDB Backup Diario

on:
  schedule:
    # Ejecutar a las 2:00 AM UTC todos los días
    - cron: '0 2 * * *'
  workflow_dispatch: # Permitir ejecución manual

jobs:
  backup:
    name: Backup de MongoDB
    runs-on: ubuntu-latest
    
    steps:
      - name: Checkout código
        uses: actions/checkout@v4
      
      - name: Configurar .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'
      
      - name: Instalar MongoDB Backup CLI
        run: dotnet tool install --global MongoBackupRestore.Cli
      
      - name: Ejecutar Backup
        env:
          MONGO_HOST: ${{ secrets.MONGO_HOST }}
          MONGO_PORT: ${{ secrets.MONGO_PORT }}
          MONGO_USER: ${{ secrets.MONGO_USER }}
          MONGO_PASSWORD: ${{ secrets.MONGO_PASSWORD }}
          MONGO_AUTH_DB: admin
          MONGO_ENCRYPTION_KEY: ${{ secrets.ENCRYPTION_KEY }}
          MONGO_COMPRESSION: zip
          MONGO_RETENTION_DAYS: 30
        run: |
          mkdir -p ./backups
          mongodb-br backup \
            --db ProductionDB \
            --out ./backups/backup-$(date +%Y%m%d-%H%M%S) \
            --compress zip \
            --encrypt \
            --retention-days 30 \
            --verbose
      
      - name: Subir Backup a S3
        uses: aws-actions/configure-aws-credentials@v4
        with:
          aws-access-key-id: ${{ secrets.AWS_ACCESS_KEY_ID }}
          aws-secret-access-key: ${{ secrets.AWS_SECRET_ACCESS_KEY }}
          aws-region: us-east-1
      
      - name: Copiar a S3
        run: |
          BACKUP_FILE=$(find ./backups -name "*.encrypted" -type f)
          aws s3 cp "$BACKUP_FILE" \
            s3://mi-empresa-backups/mongodb/production/ \
            --storage-class STANDARD_IA
      
      - name: Notificar Slack en Éxito
        if: success()
        uses: slackapi/slack-github-action@v1
        with:
          webhook-url: ${{ secrets.SLACK_WEBHOOK_URL }}
          payload: |
            {
              "text": "✅ Backup de MongoDB completado exitosamente",
              "blocks": [
                {
                  "type": "section",
                  "text": {
                    "type": "mrkdwn",
                    "text": "*Backup de MongoDB Completado*\n✅ Base de datos: ProductionDB\n📅 Fecha: $(date)"
                  }
                }
              ]
            }
      
      - name: Notificar Slack en Fallo
        if: failure()
        uses: slackapi/slack-github-action@v1
        with:
          webhook-url: ${{ secrets.SLACK_WEBHOOK_URL }}
          payload: |
            {
              "text": "❌ Error en backup de MongoDB",
              "blocks": [
                {
                  "type": "section",
                  "text": {
                    "type": "mrkdwn",
                    "text": "*Error en Backup de MongoDB*\n❌ El backup ha fallado\n🔗 Ver workflow: ${{ github.server_url }}/${{ github.repository }}/actions/runs/${{ github.run_id }}"
                  }
                }
              ]
            }
```

---

### Ejemplo 2: Backup Pre-Deployment

```yaml
# .github/workflows/deploy-with-backup.yml
name: Deploy con Backup

on:
  push:
    branches: [main]

jobs:
  backup-before-deploy:
    name: Backup Pre-Deploy
    runs-on: ubuntu-latest
    outputs:
      backup-path: ${{ steps.backup.outputs.path }}
    
    steps:
      - name: Configurar .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'
      
      - name: Instalar MongoDB Backup CLI
        run: dotnet tool install --global MongoBackupRestore.Cli
      
      - name: Crear Backup
        id: backup
        env:
          MONGO_HOST: ${{ secrets.MONGO_HOST }}
          MONGO_USER: ${{ secrets.MONGO_USER }}
          MONGO_PASSWORD: ${{ secrets.MONGO_PASSWORD }}
          MONGO_AUTH_DB: admin
          MONGO_ENCRYPTION_KEY: ${{ secrets.ENCRYPTION_KEY }}
        run: |
          BACKUP_DIR="./backups/pre-deploy-$(date +%Y%m%d-%H%M%S)"
          mkdir -p $BACKUP_DIR
          
          mongodb-br backup \
            --db ProductionDB \
            --out $BACKUP_DIR \
            --compress zip \
            --encrypt \
            --verbose
          
          echo "path=$BACKUP_DIR" >> $GITHUB_OUTPUT
      
      - name: Subir Backup como Artefacto
        uses: actions/upload-artifact@v4
        with:
          name: pre-deploy-backup
          path: ${{ steps.backup.outputs.path }}
          retention-days: 7
  
  deploy:
    name: Deploy Aplicación
    needs: backup-before-deploy
    runs-on: ubuntu-latest
    
    steps:
      - name: Checkout código
        uses: actions/checkout@v4
      
      - name: Deploy a Producción
        run: |
          # Tu proceso de deploy aquí
          echo "Deploying to production..."
      
      - name: Rollback en Caso de Fallo
        if: failure()
        needs: backup-before-deploy
        run: |
          # Descargar backup y restaurar
          echo "Restaurando desde backup..."
```

---

### Ejemplo 3: Backup en Docker con GitHub Actions

```yaml
# .github/workflows/docker-backup.yml
name: Backup MongoDB en Docker

on:
  schedule:
    - cron: '0 */6 * * *' # Cada 6 horas
  workflow_dispatch:

jobs:
  backup-docker:
    name: Backup Docker MongoDB
    runs-on: ubuntu-latest
    
    services:
      mongodb:
        image: mongo:latest
        ports:
          - 27017:27017
        env:
          MONGO_INITDB_ROOT_USERNAME: admin
          MONGO_INITDB_ROOT_PASSWORD: password
        options: >-
          --health-cmd "mongosh --eval 'db.adminCommand(\"ping\")'"
          --health-interval 10s
          --health-timeout 5s
          --health-retries 5
    
    steps:
      - name: Configurar .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'
      
      - name: Instalar MongoDB Backup CLI
        run: dotnet tool install --global MongoBackupRestore.Cli
      
      - name: Backup desde Docker
        run: |
          # Listar contenedores
          docker ps
          
          # Obtener nombre del contenedor MongoDB
          CONTAINER_ID=$(docker ps --filter "ancestor=mongo:latest" --format "{{.ID}}" | head -1)
          CONTAINER_NAME=$(docker ps --filter "id=$CONTAINER_ID" --format "{{.Names}}")
          
          echo "Contenedor MongoDB: $CONTAINER_NAME"
          
          # Ejecutar backup
          mongodb-br backup \
            --db testdb \
            --in-docker \
            --container-name $CONTAINER_NAME \
            --user admin \
            --password password \
            --auth-db admin \
            --out ./backups/docker-backup \
            --verbose
```

---

## GitLab CI/CD

### Ejemplo 1: Pipeline de Backup Básico

```yaml
# .gitlab-ci.yml
stages:
  - backup
  - verify
  - notify

variables:
  BACKUP_DIR: "./backups"
  DOTNET_VERSION: "8.0"

backup_mongodb:
  stage: backup
  image: mcr.microsoft.com/dotnet/sdk:8.0
  before_script:
    - dotnet tool install --global MongoBackupRestore.Cli
    - export PATH="$PATH:/root/.dotnet/tools"
  script:
    - mkdir -p $BACKUP_DIR
    - |
      mongodb-br backup \
        --db $DB_NAME \
        --host $MONGO_HOST \
        --port $MONGO_PORT \
        --user $MONGO_USER \
        --password $MONGO_PASSWORD \
        --auth-db admin \
        --out $BACKUP_DIR/backup-$(date +%Y%m%d-%H%M%S) \
        --compress zip \
        --encrypt \
        --encryption-key $ENCRYPTION_KEY \
        --verbose
  artifacts:
    paths:
      - $BACKUP_DIR
    expire_in: 7 days
  only:
    - schedules
  tags:
    - docker

verify_backup:
  stage: verify
  image: mcr.microsoft.com/dotnet/sdk:8.0
  script:
    - echo "Verificando integridad del backup..."
    - |
      BACKUP_FILE=$(find $BACKUP_DIR -name "*.encrypted" -type f)
      if [ -f "$BACKUP_FILE" ]; then
        echo "✓ Backup encontrado: $BACKUP_FILE"
        echo "Tamaño: $(du -h $BACKUP_FILE | cut -f1)"
      else
        echo "✗ Backup no encontrado"
        exit 1
      fi
  dependencies:
    - backup_mongodb
  tags:
    - docker

notify_success:
  stage: notify
  image: curlimages/curl:latest
  script:
    - |
      curl -X POST $SLACK_WEBHOOK_URL \
        -H 'Content-Type: application/json' \
        -d "{\"text\":\"✅ Backup de MongoDB completado en GitLab CI\"}"
  when: on_success
  tags:
    - docker

notify_failure:
  stage: notify
  image: curlimages/curl:latest
  script:
    - |
      curl -X POST $SLACK_WEBHOOK_URL \
        -H 'Content-Type: application/json' \
        -d "{\"text\":\"❌ Error en backup de MongoDB en GitLab CI\"}"
  when: on_failure
  tags:
    - docker
```

---

### Ejemplo 2: Pipeline Multi-Ambiente

```yaml
# .gitlab-ci.yml
stages:
  - backup-dev
  - backup-staging
  - backup-production

.backup_template: &backup_template
  image: mcr.microsoft.com/dotnet/sdk:8.0
  before_script:
    - dotnet tool install --global MongoBackupRestore.Cli
    - export PATH="$PATH:/root/.dotnet/tools"
  script:
    - |
      mongodb-br backup \
        --db $DB_NAME \
        --host $MONGO_HOST \
        --user $MONGO_USER \
        --password $MONGO_PASSWORD \
        --auth-db admin \
        --out ./backups/$CI_ENVIRONMENT_NAME-$(date +%Y%m%d) \
        --compress zip \
        --encrypt \
        --encryption-key $ENCRYPTION_KEY \
        --retention-days $RETENTION_DAYS \
        --verbose
  artifacts:
    paths:
      - ./backups
    expire_in: 30 days

backup_dev:
  <<: *backup_template
  stage: backup-dev
  variables:
    DB_NAME: "DevDB"
    MONGO_HOST: $DEV_MONGO_HOST
    MONGO_USER: $DEV_MONGO_USER
    MONGO_PASSWORD: $DEV_MONGO_PASSWORD
    ENCRYPTION_KEY: $DEV_ENCRYPTION_KEY
    RETENTION_DAYS: "7"
  environment:
    name: development
  only:
    - schedules

backup_staging:
  <<: *backup_template
  stage: backup-staging
  variables:
    DB_NAME: "StagingDB"
    MONGO_HOST: $STAGING_MONGO_HOST
    MONGO_USER: $STAGING_MONGO_USER
    MONGO_PASSWORD: $STAGING_MONGO_PASSWORD
    ENCRYPTION_KEY: $STAGING_ENCRYPTION_KEY
    RETENTION_DAYS: "14"
  environment:
    name: staging
  only:
    - schedules

backup_production:
  <<: *backup_template
  stage: backup-production
  variables:
    DB_NAME: "ProductionDB"
    MONGO_HOST: $PROD_MONGO_HOST
    MONGO_USER: $PROD_MONGO_USER
    MONGO_PASSWORD: $PROD_MONGO_PASSWORD
    ENCRYPTION_KEY: $PROD_ENCRYPTION_KEY
    RETENTION_DAYS: "30"
  environment:
    name: production
  only:
    - schedules
  when: manual # Requiere aprobación manual
```

---

## Azure DevOps

### Ejemplo 1: Pipeline YAML de Backup

```yaml
# azure-pipelines.yml
trigger: none # Solo manual o scheduled

schedules:
  - cron: "0 2 * * *"
    displayName: Backup Diario a las 2 AM
    branches:
      include:
        - main
    always: true

pool:
  vmImage: 'ubuntu-latest'

variables:
  - group: mongodb-secrets # Variable group con secretos
  - name: backupDir
    value: '$(Build.ArtifactStagingDirectory)/backups'

stages:
  - stage: Backup
    displayName: 'Backup MongoDB'
    jobs:
      - job: BackupJob
        displayName: 'Ejecutar Backup'
        steps:
          - task: UseDotNet@2
            displayName: 'Instalar .NET SDK'
            inputs:
              packageType: 'sdk'
              version: '8.x'
          
          - script: |
              dotnet tool install --global MongoBackupRestore.Cli
              echo "##vso[task.prependpath]$HOME/.dotnet/tools"
            displayName: 'Instalar MongoDB Backup CLI'
          
          - script: |
              mkdir -p $(backupDir)
              
              mongodb-br backup \
                --db $(dbName) \
                --host $(mongoHost) \
                --port $(mongoPort) \
                --user $(mongoUser) \
                --password $(mongoPassword) \
                --auth-db admin \
                --out $(backupDir)/backup-$(date +%Y%m%d-%H%M%S) \
                --compress zip \
                --encrypt \
                --encryption-key $(encryptionKey) \
                --retention-days 30 \
                --verbose \
                --log-file $(backupDir)/backup.log
            displayName: 'Ejecutar Backup'
            env:
              MONGO_HOST: $(mongoHost)
              MONGO_USER: $(mongoUser)
              MONGO_PASSWORD: $(mongoPassword)
              MONGO_ENCRYPTION_KEY: $(encryptionKey)
          
          - task: PublishBuildArtifacts@1
            displayName: 'Publicar Artefactos de Backup'
            inputs:
              PathtoPublish: '$(backupDir)'
              ArtifactName: 'mongodb-backup'
              publishLocation: 'Container'
          
          - task: AzureFileCopy@4
            displayName: 'Copiar Backup a Azure Blob Storage'
            inputs:
              SourcePath: '$(backupDir)'
              azureSubscription: 'Azure Subscription'
              Destination: 'AzureBlob'
              storage: 'mystorageaccount'
              ContainerName: 'mongodb-backups'
          
          - script: |
              if [ -f $(backupDir)/backup.log ]; then
                echo "=== Logs del Backup ==="
                cat $(backupDir)/backup.log
              fi
            displayName: 'Mostrar Logs'
            condition: always()

  - stage: Notify
    displayName: 'Notificaciones'
    dependsOn: Backup
    jobs:
      - job: NotifySuccess
        displayName: 'Notificar Éxito'
        condition: succeeded()
        steps:
          - script: |
              curl -X POST $(slackWebhook) \
                -H 'Content-Type: application/json' \
                -d '{"text":"✅ Backup de MongoDB completado en Azure DevOps"}'
            displayName: 'Enviar Notificación Slack'
      
      - job: NotifyFailure
        displayName: 'Notificar Fallo'
        condition: failed()
        steps:
          - script: |
              curl -X POST $(slackWebhook) \
                -H 'Content-Type: application/json' \
                -d '{"text":"❌ Error en backup de MongoDB en Azure DevOps"}'
            displayName: 'Enviar Alerta Slack'
```

---

## Jenkins

### Ejemplo 1: Pipeline Declarativo

```groovy
// Jenkinsfile
pipeline {
    agent any
    
    triggers {
        cron('0 2 * * *') // Diariamente a las 2 AM
    }
    
    environment {
        DOTNET_CLI_HOME = '/tmp/dotnet'
        BACKUP_DIR = "${WORKSPACE}/backups"
        MONGO_HOST = credentials('mongodb-host')
        MONGO_USER = credentials('mongodb-user')
        MONGO_PASSWORD = credentials('mongodb-password')
        ENCRYPTION_KEY = credentials('mongodb-encryption-key')
    }
    
    stages {
        stage('Preparación') {
            steps {
                script {
                    sh 'mkdir -p ${BACKUP_DIR}'
                }
            }
        }
        
        stage('Instalar CLI') {
            steps {
                sh '''
                    dotnet tool install --global MongoBackupRestore.Cli
                    export PATH="$PATH:$HOME/.dotnet/tools"
                '''
            }
        }
        
        stage('Backup MongoDB') {
            steps {
                sh '''
                    export PATH="$PATH:$HOME/.dotnet/tools"
                    
                    mongodb-br backup \
                        --db ProductionDB \
                        --host ${MONGO_HOST} \
                        --port 27017 \
                        --user ${MONGO_USER} \
                        --password ${MONGO_PASSWORD} \
                        --auth-db admin \
                        --out ${BACKUP_DIR}/backup-$(date +%Y%m%d-%H%M%S) \
                        --compress zip \
                        --encrypt \
                        --encryption-key ${ENCRYPTION_KEY} \
                        --retention-days 30 \
                        --verbose \
                        --log-file ${BACKUP_DIR}/backup.log
                '''
            }
        }
        
        stage('Archivar Backup') {
            steps {
                archiveArtifacts artifacts: 'backups/**/*', fingerprint: true
            }
        }
        
        stage('Subir a S3') {
            steps {
                script {
                    withAWS(credentials: 'aws-credentials', region: 'us-east-1') {
                        s3Upload(
                            file: "${BACKUP_DIR}",
                            bucket: 'mi-empresa-backups',
                            path: 'mongodb/production/'
                        )
                    }
                }
            }
        }
    }
    
    post {
        success {
            slackSend(
                color: 'good',
                message: "✅ Backup de MongoDB completado exitosamente\nJob: ${env.JOB_NAME}\nBuild: ${env.BUILD_NUMBER}"
            )
        }
        failure {
            slackSend(
                color: 'danger',
                message: "❌ Error en backup de MongoDB\nJob: ${env.JOB_NAME}\nBuild: ${env.BUILD_NUMBER}\nLogs: ${env.BUILD_URL}console"
            )
        }
        always {
            // Publicar logs
            publishHTML([
                reportDir: "${BACKUP_DIR}",
                reportFiles: 'backup.log',
                reportName: 'Backup Logs'
            ])
        }
    }
}
```

---

### Ejemplo 2: Pipeline con Múltiples Bases de Datos

```groovy
// Jenkinsfile - Múltiples Databases
pipeline {
    agent any
    
    parameters {
        choice(
            name: 'DATABASES',
            choices: ['all', 'ProductionDB', 'AnalyticsDB', 'LogsDB'],
            description: 'Base de datos a respaldar'
        )
    }
    
    environment {
        BACKUP_DIR = "${WORKSPACE}/backups"
    }
    
    stages {
        stage('Backup') {
            steps {
                script {
                    def databases = params.DATABASES == 'all' 
                        ? ['ProductionDB', 'AnalyticsDB', 'LogsDB']
                        : [params.DATABASES]
                    
                    databases.each { db ->
                        sh """
                            export PATH="\$PATH:\$HOME/.dotnet/tools"
                            
                            mongodb-br backup \
                                --db ${db} \
                                --out ${BACKUP_DIR}/${db}/backup-\$(date +%Y%m%d) \
                                --compress zip \
                                --encrypt \
                                --retention-days 30 \
                                --verbose
                        """
                    }
                }
            }
        }
    }
}
```

---

## CircleCI

### Ejemplo: Configuración de Backup

```yaml
# .circleci/config.yml
version: 2.1

orbs:
  aws-s3: circleci/aws-s3@3.0

jobs:
  mongodb-backup:
    docker:
      - image: mcr.microsoft.com/dotnet/sdk:8.0
    
    working_directory: ~/project
    
    steps:
      - checkout
      
      - run:
          name: Instalar MongoDB Backup CLI
          command: |
            dotnet tool install --global MongoBackupRestore.Cli
            echo 'export PATH="$PATH:$HOME/.dotnet/tools"' >> $BASH_ENV
      
      - run:
          name: Ejecutar Backup
          command: |
            mkdir -p ./backups
            
            mongodb-br backup \
              --db $DB_NAME \
              --host $MONGO_HOST \
              --user $MONGO_USER \
              --password $MONGO_PASSWORD \
              --auth-db admin \
              --out ./backups/backup-$(date +%Y%m%d-%H%M%S) \
              --compress zip \
              --encrypt \
              --encryption-key $ENCRYPTION_KEY \
              --retention-days 30 \
              --verbose
      
      - aws-s3/copy:
          from: ./backups
          to: 's3://mi-empresa-backups/mongodb/'
          arguments: '--recursive'
      
      - store_artifacts:
          path: ./backups
          destination: mongodb-backups

workflows:
  version: 2
  backup-schedule:
    triggers:
      - schedule:
          cron: "0 2 * * *"
          filters:
            branches:
              only:
                - main
    jobs:
      - mongodb-backup
```

---

## Mejores Prácticas

### 1. Gestión de Secretos

✅ **Usar gestores de secretos de la plataforma**:

**GitHub Actions:**
```yaml
env:
  MONGO_PASSWORD: ${{ secrets.MONGO_PASSWORD }}
  ENCRYPTION_KEY: ${{ secrets.ENCRYPTION_KEY }}
```

**GitLab CI:**
```yaml
variables:
  MONGO_PASSWORD: $CI_MONGO_PASSWORD # Configurado en CI/CD settings
```

**Azure DevOps:**
```yaml
variables:
  - group: mongodb-secrets # Variable group
```

**Jenkins:**
```groovy
environment {
    MONGO_PASSWORD = credentials('mongodb-password')
}
```

---

### 2. Notificaciones

✅ **Configurar alertas para fallos**:

```yaml
- name: Notificar en Fallo
  if: failure()
  run: |
    # Slack
    curl -X POST $SLACK_WEBHOOK \
      -d '{"text":"❌ Backup failed"}'
    
    # Email (usando sendmail)
    echo "Backup failed" | mail -s "Backup Alert" ops@ejemplo.com
    
    # PagerDuty
    curl -X POST https://events.pagerduty.com/v2/enqueue \
      -H 'Content-Type: application/json' \
      -d '{
        "routing_key": "'$PAGERDUTY_KEY'",
        "event_action": "trigger",
        "payload": {
          "summary": "MongoDB Backup Failed",
          "severity": "critical",
          "source": "CI/CD Pipeline"
        }
      }'
```

---

### 3. Verificación de Backups

✅ **Verificar integridad después del backup**:

```yaml
- name: Verificar Backup
  run: |
    BACKUP_FILE=$(find ./backups -name "*.encrypted" -type f)
    
    # Verificar que existe
    if [ ! -f "$BACKUP_FILE" ]; then
      echo "❌ Backup file not found"
      exit 1
    fi
    
    # Verificar tamaño mínimo
    MIN_SIZE=1048576 # 1 MB
    ACTUAL_SIZE=$(stat -f%z "$BACKUP_FILE" 2>/dev/null || stat -c%s "$BACKUP_FILE")
    
    if [ $ACTUAL_SIZE -lt $MIN_SIZE ]; then
      echo "❌ Backup file too small: $ACTUAL_SIZE bytes"
      exit 1
    fi
    
    echo "✅ Backup verified: $ACTUAL_SIZE bytes"
```

---

### 4. Almacenamiento Redundante

✅ **Guardar backups en múltiples ubicaciones**:

```yaml
- name: Backup Redundante
  run: |
    BACKUP_FILE=$(find ./backups -name "*.encrypted" -type f)
    
    # Subir a S3
    aws s3 cp "$BACKUP_FILE" s3://primary-backups/
    
    # Subir a Azure Blob
    az storage blob upload \
      --container-name backups \
      --file "$BACKUP_FILE"
    
    # Subir a Google Cloud Storage
    gsutil cp "$BACKUP_FILE" gs://backup-bucket/
```

---

## Troubleshooting

### Error: "dotnet tool not found"

**Solución**:
```yaml
- name: Agregar dotnet tools al PATH
  run: echo "$HOME/.dotnet/tools" >> $GITHUB_PATH
```

---

### Error: "Permission denied"

**Solución**:
```yaml
- name: Configurar permisos
  run: |
    mkdir -p ./backups
    chmod 755 ./backups
```

---

### Timeout en Backups Grandes

**Solución**:
```yaml
- name: Backup con timeout extendido
  timeout-minutes: 60 # Aumentar timeout
  run: mongodb-br backup ...
```

---

## Recursos Adicionales

- [Documentación Principal](./README.md)
- [Ejemplos End-to-End](./EJEMPLOS_END_TO_END.md)
- [Variables de Entorno](./VARIABLES_ENTORNO.md)
- [Logs y Debugging](./LOGS_Y_DEBUGGING.md)

## Soporte

Para ayuda adicional:
1. Consulta la [documentación completa](./README.md)
2. Revisa los [issues](https://github.com/JoseRGWeb/mongodb-backup-restore-cli/issues)
3. Abre un [nuevo issue](https://github.com/JoseRGWeb/mongodb-backup-restore-cli/issues/new)
