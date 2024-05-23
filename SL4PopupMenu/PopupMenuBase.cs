// Copyright (c) 2009 Ziad Jeeroburkhan. All Rights Reserved.
// GNU Library General Public License (LGPL) 
// (http://sl4popupmenu.codeplex.com/license)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Windows.Data;

namespace SL4PopupMenu
{
	public enum TriggerTypes { LeftClick, RightClick, Hover }

	public enum MenuOrientationTypes { Left, Top, Right, Bottom, MouseBottomRight, None }

	//[StyleTypedProperty(Property = "Style", StyleTargetType = typeof(PopupMenu))]
	//[TemplatePart(Name = "RootElement", Type = typeof(FrameworkElement))]	

	/// <summary>
	/// <para>A content control which confers the PopupMenu behavior onto any element placed within it.</para>
	/// <para>It successfully tackles all ZIndex issues by placing its content within the canvas element OverlayCanvas, 
	/// itself placed within the Popup control OverlayPopup, the first time the PopupMenu loads.
	/// Once open OverlayCanvas is stretched in the background across the viewport so as to track outer 
	/// mouse events. This is however not perceivable by the user since its background is set as transparent.</para>
	/// <para>Note that since this class only provides behavioral features visual properties must be set in its content
	/// elements instead.</para>
	/// </summary>
	public partial class PopupMenuBase : ContentControl
	{
		#region Hidden Properties

		private new Brush Background;
		private new Brush Foreground;

		#endregion

		#region Properties

		//public Theme Theme { get; set; }

		//public Uri ThemeUri
		//{
		//    get { return Theme.ThemeUri; }
		//    set { Theme.ThemeUri = value; }	//OverlayCanvas.GetVisualChildrenAndSelf().OfType<UIElement>().ToList().ForEach(el => el.UpdateLayout());
		//}

		/// <summary>
		/// The shortcut key used to open the menu next to AccessKeyTarget.
		///	Note that the menu uses the ShortcutTargetElement or ShortcutTargetElementName properties for positioning
		///	once the shortcut is pressed./>
		/// </summary>
		public Key ShortcutKey { get; set; }

		/// <summary>
		/// The first modifier key used to open the menu next to AccessKeyTarget.
		/// </summary>
		public ModifierKeys ShortcutKeyModifier1 { get; set; }

		/// <summary>
		/// The second modifier key used to open the menu next to AccessKeyTarget.
		/// </summary>
		public ModifierKeys ShortcutKeyModifier2 { get; set; }

		/// <summary>
		/// A string representing the shortcut key used to shortcut the menu item e.g Ctrl+Alt+X.
		/// </summary>
		public string Shortcut
		{
			get
			{
				return PopupMenuUtils.GetShortcutAsText(ShortcutKey, ShortcutKeyModifier1 | ShortcutKeyModifier2);
			}
			set
			{
				var keys = PopupMenuUtils.GetShortcutValues(value);
				ShortcutKey = (Key)keys[0];
				ShortcutKeyModifier1 = (ModifierKeys)keys[1];
				ShortcutKeyModifier2 = (ModifierKeys)keys[2];
			}
		}

		FrameworkElement _shortcutTargetElement;
		/// <summary>
		/// The target element next to which the menu will popup when the shortcut is pressed.
		/// The default value is the first trigger element used by the PopupMenu.
		/// </summary>
		public FrameworkElement ShortcutTargetElement
		{
			get
			{
				if (_shortcutTargetElement == null)
				{
					if (!string.IsNullOrEmpty(ShortcutTargetElementName))
					{
						_shortcutTargetElement = PopupMenuUtils.FindElementByName(this, ShortcutTargetElementName, "shortcut key");
					}
					else
					{
						var firstMenuTrigger = PopupMenuManager.MenuTriggers.Where(mt => mt.PopupMenuBase == this).FirstOrDefault();
						if (firstMenuTrigger != null)
							ShortcutTargetElement = firstMenuTrigger.TriggerElement;
					}
				}
				return _shortcutTargetElement;
			}

			set
			{
				_shortcutTargetElement = value;
				ShortcutTargetElementName = _shortcutTargetElement.Name;
			}
		}

		/// <summary>
		/// The name of the target element next to which the menu will popup when the shortcut is pressed.
		/// The default value is the first trigger element used by the PopupMenu.
		/// </summary>
		public string ShortcutTargetElementName { get; set; }

		/// <summary>
		/// The event called when the menu shortcut key is pressed.
		/// </summary>
		public KeyEventHandler ShortcutPressed;

		/// <summary>
		/// Prevent the menu from opening when the shortcut is pressed. 
		/// When true the menu is only given focus so as to enable keyboard navigation.
		/// </summary>
		public bool IsShortcutFocusOnly { get; set; }

		/// <summary>
		/// Restore the focus on the trigger element when the menu is closed.
		/// </summary>
		protected bool RestoreFocusOnClose;

		/// <summary>
		/// Determines if the parent menu, if any, must be closed when current menu is closed.
		/// </summary>
		/// <remarks>Its value is set to true when the escape key is pressed to avoid parent menus being closed.
		/// It is however reverted back to false once the menu is closed.</remarks>
		protected bool KeepParentMenusOpen;

		private Key _flyoutKey;
		/// <summary>
		/// The key used to open the menu when the trigger element has focus. Default value is the
		/// cursor key matching the menu Orientation but it can be disabled by setting it as Key.None.
		/// </summary>
		public Key FlyoutKey
		{
			get
			{
				return _flyoutKey == Key.None ? (Key)((int)Key.Left + (int)Orientation) : _flyoutKey;
			}
			set
			{
				_flyoutKey = value;
			}
		}

		//public bool OverlayPopupIsOpen
		//{
		//    get { return OverlayCanvas.Visibility == Visibility.Visible; }
		//    set
		//    {
		//        OverlayCanvas.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
		//        if (PopupMenuManager.OpenMenus.Count > 0 || value)
		//        {
		//            PopupMenuBase.OverlayPopup.IsOpen = true;
		//        }
		//        else
		//        {
		//            PopupMenuBase.OverlayPopup.IsOpen = false;
		//        }
		//    }
		//}

		/// <summary>
		/// The parent PopupMenuItem for the current menu if any.
		/// </summary>
		public PopupMenuItem ParentPopupMenuItem;

		/// <summary>
		/// The Popup control used to make OverlayCanvas the topmost element in the application.
		/// Its IsOpen property can be used to determine if the menu is open on not.
		/// </summary>
		/// <seealso cref="SL4PopupMenu.PopupMenuBase"/>
		public Popup OverlayPopup { get; set; }

		public Brush OverlayCanvasBackground
		{
			get { return OverlayCanvas.Background; }
			set { OverlayCanvas.Background = value; }
		}

		/// <summary>
		/// The canvas used to contain all menu elements. It lives inside OverlayPopup at runtime.
		/// </summary>
		/// <seealso cref="SL4PopupMenu.PopupMenuBase"/>
		public Canvas OverlayCanvas { get; set; }


		/// <summary>
		/// The element used as trigger element when hovered.
		/// </summary>
		public UIElement HoverElement { get; set; }

		/// <summary>
		/// The element used as trigger element when left clicked.
		/// </summary>
		/// 
		public UIElement LeftClickElement { get; set; }

		/// <summary>
		/// The element used as trigger element when right clicked.
		/// </summary>
		public UIElement RightClickElement { get; set; }

