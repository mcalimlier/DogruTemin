using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace DogruTemin.Wpf;

public partial class MainWindow
{
    private Button? _standaloneOutputNavButton;
    private Border? _datesOutputSeparator;
    private TextBlock? _documentsDatesPageTitle;
    private TextBlock? _documentsDatesPageSubtitle;
    private bool _outputNavigationEnhancementInitialized;
    private bool _outputLayoutEnhanced;

    static MainWindow()
    {
        EventManager.RegisterClassHandler(
            typeof(MainWindow),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler(OnMainWindowLoadedForOutputNavigation));
    }

    private static void OnMainWindowLoadedForOutputNavigation(object sender, RoutedEventArgs e)
    {
        if (sender is MainWindow window)
            window.InitializeStandaloneOutputNavigation();
    }

    private void InitializeStandaloneOutputNavigation()
    {
        if (_outputNavigationEnhancementInitialized)
            return;

        _outputNavigationEnhancementInitialized = true;

        _standaloneOutputNavButton = BuildStandaloneOutputNavButton();
        var documentsIndex = DataEntrySubmenu.Children.IndexOf(NavDocumentsDates);
        if (documentsIndex >= 0)
            DataEntrySubmenu.Children.Insert(documentsIndex + 1, _standaloneOutputNavButton);
        else
            DataEntrySubmenu.Children.Add(_standaloneOutputNavButton);

        if (DocumentsDatesView.Content is StackPanel pageStack)
        {
            _documentsDatesPageTitle = pageStack.Children.OfType<TextBlock>().FirstOrDefault();
            _documentsDatesPageSubtitle = pageStack.Children.OfType<TextBlock>().Skip(1).FirstOrDefault();

            var outputIndex = pageStack.Children.IndexOf(OutputView);
            if (outputIndex > 0 && pageStack.Children[outputIndex - 1] is Border separator)
            {
                _datesOutputSeparator = separator;
                _datesOutputSeparator.Visibility = Visibility.Collapsed;
            }
        }

        NavDocumentsDates.Click += (_, _) => ApplyDocumentsDatesMode();
        HomeNextStepButton.Click += (_, _) => SynchronizeOutputSubpageWithCurrentView();

        foreach (var button in new[]
        {
            NavHome, NavInstitution, NavGeneral, NavFirmsOffers, NavApprovalDocument,
            NavControl, NavStatistics, NavArchive, NavSettings, NavAboutSafety
        })
        {
            button.Click += (_, _) => ClearStandaloneOutputActiveStateWhenNeeded();
        }

        SynchronizeOutputSubpageWithCurrentView();
    }

    private Button BuildStandaloneOutputNavButton()
    {
        var button = new Button
        {
            Style = (Style)FindResource("SubNavButton"),
            ToolTip = "Çıktı durumu, önizleme, PDF ve yazdırma işlemleri"
        };

        var content = new StackPanel { Orientation = Orientation.Horizontal };
        content.Children.Add(new TextBlock
        {
            FontFamily = new FontFamily("Segoe MDL2 Assets"),
            Text = "\uE749",
            FontSize = 17,
            Width = 30
        });
        content.Children.Add(new TextBlock
        {
            Text = "ÇIKTI & YAZDIRMA",
            VerticalAlignment = VerticalAlignment.Center
        });
        button.Content = content;

        button.Click += (_, _) =>
        {
            ShowView("Output");
            ApplyStandaloneOutputMode();
        };

        return button;
    }

    private void SynchronizeOutputSubpageWithCurrentView()
    {
        if (_currentView == "Output")
            ApplyStandaloneOutputMode();
        else if (_currentView == "Dates")
            ApplyDocumentsDatesMode();
        else
            ClearStandaloneOutputActiveStateWhenNeeded();
    }

