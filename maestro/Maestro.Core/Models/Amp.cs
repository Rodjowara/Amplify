namespace Maestro.Core.Models;

public class Amp
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Name { get; }
    public string Ip { get; }
    public bool IsOnline { get; private set; }

    public event Action? StatusChanged;

    internal Amp(string name, string ip)
    {
        Name = name;
        Ip = ip;
    }

    internal void SetOnline(bool online)
    {
        if (IsOnline == online)
            return;

        IsOnline = online;
        StatusChanged?.Invoke();
    }
}