		/// <summary>
		/// A comma separated list of regex patterns used to identify hover trigger elements in the application.
		/// To match elements by their tag instead you only have to prefix the selector with a dot. 
		/// The terms ".." or "[Parent]" however are reserved words used to refer to the parent element.
		/// <example><para>e.g The selector "btn.*,.image" will target all elements with names starting with 'btn' or
		/// tagged with the word 'image'.</para></example>
		/// Note that newly created trigger elements will not be caught until the UpdateTriggers method is called.
		/// </summary>
		public string HoverElementSelector { get; set; }

		/// <summary>
		/// A comma separated list of regex patterns used to identify left click trigger elements in the application.
		/// To match elements by their tag instead you only have to prefix the selector with a dot. 
		/// The terms ".." or "[Parent]" however are reserved words used to refer to the parent element.
		/// <example><para>e.g The selector "btn.*,.image" will target all elements with names starting with 'btn' or
		/// tagged with the word 'image'.</para></example>
		/// Note that newly created trigger elements will not be caught until the UpdateTriggers method is called.
		/// </summary>
		public string LeftClickElementSelector { get; set; }

		/// <summary>
		/// A comma separated list of regex patterns used to identify right click trigger elements in the application.
		/// To match elements by their tag instead you only have to prefix the selector with a dot. 
		/// The terms ".." or "[Parent]" however are reserved words used to refer to the parent element.
		/// <example><para>e.g The selector "btn.*,.image" will target all elements with names starting with 'btn' or
		/// tagged with the word 'image'.</para></example>
		/// Note that newly created trigger elements will not be caught until the UpdateTriggers method is called.
		/// </summary>
		public string RightClickElementSelector { get; set; }

		/// <summary>
		/// All the elements in the visual tree hierarchy under the mouse when the trigger element was last clicked or hovered.
		/// </summary>
		public IEnumerable<UIElement> ElementsUnderMouse { get; set; }

		/// <summary>
		/// The type of the trigger element that fired up the menu
		/// </summary>
		public TriggerTypes ActualTriggerType;

		/// <summary>
		/// The trigger element that fired up the menu.
		/// </summary>
		public FrameworkElement ActualTriggerElement { get; set; }

		/// <summary>
		/// Returns true after the visual tree for the PopupMenu has been created.
		/// </summary>
		public bool IsVisualTreeGenerated { get; set; }

		/// <summary>
		/// This storyboard can be used to override the default fade in animation(applied to ContentRoot) when opening the menu.
		/// </summary>
		public Storyboard OpenAnimation { get; set; }

		/// <summary>
		/// The time in milliseconds after which the menu will open after its trigger element is hovered.
		/// </summary>
		public int OpenDelay { get; set; }

		/// <summary>
		/// The time in milliseconds the menu open animation takes to complete.
		/// </summary>
		public int OpenDuration { get; set; }

		/// <summary>
		/// True when the menu is opening or the menu open animation is still running.
		/// </summary>
		public bool IsOpening { get; set; }

		/// <summary>
		/// Prevents the menu from opening once only. Since its value automatically reverts back 
		/// to false afterwards it is typically used in the Opening event to prevent the menu from
		/// opening. To permanently disable the menu please use IsEnabled property instead.
		/// </summary>
		public bool IsOpeningCancelled { get; set; }

		/// <summary>
		/// The closing animation storyboard. 
		/// Use it to replace the default fade out animation applied to ContentRoot.
		/// </summary>
		public Storyboard CloseAnimation { get; set; }

		/// <summary>
		/// The time in milliseconds after which the menu is closed after the mouse is moved outside its bounds.
		/// </summary>
		public int CloseDelay { get; set; }

		/// <summary>
		/// The time in milliseconds the menu close animation takes to complete.
		/// </summary>
		public int CloseDuration { get; set; }

		/// <summary>
		/// True when the menu is closing or the menu close animation is still running.
		/// </summary>
		public bool IsClosing { get; set; }

		private Timer _timerOpen;

		private Timer _timerClose;

		private bool _clickAlreadyHandledOnMouseDown;

		/// <summary>
		/// When true focus is set on the menu when shown.
		/// However this may have an unwanted side effect notably the loss any text selection on textboxes after opening the menu.
		/// </summary>
		public bool FocusOnShow { get; set; }

		public bool FocusTriggerElementOnShow { get; set; }

		/// <summary>
		/// Determines if a hover menu is allowed to close when hovering outside the menu.
		/// Default is true.
		/// </summary>
		private bool CloseOnHoverOut { get; set; }

		private Rectangle _borderMask;

		/// <summary>
		/// A Rectangle that masks the area between the trigger and the menu element.
		/// It only helps in visually blending both elements.
		/// </summary>
		protected Rectangle BorderMask
		{
			get
			{
				if (_borderMask == null && BorderMaskFill != null)
				{
					_borderMask = new Rectangle { VerticalAlignment = VerticalAlignment.Top, HorizontalAlignment = HorizontalAlignment.Left };
					OverlayCanvas.Children.Add(_borderMask);
				}
				return _borderMask;
			}
		}

		/// <summary>
		/// The brush used by BorderMask. This would typically be a brush between the menu and
		/// the trigger element so as to visually blend both together. The default is null.
		/// </summary>
		public Brush BorderMaskFill
		{
			get { return (Brush)GetValue(BorderMaskFillProperty); }
			set { SetValue(BorderMaskFillProperty, value); }
		}

		public static readonly DependencyProperty BorderMaskFillProperty =
			DependencyProperty.Register("BorderMaskFill", typeof(Brush), typeof(PopupMenuBase), new PropertyMetadata(null));

		/// <summary>
		/// The thickness for BorderGrid. This would depend on the border thickness between the menu
		/// and its trigger element.
		/// </summary>
		public Thickness? BorderMaskThickness { get; set; }

		/// <summary>
		/// When true the menu can only be closed by clicking on any of its trigger elements.
		/// This also prevents OverlayCanvas from stretching out on top of the application, which 
		/// is normally the case while the menu is open, thus allowing surrounding elements to 
		/// receive mouse events without needing to close it first.
		/// </summary>
		public bool IsPinned
		{
			get { return (bool)GetValue(IsPinnedProperty); }
			set { SetValue(IsPinnedProperty, value); }
		}

		public static readonly DependencyProperty IsPinnedProperty =
			DependencyProperty.Register("IsPinned", typeof(bool), typeof(PopupMenuBase), new PropertyMetadata(false, (sender, e) =>
			{
				var pmb = sender as PopupMenuBase;
				if (pmb.OverlayCanvas != null)
					pmb._isOverlayCanvasExpanded = !pmb.IsPinned;

			}));

		/// <summary>
		/// Determines whether the IsPinned property can be toggled by clicking on a trigger element
		/// provided it is of hover type.
		/// </summary>
		public bool IsAutoPinnable { get; set; }

		/// <summary>
		///  Sets the expanded state for OverlayCanvas.
		///  When true it is stretched all over the application. Otherwise it is collapsed such that it can no
		///  longer captures any mouse clicks.
		/// </summary>
		private bool _isOverlayCanvasExpanded
		{
			set
			{
				OverlayCanvas.Width = value ? Application.Current.Host.Content.ActualWidth : double.NaN;
				OverlayCanvas.Height = value ? Application.Current.Host.Content.ActualHeight : double.NaN;
			}
		}

		private bool _isModal;

		/// <summary>
		/// When true the menu will behave as a modal menu.
		/// </summary>
		public bool IsModal
		{
			get { return _isModal; }
			set
			{
				_isModal = value;
				OverlayCanvas.Background = _isModal
					? (ModalBackground ?? new SolidColorBrush(Color.FromArgb(100, 100, 100, 100)))
					: new SolidColorBrush(Colors.Transparent);
			}
		}

