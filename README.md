# StackFood - Orders Service

Microserviço responsável pela criação, validação e gerenciamento completo do ciclo de vida dos pedidos.

## Tecnologias

- **.NET 8.0** - Framework principal
- **PostgreSQL** - Banco de dados SQL com EF Core
- **Entity Framework Core 8.0** - ORM
- **AWS SNS** - Publicação de eventos
- **AWS SQS** - Consumo de eventos de Payment e Production
- **LocalStack** - Emulação de serviços AWS localmente
- **Docker & Docker Compose** - Containerização

## Arquitetura

```
StackFood.Orders/
├── src/
│   ├── StackFood.Orders.API          # Controllers, Program.cs, Swagger
│   ├── StackFood.Orders.Application  # Use Cases, DTOs, Interfaces
│   ├── StackFood.Orders.Domain       # Entities, Value Objects, Events
│   └── StackFood.Orders.Infrastructure # Repositories, DbContext, Consumers, AWS
└── tests/
    └── StackFood.Orders.Tests        # Testes unitários e integração
```

## Domain Layer

### Entities
- **Order**: Pedido com status, customer e items
- **OrderItem**: Item individual do pedido

### Value Objects
- **Money**: Representação de valores monetários

### Status do Pedido
```csharp
public enum OrderStatus
{
    Pending,          // Aguardando processamento
    PaymentApproved,  // Pagamento aprovado
    InProduction,     // Em produção
    Ready,            // Pronto para retirada
    Completed,        // Concluído e entregue
    Cancelled         // Cancelado
}
```

### Eventos Publicados
- **OrderCreatedEvent**: Pedido criado (→ Payment Service)
- **OrderCancelledEvent**: Pedido cancelado
- **OrderCompletedEvent**: Pedido concluído

## Application Layer

### Use Cases
- **CreateOrderUseCase**: Criar pedido e validar com Products API
- **GetOrderByIdUseCase**: Buscar pedido por ID
- **GetAllOrdersUseCase**: Listar todos os pedidos
- **CancelOrderUseCase**: Cancelar pedido
- **UpdateOrderStatusUseCase**: Atualizar status (via eventos)

### DTOs
- **CreateOrderRequest**: Request para criar pedido
- **OrderDTO**: DTO completo do pedido
- **OrderItemDTO**: DTO de item do pedido

## Infrastructure Layer

### PostgreSQL + EF Core
- **OrdersDbContext**: Contexto com mapeamento de entidades
- **OrderRepository**: Repositório com queries otimizadas
- **Migrations**: Schema automático

### External Services
- **ProductService**: Integração HTTP com Products API
- **SNSEventPublisher**: Publicação de eventos no SNS

### Background Services (Consumers)
- **PaymentEventsConsumer**: Consome eventos de pagamento (SQS)
  - PaymentApproved → OrderStatus.PaymentApproved
  - PaymentRejected → OrderStatus.Cancelled
- **ProductionEventsConsumer**: Consome eventos de produção (SQS)
  - ProductionStarted → OrderStatus.InProduction
  - ProductionReady → OrderStatus.Ready
  - ProductionDelivered → OrderStatus.Completed

## API Endpoints

### Orders
```http
POST   /api/orders              # Criar pedido
GET    /api/orders              # Listar todos
GET    /api/orders/{id}         # Buscar por ID
POST   /api/orders/{id}/cancel  # Cancelar pedido
```

### Health Checks
```http
GET /health        # Health check geral
GET /health/ready  # Ready check
```

## Configuração

### appsettings.json
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5435;Database=stackfood_orders;Username=postgres;Password=postgres"
  },
  "ExternalServices": {
    "ProductsApiUrl": "http://localhost:8080"
  },
  "AWS": {
    "UseLocalStack": true,
    "LocalStack": {
      "ServiceUrl": "http://localhost:4566"
    },
    "SNS": {
      "OrderCreatedTopicArn": "arn:aws:sns:us-east-1:000000000000:OrderCreated",
      "OrderCancelledTopicArn": "arn:aws:sns:us-east-1:000000000000:OrderCancelled",
      "OrderCompletedTopicArn": "arn:aws:sns:us-east-1:000000000000:OrderCompleted"
    },
    "SQS": {
      "PaymentEventsQueueUrl": "http://localhost:4566/000000000000/sqs-orders-payment-events",
      "ProductionEventsQueueUrl": "http://localhost:4566/000000000000/sqs-orders-production-events"
    }
  }
}
```

## Como Executar

### 1. Com Docker Compose (Recomendado)
```bash
# Subir todos os serviços
docker-compose up -d

