# Garage S3 — Mise en place en production

## Vue d'ensemble

Garage est un objet-store S3-compatible auto-hébergé. En production, il tourne en sidecar Docker sur le même hôte que l'API. Les credentials sont injectés via **Docker secrets** (fichiers sur disque) et lus par l'API au démarrage.

```
[Internet] → nginx → web-api:8080
                         ↓ S3 (http://garage:3900, réseau Docker interne)
                      garage:3900
```

---

## 1. Pré-requis

- Docker + Docker Compose installés sur le serveur
- Accès SSH au serveur de production
- Accès aux secrets GitHub du dépôt (`GARAGE_ACCESS_KEY`, `GARAGE_SECRET_KEY`)

---

## 2. Première installation sur le serveur

### 2.1 Créer le répertoire de travail

```bash
sudo mkdir -p /opt/nexus
sudo chown $USER:$USER /opt/nexus
```

### 2.2 Créer le fichier de configuration Garage

```bash
mkdir -p /opt/nexus/Garage
cat > /opt/nexus/Garage/garage.toml << 'EOF'
metadata_dir = "/var/lib/garage/meta"
data_dir     = "/var/lib/garage/data"

replication_factor = 1

rpc_bind_addr = "0.0.0.0:3901"
rpc_secret    = "<générer avec: openssl rand -hex 32>"

[s3_api]
s3_region    = "garage"
api_bind_addr = "0.0.0.0:3900"

[admin]
api_bind_addr = "0.0.0.0:3902"
EOF
```

> **Important :** remplacer `<générer avec: openssl rand -hex 32>` par une vraie valeur :
> ```bash
> openssl rand -hex 32
> ```
> La `rpc_secret` doit être identique sur tous les nœuds Garage (ici un seul nœud).

### 2.3 Copier le `docker-compose.yml` sur le serveur

Le `docker-compose.yml` à la racine du dépôt définit déjà le service `garage` via les secrets Docker. Copier ce fichier dans `/opt/nexus/`.

### 2.4 Démarrer Garage seul (avant d'initialiser)

```bash
cd /opt/nexus
docker compose up -d garage
docker compose logs -f garage   # attendre "Garage is ready"
```

---

## 3. Initialisation du nœud Garage (une seule fois)

### 3.1 Récupérer l'ID du nœud

```bash
docker exec garage /garage node id
# Exemple de sortie :
# e2ee7984b3d5... (adresse IP)
```

