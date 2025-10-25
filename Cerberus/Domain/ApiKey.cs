namespace Cerberus.Domain;

public record ApiKey(
    Guid Id,
    string Name,
    string KeyHash,
    Guid TenantId,
    Guid? ProjectId,
    DateTime CreatedAt,
    DateTime? ExpiresAt,
    DateTime? LastUsedAt,
    bool IsActive
);
