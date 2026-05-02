# 📦 DEC – Dinheiro em Casa (Backend .NET MVC)

Este projeto é o backend principal da plataforma **DEC**, responsável por gerir utilizadores, dados educativos e comunicação com microserviços.

---

## 🚀 Tecnologias

* .NET 9 (MVC)
* Entity Framework Core
* MySQL (via Docker)
* Docker Compose

---

## 📁 Estrutura

```text
dotnet-mvc/
 ├── Controllers/
 ├── Models/
 ├── Data/
 │    └── AppDbContext.cs
 ├── Migrations/
 ├── Program.cs
 ├── appsettings.json
 └── docker-compose.yml
```

---

## ⚙️ Pré-requisitos

* .NET SDK 9
* Docker
* Docker Compose

---

## 🐳 1. Subir o MySQL com Docker

Na raiz do projeto:

```bash
docker compose up -d
```

Se a porta 3306 estiver ocupada, usa 3307 no `docker-compose.yml`:

```yaml
ports:
  - "3307:3306"
```

---

## 🔌 2. Configurar ligação à base de dados

No `appsettings.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "server=localhost;port=3307;database=dec_db;user=root;password=root"
}
```

---

## 🧱 3. Criar a base de dados (se necessário)

```bash
docker exec -it dec_mysql mysql -u root -p
```

Password:

```text
root
```

Depois:

```sql
CREATE DATABASE dec_db;
EXIT;
```

---

## 🧬 4. Executar migrations

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

---

## ✅ Verificar se está a funcionar

```bash
docker exec -it dec_mysql mysql -u root -p
```

```sql
USE dec_db;
SHOW TABLES;
```

Deves ver:

```text
Users
__EFMigrationsHistory
```

---

## ▶️ 5. Executar a aplicação

```bash
dotnet run
```

A aplicação estará disponível em:

```text
http://localhost:5000
```

---

## 🧠 Notas

* `Role` e `UserStatus` estão dentro da tabela `Users`
* O MySQL corre em container Docker
* As migrations são geridas automaticamente pelo Entity Framework

---

## 🚀 Próximos passos

* Criar tabela `Classes`
* Implementar CRUD de Users
* Integrar com Django API
