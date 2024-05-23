// Copyright (c) 2009 Ziad Jeeroburkhan. All Rights Reserved.
// GNU Library General Public License (LGPL) 
// (http://sl4popupmenu.codeplex.com/license)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.ComponentModel;
using System.Diagnostics;

namespace SL4PopupMenu
{
	/// <summary>
	/// A derivative of the PopupMenuBase control which essentially adds properties and methods for contents 
	/// including an ItemsControl.
	/// </summary>
	public class PopupMenu : PopupMenuBase
	{
		/// <summary>
		/// A readonly collection of items in the ItemsControl used by the menu.
		/// However it remains modifiable through this.ItemsControl.Items.
		/// New items and submenus can be added via AddItem or AddSubMenu respectively.
		/// </summary>
		public ItemCollection Items
		{
			get { return ItemsControl.Items; }
		}

		/// <summary>
		/// The left column image width for all PopupItem controls in the menu.
		/// </summary>
		public double? LeftColumnImageWidth { get; set; }

		/// <summary>
		/// Stores a reference to the ItemsControl element. It is not a permanent reference and is nullified during
		/// each layout update such that the ItemsControl property getter is forced to reassign its value each time.
		/// </summary>
		private ItemsControl _itemsControl;

		/// <summary>
		/// Gets or sets a reference to the ItemsControl(typically a ListBox) used to accomodate the menu items.
		/// </summary>
		public ItemsControl ItemsControl
		{
			get // Gets the first ItemsControl in ContentRoot
			{
				if (_itemsControl != null)
					return _itemsControl;

				// If our control has contains anything.
				if (base.Content != null && base.Content is FrameworkElement)
					ContentRoot = base.Content as FrameworkElement; // Move the content to OverlayCanvas via the ContentRoot setter

				// Add a ListBox by default to OverlayCanvas if none was found.
				if (ContentRoot == null)
					ContentRoot = new ListBox { Background = new SolidColorBrush(Color.FromArgb(255, 240, 240, 240)) };

				_itemsControl = base.OverlayCanvas.GetVisualDescendants().OfType<ItemsControl>().FirstOrDefault();
				return _itemsControl;
			}
			set // Make the ItemsControl a child of OverlayCanvas.
			{
				PopupMenuUtils.MoveElementTo(value, OverlayCanvas, EnableShadowEffect, true);
			}
		}

		/// <summary>
		/// Do not reset the item selection on reopening the menu.
		/// </summary>
		public bool KeepSelection { get; set; }

		public PopupMenu() : this(null) { }

		public PopupMenu(FrameworkElement contentRoot)
			: base(contentRoot)
		{
			OverlayCanvas.Background = new SolidColorBrush(Colors.Transparent);

			base.Opening += delegate
			{
				if (ItemsControl != null)
				{
					if (!KeepSelection && ItemsControl is Selector)
						(ItemsControl as Selector).SelectedIndex = -1; // Reset selected item in menu
				}
			};

			this.Shown += delegate
			{
				if (base.RestoreFocusOnClose && ItemsControl != null)
					ItemsControl.Focus();
			};

			this.LayoutUpdated += delegate { _itemsControl = null; }; // Nullify _itemsControl to be sure the ItemsControl getter always uses an up to date reference.
			this.LayoutUpdated += PopupMenu_LayoutUpdated; // Self unsubscribing event handler mainly dealing with ItemsControl.
		}

		private void PopupMenu_LayoutUpdated(object sender, EventArgs e)
		{
			if (ItemsControl != null)
			{
				this.LayoutUpdated -= PopupMenu_LayoutUpdated;

				ItemsControl.KeyUp += ItemsControl_KeyUp;

				ItemsControl.SizeChanged += ItemsControl_SizeChanged;

				if (this.Style != null && ItemsControl.Style == null)
					ItemsControl.Style = this.Style.BasedOn;

				ItemsControl.FlowDirection = this.FlowDirection;

				if (BorderMaskFill != null && DesignerProperties.IsInDesignTool)
					base.AdjustBorderMaskPosition(new Point(), ItemsControl);
			}
		}

		private void ItemsControl_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			if (LeftColumnImageWidth.HasValue)
				foreach (var item in ItemsControl.Items)
					if (item is PopupMenuItem)
						(item as PopupMenuItem).ImageWidth = LeftColumnImageWidth.Value;
		}

