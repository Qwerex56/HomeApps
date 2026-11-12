using FluentResults;
using HomeApps.BuildingBlocks.Domain.Errors;

namespace HomeMix.Domain.Data;

public static class DataErrors
{
    public static Error NotFound => new AppError("homemix.document_not_found", "There is no such document.", ErrorKind.NotFound);

    public static Error Conflict => new AppError(
        "homemix.conflict", "Someone changed this document first; read it again and retry.", ErrorKind.Conflict);

    public static Error CapabilityMissing(string capability) => new AppError(
        "homemix.capability_missing", $"This add-on was not granted {capability}.", ErrorKind.Forbidden);

    public static Error ReadOnly => new AppError(
        "homemix.read_only", "Your role in this household may only read this add-on's data.", ErrorKind.Forbidden);

    public static Error TooLarge(long maxBytes) => new AppError(
        "homemix.document_too_large", $"A document may have at most {maxBytes / 1024} KB.", ErrorKind.Validation);

    public static Error StorageFull(long maxBytes) => new AppError(
        "homemix.storage_full", $"This add-on's data may take at most {maxBytes / (1024 * 1024)} MB here.", ErrorKind.Conflict);

    public static Error CollectionFull(int maxDocuments) => new AppError(
        "homemix.collection_full", $"A collection may hold at most {maxDocuments} documents.", ErrorKind.Conflict);
}
