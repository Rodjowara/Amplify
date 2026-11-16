using Qt.Quick;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Maestro.Desktop;

/// <summary>
/// Sidebar state and a simple router: CurrentPage is "playlist", "settings" or "tuning"
/// (the tuning page is for the amp in SelectedAmpIp).
/// </summary>
[QmlElement]
public class SidebarViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    // TODO: fill from IAmpManager once connections are mapped to a UI amp model.
    public ObservableCollection<AmpItem> Amps { get; } = new()
    {
        new AmpItem { Name = "Stage 1 Left", Ip = "192.168.1.21", Online = true },
        new AmpItem { Name = "Stage 1 Right", Ip = "192.168.1.22", Online = true },
        new AmpItem { Name = "Woofer", Ip = "192.168.1.23", Online = false },
    };

    private string _currentPage = "playlist";
    public string CurrentPage
    {
        get => _currentPage;
        private set
        {
            if (_currentPage == value)
                return;
            _currentPage = value;
            OnPropertyChanged();
        }
    }

    private string _selectedAmpIp = "";
    public string SelectedAmpIp
    {
        get => _selectedAmpIp;
        private set
        {
            if (_selectedAmpIp == value)
                return;
            _selectedAmpIp = value;
            OnPropertyChanged();
        }
    }

    public void Navigate(string page)
    {
        SelectedAmpIp = "";
        CurrentPage = page;
    }

    public void SelectAmp(string ip)
    {
        SelectedAmpIp = ip;
        CurrentPage = "tuning";
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
