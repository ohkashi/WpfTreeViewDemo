using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using static WpfTreeViewDemo.TopologyView;


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
			_nodeItemCollection.ownerView = this;
			var count = Items.Count;
			if (count > 0) {
				var pos = new Vector2(w / 2, h / 2);
				Items[0]!.Init(pos, this);
			}
		}

		private readonly DispatcherTimer? dispatcherTimer;
		private DateTime gameTime;

		public enum NodeDirection { Left, Right, Top, Bottom, Center }
		private NodeDirection rootDirection = NodeDirection.Left;

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

		private float nodeRadius = 0.03f;

		[Category("NodeRadius"), Description("Node radius")]
		public float NodeRadius
		{
			get => nodeRadius;
			set => nodeRadius = value;
		}

		private float nodeDistance = 0.3f;

		[Category("NodeDistance"), Description("Node distance")]
		public float NodeDistance
		{
			get => nodeDistance;
			set => nodeDistance = value;
		}

		private float childDegree = 20.0f;

		[Category("ChildDegree"), Description("Child Node degree")]
		public float ChildDegree
		{
			get => childDegree;
			set => childDegree = value;
		}

		private void RenderImpl(DrawingContext dc, float w, float h, float delta)
		{
			var aspect = w / h;
			var x = w / 2.0f;
			var y = h / 2.0f;
			foreach (NodeItem node in Items) {
				node.Update(w, h, delta);
				node.DrawStem(dc, w, h, delta);
			}
			foreach (NodeItem node in Items)
				node.Draw(dc, w, h, delta);

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

			private int GetIndex(NodeItem item)
			{
				for (int i = 0; i < Items.Count; i++) {
					if (Items[i] == item) {
						return i;
					}
				}
				return -1;
			}

			public void Init(Vector2 pos, TopologyView view)
			{
				ownerView = view;
				Position = pos;
				foreach (NodeItem child in Items) {
					child.Init(pos, view);
				}
			}

			public void DrawStem(DrawingContext dc, float w, float h, float delta)
			{
				if (Parent != null) {
					var dot_pen = new Pen(ownerView!.NodeLineBrush, 1);
					dot_pen.DashStyle = System.Windows.Media.DashStyles.Dot;
					dc.DrawLine(dot_pen, new Point(Position.X, Position.Y), new Point(Parent.Position.X, Parent.Position.Y));
				}
				foreach (NodeItem child in this.Items) {
					child.DrawStem(dc, w, h, delta);
				}
			}

			public void Draw(DrawingContext dc, float w, float h, float delta)
			{
				var pos = new Point(Position.X, Position.Y);
				float radius = Math.Min(w, h) * ownerView!.NodeRadius;
				double degree = Math.PI * Angle / 180.0;
				var pen = new Pen(ownerView!.NodeBorderBrush, 1);
				dc.DrawEllipse(ownerView!.NodeFillBrush, pen, pos, radius, radius);
#if DEBUG
				dc.DrawLine(pen, pos, new Point(Position.X + Math.Sin(degree) * radius, Position.Y + Math.Cos(degree) * radius));
#endif
				foreach (NodeItem child in this.Items) {
					child.Draw(dc, w, h, delta);
				}
			}

			public void Update(float w, float h, float delta)
			{
				if (Parent != null) {
					int idx = Parent.GetIndex(this);
					Debug.Assert(idx >= 0);
					var count = Parent.Items.Count;
					float distance = Math.Min(w, h) * ownerView!.NodeDistance;
					float step = 0;
					Angle = Parent.Angle;
					if (count > 1) {
						step = ownerView!.ChildDegree;
						Angle -= ownerView!.ChildDegree * (count - 1) / 2.0f;
					}
					Angle += step * idx;
					double degree = Math.PI * (Angle + 180.0f) / 180.0;
					Position = new Vector2((float)(Parent.Position.X + Math.Sin(degree) * distance),
						(float)(Parent.Position.Y + Math.Cos(degree) * distance));
				} else {
					float radius = Math.Min(w, h) * ownerView!.NodeRadius;
					switch (ownerView!.RootDirection) {
						case NodeDirection.Left:
							Position = new Vector2(radius + (float)ownerView!.Padding.Left, h / 2);
							Angle = 270.0f;
							break;
						case NodeDirection.Right:
							Position = new Vector2(w - radius - (float)ownerView!.Padding.Right, h / 2);
							Angle = 90.0f;
							break;
						case NodeDirection.Top:
							Position = new Vector2(w / 2, radius + (float)ownerView!.Padding.Top);
							Angle = 180.0f;
							break;
						case NodeDirection.Bottom:
							Position = new Vector2(w / 2, h - radius - (float)ownerView!.Padding.Bottom);
							Angle = 0.0f;
							break;
						case NodeDirection.Center:
							Position = new Vector2(w / 2, h / 2);
							Angle = 90.0f;
							break;
					}
				}
				foreach (NodeItem child in this.Items) {
					child.Update(w, h, delta);
				}
			}
		}

		public class NodeItemCollection : CollectionBase
		{
			public TopologyView? ownerView;

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