# Ver logs
docker-compose logs -f orders-api

# Parar serviços
docker-compose down
```

### 2. Localmente (Desenvolvimento)
```bash
# Restaurar dependências
dotnet restore

# Aplicar migrations (automático no startup)
dotnet run --project src/StackFood.Orders.API

# API rodando em http://localhost:5000
# Swagger em http://localhost:5000
```

## Testes

```bash
# Executar todos os testes
dotnet test

# Com cobertura
dotnet test /p:CollectCoverage=true
```

## Integração com outros serviços

### Depende de (HTTP)
- **Products API**: Validação de produtos e disponibilidade

### Publica eventos (SNS)
- **OrderCreated** → Payment Service (via sns-order-events)
- **OrderCancelled** → Notificações
- **OrderCompleted** → Notificações/Analytics

### Consome eventos (SQS)
- **sqs-orders-payment-events**: Recebe PaymentApproved/PaymentRejected
- **sqs-orders-production-events**: Recebe ProductionStarted/Ready/Delivered

## Fluxo do Pedido

```
1. Cliente cria pedido → POST /api/orders
   ↓ Valida produtos com Products API
   ↓ OrderCreatedEvent → Payment Service

2. Payment Service processa pagamento
   ↓ PaymentApproved → Orders atualiza status

3. Production Service recebe OrderCreated
   ↓ ProductionStarted → Orders atualiza status
   ↓ ProductionReady → Orders atualiza status
   ↓ ProductionDelivered → Orders atualiza status

4. Order concluído → OrderCompletedEvent
```

## Desenvolvimento

### Estrutura do Banco de Dados

```sql
CREATE TABLE orders (
    id uuid PRIMARY KEY,
    customer_id uuid,
    customer_name varchar(200),
    status int NOT NULL,
    total_amount decimal(18,2) NOT NULL,
    created_at timestamp NOT NULL,
    updated_at timestamp NOT NULL
);

CREATE TABLE order_items (
    id uuid PRIMARY KEY,
    order_id uuid NOT NULL,
    product_id uuid NOT NULL,
    product_name varchar(200) NOT NULL,
    quantity int NOT NULL,
    unit_price decimal(18,2) NOT NULL,
    total_price decimal(18,2) NOT NULL,
    FOREIGN KEY (order_id) REFERENCES orders(id)
);

-- Índices
CREATE INDEX idx_orders_customer_id ON orders(customer_id);
CREATE INDEX idx_orders_status ON orders(status);
CREATE INDEX idx_order_items_order_id ON order_items(order_id);
```

## Variáveis de Ambiente

| Variável | Descrição | Padrão |
|----------|-----------|--------|
| `ASPNETCORE_ENVIRONMENT` | Ambiente de execução | `Development` |
| `ConnectionStrings__DefaultConnection` | String de conexão PostgreSQL | `Host=localhost;Port=5435...` |
| `ExternalServices__ProductsApiUrl` | URL da Products API | `http://localhost:8080` |
| `AWS__UseLocalStack` | Usar LocalStack para AWS | `true` |
| `AWS__LocalStack__ServiceUrl` | URL do LocalStack | `http://localhost:4566` |
| `AWS__SNS__*TopicArn` | ARNs dos tópicos SNS | `arn:aws:sns:...` |
| `AWS__SQS__*QueueUrl` | URLs das filas SQS | `http://localstack:...` |

## Features

- ✅ Clean Architecture (4 camadas)
- ✅ Domain-Driven Design (Value Objects, Entities)
- ✅ Event-Driven Architecture (SNS/SQS)
- ✅ Background Services para consumers
- ✅ Integração com Products API
- ✅ Swagger/OpenAPI documentation
- ✅ Health checks
- ✅ Auto-migration no startup
- ✅ CORS configurado
- ✅ Logging estruturado

## Build Status

Compilação com êxito - 0 Erros, 0 Avisos

## Próximos Passos

- [ ] Implementar testes BDD com SpecFlow
- [ ] Adicionar retry policy para eventos
- [ ] Implementar circuit breaker para Products API
- [ ] Adicionar cache para produtos
- [ ] Métricas e observabilidade
- [ ] Rate limiting

## Licença

MIT
