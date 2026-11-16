using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Maestro.Desktop;

/// <summary>One amp as shown in the sidebar.</summary>
public class AmpItem : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private string _name = "";
    public string Name
    {
        get => _name;
        set
        {
            if (_name == value)
                return;
            _name = value;
            OnPropertyChanged();
        }
    }

    private string _ip = "";
    public string Ip
    {
        get => _ip;
        set
        {
            if (_ip == value)
                return;
            _ip = value;
            OnPropertyChanged();
        }
    }

    private bool _online;
    public bool Online
    {
        get => _online;
        set
        {
            if (_online == value)
                return;
            _online = value;
            OnPropertyChanged();
        }
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
