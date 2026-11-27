using Maestro.Core.Errors;
using Maestro.Core.Interfaces;
using Maestro.Core.Models;
using Qt.Quick;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Xml.Linq;

namespace Maestro.Desktop.ViewModels;

/// <summary>
/// Sidebar state and a simple router: CurrentPage is "playlist", "settings" or "tuning"
/// (the tuning page is for the amp in SelectedAmpIp).
/// </summary>
/// 

// TODO: split into AmpViewModel and NavigationViewModel? (could be clenaer later when we add more logic)

[QmlElement]
public class SidebarViewModel : BaseViewModel, INotifyPropertyChanged, IDisposable
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private readonly IAmpManager _ampManager;

    public ObservableCollection<AmpViewModel> Amps { get; } = new();

    public SidebarViewModel()
    {
        _ampManager = GetRequiredService<IAmpManager>();

        _ampManager.Added += OnAmpAdded;
        _ampManager.Removed += OnAmpRemoved;
    }

    public void Dispose()
    {
        _ampManager.Added -= OnAmpAdded;
        _ampManager.Removed -= OnAmpRemoved;
    }

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

    // TODO: better way?
    private bool _isAddingAmp;
    public bool IsAddingAmp
    {
        get => _isAddingAmp;
        private set
        {
            if (_isAddingAmp == value)
                return;
            _isAddingAmp = value;
            OnPropertyChanged();
        }
    }

    public void AddAmp(string name, string ip) => _ = AddAmpAsync(name, ip);

    private async Task AddAmpAsync(string name, string ip)
    {
        SetAddAmpError(null);
        IsAddingAmp = true;

        try
        {
            await _ampManager.AddAsync(name.Trim(), ip.Trim());
        }
        catch (MaestroException exception)
        {
            SetAddAmpError(exception);
        }
        finally
        {
            IsAddingAmp = false;
        }
    }

    public void ClearAddAmpError() => SetAddAmpError(null);

    private void SetAddAmpError(MaestroException? error)
    {
        _addAmpError = error;
        OnPropertyChanged(nameof(AddAmpErrorCode));
    }

    private void OnAmpAdded(Amp amp) => Amps.Add(new AmpViewModel(amp));

    private void OnAmpRemoved(Amp amp)
    {
        foreach (var item in Amps.Where(x => x.Id == amp.Id).ToList())
        {
            item.Dispose();
            Amps.Remove(item);
        }
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
