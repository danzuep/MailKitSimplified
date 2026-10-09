# Fluent Mail Architecture and Implementation Plan

## Current Focus

Prioritize usable fluent, composable email. Do not expand this phase into immutable envelope redesign, ordered header nodes, HTML sanitization, or general rewriting. AST code is not part of the fluent/send path; further work on it is deferred to phase 7.

## Executive Architectural Blueprint

Make `MailKitSimplified.Generic` the dependency-free domain layer and `MailKitSimplified.Email` the composition and provider-integration layer. Represent content with one authoritative Email AST, keeping delivery-envelope recipients separate from rendered headers. Typed nodes support `IEmailVisitor` double-dispatch; C# pattern matching supports structural validation and immutable transformations. An email AST models message structure, not HTML syntax: HTML sanitization requires a dedicated parser outside the domain.

`GenericEmailBuilder` produces validated snapshots using `To()`, `Subject()`, `Body()`, `Attach()`, and `Build()`, without opening files or connecting to servers. Host-style registration configures providers; only `SendAsync()` or `EnqueueAsync()` initiates execution. Explicit adapters such as `MimeKitAstVisitor` and `ToMimeMessage()` translate domain content into provider payloads. Cloud adapters report unsupported capabilities instead of silently losing content. Dependency-minimal core libraries dual-target `netstandard2.0;net10.0`; infrastructure and applications target `net10.0`, with platform-specific variants where necessary.

SMTP and IMAP connections are stateful resources requiring exclusive leases. A bounded `MailKitConnectionPool<TClient>` reuses authenticated connections; `PooledClientLease<T>` implements `IAsyncDisposable` and uses `Interlocked.Exchange` for exactly-once release. `ConcurrentBag` and atomic operations support efficient reuse, but do not make the entire pool lock-free. Capacity control, health checks, and shutdown coordination remain necessary. IMAP folder selection and IDLE sessions need distinct lifecycle rules.

A unified `IEmailSender` supports MailKit, cloud, decorated, and null implementations. Compose telemetry and resilience decorators. `Microsoft.Extensions.Http.Resilience` applies to cloud HTTP clients; SMTP uses an explicit Polly resilience pipeline that accounts for ambiguous delivery and duplicate risks. Bounded `System.Threading.Channels` queues feed `BackgroundService` workers with backpressure, but are not durable delivery stores. Pin the .NET SDK and use xUnit with Microsoft.Testing.Platform v2 for reproducible direct `dotnet test --project` execution.

## Baseline at Start

Status recorded on 2026-10-09. Checked boxes mean implemented and verified, not simply designed.

- Phase 1 is reported complete by the maintainer. `global.json` selects Microsoft.Testing.Platform and existing tests target `net10.0`. Remaining reconciliation: no SDK pin, legacy VSTest packages remain, Generic/Email declare `TargetFramework` alongside inherited `TargetFrameworks`, and Generic still has infrastructure dependencies. Do not repeat a framework migration without first checking these details.
- Phase 2 has started: generic email/contact models and SMTP options exist, but there is no typed AST or visitor. Two public `GenericEmail` classes exist in `Models` and `Services`; preserve compatibility while choosing a canonical model.
- Phase 3 has started: fluent construction and MIME conversion exist. The builder exposes mutable `AsEmail`; its original `Copy()` shares the same email. MIME mapping lives inside `GenericEmailSender` and accepts untyped attachments.
- The first implementation slice adds detached `Build()` snapshots and independent `Copy()`. Legacy emails remain mutable; snapshots copy contacts, collections, and byte arrays, but borrow streams and arbitrary attachment objects. This is not yet immutable or fully replayable content.

## Phase-Based Task List

### Phase 1: Project Setup, Target Framework and Testing Platform v2

- [ ] Confirm the reported completion against an approved .NET 10 SDK pin and `rollForward` policy in `global.json`.
- [ ] Normalize framework declarations: core `netstandard2.0;net10.0`, infrastructure/tests `net10.0`, WPF `net10.0-windows`.
- [ ] Reconcile central package management and move Options, Logging, Annotations, and filesystem dependencies out of the pure domain.
- [ ] Use executable xUnit projects with an MTP v2-compatible runner; remove legacy test adapters and VSTest-only collectors.
- [ ] Verify both core TFMs and direct `dotnet test --project <TestProject.csproj>` in CI with MTP-native coverage/reporting.

### Phase 2: Generic Domain, Options and Email AST

