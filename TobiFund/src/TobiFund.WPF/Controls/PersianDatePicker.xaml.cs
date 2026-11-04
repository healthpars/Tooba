using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using TobiFund.Application.Common;

namespace TobiFund.WPF.Controls;

/// <summary>
/// Persian (Shamsi) date picker. The displayed date is read-only; a popup lets the user
/// pick Year / Month / Day in the Persian calendar. Bind via <see cref="SelectedDate"/>.
/// The Gregorian value is stored; conversion happens here (presentation layer).
/// </summary>
public partial class PersianDatePicker : UserControl
{
    public static readonly DependencyProperty SelectedDateProperty =
        DependencyProperty.Register(
            nameof(SelectedDate),
            typeof(DateTime?),
            typeof(PersianDatePicker),
            new FrameworkPropertyMetadata(null,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnSelectedDateChanged));

    public DateTime? SelectedDate
    {
        get => (DateTime?)GetValue(SelectedDateProperty);
        set => SetValue(SelectedDateProperty, value);
    }

    private bool _suppressPartChanged;

    public PersianDatePicker()
    {
        InitializeComponent();
        Loaded += PersianDatePicker_Loaded;
    }

    private void PersianDatePicker_Loaded(object sender, RoutedEventArgs e)
    {
        PopulateCombos();
        UpdateDisplay();
    }

    private static void OnSelectedDateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is PersianDatePicker picker)
            picker.UpdateDisplay();
    }

    private void PopulateCombos()
    {
        if (YearCombo.Items.Count > 0) return;
        var pc = new PersianCalendar();
        var currentYear = pc.GetYear(DateTime.Today);
        for (var y = 1300; y <= currentYear + 5; y++)
            YearCombo.Items.Add(MakeItem(y, PersianDateHelper.ToPersianDigits(y.ToString())));
        for (var m = 1; m <= 12; m++)
            MonthCombo.Items.Add(MakeItem(m, PersianDateHelper.PersianMonthName(m)));
        PopulateDays(31);
    }

    private static ComboBoxItem MakeItem(int value, string text) =>
        new() { Content = text, Tag = value };

    private void PopulateDays(int maxDay)
    {
        _suppressPartChanged = true;
        var current = SelectedTag(DayCombo);
        DayCombo.Items.Clear();
        for (var d = 1; d <= maxDay; d++)
            DayCombo.Items.Add(MakeItem(d, PersianDateHelper.ToPersianDigits(d.ToString())));
        if (current.HasValue && current.Value <= maxDay)
            SelectByTag(DayCombo, current.Value);
        else if (DayCombo.Items.Count > 0)
            DayCombo.SelectedIndex = 0;
        _suppressPartChanged = false;
    }

    private void UpdateDisplay()
    {
        if (SelectedDate is { } date)
        {
            DisplayBox.Text = PersianDateHelper.ToPersianDate(date);
            DisplayBox.Foreground = Brushes.Black;
        }
        else
        {
            DisplayBox.Text = "انتخاب تاریخ";
            DisplayBox.Foreground = Brushes.Gray;
        }
    }

    private void OpenPopup()
    {
        PopulateCombos();
        SyncCombosFromDate();
        PickerPopup.IsOpen = true;
    }

    private void SyncCombosFromDate()
    {
        var date = SelectedDate ?? DateTime.Today;
        var (y, m, d) = PersianDateHelper.GetPersianDateParts(date);
        _suppressPartChanged = true;
        SelectByTag(YearCombo, y);
        SelectByTag(MonthCombo, m);
        PopulateDays(new PersianCalendar().GetDaysInMonth(y, m));
        SelectByTag(DayCombo, d);
        _suppressPartChanged = false;
        UpdatePreview();
    }

    private void Part_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressPartChanged) return;
        var y = SelectedTag(YearCombo);
        var m = SelectedTag(MonthCombo);
        if (y.HasValue && m.HasValue)
        {
            var daysInMonth = new PersianCalendar().GetDaysInMonth(y.Value, m.Value);
            var currentDay = SelectedTag(DayCombo);
            if (currentDay is null || currentDay.Value > daysInMonth)
                PopulateDays(daysInMonth);
        }
        UpdatePreview();
    }

    private void UpdatePreview()
    {
        var y = SelectedTag(YearCombo);
        var m = SelectedTag(MonthCombo);
        var d = SelectedTag(DayCombo);
        if (y.HasValue && m.HasValue && d.HasValue)
        {
            var date = PersianDateHelper.PersianToGregorian(y.Value, m.Value, d.Value);
            PreviewText.Text = PersianDateHelper.ToPersianDate(date);
        }
        else
        {
            PreviewText.Text = string.Empty;
        }
    }

    private void Today_Click(object sender, RoutedEventArgs e)
    {
        var today = DateTime.Today;
        var (y, m, d) = PersianDateHelper.GetPersianDateParts(today);
        _suppressPartChanged = true;
        SelectByTag(YearCombo, y);
        SelectByTag(MonthCombo, m);
        PopulateDays(new PersianCalendar().GetDaysInMonth(y, m));
        SelectByTag(DayCombo, d);
        _suppressPartChanged = false;
        UpdatePreview();
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        var y = SelectedTag(YearCombo);
        var m = SelectedTag(MonthCombo);
        var d = SelectedTag(DayCombo);
        if (y.HasValue && m.HasValue && d.HasValue)
            SelectedDate = PersianDateHelper.PersianToGregorian(y.Value, m.Value, d.Value);
        PickerPopup.IsOpen = false;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => PickerPopup.IsOpen = false;

    private void DropButton_Click(object sender, RoutedEventArgs e) => OpenPopup();

    private void DisplayBox_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) => OpenPopup();

    private static int? SelectedTag(ComboBox combo)
    {
        if (combo.SelectedItem is ComboBoxItem cbi && cbi.Tag is int tag)
            return tag;
        return null;
    }

    private static void SelectByTag(ComboBox combo, int value)
    {
        foreach (var item in combo.Items)
        {
            if (item is ComboBoxItem cbi && cbi.Tag is int tag && tag == value)
            {
                combo.SelectedItem = cbi;
                return;
            }
        }
    }
}
