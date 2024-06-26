﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using System.Windows.Shapes;
using SL4PopupMenu;

namespace SL4PopupMenuDemo
{
	public partial class MainPage : UserControl
	{
		public MainPage()
		{
			InitializeComponent();
		}

		// After the Frame navigates, ensure the HyperlinkButton representing the current page is selected
		private void ContentFrame_Navigated(object sender, NavigationEventArgs e)
		{

		}

		// If an error occurs during navigation, show an error window
		private void ContentFrame_NavigationFailed(object sender, NavigationFailedEventArgs e)
		{
			e.Handled = true;
			var msg = "Page not found: \"" + e.Uri.ToString() + "\"";
			new SL4PopupMenu.PopupMenu(new PopupMenuItem { Header = msg }) { IsModal = true }
				.OpenNextTo(MenuOrientationTypes.MouseBottomRight, null, false, false);
		}
	}
}