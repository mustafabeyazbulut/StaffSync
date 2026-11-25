using System.Collections;
using System.Windows.Input;

namespace StaffSync.Controls
{
    public partial class SegmentedControl : ContentView
    {
        public static readonly BindableProperty ItemsSourceProperty =
            BindableProperty.Create(
                nameof(ItemsSource),
                typeof(IEnumerable),
                typeof(SegmentedControl),
                null,
                propertyChanged: OnItemsSourceChanged);

        public static readonly BindableProperty SelectedIndexProperty =
            BindableProperty.Create(
                nameof(SelectedIndex),
                typeof(int),
                typeof(SegmentedControl),
                0,
                BindingMode.TwoWay,
                propertyChanged: OnSelectedIndexChanged);

        public static readonly BindableProperty SelectedItemProperty =
            BindableProperty.Create(
                nameof(SelectedItem),
                typeof(object),
                typeof(SegmentedControl),
                null,
                BindingMode.TwoWay,
                propertyChanged: OnSelectedItemChanged);

        public static readonly BindableProperty SelectionChangedCommandProperty =
            BindableProperty.Create(
                nameof(SelectionChangedCommand),
                typeof(ICommand),
                typeof(SegmentedControl),
                null);

        public IEnumerable? ItemsSource
        {
            get => (IEnumerable?)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public int SelectedIndex
        {
            get => (int)GetValue(SelectedIndexProperty);
            set => SetValue(SelectedIndexProperty, value);
        }

        public object? SelectedItem
        {
            get => GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
        }

        public ICommand? SelectionChangedCommand
        {
            get => (ICommand?)GetValue(SelectionChangedCommandProperty);
            set => SetValue(SelectionChangedCommandProperty, value);
        }

        public event EventHandler<int>? SelectedIndexChanged;

        private readonly List<object> _items = new();

        public SegmentedControl()
        {
            InitializeComponent();
        }

        private static void OnItemsSourceChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is SegmentedControl control)
            {
                control.RebuildSegments();
            }
        }

        private static void OnSelectedIndexChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is SegmentedControl control)
            {
                var newIndex = (int)newValue;
                if (control._items.Count > newIndex && newIndex >= 0)
                {
                    control.SelectedItem = control._items[newIndex];
                }
                control.UpdateVisualState();
                control.SelectedIndexChanged?.Invoke(control, newIndex);
                if (control.SelectionChangedCommand?.CanExecute(newIndex) == true)
                {
                    control.SelectionChangedCommand.Execute(newIndex);
                }
            }
        }

        private static void OnSelectedItemChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is SegmentedControl control && newValue != null)
            {
                var index = control._items.IndexOf(newValue);
                if (index >= 0 && index != control.SelectedIndex)
                {
                    control.SelectedIndex = index;
                }
            }
        }

        private void RebuildSegments()
        {
            SegmentsGrid.Children.Clear();
            SegmentsGrid.ColumnDefinitions.Clear();
            _items.Clear();

            if (ItemsSource == null)
                return;

            if (ItemsSource is string str)
            {
                foreach (var part in str.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                {
                    _items.Add(part);
                }
            }
            else
            {
                foreach (var item in ItemsSource)
                {
                    _items.Add(item);
                }
            }

            for (int i = 0; i < _items.Count; i++)
            {
                SegmentsGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

                var index = i;
                var itemText = _items[i]?.ToString() ?? string.Empty;

                var border = new Border
                {
                    StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(8) },
                    StrokeThickness = 0,
                    HeightRequest = 34,
                    Padding = new Thickness(8, 0)
                };

                var label = new Label
                {
                    Text = itemText,
                    FontSize = 14,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                    LineBreakMode = LineBreakMode.TailTruncation
                };

                border.Content = label;

                var tapGesture = new TapGestureRecognizer();
                tapGesture.Tapped += (s, e) =>
                {
                    SelectedIndex = index;
                };
                border.GestureRecognizers.Add(tapGesture);

                Grid.SetColumn(border, i);
                SegmentsGrid.Children.Add(border);
            }

            UpdateVisualState();
        }

        private void UpdateVisualState()
        {
            if (SegmentsGrid.Children.Count != _items.Count)
                return;

            for (int i = 0; i < SegmentsGrid.Children.Count; i++)
            {
                if (SegmentsGrid.Children[i] is Border border && border.Content is Label label)
                {
                    var isSelected = (i == SelectedIndex);

                    if (isSelected)
                    {
                        border.SetAppThemeColor(Border.BackgroundColorProperty,
                            (Color)Application.Current!.Resources["SegOnLight"],
                            (Color)Application.Current!.Resources["SegOnDark"]);
                        border.Shadow = new Shadow
                        {
                            Opacity = 0.12f,
                            Radius = 2,
                            Offset = new Point(0, 1)
                        };

                        label.FontAttributes = FontAttributes.Bold;
                        label.SetAppThemeColor(Label.TextColorProperty,
                            (Color)Application.Current!.Resources["TextPrimaryLight"],
                            (Color)Application.Current!.Resources["TextPrimaryDark"]);
                    }
                    else
                    {
                        border.BackgroundColor = Colors.Transparent;
                        border.ClearValue(Border.ShadowProperty);

                        label.FontAttributes = FontAttributes.None;
                        label.SetAppThemeColor(Label.TextColorProperty,
                            (Color)Application.Current!.Resources["TextMutedLight"],
                            (Color)Application.Current!.Resources["TextMutedDark"]);
                    }
                }
            }
        }
    }
}
