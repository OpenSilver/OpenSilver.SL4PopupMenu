// Copyright (c) 2009 Ziad Jeeroburkhan. All Rights Reserved.
// GNU Library General Public License (LGPL) 
// (http://sl4popupmenu.codeplex.com/license)

using System;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.ComponentModel;

namespace SL4PopupMenu
{
	/// <summary>
	/// The PopupMenuItem class.
	/// </summary>
	[TemplateVisualState(Name = "Normal", GroupName = "CommonStates")]
	[TemplateVisualState(Name = "Disabled", GroupName = "CommonStates")]
	[TemplateVisualState(Name = "Unfocused", GroupName = "FocusStates")]
	[TemplateVisualState(Name = "Focused", GroupName = "FocusStates")]

	//[StyleTypedProperty(Property = "ItemContainerStyle", StyleTargetType = typeof(PopupMenuItem))]
	public class PopupMenuItem : HeaderedItemsControl//, INotifyPropertyChanged
	{
		public event RoutedEventHandler Click;

		private bool _isLayoutUpdated = false;
		private bool _isFocused = false;

		/// <summary>
		/// The global shortcut key for the menu item.
		/// </summary>
		public Key ShortcutKey { get; set; }

		/// <summary>
		/// The global shortcut key modifier for the menu item.
		/// </summary>
		public ModifierKeys ShortcutKeyModifier1 { get; set; }

		/// <summary>
		/// The global shortcut key modifier for the menu item.
		/// </summary>
		public ModifierKeys ShortcutKeyModifier2 { get; set; }

		public bool DisplayShortcutKeyInRightMargin { get; set; }

		/// <summary>
		/// A string representing the shortcut key for the menu item e.g Ctrl+Alt+X.
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


		internal static DependencyProperty RegisterDependency<T>(string name, T defaultValue, PropertyChangedCallback propertyChangedCallback)
		{
			return DependencyProperty.Register(name, typeof(T), typeof(PopupMenuItem), new PropertyMetadata(defaultValue, propertyChangedCallback));
		}

		#region Commanding

		public ICommand Command
		{
			get { return (ICommand)GetValue(CommandProperty); }
			set { SetValue(CommandProperty, value); }
		}

		public static readonly DependencyProperty CommandProperty 
			= RegisterDependency<ICommand>("Command", null, (d, e) =>
		{
			((PopupMenuItem)d).OnCommandChanged((ICommand)e.OldValue, (ICommand)e.NewValue);
		});

		private void OnCommandChanged(ICommand oldValue, ICommand newValue)
		{
			if (oldValue != null)
				oldValue.CanExecuteChanged -= HandleCanExecuteChanged;
			if (newValue != null)
				newValue.CanExecuteChanged += HandleCanExecuteChanged;
			UpdateIsEnabled();
		}

		/// <summary>
		/// Gets or sets the parameter to pass to the Command property of a PopupMenuItem.
		/// </summary>
		public object CommandParameter
		{
			get { return (object)GetValue(CommandParameterProperty); }
			set { SetValue(CommandParameterProperty, value); }
		}

		public static readonly DependencyProperty CommandParameterProperty 
			= RegisterDependency<object>("CommandParameter", null, (d, e) =>
		{
			((PopupMenuItem)d).UpdateIsEnabled();
		});

		/// <summary>
		/// Handles the CanExecuteChanged event of the Command property.
		/// </summary>
		private void HandleCanExecuteChanged(object sender, EventArgs e)
		{
			UpdateIsEnabled();
		}

		/// <summary>
		/// Updates the IsEnabled property.
		/// </summary>
		private void UpdateIsEnabled()
		{
			IsEnabled = Command == null || Command.CanExecute(CommandParameter);
			ChangeVisualState(true);
		}

		#endregion Commanding


		private Control _container; // Value is reset in LayoutUpdated

		/// <summary>
		/// The containing element in the visual tree that's generated when the item is placed in an ItemsControl.
		/// </summary>
		public Control Container
		{
			get
			{
				if (_container == null)
					_container = PopupMenuUtils.GetItemContainer(null, this);
				return _container;
			}
		}

