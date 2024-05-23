// Copyright (c) 2009 Ziad Jeeroburkhan. All Rights Reserved.
// GNU Library General Public License (LGPL) 
// (http://sl4popupmenu.codeplex.com/license)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Reflection;
using System.Diagnostics;

namespace SL4PopupMenu
{
	/// <summary>
	/// This class manages all PopupMenus within the application and manages any registered 
	/// keyboard shortcuts.
	/// </summary>
	public static class PopupMenuManager
	{
		//public static bool Debug { get; set; }
		private static bool _initialized;

		private static bool _isKeyboardCaptureEnabled;

		/// <summary>
		/// Allow the menu manager to listen to keyboard events.
		/// </summary>
		public static bool IsKeyboardCaptureEnabled
		{
			get
			{
				return _isKeyboardCaptureEnabled;
			}
			set
			{
				if (_isKeyboardCaptureEnabled != value)
				{
					_isKeyboardCaptureEnabled = value;

					var rootVisual = Application.Current.RootVisual as FrameworkElement;

					if (value)
					{
						// Add keyboard event handlers
						rootVisual.KeyDown += AppRoot_KeyDown;
						rootVisual.KeyUp += AppRoot_KeyUp;
					}
					else
					{
						// Remove keyboard event handlers
						rootVisual.KeyDown -= AppRoot_KeyDown;
						rootVisual.KeyUp -= AppRoot_KeyUp;
					}
				}
			}
		}

		/// <summary>
		/// A list of all menus in the application with their associated trigger elements.
		/// </summary>
		public static List<MenuTriggerRecord> MenuTriggers = new List<MenuTriggerRecord>();

		/// <summary>
		/// The list of menus actually open.
		/// </summary>
		public static List<PopupMenuBase> OpenMenus = new List<PopupMenuBase>();

		/// <summary>
		/// The list of menu items having a shortcut key.
		/// </summary>
		public static Dictionary<string, WeakReference> Shortcuts = new Dictionary<string, WeakReference>();

		internal static MouseEventArgs TopOverlayMouseEventArgs { get; set; }

		/// <summary>
		/// The neighbouring left click element being hovered, if any.
		/// This is only used after a left click menu has already been fired.
		/// </summary>
		public static FrameworkElement NeighbouringLeftClickElementUnderMouse;

		/// <summary>
		/// Adds the logic to clear the global menu states when the navigation state is changed.
		/// This is meant to be called only once during the application lifetime.
		/// </summary>
		/// <returns>True when successful. False when the iframe with id _sl_historyFrame that is used
		/// to manage the navigation state is not found in the DOM.</returns>
		private static bool Initialize()
		{
			IsKeyboardCaptureEnabled = true;
			//try
			//{
			// Always reset the global shortcut list whenever the navigation state changes.
			Application.Current.RootVisual.Dispatcher.BeginInvoke(delegate
			{
				Application.Current.Host.NavigationStateChanged += delegate
				{
					Reset();
				};
			});
			//}
			//// An error may occur if the host page does not contain an iframe with id _sl_historyFrame. In this case Reset has to be called manually.
			//catch (Exception e) 
			//{
			//	MessageBox.Show(e.Message + "\n" + e.InnerException + "\n\n" + e.StackTrace, "SL4PopupMenu", MessageBoxButton.OK);
			//}
			return true;
		}

		/// <summary>
		/// Clear the global menu records from the application.
		/// </summary>
		public static void Reset()
		{
			Shortcuts.Clear();
			MenuTriggers.Clear();
			CloseChildMenus(0, false, true, null);
		}

		/// <summary>
		/// Add a menu and its associated trigger element to the global menu list.
		/// </summary>
		/// <param name="triggerElement">The trigger element associated with the menu.</param>
		/// <param name="triggerType">The trigger type used with the associated trigger element.</param>
		/// <param name="targetMenu">The menu targeted by the specified trigger element.</param>
		/// <returns>The registered MenuTriggerRecord or a null value if the record has already
		/// been registered before.</returns>
		public static MenuTriggerRecord RegisterMenu(FrameworkElement triggerElement, TriggerTypes triggerType, PopupMenuBase targetMenu)
		{
			if (MenuTriggers.ToArray().Where(mt =>
				mt.PopupMenuBase == targetMenu
				&& mt.TriggerType == triggerType
				&& mt.TriggerElement == triggerElement).FirstOrDefault() == null)
			{
				if (!_initialized)
					_initialized = Initialize();

				var menuTriggerRecord = new MenuTriggerRecord
				{
					PopupMenuBase = targetMenu,
					TriggerType = triggerType,
					TriggerElement = triggerElement
				};

				if (targetMenu.ShortcutTargetElement == null)
					MenuTriggers.Add(menuTriggerRecord);
				else
					MenuTriggers.Insert(0, menuTriggerRecord);

				return menuTriggerRecord;
			}

			return null;
		}


