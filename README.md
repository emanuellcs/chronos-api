# Chronos API - Scheduling Microservice Specification

> [!IMPORTANT]
> Para a avaliação deste projeto no âmbito do programa Capacita iRede (Trilha de Provimento de Serviços Computacionais), a documentação técnica completa, traduzida e adaptada para o português do Brasil, está disponível diretamente no arquivo [README_pt-BR.md](./README_pt-BR.md).

This repository contains the source code and configuration for the Chronos API, developed as a technical deliverable for the Capacita iRede professional training program within the Computer Services Provision ("Provimento de Serviços Computacionais") track. The documentation provided describes the architectural implementation, infrastructure mapping, and deployment configurations required by the program evaluation criteria.

The Chronos API is a stateless scheduling microservice implemented using the C# .NET 10 Minimal APIs framework. Persistence is managed via a PostgreSQL 18 relational database. The system is designed for resource efficiency and operational consistency, providing the structural baseline for scheduling data management.

**Production Environment:** [https://chronos-api-y0v6.onrender.com](https://chronos-api-y0v6.onrender.com)

## Technical Stack and Engineering Tooling

The microservice architecture is built upon a standardized set of technologies and tools to ensure reliability, performance, and portability:

| Category | Technology / Tool | Specification / Version |
|---|---|---|
| **Programming Language** | C# | 14.0 |
| **Runtime Framework** | .NET | 10.0 (Minimal APIs) |
| **Object-Relational Mapper** | Entity Framework Core | 10.0 |
| **Primary Database** | PostgreSQL | 18.0 (Managed) |
| **Testing Database** | SQLite | 3.x (In-Memory) |
| **Container Engine** | Docker | 27.x+ |
| **Container Orchestration** | Docker Compose | 2.x+ |
| **Base Image OS** | Alpine Linux | 3.x (Hardened) |
| **Cloud Platform (PaaS)** | Render | Native Blueprint Support |
| **CI Engine** | GitHub Actions | Ubuntu-latest Runners |
| **Testing Framework** | xUnit | 2.x |

## Cloud Architecture and Network Topology

The architecture is partitioned into distinct execution environments, addressing both local development requirements and cloud-based production constraints.

### Local Development Topology

In the local environment, the system utilizes a multi-container Docker configuration managed via Docker Compose. Networking is restricted to an isolated bridge network to secure internal service communication. The host machine maps port 8080 to the application container, which executes as a non-root user. The PostgreSQL 18 instance is bound strictly to the internal network on port 5432, preventing external host visibility.

```mermaid
graph TD
    HostMachine("Host Machine (Port 8080)") -->|"Port Mapping"| APILayer("API Container (Non-Root User)")
    APILayer -->|"Internal Communication (Port 5432)"| Database("PostgreSQL 18 Container")
    
    subgraph "chronos_isolated_network (Bridge)"
        APILayer
        Database
    end
```

### Production Infrastructure Topology

The production environment is deployed on the Render Platform-as-a-Service (PaaS). Inbound traffic reaches the public HTTPS endpoint, where SSL termination and load balancing are managed by the platform. Decrypted traffic is routed to the application container on port 8080. The API connects to the managed PostgreSQL instance using a dynamic URI parser with the "SSL Mode Required" parameter enabled to secure data in transit.

```mermaid
graph TD
    Client("External Traffic") -->|"HTTPS"| PublicEdge("Render Public Edge")
    PublicEdge -->|"SSL Termination & Load Balancing"| WebService("API Web Service (Port 8080)")
    WebService -->|"URI Parser + SSL Mode Required"| ManagedDB("Managed PostgreSQL")
```

## Cloud Infrastructure and Governance

The deployment strategy utilizes established cloud paradigms to maintain a consistent operational environment and define clear administrative boundaries.

### Platform-as-a-Service (PaaS) Implementation

The selection of Render's PaaS model over Infrastructure-as-a-Service (IaaS) alternatives, such as standalone virtual machines, is based on reducing operational maintenance. The PaaS model automates operating system patching, hardware management, and network interface configuration. This allows engineering resources to be directed toward application logic and domain requirements rather than infrastructure maintenance.

### Declarative Infrastructure as Code

Production infrastructure is managed through a declarative model using the `render.yaml` Blueprint manifest. This manifest defines the services, databases, and environment configurations, allowing for reproducible deployments. Using Infrastructure as Code (IaC) minimizes configuration drift and ensures that the production environment reflects the declared state in the repository.

### Multi-Cloud Deployment Equivalence

The declarative computing infrastructure declared within the root `render.yaml` Blueprint manifest maps 1:1 onto enterprise public cloud platform-as-a-service (PaaS) topologies. 
* **AWS Mapping:** The stateless Docker container definitions map directly onto AWS App Runner or Amazon ECS (Elastic Container Service) with AWS Fargate tasks, utilizing an Amazon RDS for PostgreSQL instance as the managed database backend tier.
* **Azure Mapping:** The microservice easily transitions to an Azure App Service deployment using the "Web App for Containers" runtime layer, coupled with an Azure Database for PostgreSQL flexible server instantiation. 

This platform-agnostic design demonstrates that the underlying containerized architecture maintains full architectural compliance across major enterprise cloud matrices without requiring source code modifications.

To maintain automated continuous deployment (CD) on alternative cloud ecosystems without introducing external automation servers, both target platforms provide native tracking webhooks similar to Render's architecture. On AWS, this translates to utilizing AWS App Runner linked directly to the target GitHub repository branch, automatically pulling changes and rebuilding the Fargate task tasks upon code synchronization. On Microsoft Azure, this is achieved by connecting the Azure App Service Deployment Center to the repository, utilizing native platform webhooks to re-pull, build, and recycle the Web App for Containers host instance automatically.

### Shared Responsibility Model

The operational boundaries follow a shared responsibility model. The cloud provider (Render) is responsible for physical facility security, the virtualization layer, automated database backups, and SSL certificate management. The application team is responsible for the source code, database schema design, environment variable management, input validation, and API versioning.

### Benefits and Challenges of the Architecture

The implementation of a containerized PaaS architecture introduces distinct engineering trade-offs:

* **Benefits:**
  - **Operational Efficiency:** Automated OS patching, managed firewalling, and hardware provisioning significantly reduce the system administration surface area.
  - **Environment Parity:** The combination of multi-stage Dockerfiles and declarative infrastructure guarantees that development, continuous integration, and cloud production environments share structural parity, preventing environment-specific runtime anomalies.
  - **Cost Optimization:** Using PaaS resource plans eliminates the idle-compute cost overhead typical of over-provisioned IaaS virtual machine clusters.

* **Challenges:**
  - **Platform Lock-in Risk:** Heavy reliance on platform-specific declarative formats (like Render Blueprints) requires refactoring the configuration manifests if migrating to a different cloud vendor (e.g., transitioning to AWS CloudFormation or Terraform).
  - **Compute Cold Starts:** The platform's compute layer features an automated sleep cycle during periods of extended inactivity, which introduces latent cold-start request processing delays when recycling application threads.
  - **Database Connection Caps:** Managed cloud tiers enforce strict concurrent database connection pool upper limits, necessitating defensive application-level pooling configurations inside the Entity Framework Core initialization layer to avoid connection exhaustion anomalies.

### Scalability, Elasticity, and Stateless Design

The Chronos API achieves horizontal scalability by operating as a strictly stateless computing service layer. Because the containerized application does not persist session metadata or local state files within its runtime kernel memory, incoming HTTP traffic can be balanced across an arbitrary number of concurrent container instances. 

Elasticity is maintained at the PaaS routing gateway. When performance boundaries or CPU/Memory resource constraints are exceeded due to traffic spikes, the underlying infrastructure scales the microservice horizontally by instantiating parallel container tasks behind the platform's public load balancer. Once traffic drops, automated down-scaling reduces active instances to the baseline limit, maximizing resource efficiency and controlling compute consumption metrics.

### Configuration and Environment Variables

The microservice dynamically adapts its behavior, framework configuration, and infrastructure connections based on host environment keys. The following configuration matrix must be satisfied at runtime:

| Variable Name | Environment Context | Description | Expected Value Example |
|---|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | Development / Production | Dictates the framework optimization behavior, logging verbosity, and active middleware pipeline layers. | `Production` |
| `ASPNETCORE_URLS` | Local Docker / Render PaaS | Defines the internal network binding endpoint ports for the Kestrel server hosting engine. | `http://+:8080` |
| `ConnectionStrings__DefaultConnection` | Live Runtime Mesh | The relational database connection payload. Fully supports both standard ADO.NET semicolon-separated strings and RFC-compliant database URIs. | `postgresql://user:pass@host/db` |

## Container Hardening and Security Configuration

The containerization strategy implements specific configurations to minimize the security surface area and optimize build performance.

### Build Layer Optimization

The multi-stage Docker build utilizes layer caching by isolating the `.csproj` copy and `dotnet restore` operations. This ensures that dependencies are only re-downloaded when the project definition changes, reducing the build duration during the development and CI/CD cycles.

### Surface Area Reduction

The application utilizes the `.NET 10 Alpine Linux` base image for the final execution stage. The Alpine distribution contains a minimal set of installed packages, which reduces the number of potential vulnerabilities compared to general-purpose base images.

### Privilege Management

The container is configured to run as a non-root user using the `USER app` directive. This restricts the application's kernel authorization and limits the potential impact of an application-level security compromise by preventing administrative access within the container environment.

### Network Isolation

The local Docker Compose configuration establishes a dedicated bridge network named `chronos_isolated_network`. This configuration ensures that the database service is not exposed to the host network, requiring all access to originate from within the container mesh.

### Cross-Origin Resource Sharing (CORS)

To support integration with frontend applications during local development and within the Docker Compose mesh, the API implements a permissive CORS policy. The `AllowAll` policy permits requests from any origin, method, and header. This configuration ensures that the backend is accessible by decoupled frontend services while maintaining a consistent development experience. In a production environment with fixed frontend origins, this policy should be narrowed to restrict access to specific trusted domains.

### Volume Persistence and Data Integrity

To prevent data volatility and ensure the persistence of domain records across container lifecycles, the local infrastructure configures an isolated named volume called `postgres_data` mapped to the PostgreSQL container filesystem path `/var/lib/postgresql/data`. This architectural constraint ensures that database state changes, appointment tables, and WAL (Write-Ahead Logging) structures remain persistent on the host storage disk even during complete container destruction, rebuilding (`docker compose down`), or runtime recycling operations.

## Quality Assurance and Continuous Integration

The project includes an automated testing and integration pipeline to verify code changes.

### Decoupled CI/CD Workflow Architecture

The repository enforces a strict operational separation between Continuous Integration (CI) quality gates and Continuous Deployment (CD) orchestration loops:

* **Continuous Integration (GitHub Actions):** The `.github/workflows/ci.yml` pipeline functions exclusively as an automated isolation quality gate. It handles compilation verification and runs the integration testing matrix against the ephemeral SQLite memory layer. The runner possesses zero deployment credentials and does not interact with the cloud hosting network.
* **Continuous Deployment (Render GitOps Engine):** Continuous Deployment is executed directly via native, event-driven integration between the Render PaaS and the GitHub repository provider. Render monitors the `main` branch for push and merge triggers. Upon event activation, the platform automatically intercepts the repository state, builds the hardened Alpine container layers, and applies the declarative configurations specified in the root `render.yaml` manifest. This operational topology automatically maps two live tracking states exposed natively inside the GitHub repository "Deployments" interface matrix:
  - `chronos-api` (The stateless container compute web service layer)
  - `chronos-db` (The managed PostgreSQL relational database instance cluster)

The GitHub Actions pipeline (`ci.yml`) executes on pull requests and branch merges. The workflow handles compilation, runs unit and integration tests, and verifies the build artifacts.

Integration testing is performed using the .NET `WebApplicationFactory`, which instantiates the API in-memory for request-response verification. This approach allows for the validation of the middleware pipeline and routing logic without external network dependencies.

Relational data parity is maintained in the CI environment using a persistent `SqliteConnection` in-memory. The infrastructure configuration includes a detection mechanism that switches between PostgreSQL and SQLite based on the execution context, ensuring that tests run against a relational store while avoiding the overhead of external database containers during CI execution.

### Runtime Database Initialization Mechanics

To preserve complete environmental decoupling, the system features a dynamic provider detection engine configured within `Program.cs`. The persistence layer automatically switches initialization behaviors based on the active execution context:

* **Production Context (PostgreSQL 18):** On service bootstrap, the application invokes `Database.GetPendingMigrationsAsync()`. If pending structural scripts exist within the infrastructure migration metadata directory, it executes them sequentially via `Database.MigrateAsync()`. This ensures automated, hands-free database schema synchronization across live cloud container rollouts.
* **Testing Context (In-Memory SQLite):** During deterministic integration test runs, the framework intercepts the pipeline, bypasses the migration history verification, and executes `Database.EnsureCreatedAsync()`. This instantiates the structural schema directly from the entity model configuration within an ephemeral, high-speed memory environment.

## API Specification and HTTP Contracts

The API follows RESTful principles for data interaction. Input validation failures result in a `400 Bad Request` response with a `HttpValidationProblemDetails` body. The root `/` path is configured to redirect to the `/swagger` documentation interface.

| HTTP Method | Route Endpoint | Request Body | Success Code | Error Codes |
|-------------|----------------|--------------|--------------|-------------|
| GET         | `/healthz` | None | 200 OK | 503 Service Unavailable |
| GET         | `/api/appointments` | None | 200 OK | None |
| GET         | `/api/appointments/{id}` | None | 200 OK | 404 Not Found |
| POST        | `/api/appointments` | `{ "clientName": "string", "service": "string", "targetedAt": "string(date-time)" }` | 201 Created | 400 Bad Request |
| PUT         | `/api/appointments/{id}` | `{ "clientName": "string", "service": "string", "targetedAt": "string(date-time)" }` | 204 No Content | 400 Bad Request, 404 Not Found |
| DELETE      | `/api/appointments/{id}` | None | 204 No Content | 404 Not Found |

## Repository Directory Layout

The workspace architecture isolates system boundaries into dedicated engineering segments matching the following concrete repository topology:

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

## Installation, Verification, and Execution Manual

Local orchestration and test validation require Docker Desktop and the .NET 10 SDK runtime pre-installed on the host machine.

### 1. Clone Workspace Dependencies

```bash
git clone https://github.com/emanuellcs/chronos-api.git
cd chronos-api
```

### 2. Compile and Build Verification

To restore dependencies and verify the integrity of the source code across the entire solution:

```bash
dotnet build
```

### 3. Execute the Local Integration Test Suite

To run the deterministic integration testing matrix locally inside an isolated in-memory context using SQLite, bypassing the requirement for external database containers:

```bash
dotnet test
```

### 4. Orchestrate the Multi-Container Local Environment

To compile the multi-stage Alpine Dockerfiles and spin up the complete API computing layer and the PostgreSQL 18 engine securely bound to the isolated private bridge network:

```bash
docker compose up --build
```

### 5. Interactive Live Verification

Once the console streams report operational readiness, the application endpoints can be reached directly via the following verified host pathways:

*   Interactive Documentation Engine (Scalar UI): [http://localhost:8080/swagger](http://localhost:8080/swagger)
*   Platform Health Probe Status Vector: [http://localhost:8080/healthz](http://localhost:8080/healthz)
