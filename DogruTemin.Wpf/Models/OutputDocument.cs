using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DogruTemin.Wpf.Models;

public class OutputDocument : INotifyPropertyChanged
{
    private bool _isSelected = true;
    private string _status = "Hazır";
    private string _note = string.Empty;

    public bool IsSelected { get => _isSelected; set { if (_isSelected != value) { _isSelected = value; OnPropertyChanged(); } } }
    public string Name { get; set; } = string.Empty;
    public string Status { get => _status; set { if (_status != value) { _status = value; OnPropertyChanged(); } } }
    public string Note { get => _note; set { if (_note != value) { _note = value; OnPropertyChanged(); } } }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