- [x] Consolidate the generic email implementation, preserve dictionary headers and existing contacts, and add typed `EmailAttachment` contracts for core composition.
- [x] Add `IEmailAstNode`, `HeaderNode`, abstract `BodyNode`, `TextBodyNode`, `HtmlBodyNode`, `AttachmentNode`, and `MultipartContainerNode` with explicit mixed/alternative/related semantics.
- [x] Implement typed `IEmailVisitor` double-dispatch through `Accept()` and deterministic traversal; use C# switch expressions and positional/property patterns for inspection.
- [x] Add bounded structural validation for composed multipart bodies. General transformations, HTML policies, and richer inspections are deferred to phase 7.
- [ ] Introduce a binding-friendly `GenericSmtpOptions` record with neutral TLS/authentication settings; use infrastructure `IValidateOptions<T>`, redact secrets, and accommodate `IsExternalInit` on `netstandard2.0`.

### Phase 3: Fluent Builder and Provider Mapping

- [x] Add `Compose()`, `Body(text, html)`, `Body(BodyNode)`, and typed `Attach()`; preserve independent `Build()` snapshots and existing `AsEmail`/body methods.
- [ ] Complete core input validation for envelope/header fields without replacing dictionary headers or mutable snapshots.
- [x] Replace shallow `MemberwiseClone()` with independent builder state; test contacts, every recipient collection, headers, and byte-array attachments.
- [x] Add `ToMimeMessageAsync()` for composed bodies, dictionary headers, typed/legacy attachments, encoding, content IDs, cancellation, and message ownership; use it from the sender.
- [ ] Verify Bcc envelope/wire behavior and migrate remaining synchronous legacy conversion callers before retiring the old converter.
- [x] Define replayable attachment factories and stream ownership; defer file access to cancellable MIME materialization rather than `Build()`.
- [ ] Add a concrete cloud adapter with capability checks; expose host-style provider configuration and explicit terminal `SendAsync()`/`EnqueueAsync()` execution.

### Phase 4: High-Performance Connection Pooling

- [ ] Implement bounded `MailKitConnectionPool<TClient>` with `ConcurrentBag`, async client factories, and `SemaphoreSlim`; key pools by endpoint, identity, and TLS policy.
- [ ] Implement `ValueTask<PooledClientLease<TClient>> RentAsync(CancellationToken)` and reference-type `IAsyncDisposable` leases using `Interlocked.Exchange` for exactly-once release.
- [ ] Connect/authenticate before leasing; enforce exclusive client use and discard expired, disconnected, faulted, or protocol-uncertain clients.
- [ ] Add idle/lifetime eviction and async shutdown; cover cancellation, creation failure, and return/dispose races without leaking clients or permits.
- [ ] Specify IMAP folder-state reset and dedicated IDLE leases; benchmark handshake reuse and allocation costs without claiming the complete pool is lock-free.

### Phase 5: Resilient Decorators and Null Sender

- [ ] Define `Task IEmailSender.SendAsync(GenericEmail, CancellationToken)` and compatibility adapters for current sender interfaces.
- [ ] Implement MailKit/cloud senders with per-attempt leases, replayable materialization, and end-to-end cancellation.
- [ ] Add Polly retry/timeout/circuit-breaker decorators; configure `Microsoft.Extensions.Http.Resilience` only on HTTP providers and avoid stacked retries.
- [ ] Classify transient, permanent, cancelled, and ambiguous outcomes; avoid blind SMTP retries after possible acceptance and use supported cloud idempotency keys.
- [ ] Register telemetry/resilience in documented order; provide cancellation-aware `NullEmailSender` with explicit dry-run status and privacy-safe logging.

### Phase 6: Asynchronous Channel Pipelines

- [ ] Create bounded `Channel<QueuedEmail>` with immutable snapshots, correlation metadata, cancellation-aware `ValueTask` enqueue, and explicit wait/reject backpressure.
- [ ] Consume via `BackgroundService.ReadAllAsync()` with bounded workers and DI lifetimes appropriate to each decorated sender.
- [ ] Separate queued execution deadlines from HTTP request tokens; own or durably reference attachment content before queue acceptance.
- [ ] Implement completion, graceful drain, shutdown deadlines, failure routing, and metrics; isolate per-message failures without swallowing cancellation.
- [ ] Document process-crash loss and acceptance-versus-delivery semantics; define an outbox/broker boundary for durable workloads.

### Phase 7: DI, Tests and Documentation

- [ ] Revisit `EmailAstRewriter`, link rewriting, HTML sanitization, richer inspections, and optional immutable address/envelope/header contracts after the core sending workflow is complete.
- [ ] Add host/service registration for providers, configuration binding, `ValidateOnStart()`, pools/queues, decorators, and hosted workers.
- [ ] Add xUnit tests for AST traversal/pattern matching, MIME/cloud mapping, snapshot isolation, replay/disposal, TLS options, and domain dependency boundaries.
- [ ] Add deterministic race tests for `Interlocked.Exchange`, exclusive leasing, limits, cancellation, retries, shutdown, and Channel draining/backpressure.
- [ ] Add local SMTP/IMAP and cloud HTTP contract tests using direct MTP v2 `dotnet test --project` execution, filters, and coverage.
- [ ] Document fluent usage, deferred execution, migration, AST visitors, provider registration, dry runs, retry ambiguity, durability, and measured throughput.

