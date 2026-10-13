namespace KnOwl.ButterMorph.SampleTypes;

using global::ButterMorph.Contracts;

/// <summary>
/// Stable customer identifier used by sample contracts.
/// </summary>
[ButterMorphLanguageType("CustomerCode", Name = "Customer Code", Version = "1.0.0", Description = "Stable customer identifier value object.")]
public readonly record struct CustomerCode(string Value);

/// <summary>
/// Business order number used by sample command payloads.
/// </summary>
[ButterMorphLanguageType("OrderNumber", Name = "Order Number", Version = "1.0.0", Description = "Business order number value object.")]
public readonly record struct OrderNumber(string Value);

/// <summary>
/// Carrier tracking identifier used by sample events.
/// </summary>
[ButterMorphLanguageType("TrackingNumber", Name = "Tracking Number", Version = "1.0.0", Description = "Carrier tracking identifier value object.")]
public readonly record struct TrackingNumber(string Value);

/// <summary>
/// ISO-4217 currency code.
/// </summary>
[ButterMorphLanguageType("IsoCurrencyCode", Name = "ISO Currency Code", Version = "1.0.0", Description = "Three-letter ISO-4217 currency code.")]
public readonly record struct IsoCurrencyCode(string Value);

/// <summary>
/// Decimal money amount value object.
/// </summary>
[ButterMorphLanguageType("MoneyAmount", Name = "Money Amount", Version = "1.0.0", Description = "Decimal money amount used by generated contracts.")]
public readonly record struct MoneyAmount(decimal Value);

/// <summary>
/// Geographic latitude coordinate.
/// </summary>
[ButterMorphLanguageType("Latitude", Name = "Latitude", Version = "1.0.0", Description = "Latitude coordinate value object.")]
public readonly record struct Latitude(decimal Value);

/// <summary>
/// Geographic longitude coordinate.
/// </summary>
[ButterMorphLanguageType("Longitude", Name = "Longitude", Version = "1.0.0", Description = "Longitude coordinate value object.")]
public readonly record struct Longitude(decimal Value);

/// <summary>
/// UTC timestamp wrapper for audit and integration contracts.
/// </summary>
[ButterMorphLanguageType("UtcTimestamp", Name = "UTC Timestamp", Version = "1.0.0", Description = "UTC timestamp value object.")]
public readonly record struct UtcTimestamp(DateTimeOffset Value);

/// <summary>
/// User or system actor captured by audit metadata.
/// </summary>
[ButterMorphLanguageType("AuditActor", Name = "Audit Actor", Version = "1.0.0", Description = "User or system actor for audit metadata.")]
public readonly record struct AuditActor(string Value);

/// <summary>
/// Correlation identifier propagated across integrations.
/// </summary>
[ButterMorphLanguageType("CorrelationId", Name = "Correlation Id", Version = "1.0.0", Description = "Correlation identifier value object.")]
public readonly record struct CorrelationId(Guid Value);

/// <summary>
/// Tax identifier for sample billing contracts.
/// </summary>
[ButterMorphLanguageType("TaxIdentifier", Name = "Tax Identifier", Version = "1.0.0", Description = "Business tax identifier value object.")]
public readonly record struct TaxIdentifier(string Value);

/// <summary>
/// Shipping priority values used by sample release artifacts.
/// </summary>
[ButterMorphLanguageType("FulfillmentPriority", Name = "Fulfillment Priority", Version = "1.0.0", Description = "Allowed fulfillment priority values.")]
public enum FulfillmentPriority
{
    /// <summary>
    /// Standard fulfillment priority.
    /// </summary>
    Standard,

    /// <summary>
    /// Expedited fulfillment priority.
    /// </summary>
    Expedited,

    /// <summary>
    /// Critical fulfillment priority.
    /// </summary>
    Critical
}
