namespace JeffHelper;

/// <summary>
/// Provides methods to validate answers.
/// </summary>
public static class Answer
{
    private static readonly IReadOnlyCollection<string> s_yesAnswers = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "y", "yes", "t", "true"
    };

    /// <summary>
    /// Checks if the input is yes.
    /// </summary>
    /// <param name="input">The input to check.</param>
    /// <returns><see langword="true"/> if the input is a yes answer; otherwise, <see langword="false"/>.</returns>
    public static bool IsYes(string input)
    {
        return s_yesAnswers.Contains(input);
    }
}
