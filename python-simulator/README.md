# Python Simulator — DEC Dinheiro em Casa

## Estrutura
- `app/main.py` — Ponto de entrada e endpoints
- `app/strategies/` — Padrão Strategy para modelos de simulação
- `app/factories/` — Padrão Factory para criação de cenários

## Como correr localmente

### Sem Docker
pip install -r requirements.txt
python app/main.py

### Com Docker
docker build -t python-simulator .
docker run -p 5000:5000 python-simulator

## Endpoints
| Método | Endpoint | Descrição |
|---|---|---|
| GET | /health-check | Verifica se o serviço está ativo |