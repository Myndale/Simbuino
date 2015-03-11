using Simbuino.UI.Disassembly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interactivity;
using System.Windows.Media;

namespace Simbuino.UI.Behaviors
{
	public static class ScrollToPCBehavior
	{
		public static readonly DependencyProperty CurrentInstructionProperty = DependencyProperty.RegisterAttached(
			"CurrentInstruction", typeof(InstructionViewModel),
			typeof(ScrollToPCBehavior), new FrameworkPropertyMetadata(null, OnCurrentInstructionChanged));

		public static void SetCurrentInstruction(UIElement element, InstructionViewModel value)
		{
			element.SetValue(CurrentInstructionProperty, value);
		}

		public static InstructionViewModel GetCurrentInstruction(UIElement element)
		{
			return (InstructionViewModel)element.GetValue(CurrentInstructionProperty);
		}

		private static void OnCurrentInstructionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var currentInstruction = d.GetValue(CurrentInstructionProperty) as InstructionViewModel;
			ListBox listBox = d as ListBox;
			if ((currentInstruction != null) && (listBox != null))
			{
				listBox.UpdateLayout();
				int index = listBox.Items.IndexOf(currentInstruction);
				if (index == -1)
					return;
				ListBoxItem lbi = (ListBoxItem)(listBox.ItemContainerGenerator.ContainerFromIndex(index));
				if (!IsItemVisible(listBox, index))
					listBox.Dispatcher.BeginInvoke((Action)(() =>
					{
						listBox.UpdateLayout();
						ScrollViewer scrollViewer = GetScrollViewer(listBox) as ScrollViewer;
						if (scrollViewer != null)
							scrollViewer.ScrollToBottom();
						listBox.ScrollIntoView(currentInstruction);
					}));
			}
		}

		private static bool IsItemVisible(ListBox lb, int index)
		{
			if (lb.Items.Count == 0)
				return false;			
			ListBoxItem lbi = lb.ItemContainerGenerator.ContainerFromIndex(index) as ListBoxItem;
			if (lbi == null)
				return false;
			VirtualizingStackPanel vsp = VisualTreeHelper.GetParent(lbi) as VirtualizingStackPanel;
			int FirstVisibleItem = (int)vsp.VerticalOffset;
			int VisibleItemCount = (int)vsp.ViewportHeight;
			if (index >= FirstVisibleItem && index <= FirstVisibleItem + VisibleItemCount)
				return true;
			return false;

		}

		private static DependencyObject GetScrollViewer(DependencyObject o)
		{
			if (o is ScrollViewer)
			{ return o; }

			for (int i = 0; i < VisualTreeHelper.GetChildrenCount(o); i++)
			{
				var child = VisualTreeHelper.GetChild(o, i);

				var result = GetScrollViewer(child);
				if (result == null)
				{
					continue;
				}
				else
				{
					return result;
				}
			}

			return null;
		}

	}
}
