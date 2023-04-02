using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace EmailWpfApp.UserControls
{
    /// <summary>
    /// Interaction logic for InputBoxUserControl.xaml
    /// </summary>
    public partial class InputBoxUserControl : UserControl
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
            "LabelContent", typeof(string), typeof(InputBoxUserControl), new PropertyMetadata(string.Empty));

        public Style LabelStyle
        {
            get => (Style)GetValue(LabelStyleProperty);
            set => SetValue(LabelStyleProperty, value);
        }

        public static readonly DependencyProperty LabelStyleProperty = DependencyProperty.Register(
            "LabelStyle", typeof(Style), typeof(InputBoxUserControl), new PropertyMetadata(null));
        #endregion

        #region TextBlock
        public string TextBlockText
        {
            get => (string)GetValue(TextBlockTextProperty);
            set => SetValue(TextBlockTextProperty, value);
        }

        public static readonly DependencyProperty TextBlockTextProperty = DependencyProperty.Register(
            "TextBlockText", typeof(string), typeof(InputBoxUserControl), new PropertyMetadata(string.Empty));

        public string TextBlockToolTip
        {
            get => (string)GetValue(TextBlockToolTipProperty);
            set => SetValue(TextBlockToolTipProperty, value);
        }

        public static readonly DependencyProperty TextBlockToolTipProperty = DependencyProperty.Register(
            "TextBlockToolTip", typeof(string), typeof(InputBoxUserControl), new PropertyMetadata(string.Empty));

        public Style TextBlockStyle
        {
            get => (Style)GetValue(TextBlockStyleProperty);
            set => SetValue(TextBlockStyleProperty, value);
        }

        public static readonly DependencyProperty TextBlockStyleProperty = DependencyProperty.Register(
            "TextBlockStyle", typeof(Style), typeof(InputBoxUserControl), new PropertyMetadata(null));
        #endregion

        #region Button
        public string ButtonContent
        {
            get => (string)GetValue(ButtonContentProperty);
            set => SetValue(ButtonContentProperty, value);
        }

        public static readonly DependencyProperty ButtonContentProperty = DependencyProperty.Register(
            "ButtonContent", typeof(string), typeof(InputBoxUserControl), new PropertyMetadata(string.Empty));

        public ICommand ButtonCommand
        {
            get => (ICommand)GetValue(ButtonCommandProperty);
            set => SetValue(ButtonCommandProperty, value);
        }

        public static readonly DependencyProperty ButtonCommandProperty = DependencyProperty.Register(
            "ButtonCommand", typeof(ICommand), typeof(InputBoxUserControl), new PropertyMetadata(null));

        public Style ButtonStyle
        {
            get => (Style)GetValue(ButtonStyleProperty);
            set => SetValue(ButtonStyleProperty, value);
        }

        public static readonly DependencyProperty ButtonStyleProperty = DependencyProperty.Register(
            "ButtonStyle", typeof(Style), typeof(InputBoxUserControl), new PropertyMetadata(null));
        #endregion
    }
}