		/// <summary>
		/// The overlay background used when in modal mode.
		/// </summary>
		public Brush ModalBackground { get; set; }

		/// <summary>
		/// This event is called when the menu is being opened.
		/// It is actually the only event where submenus can be safely added through the AddSubMenu function.
		/// </summary>
		public event RoutedEventHandler Opening;

		/// <summary>
		/// This event is called after the menu is open but is still at the initial phase of its storyboard.
		/// If addition of submenu items is intended here then it is recommended to use the Opening event handler
		/// instead since references to those elements may have already been broken by the layouting process by
		/// the time this event is called.</summary>
		public event RoutedEventHandler Showing;

		/// <summary>
		/// This event is called when the storyboard animation used to display the menu has completed.
		/// </summary>
		public event RoutedEventHandler Shown;

		/// <summary>
		/// This event is called when the menu is closing.
		/// </summary>
		public event RoutedEventHandler Closing;

		/// <summary>
		/// This event is called when the mouse is clicked outside the menu.
		/// </summary>
		public event RoutedEventHandler OuterClick;

		public double OffsetX { get; set; }

		public double OffsetY { get; set; }

		/// <summary>
		/// The menu orientation relative to its trigger element.
		/// Default is MenuOrientationTypes.Bottom.
		/// </summary>
		public MenuOrientationTypes Orientation { get; set; }

		/// <summary>
		/// Enable the shadow effect for all elements added to OverlayCanvas.
		/// Default is true.
		/// </summary>
		public bool EnableShadowEffect { get; set; }

		bool _autoSelectItem = true;

		/// <summary>
		/// Automatically select ListBoxItem when hovered or clicked.
		/// </summary>
		public bool AutoSelectItem
		{
			get { return _autoSelectItem; }
			set { _autoSelectItem = value; }
		}

		/// <summary>
		/// Map the ActualTriggerElement property to the selected item of the trigger element,
		/// if any, provided it derives from a Selector such as a ListBox, ComboBox or DataGrid.
		/// Default is true. Note that when InheritDataContext is true the data context is
		/// pointed to the data context of the selected item within the trigger element instead.
		/// Default is true.
		/// </summary>
		public bool IsTriggerElementMappedToSelectedItem { get; set; }

		/// <summary>
		/// Inherit the datacontext from the trigger element each time the menu is open. Note that
		/// when IsTriggerElementMappedToSelectedItem is true the latter is always set as the 
		/// selected item of the trigger element instead.
		/// </summary>
		public bool InheritDataContext { get; set; }

		/// <summary>
		/// Whenever a hover trigger element is identified as a TextBlock with no definite width
		/// use its parent bounds for positioning to avoid the latter varying with the text length. 
		/// </summary>
		public bool UseParentHoverBoundsForWidthlessTextBlockTriggers { get; set; }

		/// <summary>
		/// Returns the OverlayCanvas content which would initially be the local content since the latter is 
		/// moved to OverlayCanvas at runtime. The relocation process can however break up any markup references
		/// directly targeting it. Due to this themes have to be placed within the local content to be effective.
		/// </summary>
		public FrameworkElement ContentRoot
		{
			get
			{
				if (this.Content is FrameworkElement)
					ContentRoot = this.Content as FrameworkElement; // Use the setter to move the content to OverlayCanvas

				return OverlayCanvas.GetVisualChildren().OfType<FrameworkElement>().FirstOrDefault();
			}
			set // Any element set as ContentRoot is made a child of OverlayCanvas.
			{
				PopupMenuUtils.MoveElementTo(value, OverlayCanvas, EnableShadowEffect, true);
			}
		}

		#endregion Properties

		public PopupMenuBase()
			: this(null)
		{ }

		public PopupMenuBase(FrameworkElement contentRoot)
		{
			//this.DefaultStyleKey = typeof(PopupMenu);
			//this.ApplyTemplate();

			// Default values
			Orientation = MenuOrientationTypes.Bottom;
			IsTriggerElementMappedToSelectedItem = true;
			EnableShadowEffect = true;
			CloseOnHoverOut = true;
			//UseParentHoverBoundsForWidthlessTextBlockTriggers = true;
			OpenDelay = 200;
			CloseDelay = 300;
			OpenDuration = 150;
			CloseDuration = 110;

			// OverlayCanvas is stretched across the window to capture outer mouse events(for some 
			// reason a Background value is required to activate its auto stretch behavior) and is 
			// itself contained by OverlayPopup whose purpose is to make it the top most element.		
			OverlayPopup = new Popup
			{
				Child = OverlayCanvas = new Canvas { Background = new SolidColorBrush(Colors.Transparent) } // Opacity is set to 0 each time menu is open.
			};


			// Note that all content of our PopupMenu is moved to OverlayCanvas at runtime and this can 
			// disrupt any references already there in markup code.
			if (contentRoot != null)
				ContentRoot = contentRoot;

			if (!DesignerProperties.IsInDesignTool)
			{
				this.Visibility = Visibility.Collapsed;
				// Update the triggers if the menu is loaded again
				this.Loaded += delegate
				{
					// The parent element is a PopupMenuItem.
					if (this.Parent is PopupMenuItem)
					{
						// Set it as a  hover trigger element through ParentPopupMenuItem.
						ParentPopupMenuItem = this.Parent as PopupMenuItem;
						// Disable its 'close on click' behavior.
						ParentPopupMenuItem.CloseOnClick = false;

						// Tell the menu to open on the right side of its parent. 
						if (Orientation == MenuOrientationTypes.Bottom)
							Orientation = MenuOrientationTypes.Right;
					}
					else
					{
						ParentPopupMenuItem = null;
					}

					string shortcut = PopupMenuUtils.GetShortcutAsText(ShortcutKey, ShortcutKeyModifier1 | ShortcutKeyModifier2);
					PopupMenuManager.RegisterShortcut(shortcut, this);

					UpdateTriggers();
				};


				this.Unloaded += delegate
				{
					ClearTriggers();
				};

				if (!IsVisualTreeGenerated)
					OverlayCanvas.Dispatcher.BeginInvoke(delegate
					{
						AddOverlayCanvasEventHandlers();
					});
			}
		}


		~PopupMenuBase()
		{
			this.ClearTriggers();
		}

		/*/// <summary>
		/// Called when the template's tree is generated.
		/// </summary>
		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();
			OverlayPopup = GetTemplateChild("OverlayPopup") as Popup;
			OverlayCanvas = GetTemplateChild("OverlayCanvas") as Canvas;
			//ContentRoot = GetTemplateChild("ContentRoot") as Grid;
		}*/

		/// <summary>
		/// Identifies all trigger elements within the visual tree from the specified element names or selectors and
		/// adds their respective event handlers depending on the trigger type. Note that only previously unregistered
		/// trigger associations are added using this method.
		/// <para>Therefore this method is meant to be called only when new target elements, that can potentially match the 
		/// specified element selectors, are added to the visual tree.</para>
		/// </summary>
		public void UpdateTriggers()
		{
			//// Target parent element by default if not is specified.
			//if (HoverElement == null && RightClickElement == null && LeftClickElement == null
			//    && HoverElementSelector == null && RightClickElementSelector == null && LeftClickElementSelector == null 
			//    && this.Parent != null)
			//    AddTrigger(TriggerTypes.RightClick, "[Parent]");

			if (ParentPopupMenuItem != null)
				AddTrigger(TriggerTypes.Hover, ParentPopupMenuItem.Container ?? ParentPopupMenuItem);

			if (HoverElement != null)
				AddTrigger(TriggerTypes.Hover, HoverElement);

			if (RightClickElement != null)
				AddTrigger(TriggerTypes.RightClick, RightClickElement);

			if (LeftClickElement != null)
				AddTrigger(TriggerTypes.LeftClick, LeftClickElement);


			if (!string.IsNullOrEmpty(HoverElementSelector))
				AddTrigger(TriggerTypes.Hover, HoverElementSelector);

			if (!string.IsNullOrEmpty(RightClickElementSelector))
				AddTrigger(TriggerTypes.RightClick, RightClickElementSelector);

			if (!string.IsNullOrEmpty(LeftClickElementSelector))
				AddTrigger(TriggerTypes.LeftClick, LeftClickElementSelector);
		}

