using System;
using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace EmailWpfApp.Controls
{
    /// <summary>
    /// Interaction logic for DropdownBoxUserControl.xaml
    /// </summary>
    public partial class DropdownBoxUserControl : UserControl
    {
        protected override void OnInitialized(EventArgs e)
        {
            InitializeComponent();
            base.OnInitialized(e);
        }

        #region Grid
        public int GridWidthFirstColumn
        {
            get => (int)GetValue(GridWidthFirstColumnProperty);
            set => SetValue(GridWidthFirstColumnProperty, value);
        }

        public static readonly DependencyProperty GridWidthFirstColumnProperty = DependencyProperty.Register(
            "GridWidthFirstColumn", typeof(int), typeof(DropdownBoxUserControl), new PropertyMetadata(150));

        public int GridWidthLastColumn
        {
            get => (int)GetValue(GridWidthLastColumnProperty);
            set => SetValue(GridWidthLastColumnProperty, value);
        }

        public static readonly DependencyProperty GridWidthLastColumnProperty = DependencyProperty.Register(
            "GridWidthLastColumn", typeof(int), typeof(DropdownBoxUserControl), new PropertyMetadata(150));

        public Style GridStyle
        {
            get => (Style)GetValue(GridStyleProperty);
            set => SetValue(GridStyleProperty, value);
        }

        public static readonly DependencyProperty GridStyleProperty = DependencyProperty.Register(
            "GridStyle", typeof(Style), typeof(DropdownBoxUserControl), new PropertyMetadata(null));
        #endregion

        #region Label
        public string LabelContent
        {
            get => (string)GetValue(LabelContentProperty);
            set => SetValue(LabelContentProperty, value);
        }

        public static readonly DependencyProperty LabelContentProperty = DependencyProperty.Register(
            "LabelContent", typeof(string), typeof(DropdownBoxUserControl), new PropertyMetadata(string.Empty));

        public Style LabelStyle
        {
            get => (Style)GetValue(LabelStyleProperty);
            set => SetValue(LabelStyleProperty, value);
        }

        public static readonly DependencyProperty LabelStyleProperty = DependencyProperty.Register(
            "LabelStyle", typeof(Style), typeof(DropdownBoxUserControl), new PropertyMetadata(null));
        #endregion

        #region ComboBox
        public IEnumerable ItemsSource
        {
            get => (IEnumerable)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
            "ItemsSource", typeof(IEnumerable), typeof(DropdownBoxUserControl), new PropertyMetadata(null));

        public object SelectedItem
        {
            get => (object)GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
        }

        public static readonly DependencyProperty SelectedItemProperty = DependencyProperty.Register(
            "SelectedItem", typeof(object), typeof(DropdownBoxUserControl), new PropertyMetadata(null));

        public string ComboBoxText
        {
            get => (string)GetValue(ComboBoxTextProperty);
            set => SetValue(ComboBoxTextProperty, value);
        }

        public static readonly DependencyProperty ComboBoxTextProperty = DependencyProperty.Register(
            "ComboBoxText", typeof(string), typeof(DropdownBoxUserControl), new PropertyMetadata(string.Empty));

        public string ComboBoxToolTip
        {
            get => (string)GetValue(ComboBoxToolTipProperty);
            set => SetValue(ComboBoxToolTipProperty, value);
        }

        public static readonly DependencyProperty ComboBoxToolTipProperty = DependencyProperty.Register(
            "ComboBoxToolTip", typeof(string), typeof(DropdownBoxUserControl), new PropertyMetadata(string.Empty));

        public Style ComboBoxStyle
        {
            get => (Style)GetValue(ComboBoxStyleProperty);
            set => SetValue(ComboBoxStyleProperty, value);
        }

        public static readonly DependencyProperty ComboBoxStyleProperty = DependencyProperty.Register(
            "ComboBoxStyle", typeof(Style), typeof(DropdownBoxUserControl), new PropertyMetadata(null));
        #endregion

        #region Button
        public string ButtonContent
        {
            get => (string)GetValue(ButtonContentProperty);
            set => SetValue(ButtonContentProperty, value);
        }

        public static readonly DependencyProperty ButtonContentProperty = DependencyProperty.Register(
            "ButtonContent", typeof(string), typeof(DropdownBoxUserControl), new PropertyMetadata(string.Empty));

        public ICommand ButtonCommand
        {
            get => (ICommand)GetValue(ButtonCommandProperty);
            set => SetValue(ButtonCommandProperty, value);
        }

        public static readonly DependencyProperty ButtonCommandProperty = DependencyProperty.Register(
            "ButtonCommand", typeof(ICommand), typeof(DropdownBoxUserControl), new PropertyMetadata(null));

        public Style ButtonStyle
        {
            get => (Style)GetValue(ButtonStyleProperty);
            set => SetValue(ButtonStyleProperty, value);
        }

        public static readonly DependencyProperty ButtonStyleProperty = DependencyProperty.Register(
            "ButtonStyle", typeof(Style), typeof(DropdownBoxUserControl), new PropertyMetadata(null));
        #endregion
    }
}