    private void ApplyDocumentsDatesMode()
    {
        DatesView.Visibility = Visibility.Visible;
        OutputView.Visibility = Visibility.Collapsed;
        DocumentsDatesView.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
        if (_datesOutputSeparator is not null)
            _datesOutputSeparator.Visibility = Visibility.Collapsed;

        if (_documentsDatesPageTitle is not null)
            _documentsDatesPageTitle.Text = "BELGELER / TARİHLER";
        if (_documentsDatesPageSubtitle is not null)
            _documentsDatesPageSubtitle.Text = "Belge tarihlerini tek sayfada yönetin.";

        NavDocumentsDates.Tag = "Active";
        if (_standaloneOutputNavButton is not null)
            _standaloneOutputNavButton.Tag = null;
    }

    private void ApplyStandaloneOutputMode()
    {
        DatesView.Visibility = Visibility.Collapsed;
        OutputView.Visibility = Visibility.Visible;
        DocumentsDatesView.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
        if (_datesOutputSeparator is not null)
            _datesOutputSeparator.Visibility = Visibility.Collapsed;

        if (_documentsDatesPageTitle is not null)
            _documentsDatesPageTitle.Text = "ÇIKTI & YAZDIRMA";
        if (_documentsDatesPageSubtitle is not null)
            _documentsDatesPageSubtitle.Text = "Çıktı durumu, belge seçimi, önizleme, PDF ve yazdırma seçeneklerini yönetin.";

        NavDocumentsDates.Tag = null;
        if (_standaloneOutputNavButton is not null)
            _standaloneOutputNavButton.Tag = "Active";

        Dispatcher.BeginInvoke(
            new Action(EnhanceOutputLayoutAndReadability),
            DispatcherPriority.Loaded);
    }

    private void EnhanceOutputLayoutAndReadability()
    {
        if (!_outputLayoutEnhanced)
        {
            var layoutGrid = OutputView.Children.OfType<Grid>().FirstOrDefault();
            if (layoutGrid is not null)
            {
                var cards = layoutGrid.Children.OfType<Border>().ToList();
                if (cards.Count >= 2)
                {
                    var documentCard = cards[0];
                    var statusCard = cards[1];

                    Grid.SetRow(documentCard, 0);
                    Grid.SetColumn(documentCard, 0);
                    Grid.SetColumnSpan(documentCard, 1);
                    documentCard.VerticalAlignment = VerticalAlignment.Stretch;
                    documentCard.Margin = new Thickness(0);
                    documentCard.Padding = new Thickness(16, 14, 16, 14);

                    Grid.SetRow(statusCard, 0);
                    Grid.SetColumn(statusCard, 2);
                    Grid.SetColumnSpan(statusCard, 1);
                    statusCard.VerticalAlignment = VerticalAlignment.Stretch;
                    statusCard.Margin = new Thickness(0);
                    statusCard.Padding = new Thickness(16, 14, 16, 14);
                }
            }

            _outputLayoutEnhanced = true;
        }

        ImproveOutputTypography(OutputView);
    }

    private static void ImproveOutputTypography(DependencyObject parent)
    {
        var count = VisualTreeHelper.GetChildrenCount(parent);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);

            switch (child)
            {
                case TextBlock text when text.FontSize <= 11:
                    text.FontSize = 13.5;
                    break;
                case TextBlock text when text.FontSize <= 13:
                    text.FontSize = 15.5;
                    break;
                case Button button when button.FontSize < 13.5:
                    button.FontSize = 15.5;
                    break;
                case CheckBox checkBox when checkBox.FontSize < 13:
                    checkBox.FontSize = 14;
                    break;
            }

            ImproveOutputTypography(child);
        }
    }

    private void ClearStandaloneOutputActiveStateWhenNeeded()
    {
        if (_currentView == "Output")
        {
            ApplyStandaloneOutputMode();
            return;
        }

        if (_standaloneOutputNavButton is not null)
            _standaloneOutputNavButton.Tag = null;
    }
}