		public bool CloseOnClick
		{
			get { return (bool)GetValue(CloseOnClickProperty); }
			set { SetValue(CloseOnClickProperty, value); }
		}

		public static readonly DependencyProperty CloseOnClickProperty
			= RegisterDependency<bool>("CloseOnClick", true, null);

		public new bool IsEnabled
		{
			get
			{
				return (bool)GetValue(IsEnabledProperty);
			}
			set
			{
				SetValue(IsEnabledProperty, value);

				if (_isLayoutUpdated && this.Container != null)
					this.Container.IsEnabled = value; // Sync the container state.

				ChangeVisualState(true); // Always called last since putting the menu item in a disabled state also freezes its.
			}
		}

		public bool IsVisible
		{
			get { return (bool)GetValue(IsVisibleProperty); }
			set { SetValue(IsVisibleProperty, value); }
		}

		public static readonly DependencyProperty IsVisibleProperty
			= RegisterDependency<bool>("IsVisible", true, (d, e) =>
		{
			var pmi = (PopupMenuItem)d;
			// There is no need to bother about calling synchContainerState if the container
			// does not yet exist since OnApplyTemplate will eventually do it anyway.
			if (pmi._isLayoutUpdated)
			{
				// Dealing with the visibility state is a bit tricky because some visual changes may not get applied
				// when our item is collapsed. It is therefore made visible before calling syncContainerState.
				pmi.Visibility = Visibility.Visible;

				pmi.syncContainerState(); // Sync the container state.

				// The menu item can now safely be collapsed provided IsVisible is false.
				if (!pmi.IsVisible)
					pmi.Visibility = Visibility.Collapsed;
			}
		});

		public ImageSource ImageSource
		{
			get { return (ImageSource)GetValue(ImageSourceProperty); }
			set { SetValue(ImageSourceProperty, value); }
		}

		public static readonly DependencyProperty ImageSourceProperty
			= RegisterDependency<ImageSource>("ImageSource", null, (d, e) =>
		{
			((PopupMenuItem)d).ImageSource = e.NewValue as ImageSource;
		});

		public object ContentLeft
		{
			get { return (ImageSource)GetValue(ContentLeftProperty); }
			set { SetValue(ContentLeftProperty, value); }
		}

		public static readonly DependencyProperty ContentLeftProperty
			= RegisterDependency<object>("ContentLeft", null, null);

		public ImageSource ImageRightSource
		{
			get { return (ImageSource)GetValue(ImageRightSourceProperty); }
			set { SetValue(ImageRightSourceProperty, value); }
		}

		public static readonly DependencyProperty ImageRightSourceProperty
			= RegisterDependency<ImageSource>("ImageRightSource", null, (d, e) =>
		{
			((PopupMenuItem)d).ImageRightSource = e.NewValue as ImageSource;
		});

		public object ContentRight
		{
			get { return (ImageSource)GetValue(ContentRightProperty); }
			set { SetValue(ContentRightProperty, value); }
		}

		public static readonly DependencyProperty ContentRightProperty
			= RegisterDependency<object>("ContentRight", null, null);

		public Effect ImageEffect
		{
			get { return (Effect)GetValue(ImageEffectProperty); }
			set { SetValue(ImageEffectProperty, value); }
		}

		public static readonly DependencyProperty ImageEffectProperty
			= RegisterDependency<Effect>("ImageEffect", null, (d, e) =>
		{
			((PopupMenuItem)d).ImageEffect = e.NewValue as Effect;
		});

		public object Tooltip
		{
			get { return (string)GetValue(TooltipProperty); }
			set { SetValue(TooltipProperty, value); }
		}

		public static readonly DependencyProperty TooltipProperty
			= RegisterDependency<object>("Tooltip", null, (d, e) =>
		{
			ToolTipService.SetToolTip((PopupMenuItem)d, e.NewValue);
		});

		public Visibility VerticalSeparatorVisibility
		{
			get { return (Visibility)GetValue(VerticalSeparatorVisibilityProperty); }
			set { SetValue(VerticalSeparatorVisibilityProperty, value); }
		}

