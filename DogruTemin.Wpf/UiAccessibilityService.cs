using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace DogruTemin.Wpf;

internal static class UiAccessibilityService
{
    private static readonly SolidColorBrush Gold = BrushFrom("#FFB41F");
    private static readonly ConditionalWeakTable<Control, FocusSnapshot> FocusSnapshots = new();

    private static readonly string[] PreferredTabOrder =
    {
        "InstitutionTopAuthorityBox", "InstitutionFullNameBox", "InstitutionProvinceBox",
        "InstitutionAddressBox", "InstitutionPhoneBox", "InstitutionEmailBox",
        "AuthorizedNameBox", "AuthorizedDutyBox", "AuthorizedTitleBox",
        "AssignmentDatePicker", "AssignmentNoBox",
        "JobTitleBox", "ProductNameBox", "ProductSpecBox", "ProductUnitBox", "ProductQuantityBox",
        "ApprovalDatePicker", "CostCalculationDatePicker", "ApprovalDocumentDatePicker", "MarketReportDatePicker",
        "InspectionReportDatePicker", "DeliveryDatePicker", "ApprovalDocumentNumberBox", "ApprovalAvailableBudgetBox",
        "ApprovalInvestmentProjectBox", "ApprovalBudgetCodeBox", "ApprovalAdvanceConditionsBox", "ApprovalTenderProcedureBox",
        "ApprovalAnnouncementMethodBox", "ApprovalPrequalificationSalePriceBox", "ApprovalPriceDifferenceBasisBox",
        "ApprovalNotesBox", "ApprovalSubmittedByNameBox", "ApprovalSubmittedByTitleBox", "OfferDatePicker", "ArchiveSearchBox"
    };

    public static void Apply(MainWindow window)
    {
        ApplyAutomationMetadata(window);
        ApplyPreferredTabOrder(window);
        AttachFocusPresentation(window);
        AttachKeyboardShortcuts(window);
        if (SystemParameters.HighContrast) ApplyHighContrast(window);
        KeyboardNavigation.SetTabNavigation(window, KeyboardNavigationMode.Continue);
    }

    private static void ApplyAutomationMetadata(MainWindow window)
    {
        foreach (var control in UiTree.Descendants<Control>(window))
        {
            if (string.IsNullOrWhiteSpace(AutomationProperties.GetName(control)))
            {
                if (control is Button { Content: string content })
                    AutomationProperties.SetName(control, content.Replace("/", " "));
                else if (control.ToolTip is string tip && !string.IsNullOrWhiteSpace(tip))
                    AutomationProperties.SetName(control, tip);
                else if (!string.IsNullOrWhiteSpace(control.Name))
                    AutomationProperties.SetName(control, FriendlyName(control.Name));
            }

            if (string.IsNullOrWhiteSpace(AutomationProperties.GetHelpText(control))
                && control.ToolTip is string help && !string.IsNullOrWhiteSpace(help))
                AutomationProperties.SetHelpText(control, help);
        }
    }

    private static void ApplyPreferredTabOrder(MainWindow window)
    {
        var assigned = new HashSet<Control>();
        var index = 0;
        foreach (var name in PreferredTabOrder)
        {
            if (window.FindName(name) is not Control control || !control.Focusable) continue;
            control.TabIndex = index++;
            assigned.Add(control);
        }
        foreach (var control in UiTree.Descendants<Control>(window))
        {
            if (!control.Focusable || assigned.Contains(control)) continue;
            control.TabIndex = index++;
        }
    }

    private static void AttachFocusPresentation(MainWindow window)
    {
        foreach (var control in UiTree.Descendants<Control>(window))
        {
            if (control is not (Button or TextBox or ComboBox or LargeDatePicker)) continue;
            if (control is LargeDatePicker) continue; // kendi altın odak çerçevesi var.

            control.GotKeyboardFocus += (_, _) =>
            {
                var snapshot = FocusSnapshots.GetValue(control, c => new FocusSnapshot(c.BorderBrush, c.BorderThickness));
                control.BorderBrush = Gold;
                control.BorderThickness = new Thickness(
                    Math.Max(1.5, snapshot.BorderThickness.Left),
                    Math.Max(1.5, snapshot.BorderThickness.Top),
                    Math.Max(1.5, snapshot.BorderThickness.Right),
                    Math.Max(1.5, snapshot.BorderThickness.Bottom));
            };
            control.LostKeyboardFocus += (_, _) =>
            {
                if (!FocusSnapshots.TryGetValue(control, out var snapshot)) return;
                control.BorderBrush = snapshot.BorderBrush;
                control.BorderThickness = snapshot.BorderThickness;
            };
        }
    }

    private static void AttachKeyboardShortcuts(MainWindow window)
    {
        window.PreviewKeyDown += (_, e) =>
        {
            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.F)
            {
                if (UiTree.IsVisible(window, "ArchiveView") && window.FindName("ArchiveSearchBox") is TextBox search)
                {
                    search.Focus(); search.SelectAll(); e.Handled = true;
                }
                return;
            }

            if ((Keyboard.Modifiers & ModifierKeys.Alt) != ModifierKeys.Alt) return;
            var key = e.Key == Key.System ? e.SystemKey : e.Key;
            var navName = key switch
            {
                Key.A => "NavHome",
                Key.V => "NavInstitution",
                Key.K => "NavControl",
                Key.I => "NavStatistics",
                Key.R => "NavArchive",
                Key.H => "NavAboutSafety",
                _ => null
            };
            if (navName is null || window.FindName(navName) is not Button button || !button.IsEnabled) return;
            button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, button));
            e.Handled = true;
        };
    }

    private static void ApplyHighContrast(MainWindow window)
    {
        foreach (var text in UiTree.Descendants<TextBlock>(window)) text.Foreground = SystemColors.WindowTextBrush;
        foreach (var control in UiTree.Descendants<Control>(window))
        {
            control.Foreground = SystemColors.WindowTextBrush;
            control.Background = SystemColors.WindowBrush;
            control.BorderBrush = SystemColors.WindowTextBrush;
        }
        foreach (var border in UiTree.Descendants<Border>(window))
        {
            border.Background = SystemColors.WindowBrush;
            border.BorderBrush = SystemColors.WindowTextBrush;
        }
    }

    private static string FriendlyName(string name) => name
        .Replace("Box", string.Empty, StringComparison.Ordinal)
        .Replace("Picker", " tarih alanı", StringComparison.Ordinal)
        .Replace("Button", " düğmesi", StringComparison.Ordinal);

    private static SolidColorBrush BrushFrom(string value) =>
        new((Color)ColorConverter.ConvertFromString(value));

    private sealed record FocusSnapshot(Brush? BorderBrush, Thickness BorderThickness);
}
