using Serilog;

namespace JeffHelper;

/// <summary>
/// Provides methods to ask questions.
/// </summary>
public static class Question
{
    /// <summary>
    /// Asks the user a question and validates the input.
    /// </summary>
    /// <typeparam name="T">The type of the expected answer.</typeparam>
    /// <param name="messageTemplate">The question to ask.</param>
    /// <param name="templateParameters">The parameters for the question.</param>
    /// <param name="validator">The function to validate the answer.</param>
    /// <returns>The validated answer.</returns>
    public static T Ask<T>(string messageTemplate, object[] templateParameters, T defaultValue, Func<string, (bool, T)> validator)
        where T : notnull
    {
        Log.Information(messageTemplate, templateParameters);
        var input = Console.ReadLine();

        if (string.IsNullOrEmpty(input))
        {
            return defaultValue;
        }

        var (success, value) = validator(input);
        if (success)
        {
            return value;
        }

        Log.Warning("Invalid input");
        return Ask(messageTemplate, templateParameters, defaultValue, validator);
    }
}