## Ordered Route to the End of Phase 3

1. Establish builder isolation first. Add `Build()` and independent `Copy()` without changing `AsEmail` or performing I/O. Test mutable contacts and byte arrays; explicitly retain borrowed stream semantics until typed attachments replace them.
2. Resolve the dual `GenericEmail` types through a compatibility facade rather than removing a public class. Keep existing contacts and dictionary headers; use one composed body tree with legacy text/HTML setters as adapters.
3. Define replayable attachment sources (bytes and fresh-stream factories), ownership, file-path behavior, MIME metadata, and cancellation. Reject unsupported arbitrary objects in the new typed API while isolating legacy handling.
4. Implement mixed/alternative/related body composition and basic structural validation. Defer general immutable rewrites and content policies; keep the current phase focused on fluent construction and sending.
5. Remove domain infrastructure dependencies and migrate SMTP options to a binding-friendly record. Keep IConfiguration binding, validation, logging, filesystem access, and provider types in integration assemblies. Build both domain TFMs.
6. Teach the builder fluent reusable composition using `Compose()`, `Body()`, and typed `Attach()`. Preserve mutable snapshots while sharing immutable body nodes; test independence, compatibility, and deferred file access.
7. Extract MIME conversion into an adapter/visitor. Use project references to the local Generic/Sender/Receiver implementations rather than published packages when validating local changes. Preserve sender connection/authentication behavior while replacing only mapping.
8. Test MIME structure, attachment ownership, header injection, Bcc envelope behavior, repeated sends, and cancellation. Handle separate SMTP envelope arguments when Bcc is absent from serialized headers.
9. Implement one cloud mapping adapter with explicit unsupported-feature checks and contract tests. Choose the cloud provider before adding its SDK/package or transport implementation.
10. Add host-style configuration and explicit execution endpoints without implementing phase 4 pooling or phase 6 workers prematurely. Document examples and run focused MTP tests plus both domain TFMs.

### Acceptance Gates

- [ ] Generic has no external package dependencies; its public model/AST does not reference MimeKit or MailKit.
- [x] A single body tree backs text/HTML accessors; build snapshots remain independent and mutable, sharing immutable body nodes.
- [ ] Typed attachment materialization is replayable, cancellation-aware, ownership-safe, and deferred until execution.
- [ ] MIME and one cloud adapter preserve supported semantics and reject unsupported features explicitly.
- [ ] Existing fluent APIs retain documented compatibility; focused tests and both core framework builds pass.

### Implementation Progress

- [x] Initial increment: detached `Build()` and independent `Copy()`, tested through the Generic xUnit project.
- [x] Repair Generic's central package management mismatch and inherit `netstandard2.0;net10.0` from source build props; retain existing package versions.
- [x] Consolidate duplicate mutable email implementations: `Models.GenericEmail` owns state; `Services.GenericEmail` is a compatibility wrapper.
- [x] Add immutable `EmailAttachment` metadata with copied bytes, deferred file access, fresh-stream factories, cancellation, and explicit consumer ownership.
- [x] Add immutable AST nodes, typed visitor dispatch, ordered `EmailAstWalker`, and bounded `EmailAstValidator`.
- [x] Add `EmailAstRewriter` using C# pattern matching (historical increment; further work deferred to phase 7 and not used by the fluent/send path).
- [x] Add fluent reusable composition and connect composed bodies/typed attachments to asynchronous MIME materialization and sender execution.
- [ ] Follow-up: core envelope/header validation and end-to-end send tests, without redesigning headers or making emails immutable.
- [ ] Follow-up: MIME/cloud adapters, host-style integration, and phase 3 acceptance gates.

### Decisions

- Preserve existing public APIs by adding new contracts and compatibility adapters rather than an immediate breaking replacement.
- Keep legacy stream/arbitrary-object attachment references borrowed in initial snapshots; do not promise replayability or transfer ownership implicitly.
- Cloud-provider selection is pending; no cloud SDK is introduced in the first increment.
- `Models.GenericEmail` owns the mutable implementation; `Services.GenericEmail` forwards to it. Neither public name is removed. `BodyText`/`BodyHtml` read the first matching body representation and replace matching representations when set, preserving surrounding multipart structure.
- Headers remain `IDictionary<string, string>`. Immutable envelope/address redesign and ordered headers are not prerequisites for this phase.
- Typed attachment factories must return a fresh readable stream on every call and transfer ownership to the consumer. The descriptor disposes returned streams if validation/cancellation fails before ownership transfer.
- Byte attachments snapshot content immediately. File attachments pin an absolute path without opening it; the file must remain available and unchanged for stable replay. Arbitrary stream factories are replayable by contract, not guaranteed by the library.

