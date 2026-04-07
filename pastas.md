# Pasta nginx 

É um reverse proxy e como se fosse um porteiro/segurança do sistema.

O que faz:
    - É o único ponto de entrada exposto à internet (porta 443)
    - Recebe TODOS os pedidos do browser
    - Redireciona para o serviço certo (dotnet-mvc ou django-api)
    - Trata do HTTPS/TLS (encriptação, o cadeado verde no browser)

Sem ele tinhamos de expor cada serviço individualmente, sem segurança.

# Pasta dotnet-mvc
É o portal web, o frontend que o utilizador vê e interage.

O que faz:
    - Páginas HTML com formulários, dashboards, gráficos
    - Login, criar famílias, ver simulações
    - Não faz cálculos — apenas mostra dados e envia pedidos à API

UC associada - TINT3 (Tecnologias de Internet III)

# Pasta django-api
É a API REST o cérebro do sistema.

O que faz:
    - Autenticação (quem pode aceder ao quê)
    - Regras de negócio (validar dados, limites, permissões)
    - CRUD — criar, ler, atualizar e apagar dados (famílias, despesas, objetivos)
    - Recebe pedidos do dotnet-mvc e responde com JSON

UC associada: SMUL2 (Sistemas Multimédia II)

# Pasta python-simulator
É o microserviço de cálculos financeiros, a calculadora do sistema.

O que faz:
    - Juros compostos (quanto cresce a poupança)
    - Prestações de crédito (método francês)
    - Projeções com inflação
    - Taxa de esforço mensal
    - Recebe pedidos da django-api e devolve resultados

UC associada: PROG4 (Programação IV) + CAFIN (Cálculo Financeiro)

# Pasta mysql/scripts
É a base de dados, onde tudo fica guardado.

O que faz:
    - Guarda utilizadores, cenários, lançamentos, simulações
    - A pasta scripts/ terá os ficheiros SQL para criar as tabelas

UC associada: SGBDD (Sistemas de Gestão de Bases de Dados)

# Pasta docs
É a pasta para documentação do projeto.

O que faz:
    - Diagramas UML (componentes, deployment, use cases)
    - Relatórios
    - Apresentações

# Ficheiro docker-compose.yml
É o ficheiro que orquestra tudo.

O que faz:
    - Define os 5 serviços (nginx, dotnet, django, python, mysql)
    - Com um comando (docker-compose up) levanta o sistema inteiro
    - Cada serviço corre no seu container isolado, mas comunicam entre si numa rede interna Docker