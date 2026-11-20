using Maestro.Core.Errors;
using Qt.Quick;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
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

    public ObservableCollection<AmpItem> Amps { get; } = new();

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

    private MaestroException? _addAmpError;
    public string AddAmpErrorCode => _addAmpError?.Code.ToString() ?? "";

    public string AddAmpErrorArgument(string name) => _addAmpError?.Arguments.GetValueOrDefault(name) ?? "";

    public bool AddAmp(string name, string ip)
    {
        name = name.Trim();
        ip = ip.Trim();
        try
        {
            if (!IPAddress.TryParse(ip, out var address)
                || address.AddressFamily != AddressFamily.InterNetwork
                || address.ToString() != ip)
                throw new MaestroException(ErrorCode.InvalidAmpAddress, new { address = ip });

            if (Amps.Any(amp => amp.Ip == ip))
                throw new MaestroException(ErrorCode.AmpAlreadyAdded, new { address = ip });

            // TODO: connect through IAmpManager and run the handshake; its MaestroExceptions show in the dialog too.
            Amps.Add(new AmpItem { Name = name, Ip = ip });
            SetAddAmpError(null);
            return true;
        }
        catch (MaestroException exception)
        {
            SetAddAmpError(exception);
            return false;
        }
    }

    public void ClearAddAmpError() => SetAddAmpError(null);

    private void SetAddAmpError(MaestroException? error)
    {
        _addAmpError = error;
        OnPropertyChanged(nameof(AddAmpErrorCode));
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