		/// <summary>
		/// Unregister a menu to allow the garbage collector to free up memory taken by it.
		/// </summary>
		/// <param name="menu">The PopupMenu to be unregistered.</param>
		public static void UnregisterMenu(PopupMenuBase menu)
		{
			var sm = Shortcuts.Values.OfType<PopupMenuBase>().Where(m => m == menu).FirstOrDefault();
			if (sm != null)
				Shortcuts.Remove(PopupMenuUtils.GetShortcutAsText(sm.ShortcutKey, sm.ShortcutKeyModifier1 | sm.ShortcutKeyModifier2));

			var menuTriggers = PopupMenuManager.MenuTriggers
					.Where(mt => mt != null && mt.PopupMenuBase == menu).ToArray();

			foreach (var mt in menuTriggers)
			{
				menu.RemoveTrigger(mt.TriggerElement, true);
				MenuTriggers.Remove(mt);
			}
		}

		/// <summary>
		/// Register a shortcut with its associated dependency object in the global dictionary Shortcuts.
		/// </summary>
		/// <param name="uniqueShortcut">The shortcut. It must be unique.</param>
		/// <param name="dp">The associated dependency property to be triggered(only 
		/// PopupMenuBase and PopupMenuItem types supported as yet).</param>
		public static void RegisterShortcut(string uniqueShortcut, DependencyObject dp)
		{
			if (!_initialized)
				_initialized = Initialize();

			if (!uniqueShortcut.EndsWith("None"))
			{
				if (Shortcuts.ContainsKey(uniqueShortcut))
				{
					if (!DesignerProperties.IsInDesignTool)
					{
						throw new ArgumentException("Shorcut " + uniqueShortcut + " is already taken by another object."
							+ " To avoid this error either use a different shortcut or use the PopupManager.Reset() method to clear all previous menu records.");
					}
					return;
				}

				Shortcuts.Add(uniqueShortcut, new WeakReference(dp));
			}
		}

		internal static void AppRoot_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Escape && OpenMenus.Count > 0)
			{
				CloseChildMenus(OpenMenus.First().CloseDuration, false, false, null);
			}
			else
			{
				string pressedKey = PopupMenuUtils.GetShortcutAsText(e.Key, Keyboard.Modifiers);
				if (Shortcuts.ContainsKey(pressedKey))
				{
					e.Handled = true;
					var dpo = Shortcuts[pressedKey];
					if (dpo != null)
					{
						// If the shorcut belongs to a PopupMenuItem call its Click event handler.
						if (dpo.Target is PopupMenuItem)
						{
							var menuItem = dpo.Target as PopupMenuItem;
							if (menuItem.IsEnabled)
								menuItem.OnClick();
						}
						// Else if the shortcut belongs to a popup menu then open it.
						else if (dpo.Target is PopupMenuBase)
						{
							var menu = dpo.Target as PopupMenuBase;

							// Call the AccesskeyPressed event handler if available.
							if (menu.ShortcutPressed != null)
								menu.ShortcutPressed(menu, e);

							// Open the menu next to AccessKeyTarget
							if (menu.IsShortcutFocusOnly)
								menu.OpenNextTo(menu.Orientation, menu.ShortcutTargetElement, true, true);

							// Set focus on the parent element if it is a control.
							if (menu.ShortcutTargetElement is Control)
								(menu.ShortcutTargetElement as Control).Focus();
						}
					}
				}
			}
		}

		internal static void AppRoot_KeyUp(object sender, KeyEventArgs e)
		{
			if (PopupMenuManager.OpenMenus.Count > 0)
			{
				if (e.Key == Key.Down || e.Key == Key.Up || e.Key == Key.Left || e.Key == Key.Right)
					if (e.Key != OpenMenus.First().FlyoutKey)
						CloseTopMenu(e);
			}
		}

		/// <summary>
		/// Close the last open menu after a keystroke.
		/// </summary>
		/// <param name="e">The KeyEventArgs associated with the keystroke.</param>
		public static void CloseTopMenu(KeyEventArgs e)
		{
			if (OpenMenus.Count > 0)
			{
				e.Handled = true;
				OpenMenus.First().Close(0);
			}
		}

		/// <summary>
		/// Closes all menus except for the one being hovered(or whose trigger element is being hovered) and its parents.
		/// </summary>
		/// <param name="closeDuration">The animation duration for closing the menus.</param>
		/// <param name="closeHoverMenusOnly">Close hover menus only.</param>
		/// <param name="closePinnedMenus">Close pinned menus as well.</param>
		/// <param name="e">The MouseEventArgs value used to get the mouse position. It is used to check which menus are
		/// under the mouse so as to avoid closing them. A null value cancels the verification.</param>
		public static void CloseChildMenus(int closeDuration, bool closeHoverMenusOnly, bool closePinnedMenus, MouseEventArgs e)
		{
			foreach (PopupMenuBase menu in OpenMenus.ToList())
			{
				if ((e == null || !PopupMenuUtils.HitTestAny(e, false, true, menu.ContentRoot, menu.ActualTriggerElement)))
				{
					if (!menu.IsModal && (menu.ActualTriggerType == TriggerTypes.Hover || !closeHoverMenusOnly))
						menu.Close(closeDuration, closePinnedMenus);
				}
				else
				{
					break;
				}
			}
		}
	}
}