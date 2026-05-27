namespace Merkatto.Application.Common;

/// <summary>Requested entity does not exist (→ 404).</summary>
public sealed class NotFoundException(string message) : Exception(message);

/// <summary>Request conflicts with current state, e.g. duplicate (→ 409).</summary>
public sealed class ConflictException(string message) : Exception(message);

/// <summary>Business rule violated (→ 422).</summary>
public sealed class BusinessRuleException(string message) : Exception(message);