		private void ItemsControl_KeyUp(object sender, KeyEventArgs e)
		{
			// Debug.WriteLine(e.Key + " " + DateTime.Now.ToString());
			UIElement selectedItem = ItemsControl is Selector ? (ItemsControl as Selector).SelectedItem as UIElement : null;

			if (selectedItem == null)
			{
				if (e.Key != Key.Right && e.Key != Key.Left && e.Key != Key.Up && e.Key != Key.Down
					&& !(FocusManager.GetFocusedElement() is TextBox))
				{
					ItemsControl.Focus(); // Set the selected index to zero if the ItemsControl is a Selector.
					e.Handled = true;
				}
			}
			else
			{
				if (e.Key == Key.Enter)
				{
					if (selectedItem is PopupMenuItem)
						(selectedItem as PopupMenuItem).OnClick();
				}
				else if (e.Key == Key.Space)
				{
					if (selectedItem is PopupMenuCheckBox)
						(selectedItem as PopupMenuCheckBox).ToggleCheckedValue(false, e);
				}
				else if (e.Key == (base.Orientation == MenuOrientationTypes.Left ? Key.Left : Key.Right))
				{
					var selectedItemContainer = PopupMenuUtils.GetItemContainer(ItemsControl, selectedItem as FrameworkElement);
					if (selectedItemContainer != null)
					{
						// Identify all navigable menus related to the selected item or its parent.
						var menuTriggersKeyedUp = PopupMenuManager.MenuTriggers.Where(mt =>
							(mt.TriggerType == TriggerTypes.Hover || mt.TriggerType == TriggerTypes.LeftClick)
							&& (mt.TriggerElement == selectedItem || mt.TriggerElement == selectedItemContainer));

						if (menuTriggersKeyedUp.Count() > 0)
						{	// Open the associated menus.
							foreach (var menuTrigger in menuTriggersKeyedUp)
								menuTrigger.PopupMenuBase.OpenNextTo(menuTrigger.PopupMenuBase.Orientation, selectedItemContainer, true, true);
						}
						else
						{
							PopupMenuManager.CloseChildMenus(0,false, false, null);
						}
					}
				}
			}

			// Find any shortcut key matching the currently pressed key in the ItemsControl and handle it.
			foreach (var item in ItemsControl.Items.Where(i => i is HeaderedItemsControl))
			{
				object header = (item as HeaderedItemsControl).Header;
				if (header is StackPanel && ((header as StackPanel).Tag ?? "").ToString().ToUpper() == e.Key.ToString())
				{
					if (item is PopupMenuItem)
						(item as PopupMenuItem).OnClick();
					else
						(ItemsControl as Selector).SelectedItem = item;
				}
			}

			if (e.Key == Key.Escape
				|| selectedItem == null && (e.Key == Key.Up || e.Key == Key.Down) && e.Key != base.FlyoutKey
				|| e.Key == (base.Orientation == MenuOrientationTypes.Left ? Key.Right : Key.Left))// e.Key == (Key)((int)Key.Left + (((int)Orientation) % 4)))
			{
				base.KeepParentMenusOpen = true;
				PopupMenuManager.CloseTopMenu(e);
			}

			//if (selectedItem != null)
			//{
			//    var child = (selectedItem as FrameworkElement).GetVisualDescendants().OfType<Control>().LastOrDefault();
			//    if (child != null)
			//        child.Focus();
			//}		}

			//if (Orientation == MenuOrientationTypes.Bottom && e.Key == Key.Up)
			//    if (ItemsControl is Selector && (ItemsControl as Selector).SelectedIndex == 0)
			//        CloseTopMenu(null);
		}


		#region AddItem

		public PopupMenuItem AddItem(FrameworkElement item)
		{
			return InsertItem(-1, null, item, null, null, null);
		}

		public PopupMenuItem AddItem(string header, RoutedEventHandler clickHandler)
		{
			return InsertItem(-1, null, header, null, null, null, clickHandler);
		}

		public PopupMenuItem AddItem(FrameworkElement item, RoutedEventHandler clickHandler)
		{
			return InsertItem(-1, item, clickHandler);
		}

		public PopupMenuItem AddItem(bool showLeftMargin, FrameworkElement item, RoutedEventHandler clickHandler)
		{
			return InsertItem(-1, showLeftMargin, item, clickHandler);
		}

		public PopupMenuItem AddItem(string iconUrl, FrameworkElement item, RoutedEventHandler clickHandler)
		{
			return InsertItem(-1, iconUrl, item, null, null, clickHandler);
		}

		public PopupMenuItem AddItem(string iconUrl, string header, string tag, RoutedEventHandler clickHandler)
		{
			return InsertItem(-1, iconUrl, new TextBlock() { Text = header, Tag = tag }, null, null, clickHandler);
		}

