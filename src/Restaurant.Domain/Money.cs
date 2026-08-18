using Vogen;

namespace Restaurant.Domain;

[ValueObject<decimal>]
public partial struct Money
{
    private static Validation Validate(decimal value) =>
        value >= 0 ? Validation.Ok : Validation.Invalid("Money cannot be negative.");
}
