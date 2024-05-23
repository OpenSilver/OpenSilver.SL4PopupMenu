
using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Controls.Theming;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using SL4PopupMenu;

namespace SL4PopupMenuDemo
{
	// This demonstrates a variety of ways to generate a menu both in code and in XAML.
	public partial class Demo2 : Page
	{
		// Stores a reference to the clicked TextBox if any.
		TextBox _txtClicked;

		PopupMenu _pmOrientation;

		CheckBox _chkShowMore = new CheckBox { Content = "Show Clipboard Menu", IsChecked = true };

		TextBlock _txbDeleteItem = new TextBlock { Tag = "DeleteItemTag", Name = "DeleteMenu", FontWeight = FontWeights.Bold };

		PopupMenuCheckBox _pmcbToggleMenuState = new PopupMenuCheckBox
		{
			Header = "Toggle ^State", // Also sets Key.S as shortcut key.
			IsThreeState = true, // Changes the value changes from true to null and then to false when clicked.
			CloseOnClick = false,
			IsChecked = false,
			ImageCheckedSource = new BitmapImage(new Uri("images/IsModal.png", UriKind.Relative)),
			ImageIntermediateSource = new BitmapImage(new Uri("images/IsPinned.png", UriKind.Relative))
		};

		//Storyboard stbSpinner;

		public Demo2()
		{
			InitializeComponent();
			// To apply an external storyboard on an element inside a PopupMenu we must register it using the RegisterStoryBoardTargets method first.
			// To get around this however just place the storyboard inside the PopupMenu control itself.
			PopupMenuUtils.RegisterStoryBoardTargets(stbShowHomepageLink, txbHomepage, lnkHomepage);

			_chkShowMore.Style = Application.Current.Resources["MenuBaseStyle"] as Style; // Use same style as menu.

			//stbSpinner = PopupMenuUtils.CreateStoryBoard(150, 2000,
			//        lnkHomepage, "(UIElement.Projection).(PlaneProjection.RotationX)", 720,
			//        new ElasticEase { EasingMode = EasingMode.EaseOut });

			GeneratePopupMenu();
		}

		private void GeneratePopupMenu()
		{
			// Menu items can be added in several ways as seen below:
			pmMain.InsertItem(0, "images/delete.png", _txbDeleteItem, null, Menu_RemoveItem); // Menu used to delete ListBoxItems.
			pmMain.InsertSeparator(1, "DeleteItemTag");
			pmMain.AddItem(null, null, null, null, "TriggerTextMenu", MenuItem_Clicked); // Menu used to display the clicked item type.
			pmMain.AddSeparator();
			pmMain.AddItem(_pmcbToggleMenuState);
			pmMain.AddSeparator();
			pmMain.AddItem(stpTransparency).CloseOnClick = false; // CloseOnClick is set to false to avoid the menu from closing after operating the slider.
			pmMain.AddItem(new PopupMenuItem(null, _chkShowMore, false));
			//pmMain.ItemsEffect = new DropShadowEffect { Color = Colors.White, Direction = 0, BlurRadius = 10, Opacity = 0.5 };

			// Creating a menu from an existing ListBox.
			_pmOrientation = new PopupMenu(lstOrientationMenu) { Orientation = MenuOrientationTypes.Right };

			// Attaching a submenu to several parent menus. Note that the AddSubmenu method(see Demo1) can be used for this purpose.
			_pmOrientation.AddTrigger(TriggerTypes.Hover, pmiOrientableMenu);
			_pmOrientation.AddTrigger(TriggerTypes.LeftClick, lstDemo);

			// Attach an event handler to set the menu state as pinned, modal or normal.
			_pmcbToggleMenuState.CheckedValueChanged += pmiMenuState_Changed;

			// Attach Loaded and Click event handlers to the CheckBox used to show/hide clipboard menus.
			_chkShowMore.Loaded += chkShowMore_ValueChanged;
			_chkShowMore.Click += chkShowMore_ValueChanged;

			pmMain.Showing += pmMain_Showing;
		}

