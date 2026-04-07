# DEC - Dinheiro em Casa

Plataforma digital de literacia financeira para alunos do 10.o ao 12.o ano, desenvolvida no ambito do Projeto Integrador do 2.o ano da Licenciatura em Engenharia Informatica - ISTEC Porto (2025/2026).

## Sobre o Projeto

O DEC permite que os alunos criem "familias virtuais" e tomem decisoes financeiras (rendimentos, despesas, poupanca e credito), observando o impacto a curto e longo prazo atraves de simulacoes e dashboards interativos.

## Arquitetura

O sistema segue uma arquitetura de microservicos em containers Docker

Browser (Cliente)
    |
    | HTTPS/TLS
    v
[Nginx Reverse Proxy]       -> Porta 443
    |
    v
[Portal .NET Core MVC]      -> Interface do utilizador (TINT3)
    |
    v
[API Django REST]            -> Logica de negocio e autenticacao (SMUL2)
    |
    v
[Microsservico Python]       -> Calculos financeiros (PROG4 + CAFIN)
    |
    v
[MySQL]                      -> Base de dados (SGBDD)


## Tecnologias

Frontend -> .NET Core MVC 
API  ->  Django REST Framework (Python) 
Calculos  ->  Python (microsservico) 
Base de Dados ->  MySQL 
Reverse Proxy ->  Nginx 
Containers ->  Docker + Docker Compose 
Infraestrutura ->  Ubuntu Server, ufw, HTTPS/TLS 

## Funcionalidades Principais

->  Autenticacao segura (alunos, professores, administrador)
-> Criacao de perfis familiares (agregado, rendimentos, despesas)
-> Simulacao de juros compostos e planos de amortizacao (metodo frances)
-> Dashboard com graficos (saldos, taxa de esforco, evolucao de poupanca)
-> Desafios financeiros criados por professores
-> Relatorios de projecoes a 12/24 meses

## Como Executar

```bash
# Clonar o projeto
git clone https://github.com/nogueira002/dec-dinheiro-em-casa.git
cd dec-dinheiro-em-casa

# Subir todos os servicos
docker-compose up --build
```

## Equipa
Ricardo Nogueira 
Wellington

## Regras de Branching

- `main` - branch de producao (protegida)
- `develop` - branch de desenvolvimento
- `feature/<nome>` - novas funcionalidades
- `hotfix/<nome>` - correcoes urgentes

### Workflow
1. Criar branch a partir de `develop`: `git checkout -b feature/nome-da-feature develop`
2. Desenvolver e fazer commits
3. Abrir Pull Request para `develop`
4. Apos revisao, fazer merge


-> Projeto academico - ISTEC Porto, 2025/2026
