using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DogruTemin.Wpf.Models;

public class OfferEditorRow : INotifyPropertyChanged
{
    private string _approximatePrice = string.Empty;
    private string _finalPrice = string.Empty;

    public string ProductId { get; set; } = string.Empty;
    public int No { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string QuantityDisplay { get; set; } = string.Empty;

    public string ApproximatePrice
    {
        get => _approximatePrice;
        set
        {
            if (_approximatePrice == value) return;
            _approximatePrice = value ?? string.Empty;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsApproximatePriceMissing));
        }
    }

    public string FinalPrice
    {
        get => _finalPrice;
        set
        {
            if (_finalPrice == value) return;
            _finalPrice = value ?? string.Empty;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsFinalPriceMissing));
        }
    }

    public bool IsApproximatePriceMissing => string.IsNullOrWhiteSpace(ApproximatePrice);
    public bool IsFinalPriceMissing => string.IsNullOrWhiteSpace(FinalPrice);

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