		// Event handler to modify the menu before it shows up based on the clicked or hovered item.
		void pmMain_Showing(object sender, RoutedEventArgs e)
		{
			// Display the clicked item type in the PopupMenuItem named 'TriggerTextMenu'(added programmatically above).
			pmMain.FindItemByName<PopupMenuItem>("TriggerTextMenu").Header
				= "Trigger: " + (sender as FrameworkElement).Name.ToString();

			// Identify the clicked item and update the TextBlock _txbDeleteItem accordingly. Note that this can be achieved
			// in XAML by setting UseTriggerElementDataContext as true. This automatically exposes the data context of the 
			// clicked element to the menu elements.
			var clickedRow = pmMain.GetClickedElement<ListBoxItem>();
			if (clickedRow != null)
				_txbDeleteItem.Text = "Remove " + clickedRow.Content;

			// Show or hide all containers for elements tagged as 'DeleteItemTag' depending on whether a ListBoxItem was clicked 
			// or not. Note that this method hides the item container only and not the item itelf. 
			pmMain.SetContainerVisibilityByTag("DeleteItemTag", clickedRow != null);

			// Enable clipboard menus only when a TextBox is clicked. Selected text or clipboard content are also checked for.
			_txtClicked = pmMain.GetClickedElement<TextBox>();
			pmiCut.IsEnabled = _txtClicked != null;
			pmiCopy.IsEnabled = _txtClicked != null;
			pmiPaste.IsEnabled = _txtClicked != null && Clipboard.ContainsText();

			// Start the homepage hyperlink animation
			stbShowHomepageLink.Stop();
			stbShowHomepageLink.Begin();
			//stbSpinner.Begin();
		}

		// Event handler to alter the menu state as determined by the nullable pmiMenuState CheckBox.
		void pmiMenuState_Changed(object sender, EventArgs e)
		{
			var pmcb = sender as PopupMenuCheckBox;
			pmMain.IsModal = pmcb.IsChecked ?? false; // true -> Modal | false -> Normal.
			pmMain.IsPinned = pmcb.IsChecked == null; // null -> Pinned.
		}

		// CheckBox value changed event for 'Show Clipboard Menu'.
		void chkShowMore_ValueChanged(object sender, RoutedEventArgs e)
		{
			foreach (Control item in pmMain.FindItemContainersByTag("ClipboardMenuTag"))
				item.Visibility = _chkShowMore.IsChecked.Value ? Visibility.Visible : Visibility.Collapsed;
		}

		// ListBox item removal event handler.
		void Menu_RemoveItem(object sender, RoutedEventArgs e)
		{
			ListBoxItem item = pmMain.GetClickedElement<ListBoxItem>();
			(item.Parent as ListBox).Items.Remove(item);
			txbMessage.Text = "You removed: " + item.Content;
		}

		// Clipboard manipulation event handler.
		void MenuItem_Clicked(object sender, RoutedEventArgs e)
		{
			var pmiClicked = sender as PopupMenuItem;
			if (pmiClicked != null)
			{
				txbMessage.Text = "You clicked on: "
					+ (sender is PopupMenuItem ? ((PopupMenuItem)sender).Name ?? ((PopupMenuItem)sender).Header : ((TextBlock)sender).Text);

				// Clipboard copy, cut or paste.
				if (sender == pmiCut || sender == pmiCopy)
				{
					Clipboard.SetText(_txtClicked.SelectedText);
					if (sender == pmiCut)
						_txtClicked.SelectedText = "";
				}
				else if (sender == pmiPaste)
				{
					_txtClicked.SelectedText = Clipboard.GetText();
				}
				else if (pmiClicked.Tag != null)
				{
					// Set the menu orientation according to the tag of the clicked menu.
					switch (pmiClicked.Tag.ToString())
					{
						case "OrientationTop":
							_pmOrientation.Orientation = MenuOrientationTypes.Top;
							break;
						case "OrientationRight":
							_pmOrientation.Orientation = MenuOrientationTypes.Right;
							break;
						case "OrientationBottom":
							_pmOrientation.Orientation = MenuOrientationTypes.Bottom;
							break;
						case "OrientationLeft":
							_pmOrientation.Orientation = MenuOrientationTypes.Left;
							break;
					}

					// Enable and add a glow effect to the selected item only.
					foreach (var item in _pmOrientation.FindItemsByTag<PopupMenuItem>("Orientation.*"))
					{
						if (item.Tag.Equals(pmiClicked.Tag))
						{
							item.ImageEffect = new DropShadowEffect { Color = Colors.Green, ShadowDepth = 0 };
							item.IsEnabled = false;
						}
						else
						{
							item.ImageEffect = null;
							item.IsEnabled = true;
						}
					}
				}
			}
		}

		private void Theme_Click(object sender, RoutedEventArgs e)
		{
			thmDynamicTheme.ThemeUri = new Uri("/System.Windows.Controls.Theming."
				+ (sender as PopupMenuItem).Header + ";component/Theme.xaml"
				, UriKind.Relative);
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			pmMain.UpdateTriggers();
		}
	}

	//public class TestCommand : ICommand
	//{
	//    public event EventHandler CanExecuteChanged;

	//    public bool CanExecute(object parameter)
	//    {
	//        return true;
	//    }

	//    public void Execute(object parameter)
	//    {
	//        MessageBox.Show(parameter.ToString());

	//        if (CanExecuteChanged != null)
	//            CanExecuteChanged(this, new EventArgs());
	//    }
	//}
}