		public PopupMenuItem AddItem(string leftIconUrl, string header, string rightIconUrl, string tag, RoutedEventHandler clickHandler)
		{
			return InsertItem(-1, leftIconUrl, new TextBlock() { Text = header, Tag = tag }, rightIconUrl, null, clickHandler);
		}

		public PopupMenuItem AddItem(string leftIconUrl, string header, string rightIconUrl, string tag, string name, RoutedEventHandler clickHandler)
		{
			return InsertItem(-1, leftIconUrl, new TextBlock() { Text = header, Tag = tag }, rightIconUrl, name, clickHandler);
		}

		#endregion AddItem

		#region InsertItem

		public PopupMenuItem InsertItem(int index, FrameworkElement item)
		{
			return InsertItem(index, item, null);
		}

		public PopupMenuItem InsertItem(int index, string header, RoutedEventHandler leftClickHandler)
		{
			return InsertItem(index, null, new TextBlock() { Text = header }, null, null, leftClickHandler);
		}

		public PopupMenuItem InsertItem(int index, FrameworkElement item, RoutedEventHandler leftClickHandler)
		{
			return InsertItem(index, null, item, null, null, leftClickHandler);
		}

		public PopupMenuItem InsertItem(int index, bool showLeftMargin, FrameworkElement item, RoutedEventHandler leftClickHandler)
		{
			PopupMenuItem pmi = InsertItem(index, null, item, null, null, leftClickHandler);
			pmi.ShowLeftMargin = showLeftMargin;
			return pmi;
		}

		public PopupMenuItem InsertItem(int index, string leftIconUrl, FrameworkElement item, string tag, RoutedEventHandler leftClickHandler)
		{
			return InsertItem(index, leftIconUrl, item, null, null, leftClickHandler);
		}

		public PopupMenuItem InsertItem(int index, string leftIconUrl, string header, string rightIconUrl, string tag, RoutedEventHandler leftClickHandler)
		{
			return InsertItem(index, leftIconUrl, new TextBlock() { Text = header, Tag = tag }, rightIconUrl, null, leftClickHandler);
		}

		public PopupMenuItem InsertItem(int index, string leftIconUrl, string header, string rightIconUrl, string tag, string name, RoutedEventHandler leftClickHandler)
		{
			return InsertItem(index, leftIconUrl, new TextBlock() { Text = header, Tag = tag }, rightIconUrl, name, leftClickHandler);
		}

		#endregion InsertItem

		public PopupMenuItem AddSubMenu(PopupMenuBase subMenu, string header, string rightIconUrl, string tag, string name, bool closeOnClick, RoutedEventHandler clickHandler)
		{
			return InsertSubMenu(-1, subMenu, header, rightIconUrl, tag, name, closeOnClick, clickHandler);
		}

		public PopupMenuItem InsertSubMenu(int index, PopupMenuBase subMenu, string header, string rightIconUrl, string tag, string name, bool closeOnClick, RoutedEventHandler clickHandler)
		{
			PopupMenuItem pmi = InsertItem(index, null, new TextBlock() { Text = header, Tag = tag }, rightIconUrl, name, null);
			pmi.CloseOnClick = closeOnClick;
			subMenu.Orientation = MenuOrientationTypes.Right;
			subMenu.AddTrigger(TriggerTypes.Hover, pmi);
			return pmi;
		}

		public PopupMenuItem InsertItem(int index, string leftIconUrl, FrameworkElement item, string rightIconUrl, string name, RoutedEventHandler clickHandler)
		{
			if (item.Parent != null)
				(item.Parent as Panel).Children.Remove(item);

			PopupMenuItem pmi = item is PopupMenuItem
				? item as PopupMenuItem
				: new PopupMenuItem(leftIconUrl, item);

			if (clickHandler != null)
				pmi.Click += clickHandler;

			if (rightIconUrl != null)
				pmi.ImageRightSource = new BitmapImage(new Uri(rightIconUrl, UriKind.RelativeOrAbsolute));

			if (name != null)
			{
				if (ItemsControl.Items.OfType<FrameworkElement>().Where(i => i.Name == name).Count() == 0)
					pmi.Name = name;
				else
					throw new ArgumentException("An item named " + name + " already exists in the PopupMenu " + this.Name);
			}

			ItemsControl.Items.Insert(index == -1 ? ItemsControl.Items.Count : index, pmi);

			return pmi;
		}

		public void RemoveAt(int index)
		{
			ItemsControl.Items.RemoveAt(index);
		}

		public void Remove(Control item)
		{
			ItemsControl.Items.Remove(item);
		}

