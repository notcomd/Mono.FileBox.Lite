using Mono.FileBox.Lite.Abstractions.Index;
using Mono.FileBox.Lite.Abstractions.Storage;

namespace Mono.FileBox.Lite.Abstractions.UseCases;

/// <summary>A generic object use-case: takes a command, returns a result.</summary>
public interface IObjectUseCase<in TCommand, TResult>
{
    Task<TResult> ExecuteAsync(TCommand command, CancellationToken ct);
}

// -------- Commands --------

public sealed class PutObjectCommand
{
    public string NamespaceId { get; init; } = string.Empty;
    public string? ObjectKey { get; init; }
    public Stream Content { get; init; } = Stream.Null;
    public string? ContentType { get; init; }
    public IReadOnlyDictionary<string, string>? Tags { get; init; }
    public IReadOnlyDictionary<string, object>? Attributes { get; init; }
    public WriteOptions? Write { get; init; }
}

public sealed class GetObjectCommand
{
    public string ContentHash { get; init; } = string.Empty;
    public string NamespaceId { get; init; } = string.Empty;
    public long Offset { get; init; }
    public long? Length { get; init; }
}

public sealed class GetObjectResult
{
    public string ContentHash { get; init; } = string.Empty;
    public Stream? Content { get; init; }
    public long Length { get; init; }
    public string? ContentType { get; init; }

    public static GetObjectResult NotFound(string contentHash) => new() { ContentHash = contentHash };
}

public sealed class DeleteObjectCommand
{
    public string ContentHash { get; init; } = string.Empty;
    public string NamespaceId { get; init; } = string.Empty;
}

public sealed class DeleteObjectResult
{
    public string ContentHash { get; init; } = string.Empty;
    public ObjectState State { get; init; }
    public bool Deleted { get; init; }

    public static DeleteObjectResult Success(string hash, ObjectState state)
        => new() { ContentHash = hash, State = state, Deleted = true };
}

public sealed class ArchiveObjectCommand
{
    public string ContentHash { get; init; } = string.Empty;
    public string NamespaceId { get; init; } = string.Empty;
    public StorageTier Tier { get; init; } = StorageTier.Cold;
}

public sealed class ArchiveObjectResult
{
    public string ContentHash { get; init; } = string.Empty;
    public ObjectState State { get; init; }
    public bool Archived { get; init; }

    public static ArchiveObjectResult Success(string hash, ObjectState state)
        => new() { ContentHash = hash, State = state, Archived = true };
}

public sealed class RestoreObjectCommand
{
    public string ContentHash { get; init; } = string.Empty;
    public string NamespaceId { get; init; } = string.Empty;
}

public sealed class RestoreObjectResult
{
    public string ContentHash { get; init; } = string.Empty;
    public ObjectState State { get; init; }
    public bool Restored { get; init; }

    public static RestoreObjectResult Success(string hash, ObjectState state)
        => new() { ContentHash = hash, State = state, Restored = true };
}

/// <summary>Result of a full put pipeline. Contains the produced content hash.</summary>
public sealed class PutObjectResult
{
    public string ContentHash { get; init; } = string.Empty;
    public ObjectState State { get; init; }
    public bool Succeeded { get; init; }
    public string? Error { get; init; }

    public static PutObjectResult Success(string hash, ObjectState state)
        => new() { ContentHash = hash, State = state, Succeeded = true };

    public static PutObjectResult Failure(Exception ex, ObjectState state)
        => new() { State = state, Succeeded = false, Error = ex.Message };
}

// -------- Use-case interfaces --------

public interface IPutObjectUseCase : IObjectUseCase<PutObjectCommand, PutObjectResult> { }
public interface IGetObjectUseCase : IObjectUseCase<GetObjectCommand, GetObjectResult> { }
public interface IDeleteObjectUseCase : IObjectUseCase<DeleteObjectCommand, DeleteObjectResult> { }
public interface IArchiveObjectUseCase : IObjectUseCase<ArchiveObjectCommand, ArchiveObjectResult> { }
public interface IRestoreObjectUseCase : IObjectUseCase<RestoreObjectCommand, RestoreObjectResult> { }

/// <summary>
/// Rolls back a partially completed put pipeline (and other multi-step flows) in
/// reverse order of completion.
/// </summary>
public interface IPutObjectRollback
{
    Task RollbackAsync(IEnumerable<ObjectTrigger> completed, IObjectContext ctx, CancellationToken ct);
}