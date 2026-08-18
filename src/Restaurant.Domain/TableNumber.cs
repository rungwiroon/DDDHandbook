using Vogen;

namespace Restaurant.Domain;

[ValueObject<int>]
public partial struct TableNumber
{
    private static Validation Validate(int value) =>
        value > 0 ? Validation.Ok : Validation.Invalid("Table number must be greater than zero.");
}