		public PopupMenuSeparator AddSeparator()
		{
			return AddSeparator(null);
		}

		public PopupMenuSeparator AddSeparator(string tag)
		{
			var separator = new PopupMenuSeparator { Tag = tag };
			ItemsControl.Items.Add(separator);
			return separator;
		}

		public PopupMenuSeparator InsertSeparator(int index, string tag)
		{
			var separator = new PopupMenuSeparator { Tag = tag };
			ItemsControl.Items.Insert(index, separator);
			return separator;
		}

		public Control GetItemContainerByIndex(int index)
		{
			return (Control)(ItemsControl.ItemContainerGenerator.ContainerFromItem(ItemsControl.Items[index]));
		}

		/// <summary>
		/// Gets a PopupMenuItem by its name.
		/// </summary>
		/// <param name="name">The name for the PopupMenuItem.</param>
		/// <returns></returns>
		public PopupMenuItem PopupMenuItem(string name)
		{
			return GetItem<PopupMenuItem>(name);
		}

		/// <summary>
		/// Gets a PopupMenuItem by its index.
		/// </summary>
		/// <param name="name">The index for the PopupMenuItem.</param>
		/// <returns></returns>
		public PopupMenuItem PopupMenuItem(int index)
		{
			return GetItem<PopupMenuItem>(index);
		}

		/// <summary>
		/// Get the object of type T in the menu control by index position.
		/// </summary>
		/// <typeparam name="T">The object type.</typeparam>
		/// <param name="index">The object index.</param>
		public T GetItem<T>(int index) where T : FrameworkElement
		{
			T item = (ItemsControl.Items[index] as FrameworkElement).GetVisualDescendantsAndSelf().OfType<T>()
																	.FirstOrDefault();

			if (item == default(T))
				throw new Exception(string.Format("{0} at item {1} is not of type {2}", ItemsControl.Items[index].GetType(), index, typeof(T).ToString()));
			else
				return item;
		}

		/// <summary>
		/// Get the object of type T in the menu control by its name.
		/// </summary>
		/// <typeparam name="T">The object type.</typeparam>
		/// <param name="index">The name of the object.</param>
		public T GetItem<T>(string name) where T : FrameworkElement
		{
			return Items.GetVisualDescendantsAndSelf().OfType<T>()
						.Where(i => i.Name == name)
						.First();
		}
		/// <summary>
		/// Find the container control having elements with a specific tag value.
		/// This method only works after the visual tree has been created.
		/// </summary>
		public Control FindItemContainerByTag(object tag)
		{
			return FindItemContainersByTag(tag).FirstOrDefault();
		}

		/// <summary>
		/// Get a list of container controls having elements with a specific tag value.
		/// This method only works after the visual tree has been created.
		/// </summary>
		public IEnumerable<Control> FindItemContainersByTag(object tag)
		{
			return base.FindItemsByTag<FrameworkElement>(tag)
					   .Select(elem => PopupMenuUtils.GetItemContainer(ItemsControl, elem))
					   .Where(c => c != null);
		}

		public T FindItemByTag<T>(object tag) where T : FrameworkElement
		{
			return FindItemsByTag<T>(tag).FirstOrDefault();
		}

		/// <summary>
		/// Find the containing control for the first control having a name matching a regex pattern.
		/// This method only works after the visual tree has been created.
		/// </summary>
		/// <param name="regexSelector">The regex pattern to match the element name</param>
		public Control FindItemContainerByName(string regexSelector)
		{
			return base.FindItemsByName<FrameworkElement>(regexSelector)
					   .Select(i => PopupMenuUtils.GetItemContainer(ItemsControl, i)).FirstOrDefault();
		}

		/// <summary>
		/// Find the containers for each control having a name matching a regex pattern.
		/// This method only works after the visual tree has been created.
		/// </summary>
		/// <param name="regexSelector">The regex pattern to match the element names</param>
		public IEnumerable<Control> FindItemContainersByName(string regexSelector)
		{
			return base.FindItemsByName<FrameworkElement>(regexSelector)
					   .Select(i => PopupMenuUtils.GetItemContainer(ItemsControl, i)).ToList();
		}

		/// <summary>
		/// Set the visibility for all item containers containing an element
		/// having the specified tag.
		/// </summary>
		/// <param name="tag">The filter tag.</param>
		/// <param name="visible">The visibility of the container for matching containers.</param>
		public void SetContainerVisibilityByTag(string tag, bool visible)
		{
			foreach (var container in FindItemContainersByTag(tag))
				container.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
		}

	}
}