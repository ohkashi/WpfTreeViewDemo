using MaterialDesignThemes.Wpf;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using NodeItem = WpfTreeViewDemo.TopologyView.NodeItem;

namespace WpfTreeViewDemo
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			DataContext = this;
			InitializeComponent();
			WindowStartupLocation = WindowStartupLocation.CenterScreen;

			var root = new NodeItem("Menu");
			var child1 = new NodeItem("Child #1", root);
			child1.Items.Add(new NodeItem("Child #1.1", child1));
			child1.Items.Add(new NodeItem("Child #1.2", child1));
			root.Items.Add(child1);
			root.Items.Add(new NodeItem("Child #2", root));
			var child3 = new NodeItem("Child #3", root);
			var child3_1 = new NodeItem("Child #3.1", child3);
			child3_1.Items.Add(new NodeItem("Child #3.1.1", child3_1));
			child3.Items.Add(child3_1);
			root.Items.Add(child3);
			treeCtrl.Items.Add(root);
			topology.Items.Add(root);

			AddItemCmd = new RelayCommand(AddItemCmdExe, CanAddItemCmdExe);
			RemoveItemCmd = new RelayCommand(RemoveItemCmdExe, CanRemoveItemCmdExe);

			topology.DataContext = this;
		}

		protected override void OnSourceInitialized(EventArgs e)
		{
			base.OnSourceInitialized(e);

			var isLightTheme = IsLightTheme();
			var source = (HwndSource)PresentationSource.FromVisual(this);
			ToggleBaseColour(source.Handle, !isLightTheme);

			// Detect when the theme changed
			source.AddHook((IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) => {
				const int WM_SETTINGCHANGE = 0x001A;
				if (msg == WM_SETTINGCHANGE) {
					if (wParam == IntPtr.Zero && Marshal.PtrToStringUni(lParam) == "ImmersiveColorSet") {
						var isLightTheme = IsLightTheme();
						ToggleBaseColour(hwnd, !isLightTheme);
					}
				}
				return IntPtr.Zero;
			});
		}

		private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			topology.Width = topologyBorder.ActualWidth;
			topology.Height = topologyBorder.ActualHeight;
		}

		public RelayCommand AddItemCmd { get; set; }

		private void AddItemCmdExe(object param)
		{
			var item = param as NodeItem;
			if (item?.Parent != null) {
				Debug.WriteLine($"AddItem: {item?.Title}, {item?.Parent.Title}");
			} else {
				Debug.WriteLine($"AddItem: {item?.Title}, (null)");
			}
		}

		private bool CanAddItemCmdExe(object param)
		{
			return true;
		}

		public RelayCommand RemoveItemCmd { get; set; }

		private void RemoveItemCmdExe(object param)
		{
			var item = param as NodeItem;
			if (item?.Parent != null) {
				Debug.WriteLine($"RemoveItem: {item?.Title}, {item?.Parent.Title}");
			} else {
				Debug.WriteLine($"RemoveItem: {item?.Title}, (null)");
			}
		}

		private bool CanRemoveItemCmdExe(object param)
		{
			return true;
		}

		private readonly PaletteHelper _paletteHelper = new PaletteHelper();

		private void ToggleBaseColour(nint hwnd, bool isDark)
		{
			var theme = _paletteHelper.GetTheme();
			var baseTheme = isDark ? BaseTheme.Dark : BaseTheme.Light;
			theme.SetBaseTheme(baseTheme);
			_paletteHelper.SetTheme(theme);
			UseImmersiveDarkMode(hwnd, isDark);
		}

		private static bool IsLightTheme()
		{
			using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
			var value = key?.GetValue("AppsUseLightTheme");
			return value is int i && i > 0;
		}

		[DllImport("dwmapi.dll")]
		private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

		private const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19;
		private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

		private static bool UseImmersiveDarkMode(IntPtr handle, bool enabled)
		{
			if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17763)) {
				var attribute = DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1;
				if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 18985)) {
					attribute = DWMWA_USE_IMMERSIVE_DARK_MODE;
				}

				int useImmersiveDarkMode = enabled ? 1 : 0;
				return DwmSetWindowAttribute(handle, attribute, ref useImmersiveDarkMode, sizeof(int)) == 0;
			}

			return false;
		}
	}

	// https://itpro.tistory.com/90
	// https://www.codeproject.com/Tips/813345/Basic-MVVM-and-ICommand-Usage-Example
	public class RelayCommand : ICommand
	{
		private Action<object>? execute;

		private Predicate<object>? canExecute;

		private event EventHandler? CanExecuteChangedInternal;

		public RelayCommand(Action<object> execute)
			: this(execute, DefaultCanExecute)
		{
		}

		public RelayCommand(Action<object>? execute, Predicate<object>? canExecute)
		{
			ArgumentNullException.ThrowIfNull(execute);
			ArgumentNullException.ThrowIfNull(canExecute);

			this.execute = execute;
			this.canExecute = canExecute;
		}

		public event EventHandler? CanExecuteChanged
		{
			add
			{
				CommandManager.RequerySuggested += value;
				this.CanExecuteChangedInternal += value;
			}

			remove
			{
				CommandManager.RequerySuggested -= value;
				this.CanExecuteChangedInternal -= value;
			}
		}

		public bool CanExecute(object? parameter)
		{
			return this.canExecute != null && parameter != null && this.canExecute(parameter);
		}

		public void Execute(object? parameter)
		{
			if (parameter != null && this.execute != null)
				this.execute(parameter);
		}

		public void OnCanExecuteChanged()
		{
			this.CanExecuteChangedInternal?.Invoke(this, EventArgs.Empty);
		}

		public void Destroy()
		{
			this.canExecute = _ => false;
			this.execute = _ => { return; };
		}

		private static bool DefaultCanExecute(object parameter)
		{
			return true;
		}
	}
}