		/// <summary>
		/// Adds trigger elements of type left click, right click or hover.
		/// </summary>
		/// <param name="triggerType">The trigger type.</param>
		/// <param name="selectors">A comma separated list of regex supporting selectors or names for the trigger elements to be added. 
		/// <para><remarks>Whenever a selector starts with a dot however, elements are matched using their tags instead such
		/// that several elements can then be  targeted at once. 
		/// The ".." & "[Parent]" selectors are exceptions to the rule however since they refer to the parent of the PopupMenu instead.
		/// </remarks></para>
		/// </param>
		public void AddTrigger(TriggerTypes triggerType, string selectors)
		{
			var rgxIsValidFilename = new Regex("^[a-zA-Z_]+[a-zA-Z0-9_]*$");

			foreach (string selector in selectors.Split(','))
			{
				var matchingElements = new List<FrameworkElement>();

				if (selector == ".." || selector == "[Parent]")
				{
					// Get the menu parent.
					var elem = this.Parent as FrameworkElement;
					if (elem != null)
						matchingElements.Add(elem);
				}
				else if (!selector.StartsWith("."))
				{
					// Determine if the selector is a valid element name. If so use the FindElementByName function.
					// Note that \w could have been used instead of [a-zA-Z0-9_] here.
					if (rgxIsValidFilename.IsMatch(selector))
					{
						var elem = PopupMenuUtils.FindElementByName(this, selector, "trigger type " + triggerType);
						if (elem != null)
							matchingElements.Add(elem);
					}
					else
					{
						var rgxSelector = new Regex(selector);
						matchingElements = Application.Current.RootVisual.GetVisualDescendantsAndSelf().OfType<FrameworkElement>()
							.Where(i => i.Name != null && rgxSelector.IsMatch(i.Name)).ToList();
					}
				}
				else
				{
					// The selector starts with a dot. So lets match it against element tags in the visual tree instead .
					var rgxSelector = new Regex(selector.Substring(1));
					matchingElements = Application.Current.RootVisual.GetVisualDescendantsAndSelf().OfType<FrameworkElement>()
						.Where(i => i.Tag != null && i.Tag is string && rgxSelector.IsMatch(i.Tag.ToString())).ToList();
				}

				if (matchingElements.Count > 0)
					AddTrigger(triggerType, matchingElements.ToArray());
			}
		}

		/// <summary>
		/// Adds trigger elements of type left click, right click or hover.
		/// Note that any combination of trigger type and trigger element that has already been
		/// added before will be filtered out.
		/// </summary>
		/// <param name="triggerType">The trigger type.</param>
		/// <param name="triggerElements">The trigger elements to be added.</param>
		public void AddTrigger(TriggerTypes triggerType, params UIElement[] triggerElements)
		{
			foreach (FrameworkElement triggerElement in triggerElements.Where(elem => elem != null))
				AddTrigger(triggerType, triggerElement);
		}

		/// <summary>
		/// Adds a trigger element of type left click, right click or hover.
		/// Note that any combination of trigger type and trigger element that has already been
		/// added before will be filtered out.
		/// </summary>
		/// <param name="triggerType">The trigger type.</param>
		/// <param name="triggerElements">The trigger element to be added.</param>
		public void AddTrigger(TriggerTypes triggerType, FrameworkElement triggerElement)
		{
			/*If not using AddHandler make sure the _clickAlreadyHandledOnMouseDown condition in
			 * hoverTriggeredElement_LeftClick is uncommented*/

			if (PopupMenuManager.RegisterMenu(triggerElement, triggerType, this) != null)
			{
				triggerElement.Dispatcher.BeginInvoke(delegate
				{
					switch (triggerType)
					{
						case TriggerTypes.RightClick:
							triggerElement.MouseRightButtonUp += TriggerElement_MouseRightButtonUp;
							// Disable the default silverlight context menu.
							triggerElement.MouseRightButtonDown += triggerElement_MouseRightButtonDown;
							break;

						case TriggerTypes.LeftClick:
							/*ButtonBase !-> Up/Down -> Click, ItemsControl>>Selector && ListBoxItem !-> Down*/
							triggerElement.AddHandler(UIElement.MouseLeftButtonUpEvent,
								new MouseButtonEventHandler(TriggerElement_MouseLeftButtonUp), true);

							triggerElement.AddHandler(UIElement.MouseLeftButtonDownEvent,
								new MouseButtonEventHandler(triggerElement_MouseLeftButtonDown), true);

							// Monitor hover events as well.
							triggerElement.MouseEnter += leftClickTriggeredElement_Hover;
							break;

						case TriggerTypes.Hover:
							triggerElement.MouseMove += TriggerElement_Hover;

							// Monitor left click events as well to allow closing of auto pinned menus by clicking
							// on the trigger element.
							triggerElement.AddHandler(UIElement.MouseLeftButtonDownEvent,
								new MouseButtonEventHandler(hoverTriggeredElement_LeftClick), true);
							break;
					}

					// Enable keyboard navigation.
					triggerElement.KeyDown += TriggerElement_KeyDown;
					if (triggerType != TriggerTypes.RightClick)
						triggerElement.KeyUp += TriggerElement_KeyUp;

					//if(triggerElement is Selector)
					//{
					//    (triggerElement as Selector).SelectionChanged += delegate { Close(0); };
					//}
				});
			}
		}


		/// <summary>
		/// Remove all event handlers for all specified elements.
		/// </summary>
		/// <param name="triggerElements">
		/// One or more trigger elements to be unregistered.
		/// If no value is given all triggers for the menu will be removed.
		/// </param>
		public void ClearTriggers()
		{
			PopupMenuManager.UnregisterMenu(this);
		}

		public void RemoveTrigger(FrameworkElement triggerElement)
		{
			RemoveTrigger(triggerElement, false);
		}

		public void RemoveTrigger(FrameworkElement triggerElement, bool keepMenuTriggerRecord)
		{
			if (triggerElement != null)
			{
				triggerElement.Dispatcher.BeginInvoke(delegate
				{
					if (!keepMenuTriggerRecord)
						PopupMenuManager.MenuTriggers
							.Where(mt => mt.PopupMenuBase == this && mt.TriggerElement == triggerElement).ToList()
							.ForEach(mt => PopupMenuManager.MenuTriggers.Remove(mt));

					// Hover
					triggerElement.MouseEnter -= TriggerElement_Hover;
					triggerElement.MouseEnter -= leftClickTriggeredElement_Hover;
					triggerElement.MouseLeftButtonUp -= hoverTriggeredElement_LeftClick;

					// RightClick
					triggerElement.MouseRightButtonDown -= triggerElement_MouseRightButtonDown;
					triggerElement.MouseRightButtonUp -= TriggerElement_MouseRightButtonUp;

					// LeftClick
					triggerElement.MouseLeftButtonDown -= TriggerElement_MouseLeftButtonUp;
					triggerElement.MouseLeftButtonUp -= TriggerElement_MouseLeftButtonUp;

					// Keyboard
					triggerElement.KeyDown -= TriggerElement_KeyDown;
					triggerElement.KeyUp -= TriggerElement_KeyUp;
				});
			}
		}


