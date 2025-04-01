using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;


namespace WpfTreeViewDemo
{
	/// <summary>
	/// Interaction logic for TopologyView.xaml
	/// </summary>
	public partial class TopologyView : UserControl
	{
		public TopologyView()
		{
			InitializeComponent();

			gameTime = DateTime.Now;
			if (DesignerProperties.GetIsInDesignMode(this)) {
				Width = 320;
				Height = 240;
			} else {
				dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
				dispatcherTimer.Tick += new EventHandler(DispatcherTimer_Tick);
				dispatcherTimer.Interval = TimeSpan.FromMilliseconds(10);
				dispatcherTimer.Start();
			}
		}

		public void Dispose()
		{
			dispatcherTimer?.Stop();
		}

		public void InitNode(float w, float h)
		{
			var count = Items.Count;
			if (count > 0) {
				var pos = new Vector2(w - (float)this.Padding.Right, h / 2);
				Items[0]!.Init(pos, 90.0f, 0, this);
			}
		}

		private readonly DispatcherTimer? dispatcherTimer;
		private DateTime gameTime;

		public enum NodeDirection { Left, Top, Right, Bottom, Center }
		private NodeDirection rootDirection;

		[Category("RootDirection"), Description("Root node position")]
		public NodeDirection RootDirection
		{
			get { return rootDirection; }
			set { rootDirection = value; }
		}

		private SolidColorBrush? nodeFillBrush;

		[Category("NodeFillBrush"), Description("Brush to paint on nodes")]
		public SolidColorBrush NodeFillBrush
		{
			get => nodeFillBrush ??= new SolidColorBrush(Colors.DarkBlue);
			set => nodeFillBrush = value;
		}

		private SolidColorBrush? nodeBorderBrush;

		[Category("NodeBorderBrush"), Description("Node border brush")]
		public SolidColorBrush NodeBorderBrush
		{
			get => nodeBorderBrush ??= new SolidColorBrush(Colors.White);
			set => nodeBorderBrush = value;
		}

		private SolidColorBrush? nodeLineBrush;

		[Category("NodeLineBrush"), Description("Node stem line brush")]
		public SolidColorBrush NodeLineBrush
		{
			get => nodeLineBrush ??= new SolidColorBrush(Colors.Gray);
			set => nodeLineBrush = value;
		}

		private void RenderImpl(DrawingContext dc, float w, float h, float delta)
		{
			var aspect = w / h;
			var x = w / 2.0f;
			var y = h / 2.0f;
			foreach (NodeItem node in Items) {
				node.Update(w, h, delta);
				node.Draw(dc, w, h, delta);
			}

#if DEBUG
			string testString = $"delta: {delta}";

			FormattedText formattedText = new(
				testString,
				CultureInfo.GetCultureInfo("en-us"),
				FlowDirection.LeftToRight,
				new Typeface("Verdana"),
				15,
				Brushes.White,
				VisualTreeHelper.GetDpi(this).PixelsPerDip)
			{
				MaxTextWidth = w,
				MaxTextHeight = h
			};

			dc.DrawText(formattedText, new Point(10, h - 20));
#endif
		}

		private void DispatcherTimer_Tick(object? sender, EventArgs e)
		{
			var delta = (float)(DateTime.Now - gameTime).TotalMilliseconds;
			var width = (float)ActualWidth;
			var height = (float)ActualHeight;
			if (width > 0 && height > 0) {
				this.Render(width, height, delta);
			}
			gameTime = DateTime.Now;
		}

		protected override void OnRender(DrawingContext dc)
		{
			base.OnRender(dc);
			var delta = (float)(DateTime.Now - gameTime).TotalMilliseconds;
			var width = (float)ActualWidth;
			var height = (float)ActualHeight;
			if (width > 0 && height > 0)
				Render(width, height, delta);
			dc.DrawDrawing(backingStore);
			gameTime = DateTime.Now;
		}

		private void OnSizeChanged(float width, float height)
		{
			Debug.WriteLine($"OnSizeChanged({width}, {height})");
		}

		private bool node_inited = false;
		private Vector2 client_size = new();
		private readonly DrawingGroup backingStore = new();

		private void Render(float width, float height, float delta)
		{
			if (!node_inited) {
				InitNode(width, height);
				node_inited = true;
			}
			if (width != client_size.X || height != client_size.Y) {
				client_size.X = width;
				client_size.Y = height;
				this.OnSizeChanged(width, height);
			}

			var dc = backingStore.Open();
			RenderImpl(dc, width, height, delta);
			dc.Close();
		}

		public class NodeItem(string title, TopologyView.NodeItem? parent = null)
		{
			public string Title { get; set; } = title;
			public NodeItem? Parent { get; set; } = parent;

			public Vector2 Position { get; set; }
			public float Angle { get; set; } = 90.0f;

			public ObservableCollection<NodeItem> Items { get; set; } = [];

			private TopologyView? ownerView;

			public void Init(Vector2 pos, float angle, int depth, TopologyView? view = null)
			{
				ownerView = view;
				Position = pos;
				Angle = angle;
				if (Items.Count > 0) {
					depth++;
					var count = Items.Count;
					float distance = 100.0f;
					float step = 20.0f;
					angle -= (count - 1) / 2.0f * step;
					foreach (NodeItem child in Items) {
						double degree = Math.PI * (angle + 180.0f) / 180.0;
						pos.X = (float)(Position.X + Math.Sin(degree) * distance);
						pos.Y = (float)(Position.Y + Math.Cos(degree) * distance);
						child.Init(pos, angle, depth, view);
						angle += step;
					}
				}
			}

			public void Draw(DrawingContext dc, float w, float h, float delta)
			{
				var pos = new Point(Position.X, Position.Y);
				float radius = 10.0f;
				double degree = Math.PI * Angle / 180.0;
				if (Parent != null) {
					var dot_pen = new Pen(ownerView!.NodeLineBrush, 1);
					dot_pen.DashStyle = System.Windows.Media.DashStyles.Dot;
					dc.DrawLine(dot_pen, pos, new Point(Parent.Position.X, Parent.Position.Y));
				}
				var pen = new Pen(ownerView!.NodeBorderBrush, 1);
				dc.DrawEllipse(ownerView!.NodeFillBrush, pen, pos, radius, radius);
				dc.DrawLine(pen, pos, new Point(Position.X + Math.Sin(degree) * radius, Position.Y + Math.Cos(degree) * radius));
				foreach (NodeItem child in this.Items) {
					child.Draw(dc, w, h, delta);
				}
			}

			public void Update(float w, float h, float delta)
			{
			}
		}

		public class NodeItemCollection : CollectionBase
		{
			public NodeItem? this[int index] => (NodeItem?)List[index];

			public bool Contains(NodeItem item)
			{
				return List.Contains(item);
			}

			public int Add(NodeItem item)
			{
				return List.Add(item);
			}

			public void Remove(NodeItem item)
			{
				List.Remove(item);
			}

			public void Insert(int index, NodeItem item)
			{
				List.Insert(index, item);
			}

			public int IndexOf(NodeItem item)
			{
				return List.IndexOf(item);
			}
		}

		private NodeItemCollection _nodeItemCollection { get; set; } = [];

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
		public NodeItemCollection Items => _nodeItemCollection;
	}
}
