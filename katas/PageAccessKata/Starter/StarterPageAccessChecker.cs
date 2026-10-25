namespace PageAccessKata.Starter;

// Your attempt goes here. Start the timer, read README.md once, and begin
// with a comment listing your decisions about each edge case.
public sealed class StarterPageAccessChecker : IPageAccessChecker
{
    public IReadOnlyDictionary<string, IReadOnlyList<string>> AccessiblePagesByUser(
        IEnumerable<KataUser> users,
        IEnumerable<KataPage> pages)
    {
        throw new NotImplementedException();
    }
}