Copier la partie hexadécimale (avant l'espace).

### 3.2 Assigner le layout

```bash
NODE_ID=<id_copié_ci-dessus>
docker exec garage /garage layout assign -z prod -c 100G "$NODE_ID"
docker exec garage /garage layout apply --version 1
docker exec garage /garage layout show   # vérifier
```

> `-c 100G` = capacité allouée pour le stockage. Adapter selon le disque disponible.

### 3.3 Créer la clé d'accès

```bash
docker exec garage /garage key create nexus-prod
```

Sortie :
```
Key name: nexus-prod
Key ID:   GKxxxxxxxxxxxxxxxxxxxx
Secret key: <64 caractères hex>
```

**Conserver ces valeurs — elles ne sont plus récupérables après.**

### 3.4 Créer le bucket et accorder les droits

```bash
docker exec garage /garage bucket create nexus-attachments
docker exec garage /garage bucket allow nexus-attachments --read --write --key GKxxxx
```

Pour rendre les fichiers lisibles publiquement (URLs directes) :
```bash
docker exec garage /garage bucket allow nexus-attachments --read --anonymous
```

---

## 4. Injection des credentials

### 4.1 Docker secrets (fichiers sur disque)

```bash
sudo mkdir -p /opt/nexus/secrets
echo -n "GKxxxxxxxxxxxxxxxxxxxx" | sudo tee /opt/nexus/secrets/garage_access_key
echo -n "<secret_64_hex>"        | sudo tee /opt/nexus/secrets/garage_secret_key
sudo chmod 600 /opt/nexus/secrets/garage_access_key /opt/nexus/secrets/garage_secret_key
```

Ces fichiers sont montés en lecture seule dans le container `web-api` (voir `docker-compose.yml`) et lus par `GarageAttachmentStorage` via `ReadSecret()`.

### 4.2 GitHub Actions secrets

Dans **Settings → Secrets and variables → Actions** du dépôt, créer :

| Nom              | Valeur                    |
|------------------|---------------------------|
| `GARAGE_ACCESS_KEY` | `GKxxxxxxxxxxxxxxxxxxxx` |
| `GARAGE_SECRET_KEY` | `<secret_64_hex>`        |

Ces secrets sont utilisés par le job `deploy` du workflow CI/CD pour créer (ou recréer) les Docker secrets sur le runner self-hosted :

```yaml
env:
  GARAGE_ACCESS_KEY: ${{ secrets.GARAGE_ACCESS_KEY }}
  GARAGE_SECRET_KEY: ${{ secrets.GARAGE_SECRET_KEY }}
```

> Le workflow actuel passe les secrets en variables d'environnement. Si tu veux utiliser `docker secret create` (Swarm), adapter le step `Pull & redeploy` en conséquence.

---

## 5. Configuration de l'API

`appsettings.Production.json` est déjà configuré :

```json
{
  "Storage": {
    "Provider": "Garage",
    "Endpoint":  "http://garage:3900",
    "BucketName": "nexus-attachments",
    "AccessKey": "",
    "SecretKey":  ""
  }
}
```

`AccessKey` et `SecretKey` vides = l'API lit les fichiers Docker secrets (`/run/secrets/garage_access_key` et `/run/secrets/garage_secret_key`). Aucune modification à faire dans `appsettings.Production.json`.

---

## 6. Démarrage complet

```bash
cd /opt/nexus
docker compose pull
docker compose up -d
docker compose ps   # tous les services doivent être "Up"
```

---

## 7. Vérification

```bash
# Statut du nœud Garage
docker exec garage /garage status

# Lister les buckets
docker exec garage /garage bucket list

# Lister les clés
docker exec garage /garage key list

# Test upload manuel (nécessite awscli)
aws s3 cp test.txt s3://nexus-attachments/test.txt \
  --endpoint-url http://localhost:3900 \
  --region garage \
  --aws-access-key-id GKxxxx \
  --aws-secret-access-key <secret>
```

---

## 8. Renouvellement des credentials

Si les credentials sont compromis ou expirés :

```bash
# Supprimer l'ancienne clé
docker exec garage /garage key delete --yes GKancienne_clé

# Créer une nouvelle clé
docker exec garage /garage key create nexus-prod-v2
docker exec garage /garage bucket allow nexus-attachments --read --write --key GKnouvelle_clé

# Mettre à jour les fichiers secrets sur le serveur
echo -n "GKnouvelle_clé"  | sudo tee /opt/nexus/secrets/garage_access_key
echo -n "<nouveau_secret>" | sudo tee /opt/nexus/secrets/garage_secret_key

# Redémarrer l'API pour recharger les secrets
docker compose restart web-api

# Mettre à jour les GitHub Actions secrets
```

---

## 9. Sauvegarde

Les données Garage sont dans les volumes Docker :

```
garage_meta  →  métadonnées (index des objets)
garage_data  →  données binaires
```

Pour sauvegarder :
```bash
# Arrêter Garage proprement avant backup
docker compose stop garage

# Backup des volumes
docker run --rm \
  -v garage_meta:/src/meta:ro \
  -v garage_data:/src/data:ro \
  -v /backup/garage:/dst \
  alpine tar czf /dst/garage-$(date +%Y%m%d).tar.gz -C /src .

docker compose start garage
```

---

## Résumé des ports

| Port | Service          | Accès                          |
|------|------------------|-------------------------------|
| 3900 | S3 API           | Interne Docker uniquement      |
| 3901 | RPC inter-nœuds  | Interne (un seul nœud ici)     |
| 3902 | Admin API        | Interne (pour les `garage` CLI)|

Ne pas exposer ces ports publiquement. L'API accède à Garage via le réseau Docker interne (`http://garage:3900`).
