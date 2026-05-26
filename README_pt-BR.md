# Especificação Técnica da API Chronos - Microsserviço de Agendamento

Este repositório contém o código-fonte e as configurações da API Chronos, desenvolvida como entregável técnico para o programa de formação profissional Capacita iRede, dentro da Trilha de Provimento de Serviços Computacionais (Computação em Nuvem). A documentação aqui apresentada descreve a implementação arquitetural, o mapeamento de infraestrutura e as configurações de deploy exigidas pelos critérios de avaliação do programa.

A API Chronos é um microsserviço de agendamento livre de estado (stateless), implementado utilizando o framework .NET 10 Minimal APIs com C# 13. A persistência é gerenciada por um banco de dados relacional PostgreSQL 18. O sistema foi projetado para eficiência de recursos e consistência operacional, fornecendo a base estrutural para o gerenciamento de dados de agendamento.

**Ambiente de Produção:** [https://chronos-api-y0v6.onrender.com](https://chronos-api-y0v6.onrender.com)

## Stack Técnica e Ferramental de Engenharia

A arquitetura do microsserviço baseia-se em um conjunto padronizado de tecnologias e ferramentas para garantir confiabilidade, desempenho e portabilidade:

| Categoria | Tecnologia / Ferramenta | Especificação / Versão |
|---|---|---|
| **Linguagem de Programação** | C# | 13.0 |
| **Framework de Runtime** | .NET | 10.0 (Minimal APIs) |
| **Mapeador Objeto-Relacional** | Entity Framework Core | 10.0 |
| **Banco de Dados Primário** | PostgreSQL | 18.0 (Gerenciado) |
| **Banco de Dados de Teste** | SQLite | 3.x (In-Memory) |
| **Engine de Contêiner** | Docker | 27.x+ |
| **Orquestração de Contêineres** | Docker Compose | 2.x+ |
| **SO de Imagem Base** | Alpine Linux | 3.x (Endurecido/Hardened) |
| **Plataforma de Nuvem (PaaS)** | Render | Suporte Nativo a Blueprints |
| **Engine de CI** | GitHub Actions | Runners Ubuntu-latest |
| **Framework de Testes** | xUnit | 2.x |

## Arquitetura de Nuvem e Topologia de Rede

A arquitetura é particionada em ambientes de execução distintos, abordando tanto os requisitos de desenvolvimento local quanto as restrições de produção em nuvem.

### Topologia de Desenvolvimento Local

No ambiente local, o sistema utiliza uma configuração multi-contêiner Docker gerenciada via Docker Compose. A rede é restrita a uma rede de ponte (bridge) isolada para proteger a comunicação interna entre serviços. A máquina hospedeira mapeia a porta 8080 para o contêiner da aplicação, que é executado como um usuário não-root. A instância do PostgreSQL 18 está vinculada estritamente à rede interna na porta 5432, impedindo a visibilidade externa pelo host.

```mermaid
graph TD
    HostMachine("Máquina Hospedeira (Porta 8080)") -->|"Mapeamento de Porta"| APILayer("Contêiner da API (Usuário Não-Root)")
    APILayer -->|"Comunicação Interna (Porta 5432)"| Database("Contêiner PostgreSQL 18")
    
    subgraph "chronos_isolated_network (Bridge)"
        APILayer
        Database
    end
```

### Topologia de Infraestrutura de Produção

O ambiente de produção é implantado na Plataforma como Serviço (PaaS) Render. O tráfego de entrada atinge o endpoint público HTTPS, onde a terminação SSL e o balanceamento de carga são gerenciados pela plataforma. O tráfego descriptografado é roteado para o contêiner da aplicação na porta 8080. A API conecta-se à instância gerenciada do PostgreSQL utilizando um parser dinâmico de URI com o parâmetro "SSL Mode Required" ativado para garantir a segurança dos dados em trânsito.

```mermaid
graph TD
    Client("Tráfego Externo") -->|"HTTPS"| PublicEdge("Borda Pública da Render")
    PublicEdge -->|"Terminação SSL e Balanceamento de Carga"| WebService("Serviço Web da API (Porta 8080)")
    WebService -->|"Parser de URI + SSL Mode Required"| ManagedDB("PostgreSQL Gerenciado")
```

## Infraestrutura e Governança de Nuvem

A estratégia de implantação utiliza paradigmas estabelecidos de nuvem para manter um ambiente operacional consistente e definir fronteiras administrativas claras.

### Implementação de Plataforma como Serviço (PaaS)

A escolha do modelo PaaS da Render em vez de alternativas de Infraestrutura como Serviço (IaaS), como máquinas virtuais isoladas, baseia-se na redução da manutenção operacional. O modelo PaaS automatiza a aplicação de patches no sistema operacional, o gerenciamento de hardware e a configuração de interfaces de rede. Isso permite que os recursos de engenharia sejam direcionados para a lógica da aplicação e requisitos de domínio, em vez da manutenção da infraestrutura.

### Infraestrutura como Código Declarativa

A infraestrutura de produção é gerenciada através de um modelo declarativo utilizando o manifesto de Blueprint `render.yaml`. Este manifesto define os serviços, bancos de dados e configurações de ambiente, permitindo implantações reproduzíveis. O uso de Infraestrutura como Código (IaC) minimiza o desvio de configuração (configuration drift) e garante que o ambiente de produção reflita o estado declarado no repositório.

### Equivalência de Implantação Multi-Nuvem

A infraestrutura de computação declarativa declarada no manifesto de Blueprint `render.yaml` na raiz mapeia-se de forma direta (1:1) em topologias de plataforma como serviço (PaaS) de nuvens públicas empresariais.
* **Mapeamento AWS:** As definições de contêiner Docker stateless mapeiam-se diretamente no AWS App Runner ou Amazon ECS (Elastic Container Service) com tarefas AWS Fargate, utilizando uma instância Amazon RDS para PostgreSQL como camada de banco de dados gerenciado.
* **Mapeamento Azure:** O microsserviço transita facilmente para uma implantação no Azure App Service utilizando a camada de runtime "Web App for Containers", acoplada a uma instância de servidor flexível do Azure Database for PostgreSQL.

Este design agnóstico de plataforma demonstra que a arquitetura containerizada subjacente mantém total conformidade arquitetural em todas as principais matrizes de nuvem corporativa sem exigir modificações no código-fonte.

Para manter a implantação contínua (CD) automatizada em ecossistemas de nuvem alternativos sem introduzir servidores de automação externos, ambas as plataformas de destino fornecem webhooks de rastreamento nativos semelhantes à arquitetura da Render. Na AWS, isso se traduz no uso do AWS App Runner vinculado diretamente ao branch do repositório GitHub de destino, puxando automaticamente as alterações e reconstruindo as tarefas Fargate após a sincronização do código. No Microsoft Azure, isso é alcançado conectando o Centro de Implantação do Azure App Service ao repositório, utilizando webhooks nativos da plataforma para realizar o re-pull, build e reciclagem automática da instância host do Web App for Containers.

### Modelo de Responsabilidade Compartilhada

As fronteiras operacionais seguem um modelo de responsabilidade compartilhada. O provedor de nuvem (Render) é responsável pela segurança física das instalações, pela camada de virtualização, pelos backups automatizados do banco de dados e pelo gerenciamento de certificados SSL. A equipe da aplicação é responsável pelo código-fonte, pelo design do esquema do banco de dados, pelo gerenciamento de variáveis de ambiente, pela validação de entrada e pelo versionamento da API.

### Benefícios e Desafios da Arquitetura

A implementação de uma arquitetura PaaS containerizada introduz compromissos de engenharia distintos:

* **Benefícios:**
  - **Eficiência Operacional:** Patches automatizados de SO, firewall gerenciado e provisionamento de hardware reduzem significativamente a superfície de administração do sistema.
  - **Paridade de Ambiente:** A combinação de Dockerfiles multi-estágio e infraestrutura declarativa garante que os ambientes de desenvolvimento, integração contínua e produção em nuvem compartilhem paridade estrutural, prevenindo anomalias de execução específicas de ambiente.
  - **Otimização de Custos:** O uso de planos de recursos PaaS elimina o custo excessivo de computação ociosa típico de clusters de máquinas virtuais IaaS superprovisionados.

* **Desafios:**
  - **Risco de Aprisionamento Tecnológico (Platform Lock-in):** A forte dependência de formatos declarativos específicos da plataforma (como os Blueprints da Render) exige a refatoração dos manifestos de configuração caso ocorra migração para um provedor de nuvem diferente (ex: transição para AWS CloudFormation ou Terraform).
  - **Inicializações a Frio (Cold Starts):** A camada de computação da plataforma apresenta um ciclo de suspensão automática durante períodos de inatividade prolongada, o que introduz atrasos latentes no processamento de requisições de inicialização a frio ao reciclar threads da aplicação.
  - **Limites de Conexão do Banco de Dados:** As camadas gerenciadas de nuvem impõem limites estritos de conexões simultâneas ao banco de dados, necessitando de configurações defensivas de pooling de conexões na camada de inicialização do Entity Framework Core para evitar anomalias de exaustão de conexões.

### Escalabilidade, Elasticidade e Design Stateless

A API Chronos atinge a escalabilidade horizontal operando como uma camada de serviço de computação estritamente livre de estado (stateless). Como a aplicação containerizada não persiste metadados de sessão ou arquivos de estado local na memória do kernel em tempo de execução, o tráfego HTTP de entrada pode ser balanceado entre um número arbitrário de instâncias de contêiner simultâneas.

A elasticidade é mantida no gateway de roteamento PaaS. Quando os limites de desempenho ou restrições de recursos de CPU/Memória são excedidos devido a picos de tráfego, a infraestrutura subjacente escala o microsserviço horizontalmente instanciando tarefas de contêiner paralelas atrás do balanceador de carga público da plataforma. Uma vez que o tráfego diminui, o escalonamento automático para baixo (down-scaling) reduz as instâncias ativas ao limite base, maximizando a eficiência de recursos e controlando as métricas de consumo computacional.

### Configurações e Variáveis de Ambiente

O microsserviço adapta dinamicamente seu comportamento, configurações de framework e conexões de infraestrutura com base em chaves de ambiente do host. A seguinte matriz de configuração deve ser satisfeita em tempo de execução:

| Nome da Variável | Contexto de Ambiente | Descrição | Exemplo de Valor Esperado |
|---|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | Desenvolvimento / Produção | Determina o comportamento de otimização do framework, verbosidade de logs e camadas ativas de pipeline de middleware. | `Production` |
| `ASPNETCORE_URLS` | Docker Local / PaaS Render | Define as portas de endpoint de vinculação da rede interna para o motor de hospedagem Kestrel. | `http://+:8080` |
| `ConnectionStrings__DefaultConnection` | Runtime em Execução | Payload de conexão do banco de dados relacional. Suporta strings ADO.NET padrão e URIs de banco de dados compatíveis com RFC. | `postgresql://user:pass@host/db` |

## Hardening de Contêineres e Configurações de Segurança

A estratégia de containerização implementa configurações específicas para minimizar a superfície de ataque e otimizar o desempenho do build.

### Otimização de Camadas de Build

O build Docker multi-estágio utiliza o cache de camadas isolando as operações de cópia do arquivo `.csproj` e `dotnet restore`. Isso garante que as dependências sejam baixadas novamente apenas quando a definição do projeto for alterada, reduzindo a duração do build durante os ciclos de desenvolvimento e CI/CD.

### Redução da Superfície de Ataque

A aplicação utiliza a imagem base `.NET 10 Alpine Linux` para o estágio final de execução. A distribuição Alpine contém um conjunto mínimo de pacotes instalados, o que reduz o número de vulnerabilidades potenciais em comparação com imagens base de uso geral.

### Gerenciamento de Privilégios

O contêiner é configurado para ser executado como um usuário não-root utilizando a diretiva `USER app`. Isso restringe a autorização de kernel da aplicação e limita o impacto potencial de um comprometimento de segurança a nível de aplicação, impedindo o acesso administrativo dentro do ambiente do contêiner.

### Isolamento de Rede

A configuração local do Docker Compose estabelece uma rede de ponte dedicada chamada `chronos_isolated_network`. Esta configuração garante que o serviço de banco de dados não seja exposto à rede do host, exigindo que todo o acesso se origine de dentro da malha de contêineres.

### Persistência de Volume e Integridade de Dados

Para evitar a volatilidade dos dados e garantir a persistência dos registros de domínio ao longo dos ciclos de vida dos contêineres, a infraestrutura local configura um volume nomeado isolado chamado `postgres_data` mapeado para o caminho do sistema de arquivos do contêiner PostgreSQL `/var/lib/postgresql/data`. Esta restrição arquitetural garante que as alterações de estado do banco de dados, as tabelas de agendamento e as estruturas de WAL (Write-Ahead Logging) permaneçam persistentes no disco de armazenamento do host, mesmo durante operações completas de destruição de contêiner, reconstrução (`docker compose down`) ou reciclagem em tempo de execução.

## Garantia de Qualidade e Integração Contínua (CI)

O projeto inclui um pipeline automatizado de testes e integração para verificar as alterações no código.

### Arquitetura de Fluxo de Trabalho CI/CD Desacoplada

O repositório impõe uma separação operacional estrita entre portões de qualidade de Integração Contínua (CI) e loops de orquestração de Implantação Contínua (CD):

* **Integração Contínua (GitHub Actions):** O pipeline `.github/workflows/ci.yml` funciona exclusivamente como um portão de qualidade de isolamento automatizado. Ele lida com a verificação de compilação e executa a matriz de testes de integração contra a camada de memória efêmera do SQLite. O runner possui zero credenciais de implantação e não interage com a rede de hospedagem em nuvem.
* **Implantação Contínua (Motor GitOps da Render):** A Implantação Contínua é executada diretamente através da integração nativa e orientada a eventos entre a PaaS Render e o provedor de repositório GitHub. A Render monitora o branch `main` para gatilhos de push e merge. Após a ativação do evento, a plataforma intercepta automaticamente o estado do repositório, constrói as camadas de contêiner Alpine endurecidas e aplica as configurações declarativas especificadas no manifesto `render.yaml` na raiz. Esta topologia operacional mapeia automaticamente dois estados de rastreamento ativos expostos nativamente na matriz de interface de "Deployments" do repositório GitHub:
  - `chronos-api` (A camada de serviço web de computação em contêiner stateless)
  - `chronos-db` (A instância de cluster de banco de dados relacional PostgreSQL gerenciado)

O pipeline do GitHub Actions (`ci.yml`) é executado em pull requests e merges de branch. O fluxo de trabalho lida com a compilação, executa testes unitários e de integração e verifica os artefatos de build.

Os testes de integração são realizados utilizando a `WebApplicationFactory` do .NET, que instancia a API em memória para verificação de requisição-resposta. Esta abordagem permite a validação do pipeline de middleware e da lógica de roteamento sem dependências de rede externa.

A paridade de dados relacionais é mantida no ambiente de CI utilizando uma `SqliteConnection` persistente em memória. A configuração da infraestrutura inclui um mecanismo de detecção que alterna entre PostgreSQL e SQLite com base no contexto de execução, garantindo que os testes sejam executados contra um armazenamento relacional, evitando o overhead de contêineres de banco de dados externos durante a execução do CI.

### Mecânica de Inicialização do Banco de Dados em Tempo de Execução

Para preservar o desacoplamento ambiental completo, o sistema apresenta um motor de detecção dinâmica de provedor configurado dentro de `Program.cs`. A camada de persistência alterna automaticamente os comportamentos de inicialização com base no contexto de execução ativo:

* **Contexto de Produção (PostgreSQL 18):** No bootstrap do serviço, a aplicação invoca `Database.GetPendingMigrationsAsync()`. Se existirem scripts estruturais pendentes no diretório de metadados de migração da infraestrutura, ela os executa sequencialmente via `Database.MigrateAsync()`. Isso garante a sincronização automatizada do esquema do banco de dados em rollouts de contêineres em nuvem em tempo real.
* **Contexto de Teste (SQLite em Memória):** Durante as execuções de testes de integração determinísticos, o framework intercepta o pipeline, ignora a verificação do histórico de migrações e executa `Database.EnsureCreatedAsync()`. Isso instancia o esquema estrutural diretamente da configuração do modelo de entidade dentro de um ambiente de memória efêmero e de alta velocidade.

## Especificação da API e Contratos HTTP

A API segue os princípios RESTful para interação de dados. Falhas de validação de entrada resultam em uma resposta `400 Bad Request` com um corpo `HttpValidationProblemDetails`. O caminho raiz `/` está configurado para redirecionar para a interface de documentação `/swagger`.

| Método HTTP | Endpoint de Rota | Corpo da Requisição | Código de Sucesso | Códigos de Erro |
|-------------|----------------|--------------|--------------|-------------|
| GET         | `/healthz` | Nenhum | 200 OK | 503 Service Unavailable |
| GET         | `/api/appointments` | Nenhum | 200 OK | Nenhum |
| GET         | `/api/appointments/{id}` | Nenhum | 200 OK | 404 Not Found |
| POST        | `/api/appointments` | `{ "clientName": "string", "service": "string", "targetedAt": "string(date-time)" }` | 201 Created | 400 Bad Request |
| PUT         | `/api/appointments/{id}` | `{ "clientName": "string", "service": "string", "targetedAt": "string(date-time)" }` | 204 No Content | 400 Bad Request, 404 Not Found |
| DELETE      | `/api/appointments/{id}` | Nenhum | 204 No Content | 404 Not Found |

## Layout de Diretórios do Repositório

A arquitetura do workspace isola as fronteiras do sistema em segmentos de engenharia dedicados, correspondendo à seguinte topologia de repositório:

```text
├── .github/
│   └── workflows/
│       └── ci.yml
├── Chronos.API/
│   ├── Configuration/
│   │   └── ServiceCollectionExtensions.cs
│   ├── Core/
│   │   └── Entities/
│   │       └── Appointment.cs
│   ├── Endpoints/
│   │   ├── AppointmentEndpoints.cs
│   │   └── HealthEndpoints.cs
│   ├── Infrastructure/
│   │   └── Data/
│   │       ├── Migrations/
│   │       │   ├── 20260526024135_InitialCreate.cs
│   │       │   ├── 20260526024135_InitialCreate.Designer.cs
│   │       │   └── ChronosDbContextModelSnapshot.cs
│   │       └── ChronosDbContext.cs
│   ├── Properties/
│   │   └── launchSettings.json
│   ├── appsettings.Development.json
│   ├── appsettings.json
│   ├── Chronos.API.csproj
│   ├── Chronos.API.http
│   └── Program.cs
├── Chronos.API.Tests/
│   ├── IntegrationTests/
│   │   ├── AppointmentEndpointsTests.cs
│   │   └── HealthEndpointsTests.cs
│   ├── ApiFactory.cs
│   └── Chronos.API.Tests.csproj
├── .gitignore
├── Chronos.slnx
├── docker-compose.yml
├── Dockerfile
├── LICENSE
└── render.yaml
```

## Manual de Instalação, Verificação e Execução

A orquestração local e a validação de testes exigem o Docker Desktop e o runtime do SDK do .NET 10 pré-instalados na máquina hospedeira.

### 1. Clonar Dependências do Workspace

```bash
git clone https://github.com/emanuellcs/chronos-api.git
cd chronos-api
```

### 2. Compilação e Verificação de Build

Para restaurar dependências e verificar a integridade do código-fonte em toda a solução:

```bash
dotnet build
```

### 3. Executar a Suíte de Testes de Integração Local

Para executar a matriz de testes de integração determinística localmente dentro de um contexto isolado em memória utilizando SQLite, ignorando a necessidade de contêineres de banco de dados externos:

```bash
dotnet test Chronos.API.Tests/Chronos.API.Tests.csproj
```

### 4. Orquestrar o Ambiente Local Multi-Contêiner

Para compilar os Dockerfiles Alpine multi-estágio e iniciar a camada completa de computação da API e o motor PostgreSQL 18 vinculados com segurança à rede de ponte privada isolada:

```bash
docker compose up --build
```

### 5. Verificação Interativa em Tempo Real

Assim que os fluxos do console relatarem prontidão operacional, os endpoints da aplicação podem ser acessados diretamente através dos seguintes caminhos verificados no host:

*   Motor de Documentação Interativa (Interface Scalar): [http://localhost:8080/swagger](http://localhost:8080/swagger)
*   Vetor de Status de Sondagem de Saúde da Plataforma: [http://localhost:8080/healthz](http://localhost:8080/healthz)