		public static readonly DependencyProperty VerticalSeparatorVisibilityProperty
			= RegisterDependency<Visibility>("VerticalSeparatorVisibility", Visibility.Visible, null);

		public double VerticalSeparatorWidth
		{
			get { return (double)GetValue(VerticalSeparatorWidthProperty); }
			set { SetValue(VerticalSeparatorWidthProperty, value); }
		}

		public static readonly DependencyProperty VerticalSeparatorWidthProperty
			= RegisterDependency<double>("VerticalSeparatorWidth", 2, null);

		/// <summary>
		/// The vertical and horizontal and vertical separator start color
		/// </summary>
		public Color SeparatorStartColor
		{
			get { return (Color)GetValue(SeparatorStartColorProperty); }
			set { SetValue(SeparatorStartColorProperty, value); }
		}

		public static readonly DependencyProperty SeparatorStartColorProperty
			= RegisterDependency<Color>("SeparatorStartColor", Color.FromArgb(55, 55, 55, 55), (d, e) =>
		{
			var pmi = (PopupMenuItem)d;
			pmi.VerticalSeparatorFill = PopupMenuUtils.MakeColorGradient((Color)e.NewValue, pmi.SeparatorEndColor, 0);
		});

		/// <summary>
		/// The vertical and horizontal and vertical separator end color
		/// </summary>
		public Color SeparatorEndColor
		{
			get { return (Color)GetValue(SeparatorEndColorProperty); }
			set { SetValue(SeparatorEndColorProperty, value); }
		}

		public static readonly DependencyProperty SeparatorEndColorProperty
			= RegisterDependency<Color>("SeparatorEndColor", Color.FromArgb(55, 255, 255, 255), (d, e) =>
		{
			var pmi = (PopupMenuItem)d;
			pmi.VerticalSeparatorFill = PopupMenuUtils.MakeColorGradient(pmi.SeparatorStartColor, (Color)e.NewValue, 0);
		});

		public Brush VerticalSeparatorFill
		{
			get { return (Brush)GetValue(VerticalSeparatorFillProperty); }
			set { SetValue(VerticalSeparatorFillProperty, value); }
		}

		public static readonly DependencyProperty VerticalSeparatorFillProperty
			= RegisterDependency<Brush>("VerticalSeparatorFill", null, null);

		public bool ShowLeftMargin
		{
			get { return (bool)GetValue(ShowLeftMarginProperty); }
			set { SetValue(ShowLeftMarginProperty, value); }
		}

		public static readonly DependencyProperty ShowLeftMarginProperty
			= RegisterDependency<bool>("ShowLeftMargin", true, (d, e) =>
		{
			var pmi = (PopupMenuItem)d;
			pmi.ImageVisibility =
			pmi.VerticalSeparatorVisibility = (bool)e.NewValue ? Visibility.Visible : Visibility.Collapsed;
		});

		public double ImageOpacity
		{
			get { return (double)GetValue(ImageOpacityProperty); }
			set { SetValue(ImageOpacityProperty, value); }
		}

		public static readonly DependencyProperty ImageOpacityProperty
			= RegisterDependency<double>("ImageOpacity", 1, null);

		public Visibility ImageVisibility
		{
			get { return (Visibility)GetValue(ImageVisibilityProperty); }
			set { SetValue(ImageVisibilityProperty, value); }
		}

		public static readonly DependencyProperty ImageVisibilityProperty
			= RegisterDependency<Visibility>("ImageVisibility", Visibility.Visible, null);

		public double ImageWidth
		{
			get { return (double)GetValue(ImageWidthProperty); }
			set { SetValue(ImageWidthProperty, value); }
		}

		public static readonly DependencyProperty ImageWidthProperty
			= RegisterDependency<double>("ImageWidth", 16, null);

		public double ImageRightWidth
		{
			get { return (double)GetValue(ImageRightWidthProperty); }
			set { SetValue(ImageRightWidthProperty, value); }
		}

		public static readonly DependencyProperty ImageRightWidthProperty
			= RegisterDependency<double>("ImageRightWidth", 16, null);

