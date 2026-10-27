namespace AniLink.Api.Data.Downloads;

// The passwords one job may try, in the order they are tried: the one that already worked for this job,
// then what the user typed for it (newest first), and last the defaults from docker-compose.
public sealed class PasswordBook(IReadOnlyList<string> defaults)
{
    private readonly Lock _gate = new();
    private readonly List<string> _supplied = [];
    private string? _working;

    public IReadOnlyList<string> Supplied
    {
        get
        {
            lock (_gate) return [.. _supplied];
        }
    }

    public string? Working
    {
        get
        {
            lock (_gate) return _working;
        }
    }

    public void Add(string password)
    {
        if (string.IsNullOrWhiteSpace(password)) return;

        lock (_gate)
        {
            _supplied.Remove(password);
            _supplied.Insert(0, password);
        }
    }

    public void Remember(string password)
    {
        lock (_gate) _working = password;
    }

    public void Restore(IEnumerable<string> supplied, string? working)
    {
        lock (_gate)
        {
            _supplied.Clear();
            _supplied.AddRange(supplied.Where(password => !string.IsNullOrWhiteSpace(password)));
            _working = working;
        }
    }

    public IReadOnlyList<string> Candidates()
    {
        lock (_gate)
        {
            return
            [
                .. (_working is null ? [] : new[] { _working })
                    .Concat(_supplied)
                    .Concat(defaults)
                    .Where(password => !string.IsNullOrWhiteSpace(password))
                    .Distinct(StringComparer.Ordinal)
            ];
        }
    }
}
