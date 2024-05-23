using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Navigation;
using System.Collections.ObjectModel;
using SL4PopupMenu;
using System.Windows.Controls.Primitives;

namespace SL4PopupMenuDemo
{
	public partial class Demo1 : Page
	{
		// Demonstrates two ways of loading the same menu dynamically.
		public Demo1()
		{
			InitializeComponent();

			// Generate a sample menu on a DataGrid.
			GenerateDynamicDemo();

			// In this example no menu items are generated before the menu is open.
			//GenerateFullyDynamicDemo(); 
		}

		private void GenerateDynamicDemo()
		{
			// Add a DataGrid control with some sample data to the layout root
			var data = new ObservableCollection<string>("Item 1,Item 2,Item 3,Item 4,Item 6,Item 7,Item 8".Split(','));
			DataGrid dataGrid1 = new DataGrid() { Margin = new Thickness(50), ItemsSource = data };
			tbiSample1.Content = dataGrid1;

			// Create the submenu.
			var pmTimeNow = new PopupMenu();
			pmTimeNow.AddItem("Time Now", null);
			// Update the time just before the menu is shown.
			pmTimeNow.Showing += delegate
			{
				pmTimeNow.PopupMenuItem(0).Header = DateTime.Now.ToLongTimeString();
			};

			// Create the main menu. 
			var pmMain = new PopupMenu { Shortcut = "Ctrl+Alt+M", IsShortcutFocusOnly = true };
			// Add the menu items.
			pmMain.AddItem("Delete row", delegate { data.RemoveAt(pmMain.GetClickedElement<DataGridRow>().GetIndex()); });
			pmMain.AddSeparator();
			pmMain.AddSubMenu(pmTimeNow, "Get Time ", "images/arrow.png", null, null, false, null); // Attach the submenu pmTimeSub.
			pmMain.AddSeparator();
			pmMain.AddItem("Demo2", delegate { App.Current.Host.NavigationState = "/Views/Demo2.xaml"; });
			// Set dataGrid1 as the trigger element.
			pmMain.AddTrigger(TriggerTypes.RightClick, dataGrid1);
			// Update main menu just before it is shown.
			pmMain.Showing += (sender, e) =>
			{
				pmMain.PopupMenuItem(0).Header = "Delete " + dataGrid1.SelectedItem;
				pmMain.PopupMenuItem(0).IsVisible =
				pmMain.PopupMenuItem(1).IsVisible = pmMain.GetClickedElement<DataGridRow>() != null;
			};

		}

		//// In this method menu no item is generated until the menu is opened.
		//private void GenerateFullyDynamicDemo()
		//{
		//    // Add a DataGrid control with some sample data to the layout root
		//    var data = new ObservableCollection<string>("Item 1,Item 2,Item 3,Item 4,Item 6,Item 7,Item 8".Split(','));
		//    DataGrid dataGrid1 = new DataGrid() { Margin = new Thickness(50), ItemsSource = data };
		//    this.LayoutRoot.Children.Add(dataGrid1);

		//    //  Create the main menu
		//    var pmMain = new PopupMenu();
		//    pmMain.AddTrigger(TriggerTypes.RightClick, dataGrid1);
		//    // Note that the Opening event is the only one supporting addition of menus with child menus
		//    pmMain.Opening += delegate
		//    {
		//        if (pmMain.GetClickedElement<DataGridRow>() != null)
		//        {
		//            pmMain.AddItem(dataGrid1.SelectedItem.ToString(), delegate
		//            {
		//                data.RemoveAt(pmMain.GetClickedElement<DataGridRow>().GetIndex());
		//            });
		//            pmMain.AddSeparator();
		//        }

		//        // Create the submenu
		//        var pmTimeNow = new PopupMenu();
		//        pmTimeNow.Showing += delegate
		//        {
		//            pmTimeNow.AddItem(DateTime.Now.ToLongTimeString(), null);
		//        };
		//        pmMain.AddSubMenu(pmTimeNow, "Get Time ", "images/arrow.png", null, null, false, null); // Attach the submenu

		//        pmMain.AddSeparator();
		//        pmMain.AddItem("Demo2", delegate
		//        {
		//            App.Current.Host.NavigationState = "/Demo2.xaml";
		//        });
		//    };
		//    pmMain.Closing += delegate { pmMain.Items.Clear(); };
		//}

	}
}