		/// <summary>
		/// The TextBlock control used in the header. The value is null when no text value has been assigned
		/// to the Header property.
		/// </summary>
		public TextBlock HeaderTextBlock
		{
			get { return (this.Header as FrameworkElement).GetVisualChildren().OfType<TextBlock>().FirstOrDefault(); }
		}

		#region PopupMenuHorizontalSeparator

		public Visibility HorizontalSeparatorVisibility
		{
			get { return (Visibility)GetValue(HorizontalSeparatorVisibilityProperty); }
			set { SetValue(HorizontalSeparatorVisibilityProperty, value); }
		}

		public static readonly DependencyProperty HorizontalSeparatorVisibilityProperty
			= RegisterDependency<Visibility>("HorizontalSeparatorVisibility", Visibility.Collapsed, null);

		public Brush HorizontalSeparatorBrush
		{
			get { return (Brush)GetValue(HorizontalSeparatorBrushProperty); }
			set { SetValue(HorizontalSeparatorBrushProperty, value); }
		}

		public static readonly DependencyProperty HorizontalSeparatorBrushProperty
			= RegisterDependency<Brush>("HorizontalSeparatorBrush", null, null);

		public double HorizontalSeparatorHeight
		{
			get { return (double)GetValue(HorizontalSeparatorHeightProperty); }
			set { SetValue(HorizontalSeparatorHeightProperty, value); }
		}

		public static readonly DependencyProperty HorizontalSeparatorHeightProperty
			= RegisterDependency<double>("HorizontalSeparatorHeight", 2, null);

		#endregion PopupMenuHorizontalSeparator

		public PopupMenuItem() :
			this(null, null, true)
		{ }

		public PopupMenuItem(string iconUrl, object header, params UIElement[] subitems)
			: this(iconUrl, header, true, subitems)
		{ }

		public PopupMenuItem(string iconUrl, string header, string tag, bool useItemTemplate)
			: this(iconUrl, useItemTemplate, new TextBlock() { Text = header, Tag = tag })
		{ }

		public PopupMenuItem(string iconUrl, object header, bool useDefaultTemplate, params UIElement[] subitems)
			: base()
		{
			this.DefaultStyleKey = typeof(PopupMenuItem);

			DisplayShortcutKeyInRightMargin = true;

			VerticalSeparatorFill = PopupMenuUtils.MakeColorGradient(SeparatorStartColor, SeparatorEndColor, 0);

			// For some reason a null background limits the clickable area to the header text region only.
			if (Background == null)
				Background = new SolidColorBrush(Colors.Transparent);

			ShowLeftMargin = useDefaultTemplate;

			if (iconUrl != null)
				ImageSource = new BitmapImage(new Uri(iconUrl, UriKind.RelativeOrAbsolute));

			this.Header = header;

			// Add custom elements if any
			if (subitems != null)
				foreach (FrameworkElement element in subitems.Where(el => el != null))
				{
					if (element.Parent is Panel)
						(element.Parent as Panel).Children.Remove(element);
					this.Items.Add(element);
				}

			this.LayoutUpdated += delegate
			{
				var cp = this.GetVisualAncestors().OfType<ContentPresenter>().FirstOrDefault();
				if (cp != null && cp.HorizontalAlignment != HorizontalAlignment.Stretch)
					cp.HorizontalAlignment = HorizontalAlignment.Stretch;

				if (_container != null) // Don't bother calling synchContainerState if the container does not yet exist since it will eventually be called in OnApplyTemplate.
				{
					_container.Dispatcher.BeginInvoke(delegate
					{
						this.Container.IsEnabled = this.IsEnabled; // Sync the container state.
					});
				}

				_container = null; // Make sure the value returned by the Container getter is always up to date.
				
				_isLayoutUpdated = true;
			};

			PopupMenuManager.RegisterShortcut(Shortcut, this);

			this.Loaded += PopupMenuItem_Loaded;
		}