		/// <summary>
		/// Set focus on the ItemsControl when the flyout key is pressed.
		/// </summary>
		protected void TriggerElement_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == FlyoutKey)
			{
				e.Handled = true;

				if (sender is Control)
					(sender as Control).Focus();

				var itemsControl = ContentRoot.GetVisualDescendantsAndSelf().OfType<ItemsControl>().FirstOrDefault();
				if (itemsControl != null)
					itemsControl.Focus();
			}
		}

		/// <summary>
		/// Opens the menu after the flyout key is pressed.
		/// </summary>
		protected void TriggerElement_KeyUp(object sender, KeyEventArgs e)
		{
			if (PopupMenuManager.OpenMenus.Count == 0)
			{
				if (e.Key == FlyoutKey || KeepParentMenusOpen)
				{
					var triggerElement = (sender is Selector ? (sender as Selector).SelectedItem : sender) as FrameworkElement;
					OpenNextTo(Orientation, triggerElement, true, true);
				}
			}
		}

		/// <summary>
		/// This event handler is raised when a trigger element of hover type is clicked.
		/// However when IsPinned is false OverlayCanvas is stretched all over the application waiting for
		/// an outer click to trigger the close procedure. In this case since OverlayCanvas always comes 
		/// above the trigger element the latter cannot receive any mouse clicks and so the event needs to
		/// be handled in the MouseLeftButtonDown event of OverlayCanvas itself.
		/// </summary>
		private void hoverTriggeredElement_LeftClick(object triggerElement, MouseButtonEventArgs e)
		{
			// If this event has not already been handled in the MouseLeftButtonDown event for OverlayCanvas.
			//if (_clickAlreadyHandledOnMouseDown)
			//{
			//    _clickAlreadyHandledOnMouseDown = false;
			//}
			//else
			//{
			if (IsAutoPinnable)
			{
				IsPinned = !IsPinned;
				this.Close(CloseDuration);
			}
			//}
		}

		/// <summary>
		///  Whenever a left click menu is open this event handler enables neighbouring left click
		///  triggered menus to be opened by just hovering on them.
		/// </summary>
		private void leftClickTriggeredElement_Hover(object triggerElement, MouseEventArgs e)
		{
			if (PopupMenuManager.NeighbouringLeftClickElementUnderMouse != null)
				if (PopupMenuManager.NeighbouringLeftClickElementUnderMouse.Parent == (triggerElement as FrameworkElement).Parent)
					TriggerElement_Hover(triggerElement, e);
		}

		private void triggerElement_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
		{
			e.Handled = true;
		}

		protected void TriggerElement_MouseRightButtonUp(object triggerElement, MouseButtonEventArgs e)
		{
			ActualTriggerType = TriggerTypes.RightClick;

			ElementsUnderMouse = PopupMenuUtils.GetVisualTreeItemsUnderMouse(triggerElement as FrameworkElement, e);

			var element = PopupMenuUtils.GetAndSelectItemUnderMouse(ElementsUnderMouse, triggerElement as FrameworkElement, IsTriggerElementMappedToSelectedItem, AutoSelectItem)
				?? triggerElement as FrameworkElement;

			Point mousePos = e.GetSafePosition(null);

			Open(mousePos, MenuOrientationTypes.MouseBottomRight, 0, element, FocusOnShow, e);
		}

		void triggerElement_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			TriggerElement_MouseLeftButtonUp(sender, e);
			_clickAlreadyHandledOnMouseDown = true;
		}

		//protected void TriggerButton_Click(object triggerElement, RoutedEventArgs e)
		//{
		//    ActualTriggerType = TriggerTypes.LeftClick;

		//    Point mousePos = PopupMenuUtils.GetAbsoluteElementPos(!_isPopupChildLevel1, triggerElement as FrameworkElement);
		//    Open(mousePos, Orientation, 0, triggerElement as FrameworkElement, FocusOnShow, e as MouseButtonEventArgs);
		//}

		protected void TriggerElement_MouseLeftButtonUp(object triggerElement, MouseButtonEventArgs e)
		{
			ActualTriggerType = TriggerTypes.LeftClick;
			ElementsUnderMouse = PopupMenuUtils.GetVisualTreeItemsUnderMouse(triggerElement as FrameworkElement, e);

			if (_clickAlreadyHandledOnMouseDown)
			{
				_clickAlreadyHandledOnMouseDown = false;
			}
			else
			{
				if (OverlayPopup.IsOpen)
				{
					this.Close();
				}
				else
				{
					var element = PopupMenuUtils.GetAndSelectItemUnderMouse(ElementsUnderMouse, triggerElement as FrameworkElement, IsTriggerElementMappedToSelectedItem, AutoSelectItem);
					if (element != null)
					{
						Point mousePos = PopupMenuUtils.GetAbsoluteElementPos(element);
						Open(mousePos, Orientation, 0, element, FocusOnShow, e);
					}
					else
					{
						ActualTriggerElement = triggerElement as FrameworkElement; // This would normally be set when calling the Open method
					}
				}
			}
		}

		protected void TriggerElement_Hover(object triggerElement, MouseEventArgs e)
		{
			if (OverlayPopup.IsOpen || IsOpening)
				return;

			ActualTriggerType = TriggerTypes.Hover;
			ElementsUnderMouse = PopupMenuUtils.GetVisualTreeItemsUnderMouse(triggerElement as UIElement, e);

			var element = PopupMenuUtils.GetAndSelectItemUnderMouse(ElementsUnderMouse, triggerElement as FrameworkElement, IsTriggerElementMappedToSelectedItem, AutoSelectItem);
			if (element != null)
			{
				Point mousePos = PopupMenuUtils.GetAbsoluteElementPos(element);
				Open(mousePos, Orientation, OpenDelay, element, FocusOnShow, e);
			}

			// If the trigger element has an shortcut key and its tooltip is undefined then display the shortcut key through the latter.
			if (ShortcutKey != Key.None && ToolTipService.GetToolTip(triggerElement as FrameworkElement) == null)
			{
				ToolTipService.SetToolTip(triggerElement as FrameworkElement,
				   PopupMenuUtils.GetShortcutAsText(ShortcutKey, ShortcutKeyModifier1 | ShortcutKeyModifier2));
			}
		}

		/// <summary>
		/// Adds event handlers to the transparent OverlayCanvas which is stretched over the application
		/// when the menu is open. Those handlers are mainly concerned with closing the latter upon mouse
		/// activity in surrounding regions.
		/// </summary>
		private void AddOverlayCanvasEventHandlers()
		{
			OverlayCanvas.MouseWheel += (sender, e) =>
			{
				var scrollViewer = this.GetClickedElement<ScrollViewer>();
				if (scrollViewer != null)
				{
					scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - e.Delta);
					//if (scrollViewer.VerticalOffset - e.Delta <= scrollViewer.ScrollableHeight && scrollViewer.VerticalOffset - e.Delta >= 0)
					//    Canvas.SetTop(ContentRoot, Canvas.GetTop(ContentRoot) + e.Delta);
					this.Close(0);
				}
			};

			OverlayCanvas.MouseRightButtonDown += (sender, e) =>
			{
				if (OuterClick != null)
					OuterClick(sender, e);

				// Close menu on right clicking outside the menu but not when clicking on the menu itself.
				if (!IsModal && !PopupMenuUtils.HitTestAny(e, ContentRoot))
					this.Close(0);

				// Prevent the default Silverlight context menu from popping up.
				e.Handled = true;
			};

			OverlayCanvas.MouseLeftButtonDown += (sender, e) =>
			{
				if (OuterClick != null)
					OuterClick(sender, e);

				// Close menu on left clicking outside the menu but ignore those clicks if the menu is in modal mode.
				if (!IsModal)
				{
					PopupMenuManager.CloseChildMenus(0, false, false, e);
					// Determine if the trigger element was clicked through OverlayCanvas.					
					bool triggerElementWasClickedThrough = PopupMenuUtils.HitTestAny(
						e, UseParentHoverBoundsForWidthlessTextBlockTriggers, false, ActualTriggerElement);

					// If the menu is a pinnable hover menu and its trigger element was clicked.
					if (triggerElementWasClickedThrough && ActualTriggerType == TriggerTypes.Hover && IsAutoPinnable)
					{
						IsPinned = !IsPinned;
						_clickAlreadyHandledOnMouseDown = true;
					}

					// If outer canvas was clicked. We don't want to close a hover menu when its parent is clicked.
					if (ActualTriggerType != TriggerTypes.Hover)
					{
						// Avoid menu reopening via the MouseLeftButtonUp event if the trigger element itself was clicked.
						_clickAlreadyHandledOnMouseDown = triggerElementWasClickedThrough;

						if (OverlayPopup.IsOpen)
						{
							this.Close(0);
							_clickAlreadyHandledOnMouseDown = true;
						}
						else
						{
						}
					}
				}
			};

			//ContentRoot.MouseLeave += overlayCanvas_MouseMove;

			OverlayCanvas.MouseMove += overlayCanvas_MouseMove;
		}

		private void overlayCanvas_MouseMove(object sender, MouseEventArgs e)
		{
			PopupMenuManager.TopOverlayMouseEventArgs = e;

			if ((ActualTriggerType == TriggerTypes.LeftClick || ActualTriggerType == TriggerTypes.Hover)
				&& ActualTriggerElement != null && !IsModal)
			{
				// Identify all left click trigger elements in the neighbourhood if any.
				var neighbouringLeftClickElements = PopupMenuManager.MenuTriggers.ToList().Where(mt =>
					mt.TriggerType != TriggerTypes.RightClick
					&& mt.TriggerElement != null
					&& mt.TriggerElement != ActualTriggerElement
					&& mt.TriggerElement.Parent != null
					&& mt.TriggerElement.Parent == ActualTriggerElement.Parent).Select(mt => mt.TriggerElement).ToArray();

				// Identify the first trigger element lying under the mouse if any.
				// NeighbouringLeftClickElementUnderMouse is later used in the leftClickTriggerElement_Hover event.
				PopupMenuManager.NeighbouringLeftClickElementUnderMouse = PopupMenuUtils.GetFirstElementUnderMouse(
					e, UseParentHoverBoundsForWidthlessTextBlockTriggers, neighbouringLeftClickElements);

				if (PopupMenuManager.NeighbouringLeftClickElementUnderMouse != null)
				{
					// A neighbouring left click element is being hovered.
					// Close the actual menu to let it catch the hover event and open its associated menu.
					this.Close(0);
				}
				else if (ActualTriggerType == TriggerTypes.Hover)
				{
					// The menu is triggered by hover events on the trigger element. Lets check if mouse is over it.
					bool isMouseOverMenuOrTriggerElement = PopupMenuUtils.HitTestAny(e
						, UseParentHoverBoundsForWidthlessTextBlockTriggers, false, ActualTriggerElement);


					if (!isMouseOverMenuOrTriggerElement)
					{
						// The mouse isn't over the trigger element but might be hovering the menu itself. Lets check it out.
						isMouseOverMenuOrTriggerElement = PopupMenuUtils.HitTestAny(
						   e, false, false, ContentRoot.GetVisualChildren().OfType<FrameworkElement>().ToArray());
					}

					if (!isMouseOverMenuOrTriggerElement)
					{
						// The mouse is over a neighbouring hover element. 
						// So we better close the current menu to let it catch the event.
						if (PopupMenuUtils.HitTestAny(e,
							PopupMenuManager.MenuTriggers.Where(mt => mt.TriggerType == TriggerTypes.Hover).Select(mt => mt.TriggerElement as UIElement).ToArray()))
							this.Close(0);

						if (IsOpening && CloseOnHoverOut)
						{
							// The menu is still opening. We can now close it.
							this.Close(0);
						}
						else if (!IsClosing)
						{
							// The menu is not already closing. So lets close it after the amount of time specified by CloseDelay.
							_timerClose = new Timer(delegate
							{
								OverlayPopup.Dispatcher.BeginInvoke(delegate
								{
									PopupMenuManager.CloseChildMenus(CloseDuration, true, false, PopupMenuManager.TopOverlayMouseEventArgs);
								});
							}, null, CloseDelay, Timeout.Infinite);
						}
					}
					else
					{
					}
				}
			}
		}

		/// <summary>
		/// Open the menu next to a specified framework element.
		/// </summary>
		/// <param name="targetPos">The target position for the menu. A null value positions the menu on the top left corner.</param>
		/// <param name="focus">Set focus on the menu.</param>
		/// <param name="restoreFocusOnClose">Restore the the initial focus when the menu is closed.</param>
		public void Open(Point? targetPos, bool focus, bool restoreFocusOnClose)
		{
			RestoreFocusOnClose = restoreFocusOnClose;
			Open(targetPos, MenuOrientationTypes.None, 0, null, focus, null);
		}

		/// <summary>
		/// Open the menu next to a specified framework element.
		/// <remarks><para>Note that calling the menu will place itself on the top left corner of the window if called before the 
		/// visual tree is generated. Therefore always be sure to call the method from within the loaded event of the element
		/// or its page instead.</para></remarks>
		/// </summary>
		/// <param name="orientation">The orientation of the menu relative to the trigger element.</param>
		/// <param name="placementElement">The placement element. A null value can be used to refer to the application root.</param>
		/// <param name="focus">Set focus on the menu.</param>
		/// <param name="restoreFocusOnClose">Restore the the initial focus when the menu is closed.</param>
		public void OpenNextTo(MenuOrientationTypes orientation, FrameworkElement placementElement, bool focus, bool restoreFocusOnClose)
		{
			// Close the menu if it is being open or is already so.
			//if (IsOpening || OverlayPopup.IsOpen)
			//	Close(0);
			IsOpening = OverlayPopup.IsOpen = false;

			RestoreFocusOnClose = restoreFocusOnClose;

			var mousePos = placementElement == null ? new Point?()
													: PopupMenuUtils.GetAbsoluteElementPos(placementElement);

			Open(mousePos, orientation, 0, placementElement, focus, null);
		}

		/// <summary>
		/// Open the menu next to a specified framework element.
		/// </summary>
		/// <param name="mousePos">The menu position. A null value positions the menu on the top left corner.</param>
		/// <param name="orientation">The menu orientation.</param>
		/// <param name="delay">The time to wait before opening the menu.</param>
		/// <param name="triggerElement">The menu trigger elment.</param>
		/// <param name="focus">Set focus on the menu.</param>
		/// <param name="e">The mouse event arguments that will be passed to the Opening, Showing and Shown events handlers. Accepts a null value if not available.</param>
		public void Open(Point? mousePos, MenuOrientationTypes orientation, int delay, FrameworkElement triggerElement, bool focus, MouseEventArgs e)
		{
			_clickAlreadyHandledOnMouseDown = false;

			if (!base.IsEnabled || IsPinned)
				return;

			IsOpening = true;

			Debug.WriteLine("Open " + DateTime.Now.ToString());

			OverlayPopup.Dispatcher.BeginInvoke(delegate // Helps reduce jitter on simultaneous calls.
			{
				if (PopupMenuManager.OpenMenus.Contains(this))
				{
					Close(0);
					IsOpening = true;
				}

				PopupMenuManager.OpenMenus.Insert(0, this); // Add the actual menu on top of the open menus list.

				OverlayCanvas.Opacity = 0; // Make sure the menu is hidden before repositioning.

				placeMenu(mousePos, triggerElement, orientation, true);

				ActualTriggerElement = triggerElement ?? Application.Current.RootVisual as FrameworkElement;

				OverlayPopup.DataContext = InheritDataContext && ActualTriggerElement != null
					? ActualTriggerElement.DataContext
					: this.DataContext;

				if (Opening != null)
					Opening(triggerElement, e);

				// This is typically set in the Opening event handler to end the opening procedure.
				if (IsOpeningCancelled)
				{
					IsOpening = false;
					IsOpeningCancelled = false;
					return;
				}

				if (IsClosing)
					Close(0);

				OverlayPopup.IsOpen = true;

				// Start opening the menu after the period of time specified by the OpenDelay.
				_timerOpen = new Timer(delegate
				{
					IsOpening = false;

					OverlayPopup.Dispatcher.BeginInvoke(delegate
					{
						// Make sure the menu is still open. This could not be the case if the mouse.
						// is clicked elsewhere before the OpenDelay time has elapsed.
						if (OverlayPopup.IsOpen)
						{
							_isOverlayCanvasExpanded = !IsPinned;

							if (!IsVisualTreeGenerated) // Perform only on first load.
							{
								// Force the content layout to update on first load.
								// This tackles some visibility issues with menu items(UpdateLayout is not enough).
								OverlayPopup.IsOpen = false;
								OverlayPopup.IsOpen = true;
							}

							if (Showing != null)
								Showing(triggerElement, e);

							OverlayCanvas.UpdateLayout(); // Update the layout positions before placing the menu.

							var orientationPos = placeMenu(mousePos, triggerElement, orientation, true);

							if (BorderMaskFill != null && triggerElement != null)
								AdjustBorderMaskPosition(orientationPos, triggerElement);
#if OPENSILVER
							Storyboard sbOpen = OpenAnimation ?? PopupMenuUtils.CreateStoryBoard(0, OpenDuration, OverlayCanvas, "Opacity", 1, null);
#else
							Storyboard sbOpen = OpenAnimation ?? PopupMenuUtils.CreateStoryBoard(0, OpenDuration, OverlayCanvas, "UIElement.Opacity", 1, null);
#endif
							sbOpen.Begin();
							sbOpen.Completed += delegate
							{
								// Restore full opacity if sbOpen hasn't already done so.
								if (OverlayCanvas.Opacity == 0)
									OverlayCanvas.Opacity = 1;

								if (focus)
								{
									var ctrl = OverlayCanvas.GetVisualDescendants().OfType<Control>().FirstOrDefault();
									if (ctrl != null)
										ctrl.Focus();
									RestoreFocusOnClose = true;
								}

								if (FocusTriggerElementOnShow && triggerElement is Control)
								{
									(triggerElement as Control).Focus();
									RestoreFocusOnClose = true;
								}

								if (Shown != null)
									Shown(triggerElement, e);
							};

							IsVisualTreeGenerated = true;
						}

					});
				}, null, delay, Timeout.Infinite);
			});
		}

		/// <summary>
		/// Place an element next to a target coordinate or element according to the specified orientation type.
		/// </summary>
		/// <param name="targetPos">The target position of the menu.</param>
		/// <param name="placementElement">The element used for the target position when targetPos is null.</param>
		/// <param name="orientation">The menu orientation type.</param>
		/// <param name="keepWithinLayoutBounds">Make the menu stays within the layout bounds. 
		/// It must be given a false value when called recursively to avoid infinite looping.</param>
		/// <returns>Returns a the final position for the element.</returns>
		private Point placeMenu(Point? targetPos, FrameworkElement placementElement, MenuOrientationTypes orientation, bool keepWithinLayoutBounds)
		{
			Point ptMargin;

			if (placementElement != null)
			{
				ptMargin = targetPos.HasValue ? new Point(targetPos.Value.X + OffsetX, targetPos.Value.Y + OffsetY)
											  : new Point(Canvas.GetLeft(placementElement), Canvas.GetTop(placementElement));

				double offsetXForRightToLeftFlow = base.FlowDirection != FlowDirection.RightToLeft ? 0 : -ContentRoot.ActualWidth;

				switch (orientation)
				{
					case MenuOrientationTypes.Right:
						ptMargin.X += placementElement.ActualWidth + offsetXForRightToLeftFlow - 1;
						break;
					case MenuOrientationTypes.Bottom:
						ptMargin.Y += placementElement.ActualHeight;
						break;
					case MenuOrientationTypes.Left:
						ptMargin.X += -ContentRoot.ActualWidth + offsetXForRightToLeftFlow - 1;
						break;
					case MenuOrientationTypes.Top:
						ptMargin.Y += -ContentRoot.ActualHeight;
						break;
				}
			}
			else
			{
				// The menu is placed on the top left corner of the screen if no mouse coordinates
				// or positioning element is specified.
				ptMargin = targetPos ?? new Point(0, 0);

				//new Point((Application.Current.Host.Content.ActualWidth - ContentRoot.ActualWidth) / 2,
				//		  (Application.Current.Host.Content.ActualHeight - ContentRoot.ActualHeight) / 2);
			}

			Point ptMargin1 = PopupMenuUtils.SetPosition(ContentRoot, ptMargin, keepWithinLayoutBounds);
			// If the new menu coordinates had to be clipped to fit in the layout.
			if (ptMargin != ptMargin1)
			{
				// Flip the menu orientation to keep it within layout bounds if the menu orientation matches
				// the direction it went out.
				if (ptMargin.X != ptMargin1.X && (orientation == MenuOrientationTypes.Left || orientation == MenuOrientationTypes.Right))
					orientation = PopupMenuUtils.FlipOrientation(orientation);

				if (ptMargin.Y != ptMargin1.Y && (orientation == MenuOrientationTypes.Top || orientation == MenuOrientationTypes.Bottom))
					orientation = PopupMenuUtils.FlipOrientation(orientation);

				ptMargin = placeMenu(targetPos, placementElement, orientation, false); // keepWithinLayoutBounds is false to avoid an infinite looping.
			}
			else
			{
				ptMargin = PopupMenuUtils.SetPosition(ContentRoot, ptMargin, true);
			}
			return ptMargin;
		}

		/// <summary>
		/// Adjusts the position for BorderMask such that it hides the overlapping
		/// between the menu and its trigger element.
		/// </summary>
		/// <param name="position">The absolute position for the trigger element.</param>
		/// <param name="triggerElement">The current trigger element.</param>
		protected void AdjustBorderMaskPosition(Point position, FrameworkElement triggerElement)
		{
			BorderMask.Visibility = ActualTriggerType == TriggerTypes.RightClick || ActualTriggerElement is TextBlock
				? Visibility.Collapsed
				: Visibility.Visible;

			if (Orientation == MenuOrientationTypes.Bottom || Orientation == MenuOrientationTypes.Top)
			{
				if (!BorderMaskThickness.HasValue)
					BorderMaskThickness = new Thickness(-1, 2, -1, 1);

				BorderMask.Height = BorderMaskThickness.Value.Top + BorderMaskThickness.Value.Bottom;
				BorderMask.Width = Math.Abs(triggerElement.ActualWidth + BorderMaskThickness.Value.Left + BorderMaskThickness.Value.Right);
				BorderMask.VerticalAlignment = Orientation == MenuOrientationTypes.Bottom ? VerticalAlignment.Bottom : VerticalAlignment.Top;
				OffsetY = Orientation == MenuOrientationTypes.Bottom ? -1 : 1;
			}
			else if (Orientation == MenuOrientationTypes.Right || Orientation == MenuOrientationTypes.Left)
			{
				if (!BorderMaskThickness.HasValue)
					BorderMaskThickness = new Thickness(1, -1, 1, 3);

				BorderMask.Height = triggerElement.ActualHeight + BorderMaskThickness.Value.Top + BorderMaskThickness.Value.Bottom;
				BorderMask.Width = Math.Abs(BorderMaskThickness.Value.Left + BorderMaskThickness.Value.Right);
				BorderMask.HorizontalAlignment = Orientation == MenuOrientationTypes.Left ? HorizontalAlignment.Left : HorizontalAlignment.Right;
				OffsetX = Orientation == MenuOrientationTypes.Right ? -2 : 2;
			}

			BorderMask.Margin = new Thickness(position.X + -BorderMaskThickness.Value.Left, position.Y - BorderMaskThickness.Value.Top, 0, 0);
			BorderMask.Fill = BorderMaskFill;
		}


		public void Close()
		{
			Close(CloseDuration, false);
		}

		public void Close(int transitionTime)
		{
			Close(transitionTime, false);
		}

		/// <summary>
		/// Closes the menu and attempts to restore the selection state of the trigger
		/// element if it was altered during the opening process. Note the method is 
		/// not processed when IsPinned is true.
		/// </summary>
		/// <param name="transitionTime">The time</param>
		public void Close(int transitionTime, bool ignorePinnedState)
		{
			if (IsPinned && !ignorePinnedState)
				return;

			// When transitionTime is zero it is important that OverlayPopup is closed immediately especially
			// if there are other hover elements underneath sharing the same mouse events.
			if (transitionTime == 0)
				OverlayPopup.IsOpen = false;
			else
				IsClosing = true;

			IsOpening = false;
			KeepParentMenusOpen = false;
			_clickAlreadyHandledOnMouseDown = false;

			Debug.WriteLine("Close " + DateTime.Now.ToString());

			PopupMenuManager.OpenMenus.Remove(this);

			if (_timerOpen != null)
			{
				_timerOpen.Change(0, Timeout.Infinite);
				_timerOpen.Dispose();
				_timerOpen = null;
			}

			if (_timerClose != null)
			{
				_timerClose.Change(0, Timeout.Infinite);
				_timerClose.Dispose();
				_timerClose = null;
			}

			if (OpenAnimation != null)
				OpenAnimation.Stop();

			if (Closing != null)
				Closing(this, PopupMenuManager.TopOverlayMouseEventArgs);

#if !OPENSILVER
			Storyboard sbClose = CloseAnimation ?? PopupMenuUtils.CreateStoryBoard(0, transitionTime, OverlayCanvas, "UIElement.Opacity", 0, null);
#else
			Storyboard sbClose = CloseAnimation ?? PopupMenuUtils.CreateStoryBoard(0, transitionTime, OverlayCanvas, "Opacity", 0, null);
#endif
            sbClose.Begin();
			sbClose.Completed += delegate
			{
				OverlayPopup.IsOpen = false;
				IsClosing = false;
			};

			if (RestoreFocusOnClose && ActualTriggerElement is Control)
				(ActualTriggerElement as Control).Focus();

			if (ActualTriggerElement != null)
			{
				// Restore the selection state of the trigger element if it was altered during the opening process.
				if (AutoSelectItem)
				{
					var container = ActualTriggerElement is PopupMenuItem
										? (ActualTriggerElement as PopupMenuItem).Container
										: ActualTriggerElement;

					PropertyInfo property = container.GetType().GetProperty("IsSelected");
					if (property != null)
						property.SetValue(container, false, null);
				}
				ActualTriggerElement = null;
			}
		}


		/// <summary>
		/// Get the last clicked element associated with any of the triggers.
		/// </summary>
		/// <typeparam name="T">The type of the object</typeparam>
		public T GetClickedElement<T>()
		{
			return ElementsUnderMouse == null
				? default(T)
				: ElementsUnderMouse.OfType<T>().FirstOrDefault();
		}

		/// <summary>
		/// Gets the first element of type T having a name that mathces a given regex pattern.
		/// </summary>
		/// <param name="regexSelector">The regex pattern used to identify the element by its name.</param>
		/// <remarks>The method only works after the visual tree has been created.</remarks>
		public T FindItemByName<T>(string regexSelector) where T : FrameworkElement
		{
			return FindItemsByName<T>(regexSelector).FirstOrDefault();
		}

		/// <summary>
		/// Gets the elements of type T with names matching a given regex pattern.
		/// </summary>
		/// <param name="regexSelector">The regex pattern used to identify the element(s) by their name.</param>
		/// <remarks>The method only works after the visual tree has been created.</remarks>
		public IEnumerable<T> FindItemsByName<T>(string regexSelector) where T : FrameworkElement
		{
			//List<T> elements = new List<T>();
			//foreach (FrameworkElement item in Items)
			//    foreach (T element in item.GetVisualChildrenAndSelf().OfType<T>())
			//        if ((new Regex(regexSelector).IsMatch((element as FrameworkElement).Name ?? "")))
			//            elements.Add(element as T);

			//// If no element was found search all the visual tree instead(only works after the latter has been created)
			//if (elements.Count == 0)
			//return elements;
			var regex = new Regex(regexSelector);
			return ContentRoot.GetVisualDescendantsAndSelf().OfType<T>()
				.Where(i => i.Name != null && regex.IsMatch(i.Name));
		}

		/// <summary>
		/// Get the controls of type T having elements with any of the tags specified.
		/// </summary>
		/// <param name="tags">A comma delimited list of tags that will be used as identifier.</param>
		/// <remarks>This method searches through the whole visual tree of the ItemsControl and
		/// only works after the visual tree has been created.</remarks>      
		/// <remarks>The method only works after the visual tree has been created.</remarks>
		public IEnumerable<T> FindItemsByTag<T>(params object[] tags) where T : FrameworkElement
		{
			//List<T> elements = new List<T>();
			//foreach (object tag in tags)
			//{
			//foreach (FrameworkElement item in Items)
			//    foreach (var element in item.GetVisualChildrenAndSelf().OfType<T>().OfType<FrameworkElement>())
			//        if (element.Tag != null && element.Tag.Equals(tag))
			//            elements.Add(element as T);
			//// If no element was found search all the visual tree instead(only works after the latter has been created)
			//if (elements.Count == 0)
			//}
			//return element;

			return ContentRoot.GetVisualDescendantsAndSelf().OfType<T>()
				.Where(i => i.Tag != null && tags.Contains(i.Tag));
		}

		/// <summary>
		/// Find the elements of type T with tags matching a regex pattern.
		/// </summary>
		/// <param name="regexSelector">The regex pattern to match the element name</param>
		/// <remarks>This method searches through the whole visual tree of the ItemsControl and
		/// only works after the visual tree has been created.</remarks>
		public IEnumerable<T> FindItemsByTag<T>(string regexSelector) where T : FrameworkElement
		{
			var regex = new Regex(regexSelector);
			return ContentRoot.GetVisualDescendantsAndSelf().OfType<T>()
				.Where(i => i.Tag != null && regex.IsMatch(i.Tag.ToString()));
		}
	}
}