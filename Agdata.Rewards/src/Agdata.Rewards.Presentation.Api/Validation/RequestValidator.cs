using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Agdata.Rewards.Presentation.Api.Validation;

public static class RequestValidator
{
    public const string GeneralErrorsKey = "general";

    public static Dictionary<string, string[]> Validate<T>(T instance)
        where T : class
    {
        var validationResults = new List<ValidationResult>();
        var context = new ValidationContext(instance);
        Validator.TryValidateObject(instance, context, validationResults, validateAllProperties: true);

        return validationResults
            .SelectMany(result =>
                result.MemberNames.Any()
                    ? result.MemberNames.Select(member => (Member: member, Message: result.ErrorMessage ?? string.Empty))
                    : new[] { (Member: GeneralErrorsKey, Message: result.ErrorMessage ?? string.Empty) })
            .GroupBy(tuple => tuple.Member, tuple => tuple.Message)
            .ToDictionary(group => group.Key, group => group.ToArray());
    }
}
