using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
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
		}

		public class NodeItem(string title, TopologyView.NodeItem? parent = null)
		{
			public string Title { get; set; } = title;
			public NodeItem? Parent { get; set; } = parent;

			public ObservableCollection<NodeItem> Items { get; set; } = [];
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
