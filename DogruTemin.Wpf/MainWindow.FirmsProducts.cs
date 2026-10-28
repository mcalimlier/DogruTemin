using DogruTemin.Wpf.Models;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

using DogruTemin.Core.Services;

namespace DogruTemin.Wpf;

public partial class MainWindow
{
    // ---------------- FİRMALAR ----------------
    private void AddOrUpdateFirm_Click(object sender, RoutedEventArgs e)
    {
        var name = FirmNameBox.Text.Trim();
        if (name.Length == 0)
        {
            MessageBox.Show("Firma adını giriniz.", "DoğruTemin", MessageBoxButton.OK, MessageBoxImage.Warning);
            FirmNameBox.Focus();
            return;
        }

        if (!IsValidPhoneEntry(PhoneBox.Text))
        {
            MessageBox.Show("Telefon numarası boş bırakılabilir; girilirse (0) dışında 10 rakam olmalıdır.", "DoğruTemin", MessageBoxButton.OK, MessageBoxImage.Warning);
            PhoneBox.Focus();
            return;
        }

        if (_editingFirm is null && Firms.Count >= 5)
        {
            MessageBox.Show("En fazla 5 firma eklenebilir.", "DoğruTemin", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var taxNo = TaxNoBox.Text.Trim();
        var duplicate = Firms.FirstOrDefault(f => f != _editingFirm &&
            (string.Equals(f.Name.Trim(), name, StringComparison.OrdinalIgnoreCase) ||
             (taxNo.Length > 0 && string.Equals(f.TaxNo.Trim(), taxNo, StringComparison.OrdinalIgnoreCase))));
        if (duplicate is not null)
        {
            MessageBox.Show("Aynı firma adı veya vergi numarasıyla kayıtlı başka bir firma bulunuyor.", "DoğruTemin", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_editingFirm is null)
        {
            Firms.Add(new Firm
            {
                No = Firms.Count + 1,
                Name = name,
                TaxNo = taxNo,
                ContactName = ContactNameBox.Text.Trim(),
                Phone = PhoneForStorage(PhoneBox.Text),
                Email = EmailBox.Text.Trim(),
                Address = AddressBox.Text.Trim()
            });
        }
        else
        {
            _editingFirm.Name = name;
            _editingFirm.TaxNo = taxNo;
            _editingFirm.ContactName = ContactNameBox.Text.Trim();
            _editingFirm.Phone = PhoneForStorage(PhoneBox.Text);
            _editingFirm.Email = EmailBox.Text.Trim();
            _editingFirm.Address = AddressBox.Text.Trim();
            FirmGrid.Items.Refresh();
        }

        _preferredWinnerFirmId = string.Empty;
        ReNumberFirms();
        if ((_currentView is "Firms" or "Offers") && _offerEditingFirm is null)
            PrepareOffersView();
        MarkDirty();
        ClearFirmForm();
    }

    private void DeleteSelected_Click(object sender, RoutedEventArgs e)
    {
        if (FirmGrid.SelectedItem is Firm firm) DeleteFirm(firm);
        else MessageBox.Show("Silmek için tablodan bir firma seçiniz.", "DoğruTemin", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void FirmGrid_SelectionChanged(object sender, SelectionChangedEventArgs e) { }

    private void RowEdit_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.Tag is Firm firm) BeginEditFirm(firm);
    }

    private void RowDelete_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.Tag is Firm firm) DeleteFirm(firm);
    }

    private void BeginEditFirm(Firm firm)
    {
        _suppressDirtyTracking = true;
        _editingFirm = firm;
        FirmNameBox.Text = firm.Name;
        TaxNoBox.Text = firm.TaxNo;
        ContactNameBox.Text = firm.ContactName;
        PhoneBox.Text = PhoneForEntry(firm.Phone);
        EmailBox.Text = firm.Email;
        AddressBox.Text = firm.Address;
        _firmEditorDirty = false;
        _suppressDirtyTracking = false;
        FirmActionButton.Content = "KAYDET";
        FirmActionButton.ToolTip = "Düzenlenen firma bilgilerini kaydet";
        FirmNameBox.Focus();
    }

    private void DeleteFirm(Firm firm)
    {
        var answer = MessageBox.Show($"'{firm.Name}' firmasını silmek istiyor musunuz?", "DoğruTemin", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (answer != MessageBoxResult.Yes) return;
        Firms.Remove(firm);
        _preferredWinnerFirmId = string.Empty;
        ReNumberFirms();
        MarkDirty();
        if (_editingFirm == firm) ClearFirmForm();
    }

    internal static string PhoneForEntry(string? storedPhone)
    {
        var digits = new string((storedPhone ?? string.Empty).Where(char.IsDigit).ToArray());
        return digits.Length == 11 && digits[0] == '0' ? digits[1..] : digits;
    }

    internal static string PhoneForStorage(string? enteredPhone)
    {
        var digits = new string((enteredPhone ?? string.Empty).Where(char.IsDigit).ToArray());
        return digits.Length == 0 ? string.Empty : "0" + digits;
    }

    internal static bool IsValidPhoneEntry(string? enteredPhone)
    {
        var input = (enteredPhone ?? string.Empty).Trim();
        if (input.Length == 0) return true;
        if (input.Any(character => !char.IsDigit(character) && character is not (' ' or '-' or '(' or ')' or '.'))) return false;
        return input.Count(char.IsDigit) == 10;
    }

    private void ReNumberFirms()
    {
        for (var i = 0; i < Firms.Count; i++) Firms[i].No = i + 1;
        FirmGrid.Items.Refresh();
        OfferFirmSelector.Items.Refresh();
        if (_offerEditingFirm is not null && Firms.Contains(_offerEditingFirm))
        {
            _suppressOfferSelection = true;
            OfferFirmSelector.SelectedItem = _offerEditingFirm;
            _suppressOfferSelection = false;
        }
    }

    private void ClearFirmForm()
    {
        var previousSuppress = _suppressDirtyTracking;
        _suppressDirtyTracking = true;
        _editingFirm = null;
        _firmEditorDirty = false;
        FirmNameBox.Clear();
        TaxNoBox.Clear();
        ContactNameBox.Clear();
        PhoneBox.Clear();
        EmailBox.Clear();
        AddressBox.Clear();
        FirmGrid.SelectedItem = null;
        FirmActionButton.Content = "EKLE";
        FirmActionButton.ToolTip = "Yeni firma ekle";
        _suppressDirtyTracking = previousSuppress;
    }

    // ---------------- ÜRÜNLER ----------------
    private void AddOrUpdateProduct_Click(object sender, RoutedEventArgs e)
    {
        var name = ProductNameBox.Text.Trim();
        if (name.Length == 0)
        {
            MessageBox.Show("Mal / hizmet adını giriniz.", "DoğruTemin", MessageBoxButton.OK, MessageBoxImage.Warning);
            ProductNameBox.Focus();
            return;
        }

        if (_editingProduct is null && Products.Count >= ProcurementLimits.MaxProducts)
        {
            MessageBox.Show("En fazla 10 ürün / mal / hizmet eklenebilir.", "DoğruTemin", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var unit = (ProductUnitBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Adet";
        var quantity = ProductQuantityBox.Text.Trim();
        if (!double.TryParse(quantity.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var quantityValue) || quantityValue <= 0)
        {
            MessageBox.Show("Miktar sıfırdan büyük bir sayı olmalıdır.", "DoğruTemin", MessageBoxButton.OK, MessageBoxImage.Warning);
            ProductQuantityBox.Focus();
            return;
        }

        if (_editingProduct is null)
            Products.Add(new ProductItem { No = Products.Count + 1, Name = name, Specification = ProductSpecBox.Text.Trim(), Unit = unit, Quantity = quantity });
        else
        {
            _editingProduct.Name = name;
            _editingProduct.Specification = ProductSpecBox.Text.Trim();
            _editingProduct.Unit = unit;
            _editingProduct.Quantity = quantity;
            ProductGrid.Items.Refresh();
        }

        _preferredWinnerFirmId = string.Empty;
        ReNumberProducts();
        ProductCountText.Text = $"{Products.Count} / 10 ürün";
        MarkDirty();
        ClearProductForm();
    }

    private void DeleteSelectedProduct_Click(object sender, RoutedEventArgs e)
    {
        if (ProductGrid.SelectedItem is ProductItem item) DeleteProduct(item);
        else MessageBox.Show("Silmek için tablodan bir ürün seçiniz.", "DoğruTemin", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void ProductEdit_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.Tag is not ProductItem item) return;
        _suppressDirtyTracking = true;
        _editingProduct = item;
        ProductNameBox.Text = item.Name;
        ProductSpecBox.Text = item.Specification;
        var selectedUnit = ProductUnitBox.Items.OfType<ComboBoxItem>()
            .FirstOrDefault(option => option.Content?.ToString() == item.Unit);
        if (selectedUnit is null)
        {
            selectedUnit = new ComboBoxItem { Content = item.Unit };
            ProductUnitBox.Items.Add(selectedUnit);
        }
        ProductUnitBox.SelectedItem = selectedUnit;
        ProductQuantityBox.Text = item.Quantity;
        _productEditorDirty = false;
        _suppressDirtyTracking = false;
        ProductActionButton.Content = "KAYDET";
        ProductNameBox.Focus();
    }

    private void ProductDelete_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.Tag is ProductItem item) DeleteProduct(item);
    }

    private void DeleteProduct(ProductItem item)
    {
        var answer = MessageBox.Show($"'{item.Name}' kalemi silinsin mi?", "DoğruTemin", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (answer != MessageBoxResult.Yes) return;
        Products.Remove(item);
        _preferredWinnerFirmId = string.Empty;
        foreach (var firm in Firms) firm.Offers.RemoveAll(o => o.ProductId == item.Id);
        _preferredWinnerFirmId = string.Empty;
        ReNumberProducts();
        ProductCountText.Text = $"{Products.Count} / 10 ürün";
        if (_editingProduct == item) ClearProductForm();
        MarkDirty();
    }

    private void ReNumberProducts()
    {
        for (var i = 0; i < Products.Count; i++) Products[i].No = i + 1;
        ProductGrid.Items.Refresh();
    }

    private void ClearProductForm()
    {
        var previousSuppress = _suppressDirtyTracking;
        _suppressDirtyTracking = true;
        _editingProduct = null;
        _productEditorDirty = false;
        ProductNameBox.Clear();
        ProductSpecBox.Clear();
        ProductUnitBox.SelectedIndex = 0;
        ProductQuantityBox.Text = "1";
        ProductGrid.SelectedItem = null;
        ProductActionButton.Content = "EKLE";
        _suppressDirtyTracking = previousSuppress;
    }
}