		void PopupMenuItem_Loaded(object sender, EventArgs e)
		{
			this.Loaded -= PopupMenuItem_Loaded; // Call this method only once.

			if ((Header == null) && this.Items.Count > 0)
			{
				object element = this.Items.First();
				this.Items.Remove(element);
				// Use first child as header if the latter is not assigned
				Header = element;
			}

			if (Header is string && Header.ToString().Contains('^'))
				Header = PopupMenuUtils.GenerateStackPanelWithUnderlinedText(Header.ToString(), '^');

			if (DisplayShortcutKeyInRightMargin && ShortcutKey != Key.None)
				this.ContentRight = "  " + Shortcut;
		}

		~PopupMenuItem()
		{
			if (PopupMenuManager.Shortcuts.ContainsKey(Shortcut))
				PopupMenuManager.Shortcuts.Remove(Shortcut);
		}

		/// <summary>
		/// This method is called when the template tree for the menu item is generated.
		/// </summary>
		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();
			ChangeVisualState(false);
			syncContainerState();
			this.Visibility = this.IsVisible ? Visibility.Visible : Visibility.Collapsed;
		}

		protected override void OnGotFocus(RoutedEventArgs e)
		{
			base.OnGotFocus(e);
			_isFocused = true;
			ChangeVisualState(true);
		}

		protected override void OnLostFocus(RoutedEventArgs e)
		{
			base.OnLostFocus(e);
			_isFocused = false;
			ChangeVisualState(true);
		}

		protected override void OnMouseEnter(MouseEventArgs e)
		{
			base.OnMouseEnter(e);
			ChangeVisualState(true);
		}

		protected override void OnMouseLeave(MouseEventArgs e)
		{
			base.OnMouseLeave(e);
			ChangeVisualState(true);
		}

		protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
		{
			if (!e.Handled)
			{
				OnClick(true, e);
				e.Handled = true;
			}
			base.OnMouseLeftButtonDown(e);
		}

		protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
		{
			if (!e.Handled)
			{
				OnClick(false, e); // Allow the user not to close the parent menu by using right instead of left clicks.
				e.Handled = true;
			}
			base.OnMouseRightButtonDown(e);
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			if (!e.Handled && (Key.Enter == e.Key))
			{
				OnClick();
				e.Handled = true;
			}
			base.OnKeyDown(e);
		}

		public virtual bool OnClick()
		{
			return OnClick(true, null);
		}

		public virtual bool OnClick(bool closeMenu, RoutedEventArgs e)
		{
			if (this.IsEnabled && this.IsVisible && this.Visibility == Visibility.Visible
				&& (this.Container == null || this.Container.Visibility == Visibility.Visible))
			{
				if (Click != null)
					Click(this, e);

				if (Command != null && Command.CanExecute(CommandParameter))
					Command.Execute(CommandParameter);
			}
			else
			{
				return false;
			}

			if (closeMenu && CloseOnClick)
				PopupMenuManager.CloseChildMenus(0, false, false, null);

			return true;
		}

		/// <summary>
		/// Changes to the correct visual state(s) for the control.
		/// </summary>
		/// <param name="useTransitions">True to use transitions; otherwise false.</param>
		protected virtual void ChangeVisualState(bool useTransitions)
		{
			VisualStateManager.GoToState(this, IsEnabled ? "Normal" : "Disabled", useTransitions);
			VisualStateManager.GoToState(this, IsEnabled && _isFocused ? "Focused" : "Unfocused", useTransitions);
		}

		/// <summary>
		/// Syncs the state the menu item with its container according to its visible and enabled state.
		/// Note that the latter is only accessible after the control is loaded.
		/// </summary>
		private void syncContainerState()
		{
			if (this.Container != null)
			{
				// Enable the container only when the child control is both enabled and visible.
				this.Container.IsEnabled = this.IsEnabled && this.IsVisible;

				// Collapsing the container sometimes does not have the desired effect(see Demo1 after uncommenting this line).
				//this.Container.Visibility = this.IsVisible ? Visibility.Visible : Visibility.Collapsed;

				// The workaround is to collapse the ContentPresenter element instead.
				var contentPresenter = this.GetVisualAncestors().OfType<ContentPresenter>().FirstOrDefault();
				if (contentPresenter != null)
					contentPresenter.Visibility = this.IsVisible ? Visibility.Visible : Visibility.Collapsed;
			}
		}

	}
}