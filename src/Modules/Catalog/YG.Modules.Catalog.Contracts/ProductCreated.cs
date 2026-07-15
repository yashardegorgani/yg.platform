
namespace YG.Modules.Catalog.Contracts;

// Event: past-tense name, a fact, zero-to-many listeners.
public sealed record ProductCreated(Guid Id, string Name);