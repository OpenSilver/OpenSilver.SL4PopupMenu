using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace SL4PopupMenu
{
	public class PopupListBox : ListBox
	{
		public PopupMenu PopupMenu { get; set; }
		public PopupListBox()
		{
			PopupMenu = new PopupMenu();
			PopupMenu.ItemsControl = this as ListBox;
		}
	}
}