### Verified Initial Increment

- `dotnet test --project tests/MailKitSimplified.Generic.Tests/MailKitSimplified.Generic.Tests.csproj`: 9 passed, 0 failed, 0 skipped on `net10.0`.
- `dotnet build source/MailKitSimplified.Generic/MailKitSimplified.Generic.csproj --framework netstandard2.0 --no-restore -p:GeneratePackageOnBuild=false`: passed.
- The main solution already includes the Generic test project. Existing Sender/Receiver test projects were not changed or rerun.
- `Build()` copies legacy state without validation or I/O; mutable results and borrowed stream/object attachments are transitional behavior, not completion of phase 2 or phase 3.
- Phase 1 was not reimplemented wholesale. Email's framework/package declarations, SDK pinning, and legacy test adapters remain follow-ups from the starting baseline.

### Verified Attachment and AST Increment

- `dotnet test --project tests/MailKitSimplified.Generic.Tests/MailKitSimplified.Generic.Tests.csproj`: 40 passed, 0 failed, 0 skipped on `net10.0`.
- `dotnet build source/MailKitSimplified.Generic/MailKitSimplified.Generic.csproj --no-restore -p:GeneratePackageOnBuild=false`: passed for `netstandard2.0` and `net10.0`.
- Tests cover legacy facade storage, attachment snapshot isolation, deferred opening, factory cancellation/cleanup, header injection rejection, immutable multipart children, visitor order, positional patterns, depth/node/text limits, multipart rules, and immutable rewrites.
- `EmailAstValidator` checks text character counts, not encoded MIME byte size or attachment size. HTML sanitization/link policies and provider payload limits remain integration work.
- The AST and typed attachments are standalone domain contracts at this stage. Do not pass `EmailAttachment` through the legacy `Attach(string, object)` overload: the existing MIME mapper does not understand it yet.
- Next implementation slice: immutable address/envelope contracts and an authoritative AST build path, followed by a MIME adapter with attachment ownership/cancellation tests. Existing `Build()`/`AsEmail` remain legacy mutable APIs.

### Verified Fluent and MIME Increment

- Generic tests: 43 passed. Email MIME integration tests: 6 passed.
- Generic and Email build successfully for both `netstandard2.0` and `net10.0`.
- `Compose(Action<GenericEmailBuilder>)` supports reusable fluent configuration. `Body(text, html)` creates alternatives; `Body(BodyNode)` accepts explicit multipart content. `Attach(EmailAttachment)` remains deferred until MIME conversion.
- `Build()` copies mutable email state and shares immutable body nodes/descriptors. Legacy text/HTML setters update the body tree while preserving inline resources.
- `ToMimeMessageAsync()` maps specialized MimeKit alternative/related containers, typed and legacy attachments, and content IDs. Callers dispose returned messages; owned attachment streams are released with the message or on conversion failure. Legacy streams remain caller-owned.
- The sender now uses async conversion with cancellation and disposes the resulting payload after sending. Email references the local Generic project so the fluent additions are available.
- Dictionary headers are unchanged. Advanced rewriting and immutable envelope redesign are deferred, not expanded in this increment. The earlier standalone-AST warning is superseded: typed attachments now work in the async fluent/send path.

```csharp
Action<GenericEmailBuilder> defaults = email => email
	.From("sender@example.com")
	.Header("X-Campaign", "welcome")
	.Body("Welcome", "<p>Welcome</p>");

var email = new GenericEmailBuilder()
	.Compose(defaults)
	.To("recipient@example.com")
	.Subject("Hello")
	.Attach(EmailAttachment.FromBytes("note.txt", new byte[] { 72, 105 }, "text/plain"))
	.Build();

using var message = await email.ToMimeMessageAsync(cancellationToken);
```

#### Domain Construction Example

```csharp
var content = new MultipartContainerNode(MultipartKind.Mixed, new BodyNode[]
{
	new MultipartContainerNode(MultipartKind.Alternative, new BodyNode[]
	{
		new TextBodyNode("Hello"),
		new HtmlBodyNode("<p>Hello</p>")
	}),
	new AttachmentNode(EmailAttachment.FromBytes("note.txt", new byte[] { 72, 105 }, "text/plain"))
});

EmailAstValidator.Validate(content);
// Visit with an EmailAstWalker subclass or transform with an EmailAstRewriter subclass.
// This constructs domain content only; it does not send or enqueue email.
```