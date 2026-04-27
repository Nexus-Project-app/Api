# Migrer d'une architecture N-Tier vers la Clean Architecture

Guide pratique illustre avec le code de ce repository.

---

## Table des matieres

1. [Comprendre le probleme du N-Tier](#1-comprendre-le-probleme-du-n-tier)
2. [Les principes de la Clean Architecture](#2-les-principes-de-la-clean-architecture)
3. [Comparaison cote a cote](#3-comparaison-cote-a-cote)
4. [Migration etape par etape](#4-migration-etape-par-etape)
5. [Le pattern CQRS sans MediatR](#5-le-pattern-cqrs-sans-mediatr)
6. [Le pattern Result](#6-le-pattern-result)
7. [Les Domain Events](#7-les-domain-events)
8. [Les cross-cutting concerns (decorateurs)](#8-les-cross-cutting-concerns-decorateurs)
9. [Les tests d'architecture](#9-les-tests-darchitecture)
10. [Checklist de migration](#10-checklist-de-migration)

---

## 1. Comprendre le probleme du N-Tier

### Architecture N-Tier classique

```
Presentation (Controllers)
      |
      v
Business Logic (Services)
      |
      v
Data Access (Repositories / DbContext)
      |
      v
Database
```

Dans une architecture N-Tier classique, les dependances descendent : la couche Presentation depend de la couche Business, qui depend de la couche Data Access. Ca parait logique, mais ca cree un probleme fondamental :

**La logique metier depend de l'infrastructure.**

Concretement, ca veut dire que :

- `TodoService` a une reference directe vers `ApplicationDbContext` (EF Core)
- Changer de base de donnees ou d'ORM force a modifier la logique metier
- Les tests unitaires de la logique metier necessitent de mocker EF Core
- Les services deviennent des classes "fourre-tout" avec 20+ methodes

### Exemple typique N-Tier

```csharp
// Business/Services/TodoService.cs - N-TIER
public class TodoService
{
    private readonly ApplicationDbContext _context; // Dependance directe vers l'infra !
    private readonly ILogger<TodoService> _logger;

    public TodoService(ApplicationDbContext context, ILogger<TodoService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<TodoItem> CreateTodoAsync(Guid userId, string description, Priority priority)
    {
        // Validation melee a la logique metier
        if (string.IsNullOrEmpty(description))
            throw new ArgumentException("Description required");

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            throw new NotFoundException($"User {userId} not found");

        var todo = new TodoItem
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Description = description,
            Priority = priority,
            CreatedAt = DateTime.UtcNow // Dependance directe vers DateTime
        };

        _context.TodoItems.Add(todo);
        await _context.SaveChangesAsync();

        // Effets de bord (email, notification) directement dans le service
        await SendNotificationAsync(todo);

        return todo;
    }

    public async Task<List<TodoItem>> GetTodosAsync(Guid userId) { ... }
    public async Task CompleteTodoAsync(Guid todoId) { ... }
    public async Task DeleteTodoAsync(Guid todoId) { ... }
    public async Task UpdateTodoAsync(Guid todoId, ...) { ... }
    public async Task CopyTodoAsync(Guid todoId) { ... }
    // ... 15 autres methodes, le service grossit sans fin
}
```

**Problemes :**
- Le service connait EF Core directement → couplage fort
- Validation, logique metier, persistance, et effets de bord dans la meme methode
- Impossible de tester sans base de donnees ou mock complexe
- Le service grossit indefiniment (violation du Single Responsibility Principle)
- `DateTime.UtcNow` rend les tests non deterministes

---

## 2. Les principes de la Clean Architecture

### Inversion du sens des dependances

La regle d'or : **les dependances pointent vers l'interieur** (vers le domaine).

```
                    +---------------------------+
                    |         Web.Api            |  Presentation
                    |  (Endpoints, Middleware)   |
                    +---------------------------+
                                |
                    +---------------------------+
                    |      Infrastructure        |  Implementations concretes
                    |  (EF Core, JWT, Serilog)   |
                    +---------------------------+
                                |
                    +---------------------------+
                    |       Application          |  Cas d'utilisation (CQRS)
                    |  (Commands, Queries,       |
                    |   Handlers, Interfaces)    |
                    +---------------------------+
                                |
                    +---------------------------+
                    |         Domain             |  Entites, Domain Events
                    +---------------------------+
                                |
                    +---------------------------+
                    |      SharedKernel          |  Result, Entity, Error
                    +---------------------------+
```

**L'inversion de dependance en pratique :**

- La couche Application definit des **interfaces** (`IApplicationDbContext`)
- La couche Infrastructure fournit des **implementations** (`ApplicationDbContext`)
- La couche Application ne sait PAS qu'EF Core existe

### Mapping vers les projets du repository

| Couche | Projet | Role |
|--------|--------|------|
| SharedKernel | `src/SharedKernel` | Types de base partages : `Entity`, `Result<T>`, `Error`, `IDomainEvent` |
| Domain | `src/Domain` | Entites metier, Domain Events, erreurs metier |
| Application | `src/Application` | Interfaces, Commands, Queries, Handlers, Validators |
| Infrastructure | `src/Infrastructure` | EF Core, PostgreSQL, JWT, autorisations, Serilog |
| Presentation | `src/Web.Api` | Endpoints Minimal API, middleware, composition root |

---

## 3. Comparaison cote a cote

### Avant (N-Tier) vs Apres (Clean Architecture)

#### Creer un Todo

**N-Tier :**

```
Controller --> TodoService --> DbContext --> DB
```

Un seul fichier service qui fait tout.

**Clean Architecture :**

```
Endpoint --> ICommandHandler<CreateTodoCommand, Guid> --> IApplicationDbContext --> DB
   (Web.Api)        (Application)                            (Interface definie dans Application,
                                                              implementee dans Infrastructure)
```

Chaque cas d'utilisation a ses propres fichiers :

```
Application/Todos/Create/
    CreateTodoCommand.cs          -- Le "quoi" (donnees d'entree)
    CreateTodoCommandHandler.cs   -- Le "comment" (logique metier)
    CreateTodoCommandValidator.cs -- La validation (separee)
```

#### La difference cle : les interfaces

**N-Tier** -- Le service utilise directement `ApplicationDbContext` :

```csharp
public class TodoService(ApplicationDbContext context) // Implementation concrete !
```

**Clean Architecture** -- Le handler utilise une interface definie dans Application :

```csharp
// Defini dans Application (pas dans Infrastructure !)
public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<TodoItem> TodoItems { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

// Le handler ne connait que l'interface
internal sealed class CreateTodoCommandHandler(
    IApplicationDbContext context,          // Interface !
    IDateTimeProvider dateTimeProvider,     // Interface pour le temps
    IUserContext userContext)               // Interface pour l'utilisateur courant
    : ICommandHandler<CreateTodoCommand, Guid>
```

Le handler ne sait pas si c'est PostgreSQL, SQL Server, ou un mock de test derriere `IApplicationDbContext`.

---

## 4. Migration etape par etape

### Etape 1 : Creer le SharedKernel

Commencer par extraire les types de base. Ce sont les briques fondamentales que tous les autres projets utilisent.

**Fichiers a creer :**

```csharp
// SharedKernel/Result.cs - Remplace les exceptions pour les erreurs attendues
public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }

    public static Result Success() => new(true, Error.None);
    public static Result Failure(Error error) => new(false, error);
    public static Result<TValue> Success<TValue>(TValue value) => new(value, true, Error.None);
    public static Result<TValue> Failure<TValue>(Error error) => new(default, false, error);
}

// SharedKernel/Entity.cs - Base pour les entites avec Domain Events
public abstract class Entity
{
    private readonly List<IDomainEvent> _domainEvents = [];
    public List<IDomainEvent> DomainEvents => [.. _domainEvents];
    public void ClearDomainEvents() => _domainEvents.Clear();
    public void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
}
```

### Etape 2 : Extraire le Domain

Deplacer les entites dans un projet Domain qui ne reference RIEN d'autre que SharedKernel.

**Avant (N-Tier) :**

```csharp
// Models/TodoItem.cs -- dans le meme projet que le service
public class TodoItem
{
    public Guid Id { get; set; }
    public string Description { get; set; }
    // ...
}
```

**Apres (Clean Architecture) :**

```csharp
// Domain/Todos/TodoItem.cs -- projet separe, herite de Entity
public sealed class TodoItem : Entity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Description { get; set; }
    public DateTime? DueDate { get; set; }
    public List<string> Labels { get; set; } = [];
    public bool IsCompleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public Priority Priority { get; set; }
}
```

Ajouter aussi les erreurs metier en tant qu'objets typees (plutot que des exceptions) :

```csharp
// Domain/Todos/TodoItemErrors.cs
public static class TodoItemErrors
{
    public static Error AlreadyCompleted(Guid todoItemId) => Error.Problem(
        "TodoItems.AlreadyCompleted",
        $"The todo item with Id = '{todoItemId}' is already completed.");

    public static Error NotFound(Guid todoItemId) => Error.NotFound(
        "TodoItems.NotFound",
        $"The to-do item with the Id = '{todoItemId}' was not found");
}
```

### Etape 3 : Creer la couche Application avec le CQRS

C'est l'etape la plus importante. On remplace les "gros services" par des **handlers** isoles.

**Regle : 1 cas d'utilisation = 1 dossier avec Command/Query + Handler + Validator**

```
Application/
    Abstractions/
        Data/IApplicationDbContext.cs         <-- Interface (pas d'implementation ici)
        Authentication/IUserContext.cs        <-- Interface
        Messaging/ICommand.cs                 <-- Marqueur
        Messaging/ICommandHandler.cs          <-- Contrat
        Messaging/IQuery.cs                   <-- Marqueur
        Messaging/IQueryHandler.cs            <-- Contrat
    Todos/
        Create/
            CreateTodoCommand.cs              <-- Donnees d'entree
            CreateTodoCommandHandler.cs       <-- Logique metier
            CreateTodoCommandValidator.cs     <-- Validation (FluentValidation)
        Complete/
            CompleteTodoCommand.cs
            CompleteTodoCommandHandler.cs
            CompleteTodoCommandValidator.cs
        Get/
            GetTodosQuery.cs
            GetTodosQueryHandler.cs
            TodoResponse.cs                   <-- DTO de sortie
```

**Transformer un service N-Tier en handlers :**

Chaque methode publique du service devient un handler independant :

| Methode du Service | Devient |
|---|---|
| `TodoService.CreateTodoAsync(...)` | `CreateTodoCommandHandler.Handle(CreateTodoCommand)` |
| `TodoService.GetTodosAsync(...)` | `GetTodosQueryHandler.Handle(GetTodosQuery)` |
| `TodoService.CompleteTodoAsync(...)` | `CompleteTodoCommandHandler.Handle(CompleteTodoCommand)` |
| `TodoService.DeleteTodoAsync(...)` | `DeleteTodoCommandHandler.Handle(DeleteTodoCommand)` |

### Etape 4 : Deplacer les implementations dans Infrastructure

Tout ce qui est "technique" quitte Application pour aller dans Infrastructure :

- `ApplicationDbContext` (EF Core) → implemente `IApplicationDbContext`
- `TokenProvider` (JWT) → implemente `ITokenProvider`
- `PasswordHasher` → implemente `IPasswordHasher`
- `UserContext` (HttpContext) → implemente `IUserContext`
- `DateTimeProvider` → implemente `IDateTimeProvider`

```csharp
// Infrastructure/Database/ApplicationDbContext.cs
public sealed class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options,
    IDomainEventsDispatcher domainEventsDispatcher)
    : DbContext(options), IApplicationDbContext  // Implemente l'interface de Application
{
    public DbSet<User> Users { get; set; }
    public DbSet<TodoItem> TodoItems { get; set; }
}
```

### Etape 5 : Transformer les Controllers en Endpoints

**N-Tier :**

```csharp
[ApiController]
[Route("api/todos")]
public class TodoController : ControllerBase
{
    private readonly TodoService _todoService;

    [HttpPost]
    public async Task<IActionResult> Create(CreateTodoRequest request)
    {
        try
        {
            var todo = await _todoService.CreateTodoAsync(request.UserId, request.Description, request.Priority);
            return Ok(todo.Id);
        }
        catch (NotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
```

**Clean Architecture (Minimal API avec IEndpoint) :**

```csharp
// Web.Api/Endpoints/Todos/Create.cs
internal sealed class Create : IEndpoint
{
    public sealed class Request
    {
        public Guid UserId { get; set; }
        public string Description { get; set; }
        public DateTime? DueDate { get; set; }
        public List<string> Labels { get; set; } = [];
        public int Priority { get; set; }
    }

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("todos", async (
            Request request,
            ICommandHandler<CreateTodoCommand, Guid> handler,  // Injection directe du handler
            CancellationToken cancellationToken) =>
        {
            var command = new CreateTodoCommand { ... };

            Result<Guid> result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);  // Pas de try/catch !
        })
        .WithTags(Tags.Todos)
        .RequireAuthorization();
    }
}
```

**Differences notables :**

- Plus de `try/catch` -- le `Result<T>` gere les erreurs
- Plus de service injecte -- on injecte directement le handler
- Un fichier par endpoint, pas un controller qui grossit

### Etape 6 : Configurer le DI (composition root)

L'enregistrement se fait dans `Program.cs` via des methodes d'extension :

```csharp
builder.Services
    .AddApplication()       // Scan + enregistre les handlers et validators
    .AddPresentation()      // Endpoints, Swagger, exception handler
    .AddInfrastructure(builder.Configuration);  // EF Core, Auth, etc.
```

`Application/DependencyInjection.cs` utilise **Scrutor** pour scanner et enregistrer automatiquement tous les handlers :

```csharp
services.Scan(scan => scan.FromAssembliesOf(typeof(DependencyInjection))
    .AddClasses(classes => classes.AssignableTo(typeof(IQueryHandler<,>)), publicOnly: false)
        .AsImplementedInterfaces()
        .WithScopedLifetime()
    .AddClasses(classes => classes.AssignableTo(typeof(ICommandHandler<>)), publicOnly: false)
        .AsImplementedInterfaces()
        .WithScopedLifetime()
    // ...
);
```

Puis Scrutor **decore** les handlers avec la validation et le logging (voir section 8).

---

## 5. Le pattern CQRS sans MediatR

Ce repository n'utilise PAS MediatR. Le CQRS est implemente avec des interfaces simples et Scrutor.

### Pourquoi pas MediatR ?

- Moins de magie, plus de lisibilite
- Injection directe du handler concret (pas de `ISender.Send()`)
- Pas de dependance tierce pour un pattern simple
- Les decorateurs Scrutor remplacent les pipelines MediatR

### Comment ca marche

**1. Definir l'intention (Command ou Query) :**

```csharp
// Une commande qui retourne un Guid
public sealed class CreateTodoCommand : ICommand<Guid>
{
    public Guid UserId { get; set; }
    public string Description { get; set; }
    // ...
}

// Une query qui retourne une liste
public sealed record GetTodosQuery(Guid UserId) : IQuery<List<TodoResponse>>;
```

**2. Implementer le handler :**

```csharp
internal sealed class CreateTodoCommandHandler(
    IApplicationDbContext context,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext)
    : ICommandHandler<CreateTodoCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateTodoCommand command, CancellationToken cancellationToken)
    {
        // Toute la logique metier ici
        // Retourne Result.Success(id) ou Result.Failure<Guid>(error)
    }
}
```

**3. Consommer dans l'endpoint :**

```csharp
app.MapPost("todos", async (
    Request request,
    ICommandHandler<CreateTodoCommand, Guid> handler,  // Injecte automatiquement par Scrutor
    CancellationToken cancellationToken) =>
{
    Result<Guid> result = await handler.Handle(command, cancellationToken);
    return result.Match(Results.Ok, CustomResults.Problem);
});
```

### Commandes vs Queries

| Aspect | Command | Query |
|--------|---------|-------|
| Interface | `ICommand` ou `ICommand<TResponse>` | `IQuery<TResponse>` |
| Handler | `ICommandHandler<TCommand>` ou `ICommandHandler<TCommand, TResponse>` | `IQueryHandler<TQuery, TResponse>` |
| Retour | `Result` ou `Result<TResponse>` | `Result<TResponse>` |
| But | Modifier l'etat (ecriture) | Lire des donnees (lecture) |
| Validation | Oui (via `ValidationDecorator`) | Non |
| Logging | Oui (via `LoggingDecorator`) | Oui |

---

## 6. Le pattern Result

### Remplacer les exceptions par des Results

**N-Tier (exceptions) :**

```csharp
public async Task<TodoItem> GetByIdAsync(Guid id)
{
    var todo = await _context.TodoItems.FindAsync(id);
    if (todo == null)
        throw new NotFoundException($"Todo {id} not found");  // Exception = cher, implicite
    return todo;
}
```

**Clean Architecture (Result) :**

```csharp
public async Task<Result<TodoResponse>> Handle(GetTodoByIdQuery query, CancellationToken ct)
{
    var todo = await context.TodoItems.FindAsync(query.TodoItemId, ct);
    if (todo is null)
        return Result.Failure<TodoResponse>(TodoItemErrors.NotFound(query.TodoItemId));  // Explicite, type
    return new TodoResponse { ... };
}
```

### Avantages

- **Explicite** : la signature `Task<Result<T>>` montre qu'un echec est possible
- **Performant** : pas de cout de stack unwinding comme les exceptions
- **Compose** : le `Match` dans les endpoints mappe proprement vers les reponses HTTP
- **Type** : les erreurs sont des objets structures (`Error.Code`, `Error.Description`, `ErrorType`)

### Conversion Result vers HTTP

`CustomResults.Problem` dans `Web.Api` convertit automatiquement les erreurs :

| `ErrorType` | Status HTTP |
|---|---|
| `Validation` / `Problem` | 400 Bad Request |
| `NotFound` | 404 Not Found |
| `Conflict` | 409 Conflict |
| Autre | 500 Internal Server Error |

---

## 7. Les Domain Events

### Le probleme dans le N-Tier

Dans un service N-Tier, les effets de bord (envoyer un email, notifier un systeme externe) sont directement dans la methode :

```csharp
// N-Tier - tout dans le meme service
public async Task<Guid> RegisterUserAsync(string email, string password)
{
    var user = new User { ... };
    _context.Users.Add(user);
    await _context.SaveChangesAsync();

    await _emailService.SendVerificationEmail(user);   // Couplage direct
    await _analyticsService.TrackRegistration(user);   // Ca grossit...
    await _notificationService.NotifyAdmin(user);      // Et encore...
}
```

### La solution : Domain Events

**1. L'entite leve un evenement :**

```csharp
todoItem.Raise(new TodoItemCreatedDomainEvent(todoItem.Id));
```

**2. Le `SaveChangesAsync` dispatche automatiquement :**

Dans `ApplicationDbContext`, le `SaveChangesAsync` est surcharge pour extraire et publier les domain events apres la sauvegarde :

```csharp
public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
{
    List<IDomainEvent> domainEvents = ExtractDomainEvents();
    int result = await base.SaveChangesAsync(cancellationToken);
    await PublishDomainEventsAsync(domainEvents);  // Apres le save = eventual consistency
    return result;
}
```

**3. Les handlers reagissent :**

```csharp
internal sealed class UserRegisteredDomainEventHandler : IDomainEventHandler<UserRegisteredDomainEvent>
{
    public Task Handle(UserRegisteredDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        // Envoyer un email de verification, tracker l'analytics, etc.
        return Task.CompletedTask;
    }
}
```

**Avantages :**

- Le handler de creation ne sait pas ce qui se passe apres
- On peut ajouter des reactions sans modifier le code existant (Open/Closed Principle)
- Plusieurs handlers peuvent reagir au meme evenement
- Chaque handler est testable independamment

---

## 8. Les cross-cutting concerns (decorateurs)

### Le probleme dans le N-Tier

Dans un service N-Tier, la validation et le logging sont meles a la logique :

```csharp
public async Task<Guid> CreateTodoAsync(CreateTodoRequest request)
{
    _logger.LogInformation("Creating todo...");   // Logging
    var stopwatch = Stopwatch.StartNew();

    if (string.IsNullOrEmpty(request.Description))  // Validation manuelle
        throw new ValidationException("...");

    // ... logique metier ...

    _logger.LogInformation("Created todo in {Elapsed}ms", stopwatch.ElapsedMilliseconds);
    return todo.Id;
}
```

### La solution : le pattern Decorator avec Scrutor

Ce repository utilise le **decorator pattern** au lieu des pipelines MediatR.

**`Application/DependencyInjection.cs` :**

```csharp
// Chaque handler est "enveloppe" par les decorateurs dans cet ordre :
// Requete --> LoggingDecorator --> ValidationDecorator --> Handler reel

services.Decorate(typeof(ICommandHandler<,>), typeof(ValidationDecorator.CommandHandler<,>));
services.Decorate(typeof(ICommandHandler<>), typeof(ValidationDecorator.CommandBaseHandler<>));
services.Decorate(typeof(IQueryHandler<,>), typeof(LoggingDecorator.QueryHandler<,>));
services.Decorate(typeof(ICommandHandler<,>), typeof(LoggingDecorator.CommandHandler<,>));
services.Decorate(typeof(ICommandHandler<>), typeof(LoggingDecorator.CommandBaseHandler<>));
```

**Comment fonctionne le `ValidationDecorator` :**

```csharp
internal sealed class CommandHandler<TCommand, TResponse>(
    ICommandHandler<TCommand, TResponse> innerHandler,          // Le "vrai" handler
    IEnumerable<IValidator<TCommand>> validators)               // Tous les validators pour ce command
    : ICommandHandler<TCommand, TResponse>
{
    public async Task<Result<TResponse>> Handle(TCommand command, CancellationToken ct)
    {
        // 1. Valider d'abord
        ValidationFailure[] failures = await ValidateAsync(command, validators);

        if (failures.Length > 0)
            return Result.Failure<TResponse>(CreateValidationError(failures));

        // 2. Si valide, passer au handler reel
        return await innerHandler.Handle(command, ct);
    }
}
```

**Resultat :** les handlers ne contiennent AUCUN code de validation ni de logging. Ils ne font que la logique metier.

---

## 9. Les tests d'architecture

Le projet `tests/ArchitectureTests` utilise **NetArchTest** pour garantir que les regles de dependances sont respectees au compile-time.

```csharp
[Fact]
public void Domain_Should_NotHaveDependencyOnApplication()
{
    TestResult result = Types.InAssembly(DomainAssembly)
        .Should()
        .NotHaveDependencyOn("Application")
        .GetResult();

    result.IsSuccessful.ShouldBeTrue();
}
```

**Regles enforces :**

| Couche | Ne doit PAS dependre de |
|---|---|
| Domain | Application, Infrastructure, Web.Api |
| Application | Infrastructure, Web.Api |
| Infrastructure | Web.Api |

Ces tests echouent si quelqu'un ajoute un `using Infrastructure;` dans un fichier de la couche Application. C'est un filet de securite automatique.

---

## 10. Checklist de migration

### Phase 1 : Fondations

- [ ] Creer le projet `SharedKernel` avec `Entity`, `Result<T>`, `Error`, `IDomainEvent`
- [ ] Creer le projet `Domain` qui reference uniquement `SharedKernel`
- [ ] Deplacer les entites dans `Domain`, les faire heriter de `Entity`
- [ ] Creer les classes `*Errors` pour chaque entite (remplacer les exceptions)

### Phase 2 : Application

- [ ] Creer le projet `Application` avec les interfaces CQRS (`ICommand`, `IQuery`, `ICommandHandler`, `IQueryHandler`)
- [ ] Definir `IApplicationDbContext` dans `Application/Abstractions/Data/`
- [ ] Definir les autres interfaces (`IUserContext`, `ITokenProvider`, etc.)
- [ ] Pour chaque methode publique du service N-Tier, creer un dossier avec :
  - Le Command ou Query
  - Le Handler
  - Le Validator (si c'est un Command)
  - Le DTO de reponse (si necessaire)

### Phase 3 : Infrastructure

- [ ] Creer le projet `Infrastructure`
- [ ] Deplacer `ApplicationDbContext` → implementer `IApplicationDbContext`
- [ ] Deplacer l'authentification, les autorisations, etc.
- [ ] Implementer le `DomainEventsDispatcher`
- [ ] Creer `DependencyInjection.cs` pour l'enregistrement DI

### Phase 4 : Presentation

- [ ] Transformer les controllers en endpoints `IEndpoint` (ou garder les controllers)
- [ ] Remplacer les `try/catch` par `result.Match()`
- [ ] Configurer le composition root dans `Program.cs`

### Phase 5 : Securisation

- [ ] Ajouter les tests d'architecture (NetArchTest) pour enforcer les regles de dependances
- [ ] Supprimer l'ancien projet de services N-Tier
- [ ] Verifier que le build passe avec `TreatWarningsAsErrors`

---

## Resume visuel

```
N-TIER                              CLEAN ARCHITECTURE
======                              ==================

Controller                          Endpoint (IEndpoint)
    |                                   |
    v                                   v
TodoService (tout dedans)           ICommandHandler<CreateTodoCommand, Guid>
    |                                   |  (validation auto via decorateur)
    |                                   |  (logging auto via decorateur)
    v                                   v
ApplicationDbContext (direct)       IApplicationDbContext (interface)
    |                                   |
    v                                   v
PostgreSQL                          ApplicationDbContext (Infrastructure)
                                        |  (dispatch Domain Events auto)
                                        v
                                    PostgreSQL
```

**En une phrase :** on passe d'un gros service qui fait tout, a des petits handlers isoles qui ne connaissent que des interfaces, entoures de decorateurs qui gerent les concerns transversaux automatiquement.