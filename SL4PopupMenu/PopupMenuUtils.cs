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
using System.Windows.Data;
using System.Diagnostics;

namespace SL4PopupMenu
{
	//public class BindingEvaluator : FrameworkElement
	//{
	//    Binding _binding;

	//    public BindingEvaluator(Binding binding)
	//    {
	//        _binding = binding;
	//    }

	//    static readonly DependencyProperty ValueProperty =
	//        DependencyProperty.Register("Value", typeof(object), typeof(BindingEvaluator), new PropertyMetadata((o, e) =>
	//    {

	//    }));

	//    public object Value
	//    {
	//        get
	//        {
	//            Value = DependencyProperty.UnsetValue;
	//            SetBinding(BindingEvaluator.ValueProperty, _binding);
	//            return (object)GetValue(ValueProperty);
	//        }
	//        private set
	//        {
	//            SetValue(ValueProperty, value);
	//        }
	//    }
	//}

	/// <summary>
	/// A class that hosts all the project static methods and functions.
	/// </summary>
	public static class PopupMenuUtils
	{
		/// <summary>
		/// Generates a series of TextBlocks within a StackPanel which are meant to make a specified character
		/// within a text underlined.
		/// </summary>
		/// <param name="text">The text to be formatted.</param>
		/// <param name="underliningChar">The character to be underlined.</param>
		/// <returns>A series of TextBlocks within a StackPanel which are meant to make a specified character
		/// within a text underlined.</returns>
		public static StackPanel GenerateStackPanelWithUnderlinedText(string text, char underliningChar)
		{
			var headerParts = text.Split(underliningChar);

			var sp = new StackPanel { Orientation = Orientation.Horizontal, Tag = headerParts[1].Substring(0, 1) };
			sp.Children.Add(new TextBlock { Text = headerParts[0] });
			sp.Children.Add(new TextBlock { Text = sp.Tag.ToString(), TextDecorations = TextDecorations.Underline }); // Underlined character.
			sp.Children.Add(new TextBlock { Text = headerParts[1].Substring(1) });

			return sp;
		}

		/// <summary>
		/// Outputs the textual representation of a shortcut key.
		/// </summary>
		/// <param name="key">The shortcut key.</param>
		/// <param name="modifierKeys">The shortcut key modifiers.</param>
		/// <returns>The shortcut key as text. </returns>
		public static string GetShortcutAsText(Key key, ModifierKeys modifierKeys)
		{
			string pressedModifierKeys = null;

			foreach (byte flag in new byte[] { 2, 4, 1, 8 })
				if ((modifierKeys & (ModifierKeys)flag) == (ModifierKeys)flag)
					pressedModifierKeys += (ModifierKeys)flag + "+";

			return (pressedModifierKeys + key).Replace(ModifierKeys.Control.ToString(), "Ctrl");
		}

		/// <summary>
		/// Convert a textual representation of a shortcut into its constituent values starting with the main key
		/// followed by two modifier keys.
		/// </summary>
		/// <param name="value">The shortcut key text.</param>
		/// <returns>An array representing the keys used in the shortcut.</returns>
		public static byte[] GetShortcutValues(string value)
		{
			byte[] shortcutValues = new byte[] { 0, 0, 0 };

			var keys = value.ToLower().Replace("ctrl", ModifierKeys.Control.ToString())
				.Split('+').Reverse().ToArray();

			for (int i = 0; i < keys.Length; i++)
			{
				switch (i)
				{
					case 0:
						shortcutValues[0] = (byte)(Key)Enum.Parse(typeof(Key), keys[i], true);
						break;
					case 1:
						shortcutValues[1] = (byte)(ModifierKeys)Enum.Parse(typeof(ModifierKeys), keys[i], true);
						break;
					case 2:
						shortcutValues[2] = (byte)(ModifierKeys)Enum.Parse(typeof(ModifierKeys), keys[i], true);
						break;
				}
			}
			return shortcutValues;
		}

		/// <summary>
		/// Get item container of Type T for the specified item within an ItemsControl
		/// even if it is a child of another element within the container.
		/// </summary>
		/// <param name="item">The element to start from.</param>
		/// <returns>The item's container. Note that a null is returned when called before the visual tree is created.</returns>
		public static Control GetItemContainer(FrameworkElement item)
		{
			return GetItemContainer(null, item);
		}

		/// <summary>
		/// Get item container of Type T(typically a ListBoxItem for a ListBox as ItemsControl) for the specified item within an ItemsControl
		/// even if it is a child of another element within the container.
		/// </summary>
		/// <param name="itemsControl">The ItemsControl containing the item container.</param>
		/// <param name="item">The element to start from.</param>
		/// <returns>The item's container. Note that a null is returned when called before the visual tree is created.</returns>
		public static Control GetItemContainer(ItemsControl itemsControl, FrameworkElement item)// where T : class
		{
			if (itemsControl is PopupMenuItem)
				itemsControl = null;

			// Find then ItemsControl containing the item if it is not specified.
			if (itemsControl == null)
				itemsControl = item.Parent as ItemsControl;

			if (itemsControl == null)
				itemsControl = item.GetVisualAncestors().Take(5) // No more than 5 levels up
									.OfType<ItemsControl>()
									.Where(i => !(i is PopupMenuItem)).FirstOrDefault();

			if (itemsControl != null)
			{
				object container = null;
				// Look for the container while moving up the visual tree.
				var ancestors = new List<Control>();

				// All parent controls under the ItemsControl have been scanned.
				// Return the item itself if the item was generated using a basic ItemsControl(in
				// which case no intermediate parent might be there) or null otherwise.
				foreach (var ancestor in item.GetVisualAncestorsAndSelf()
											 .OfType<Control>()
											 .Where(p => !(p is ScrollViewer)))
				{
					if (ancestor == itemsControl)
						break;

					container = itemsControl.ItemContainerGenerator.ContainerFromItem(ancestor);
					if (container is Control)
						return container as Control;

					if (ancestor is ContentControl || ancestor is ItemsControl || ancestor is UserControl)
						ancestors.Insert(0, ancestor);
				}

				container = ancestors.Where(p => p is ListBoxItem
												|| p is TreeViewItem
												|| p is ComboBoxItem).FirstOrDefault();

				return (container ?? ancestors.FirstOrDefault()) as Control;
			}
			return null;
		}

		/// <summary>
		/// Flip the orientation for an MenuOrientationTypes enum.
		/// </summary>
		/// <param name="orientation">The MenuOrientationTypes enum to invert.</param>
		/// <returns>The inverted MenuOrientationTypes enum.</returns>
		public static MenuOrientationTypes FlipOrientation(MenuOrientationTypes orientation)
		{
			switch (orientation)
			{
				case MenuOrientationTypes.Right:
					orientation = MenuOrientationTypes.Left;
					break;
				case MenuOrientationTypes.Bottom:
					orientation = MenuOrientationTypes.Top;
					break;
				case MenuOrientationTypes.Left:
					orientation = MenuOrientationTypes.Right;
					break;
				case MenuOrientationTypes.Top:
					orientation = MenuOrientationTypes.Bottom;
					break;
			}
			return orientation;
		}

		/// <summary>
		/// Sets the position of an element within a canvas while optionally keeping the element within layout bounds
		/// and returns the new element position(this is adjusted to fit within the layout if keepWithinLayoutBounds is true).
		/// </summary>
		/// <param name="element">The element to be positioned.</param>
		/// <param name="targetPosition">The targeted position.</param>
		/// <param name="keepWithinLayoutBounds">When true the position is adjusted such that the element stays within the application bounds.</param>
		/// <returns>The new element position.</returns>
		public static Point SetPosition(FrameworkElement element, Point targetPosition, bool keepWithinLayoutBounds)
		{
			Point windowSize = ScaleCoordinatesToZoomValue(new Point(
				Application.Current.Host.Content.ActualWidth, Application.Current.Host.Content.ActualHeight));

			if (keepWithinLayoutBounds)
			{
				if (targetPosition.X + element.ActualWidth > windowSize.X)
					targetPosition.X = windowSize.X - element.ActualWidth;

				if (targetPosition.Y + element.ActualHeight > windowSize.Y)
					targetPosition.Y = windowSize.Y - element.ActualHeight;

				if (targetPosition.X < 0)
					targetPosition.X = 0;

				if (targetPosition.Y < 0)
					targetPosition.Y = 0;
			}

			//element.MaxHeight = Application.Current.MainWindow.Height;
			//element.MaxWidth = Application.Current.MainWindow.Width;

			Canvas.SetLeft(element, targetPosition.X);
			Canvas.SetTop(element, targetPosition.Y);
			//element.Margin = new Thickness(targetPosition.X, targetPosition.Y, 0, 0);
			return targetPosition;
		}

		/// <summary>
		/// Make a FrameworkElement the child of a specified Panel or ContentControl.
		/// </summary>
		/// <param name="childElement">The child element</param>
		/// <param name="targetedParentControlOrPanel">The parent control</param>
		/// <param name="addDefaultShadowEffect">When true the default shadow effect is also added to the child element</param>
		public static bool MoveElementTo(FrameworkElement childElement, FrameworkElement targetedParentControlOrPanel, bool addDefaultShadowEffect, bool clearTargetedParentContent)
		{
			if (DesignerProperties.IsInDesignTool || childElement == null || targetedParentControlOrPanel == null || targetedParentControlOrPanel == childElement.Parent)
				return false;

			MakeOrphan(childElement);

			if (targetedParentControlOrPanel is Panel)
			{
				if (clearTargetedParentContent)
					(targetedParentControlOrPanel as Panel).Children.Clear();
				(targetedParentControlOrPanel as Panel).Children.Add(childElement);
			}
			else if (targetedParentControlOrPanel is ContentControl)
			{
				(targetedParentControlOrPanel as ContentControl).Content = childElement;
			}

			// Add the default shadow effect if the child element doesn't have any
			if (childElement.Effect == null && addDefaultShadowEffect)
				childElement.Effect = new DropShadowEffect { Color = Colors.Black, BlurRadius = 4, Opacity = 0.5, ShadowDepth = 2 };
			return true;
		}

		/// <summary>
		/// Dissociates a framework element from its parent.
		/// </summary>
		/// <param name="childElement">The framework element to be orphaned.</param>
		public static void MakeOrphan(FrameworkElement childElement)
		{
			if (childElement.Parent != null)
			{
				if (childElement.Parent is ItemsControl) // If the control lies inside an ItemsControl
					(childElement.Parent as ItemsControl).Items.Remove(childElement); // dissociate it the ItemsControl.
				else if (childElement.Parent is ContentControl) // If the control lies inside a ContentControl or the current PopupMenu content
					(childElement.Parent as ContentControl).Content = null; // dissociate it from the current ContentControl or PopupMenu content.
				else if (childElement.Parent is Panel) // If the control lies inside a Panel
					(childElement.Parent as Panel).Children.Remove(childElement); // dissociate it from the Panel.
				else
					throw new Exception("The content element must be placed in a container that inherits from the Panel or ContentControl class. "
									  + "The actual parent type is " + childElement.Parent.GetType());
			}
		}

		/// <summary>
		/// Find an element in the application by its name.
		/// </summary>
		/// <param name="self">Any element lying besides the element being searched ie sharing the same parent.
		/// This provides a starting point for the search and reduces the overhead involved in searching the whole
		/// visual tree but can be ommited with a null value.</param>
		/// <param name="elementName">The name of the element to find.</param>
		/// <param name="userFriendlyQualifier">A short user friendly title to refer to our element within error messages.</param>
		/// </summary>
		public static FrameworkElement FindElementByName(FrameworkElement startingPointElement, string elementName, string userFriendlyQualifier)
		{
			object obj = null;

			// Look for object in neighbourhood.
			if (startingPointElement != null && startingPointElement.Parent != null)
				obj = ((FrameworkElement)startingPointElement.Parent).FindName(elementName.Trim());

			if (obj == null) // Object not found. Search down from root.
				obj = (Application.Current.RootVisual as FrameworkElement).FindName(elementName.Trim());

			if (obj == null) // Object still not found. Search using the more thorough and costly method.
				obj = Application.Current.RootVisual.GetVisualDescendants()
					.OfType<FrameworkElement>()
					.Where(elem => elem.Name == elementName).FirstOrDefault();

			if (obj != null)
			{
				if (obj is UIElement)
				{
					return obj as FrameworkElement;
				}
				else
				{
					if (userFriendlyQualifier != null && !DesignerProperties.IsInDesignTool) // Error messages are disabled at design time
						throw new ArgumentException("The " + userFriendlyQualifier + " is referenced to " + elementName + " which is not a UIElement.");
				}
			}
			else
			{
				if (userFriendlyQualifier != null && !DesignerProperties.IsInDesignTool) // Error messages are disabled at design time
					throw new ArgumentException((startingPointElement.Name == null ? "Could" : startingPointElement.Name + " could")
						+ " not find any element named " + elementName + " for " + userFriendlyQualifier + " in the visual tree.");
			}
			return null;
		}


		/// <summary>
		/// Determine if the mouse is over any of the FrameworkElements specified.
		/// </summary>
		/// <param name="e">The mouse-related arguments</param>
		/// <param name="isPopupChild">Set the value to true if the elements specified are within a Popup control.
		/// This helps compensate for the fact that the Silverlight coordinate system for elements inside a Popup control does not take into account the zoom factor.</param>
		/// <param name="autoMapHoverBoundsToParentOfTextBlocks">
		/// Switch to the parent for bounds info if the element under the mouse is a Textblock to avoid dealing with widths
		/// that can change with the text length.</param>
		/// <param name="elements">The FrameworkElements being HitTested.</param>
		public static FrameworkElement GetFirstElementUnderMouse(MouseEventArgs e, bool autoMapHoverBoundsToParentOfTextBlocks, params FrameworkElement[] elements)
		{
			foreach (FrameworkElement elem in elements)
			{
				// Textblocks have a variable width, depending on the text length, when none is specified.
				bool isIndefiniteWidthTextBlock = autoMapHoverBoundsToParentOfTextBlocks
											 && elem is TextBlock
											 && double.IsNaN((elem as TextBlock).Width)
											 && double.IsInfinity((elem as TextBlock).MaxWidth);

				// In this case the parent element is used for hit testing to avoid limiting the hover region to the TextBlock whose size varies with its content.
				if (HitTestAny(e, isIndefiniteWidthTextBlock && elem.Parent != null
					? elem.Parent as FrameworkElement
					: elem))
					return elem;
			}
			return null;
		}

		/// <summary>
		/// Gets all on top visual tree items under the mouse.
		/// </summary>
		/// <param name="sender">The element over which the mouse is.</param>
		/// <param name="e">The event arguments sent by the element.</param>
		/// <returns>An IEnumerable of a all the items under the mouse.</returns>
		public static IEnumerable<UIElement> GetVisualTreeItemsUnderMouse(UIElement sender, MouseEventArgs e)
		{
			//var topOpenMenu = PopupMenuManager.OpenMenus.Where(m => !m.IsPinned).FirstOrDefault();

			var pt = e.GetPosition(null);

			//if (topOpenMenu == null)
			//    pt = ScaleCoordinatesToZoomValue(pt);

			return VisualTreeHelper.FindElementsInHostCoordinates(pt,
				sender.GetVisualAncestorsAndSelf().OfType<UIElement>().Last());
		}

		/// <summary>
		/// Get the item under the mouse.
		/// </summary>
		/// <param name="visualTreeElementsUnderMouse">All the elements in the visual tree hierarchy where the mouse was clicked or hovered.</param>
		/// <param name="senderElement">The containing element</param>
		/// <param name="returnSelectableItemIfAny">Return the clicked or hovered item inside the trigger element if the latter is a DataGrid, a ListBox or a TreeView</param>
		/// <param name="selectItemIfSelectable">Select the item if it lies in a ListBox, Datagrid or TreeView</param>
		/// <returns>The item under the mouse</returns>
		public static FrameworkElement GetAndSelectItemUnderMouse(IEnumerable<UIElement> visualTreeElementsUnderMouse, FrameworkElement senderElement, bool returnSelectableItemIfAny, bool selectItemIfSelectable)
		{
			Control element = null;

			if (senderElement is Control)
			{
				// This is used to avoid selecting any items when the control key is pressed since this also unselects any the other selected items.
				bool isControlKeyPressed = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;

				if (senderElement.GetType().Name == "DataGrid")
				{
					if ((element = visualTreeElementsUnderMouse.OfType<Control>().Where(elm => elm.GetType().Name == "DataGridRow").FirstOrDefault()) == null)
						return null;

					if (selectItemIfSelectable && !isControlKeyPressed)
						SetValue(senderElement, "SelectedIndex", element.GetType().GetMethod("GetIndex").Invoke(element, null));
				}
				else
				{
					element = visualTreeElementsUnderMouse.OfType<Control>().FirstOrDefault();

					if (senderElement is ItemsControl || (element != null && element.Parent is ItemsControl))
						element = PopupMenuUtils.GetItemContainer(senderElement as ItemsControl, element);

					if (selectItemIfSelectable)
					{
						if (element != null)
						{
							if (!isControlKeyPressed)
								SetValue(element, "IsSelected", true);
						}
						else
						{
							senderElement = null;
						}
					}
				}
			}

			if (element != null && returnSelectableItemIfAny)
				senderElement = element;

			return senderElement;
		}

		/// <summary>
		/// Set a property value for an object using reflection.
		/// </summary>
		/// <param name="obj">The target object.</param>
		/// <param name="propertyName">The target object property name.</param>
		/// <param name="value">The property value.</param>
		public static void SetValue(object obj, string propertyName, object value)
		{
			var prop = obj.GetType().GetProperty(propertyName);
			if (prop != null && prop.GetValue(obj, null) != value)
				prop.SetValue(obj, value, null);
		}

		//public static T GetElementByTypeAndIndex<T>(IEnumerable<UIElement> elements, int index)
		//{
		//    foreach (object elem in elements.OfType<T>())
		//        if (index-- <= 0)
		//            return (T)elem;
		//    return default(T);
		//}

		/// <summary>
		/// Determine if the mouse is over any of the FrameworkElements specified.
		/// </summary>
		/// <param name="e">The mouse-related arguments</param>
		/// <param name="isPopupChild">Set the value to true if the elements specified are within a Popup control.
		/// This helps compensate for the fact that the Silverlight coordinate system for elements inside a Popup control does not take into account the zoom factor.</param>
		/// <param name="autoMapHoverBoundsToParentOfTextBlocks">
		/// Switch to the parent for bounds info if the element under the mouse is a Textblock to avoid dealing with widths
		/// that can change with the text length.</param>
		/// <param name="valueToReturnIfAnyHitTestElementIsNull">The value to return when any of the elements to HitTest is null</param>
		/// <param name="elements">The FrameworkElements being HitTested.</param>
		public static bool HitTestAny(MouseEventArgs e, bool autoMapHoverBoundsToParentOfTextBlocks, bool valueToReturnIfAnyHitTestElementIsNull, params FrameworkElement[] elements)
		{
			if (elements.Contains(null))
				return valueToReturnIfAnyHitTestElementIsNull;
			else
				return GetFirstElementUnderMouse(e, autoMapHoverBoundsToParentOfTextBlocks, elements) != null;
		}

		/// <summary>
		/// Determine if the mouse is over any of the FrameworkElements specified
		/// </summary>
		/// <param name="e">The mouse related arguments</param>
		/// <param name="isPopupChild">Set the value to true if the elements specified are within a Popup control.
		/// This helps compensate for the fact that the Silverlight coordinate system for elements inside a Popup control does not take into account the zoom factor.</param>
		/// <param name="elements">The FrameworkElements being HitTested.</param>
		public static bool HitTestAny(MouseEventArgs e, params UIElement[] elements)
		{
			var layers = PopupMenuManager.OpenMenus.Where(pm => !pm.IsPinned)
				.Select(pm => pm.OverlayPopup as UIElement).ToList();

			layers.Insert(0, Application.Current.RootVisual);

			UIElement layerBelow = null;

			foreach (var layer in layers)
			{
				foreach (var ele in VisualTreeHelper.FindElementsInHostCoordinates(e.GetSafePosition(null), layer))
					if (elements.Contains(ele))
						return true;

				foreach (var elem in VisualTreeHelper.FindElementsInHostCoordinates(e.GetSafePosition(layerBelow), layer))
					if (elements.Contains(elem))
						return true;

				layerBelow = layer;
			}

			return false;
		}


		/// <summary>
		/// Get the absolute position for a FrameworkElement.
		/// </summary>
		/// <param name="isPopupChild">Set the value to true if the elements specified are within a Popup control.
		/// This helps compensate for the fact that the Silverlight coordinate system for elements inside a Popup control does not take into account the zoom factor.</param>
		/// <param name="element">The FrameworkElement being HitTested.</param>
		public static Point GetAbsoluteElementPos(FrameworkElement element)
		{
			var topOpenMenu = PopupMenuManager.OpenMenus.Where(m => !m.IsPinned).FirstOrDefault();

			Point pt = new Point();
			try
			{
				var transform = new TransformGroup();
				transform.Children.Add(element.TransformToVisual(null) as Transform);

				if (topOpenMenu == null)
					transform.Children.Add(new ScaleTransform()
					{
						ScaleX = 1D / Application.Current.Host.Content.ZoomFactor,
						ScaleY = 1D / Application.Current.Host.Content.ZoomFactor,
					});

				pt = transform.Transform(new Point());

				//Rect rect = element.GetBoundsRelativeTo(Application.Current.RootVisual).Value;
				//pt = new Point(rect.Left, rect.Top);
			}
			catch { }

			//if (isPopupChild)
			//    pt = ScaleCoordinatesToZoomValue(pt);

			return pt;
		}

		//public static Point GetAbsoluteMousePos(MouseEventArgs e)
		//{
		//    // This will not work for MouseLeave events
		//    Point pt = e.GetSafePosition(null);// Application.Current.RootVisual.TransformToVisual(null).Transform(e.GetPosition(Application.Current.RootVisual));
		//    return pt;
		//}

		//public static Point GetAbsoluteMousePos(MouseEventArgs e, FrameworkElement element)
		//{
		//    Point pt = element.TransformToVisual(null).Transform(e.GetPosition(element));
		//    return ScaleCoordiantesToZoomValue(pt);
		//}

		private static Point ScaleCoordinatesToZoomValue(Point pt)
		{
			double zoomFactor = Application.Current.Host.Content.ZoomFactor;
			if (zoomFactor != 1)
			{
				pt.X /= zoomFactor;
				pt.Y /= zoomFactor;
			}
			return pt;
		}

		public static LinearGradientBrush MakeColorGradient(Color startColor, Color endColor, double angle)
		{
			var gradientStopCollection = new GradientStopCollection();
			gradientStopCollection.Add(new GradientStop { Color = startColor, Offset = 0 });
			gradientStopCollection.Add(new GradientStop { Color = endColor, Offset = 1 });

			return new LinearGradientBrush(gradientStopCollection, angle);
		}

		//public static Storyboard CreateStoryBoard(int beginTime, int duration, FrameworkElement element, string targetProperty, double? from, double? to)
		//{
		//    return CreateStoryBoard(beginTime, duration, element, targetProperty, from, to, false);
		//}

		//public static Storyboard CreateStoryBoard(int beginTime, int duration, FrameworkElement element, string targetProperty, double? from, double? to, bool beginNow)
		//{
		//    DoubleAnimation da = new DoubleAnimation
		//    {
		//        From = from,
		//        To = to,
		//        Duration = new TimeSpan(0, 0, 0, 0, duration < 0 ? 0 : duration)
		//    };

		//    if (element != null)
		//        Storyboard.SetTarget(da, element);
		//    Storyboard.SetTargetProperty(da, new PropertyPath(targetProperty));

		//    Storyboard sb = new Storyboard();
		//    sb.Children.Add(da);
		//    sb.BeginTime = new TimeSpan(0, 0, 0, 0, beginTime);
		//    if (beginNow)
		//        sb.Begin();
		//    RegisterStoryBoardTarget(sb, element);
		//    return sb;
		//}

		public static Storyboard CreateStoryBoard(int beginTime, int duration, FrameworkElement element, string targetProperty, double value, EasingFunctionBase easingFunction)
		{
			return CreateStoryBoard(beginTime, duration, element, targetProperty, value, easingFunction, false);
		}

		public static Storyboard CreateStoryBoard(int beginTime, int duration, FrameworkElement element, string targetProperty, double value, EasingFunctionBase easingFunction, bool beginNow)
		{
			var edkf = new EasingDoubleKeyFrame
			{
				KeyTime = new TimeSpan(0, 0, 0, 0, duration < 0 ? 0 : duration),
				Value = value,
				EasingFunction = easingFunction
			};

			var da = new DoubleAnimationUsingKeyFrames();
			da.KeyFrames.Add(edkf);

			if (element != null)
				Storyboard.SetTarget(da, element);

			if (targetProperty != null)
				Storyboard.SetTargetProperty(da, new PropertyPath(targetProperty));

			var sb = new Storyboard();
			sb.Children.Add(da);
			sb.BeginTime = new TimeSpan(0, 0, 0, 0, beginTime);

			if (beginNow)
				sb.Begin();

			RegisterStoryBoardTarget(sb, element);
			return sb;
		}

		/// <summary>
		/// Register a storyboard(storyboards outside a PopupMenu need to be registered before use).
		/// </summary>
		/// <param name="visualStateGroup">The VisualStateGroup to register.</param>
		/// <param name="targetElements">The target elements for the storyboard.</param>
		public static void RegisterVisualStateGroupTargets(VisualStateGroup visualStateGroup, params FrameworkElement[] targetElements)
		{
			foreach (VisualState state in visualStateGroup.States)
				RegisterStoryBoardTargets(state.Storyboard, targetElements);
		}

		/// <summary>
		/// Register a storyboard(storyboards outside a PopupMenu need to be registered before use).
		/// </summary>
		/// <param name="storyBoard">The storyboard to register.</param>
		/// <param name="targetElements">The target elements for the storyboard.</param>
		public static void RegisterStoryBoardTargets(Storyboard storyBoard, params FrameworkElement[] targetElements)
		{
			foreach (FrameworkElement targetElement in targetElements)
				RegisterStoryBoardTarget(storyBoard, targetElement);
		}

		/// <summary>
		/// Register a storyboard(storyboards outside a PopupMenu need to be registered before use).
		/// </summary>
		/// <param name="storyBoard">The storyboard to register.</param>
		/// <param name="targetElement">The target element for the storyboard.</param>
		public static void RegisterStoryBoardTarget(Storyboard storyBoard, FrameworkElement targetElement)
		{
			storyBoard.Stop();
			targetElement.Dispatcher.BeginInvoke(delegate
			{
				foreach (Timeline tl in storyBoard.Children.OfType<Timeline>()
						.Where(tl => Storyboard.GetTargetName(tl) == targetElement.Name))
					Storyboard.SetTarget(tl, targetElement);
			});
		}

		//public static T GetStyleValue<T>(Style style, DependencyProperty dp)
		//{
		//    Setter setter = GetStyleSetter(style, dp);
		//    return setter.Value == null ? default(T) : (T)setter.Value;
		//}

		//public static Setter GetStyleSetter(Style style, DependencyProperty dp)
		//{
		//    return style.Setters.OfType<Setter>().Where(s => s.Property == dp).FirstOrDefault();
		//}

		//public static UIElement CloneUIElement(UIElement copyMe, string instance, Canvas parent)
		//{
		//    return RecurseCloneUIElement(copyMe, instance, parent);
		//}

		//public static UIElement RecurseCloneUIElement(UIElement copyMe, string instance, Canvas parent)
		//{
		//    UIElement temp = copyMe as UIElement;
		//    switch (copyMe.ToString())
		//    {
		//        case "System.Windows.Controls.Canvas":
		//            temp = CloneCanvas((Canvas)copyMe, instance, parent);
		//            break;
		//    }
		//    return temp;
		//}

		//public static Canvas CloneCanvas(Canvas copyMe, string instance, Canvas parent)
		//{
		//    Canvas clonedCanvas = new Canvas();
		//    clonedCanvas.SetValue(Canvas.NameProperty, copyMe.GetValue(Canvas.NameProperty) + "_" + instance);
		//    clonedCanvas.SetValue(Canvas.LeftProperty, (double)copyMe.GetValue(Canvas.LeftProperty));
		//    clonedCanvas.SetValue(Canvas.TopProperty, (double)copyMe.GetValue(Canvas.TopProperty));
		//    clonedCanvas.SetValue(Canvas.WidthProperty, (double)copyMe.GetValue(Canvas.WidthProperty));
		//    clonedCanvas.SetValue(Canvas.HeightProperty, (double)copyMe.GetValue(Canvas.HeightProperty));

		//    // ONLY CLONING SOLID BRUSHES AT THE MOMENT.
		//    SolidColorBrush cloneColor = copyMe.GetValue(Canvas.BackgroundProperty) as SolidColorBrush;
		//    if (cloneColor != null) { clonedCanvas.SetValue(Canvas.BackgroundProperty, cloneColor.Color.ToString()); }

		//    foreach (var child in copyMe.Children)
		//    {
		//        clonedCanvas.Children.Add(RecurseCloneUIElement((UIElement)child, instance, clonedCanvas));
		//    }
		//    return clonedCanvas;
		//}

		///// <summary>
		///// Perform an action through the dispatchers of each element in a given list.
		///// The dispatcher respective to each element is invoked in the same order as that of the elements in the list
		///// and the action is only performed only when all dispatchers are successfully invoked.
		///// </summary>
		///// <param name="dispatcherElements">The elements whose dispatchers will be used.</param>
		///// <param name="a">The action.</param>
		//public static void InvokeMultiDispatcher(List<UIElement> dispatcherElements, Action a)
		//{
		//    var elems = dispatcherElements.Where(el => el != null);
		//    if (elems.FirstOrDefault() != null)
		//        elems.First().Dispatcher.BeginInvoke(delegate { InvokeMultiDispatcher(dispatcherElements.Skip(1).ToList(), a); });
		//    else
		//        elems.First().Dispatcher.BeginInvoke(a);
		//}

	}

	/*public static class ContextMenuService
	{
		public static PopupMenu GetContextMenu(DependencyObject obj)
		{
			return (PopupMenu)obj.GetValue(ContextMenuProperty);
		}
		public static void SetContextMenu(DependencyObject obj, PopupMenu value)
		{
			obj.SetValue(ContextMenuProperty, value);
		}

		public static readonly DependencyProperty ContextMenuProperty = DependencyProperty.RegisterAttached(
			"ContextMenu",
			typeof(PopupMenu),
			typeof(ContextMenuService),
			new PropertyMetadata(null, OnContextMenuChanged));

		/// <summary>
		/// Handles changes to the ContextMenu DependencyProperty.
		/// </summary>
		/// <param name="o">DependencyObject that changed.</param>
		/// <param name="e">Event data for the DependencyPropertyChangedEvent.</param>
		private static void OnContextMenuChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
		{
			FrameworkElement element = o as FrameworkElement;
			if (null != element)
			{
				PopupMenu oldContextMenu = e.OldValue as PopupMenu;
				if (null != oldContextMenu)
				{
					//oldContextMenu.LeftClickElementSelector = null;
				}
				PopupMenu newContextMenu = e.NewValue as PopupMenu;
				if (null != newContextMenu)
				{
					newContextMenu.AddRightClickElementSelector(element);
				}
			}
		}
	}*/



	///// <summary>
	///// Implements a weak event listener that allows the owner to be garbage
	///// collected if its only remaining link is an event handler.
	///// </summary>
	///// <typeparam name="TInstance">Type of instance listening for the event.</typeparam>
	///// <typeparam name="TSource">Type of source for the event.</typeparam>
	///// <typeparam name="TEventArgs">Type of event arguments for the event.</typeparam>
	//public class WeakEventListener<TInstance, TSource, TEventArgs> where TInstance : class
	//{

	//    private WeakReference _weakInstance;

	//    public Action<TInstance, TSource, TEventArgs> OnEventAction { get; set; }

	//    public Action<WeakEventListener<TInstance, TSource, TEventArgs>> OnDetachAction { get; set; }

	//    public WeakEventListener(TInstance instance)
	//    {
	//        if (instance == null)
	//            throw new ArgumentNullException("instance");

	//        this._weakInstance = new WeakReference(instance);
	//    }

	//    /// <summary>
	//    /// Handler for the subscribed event calls OnEventAction to handle it.
	//    /// </summary>
	//    /// <param name="source">Event source.</param>
	//    /// <param name="eventArgs">Event arguments.</param>
	//    public void OnEvent(TSource source, TEventArgs eventArgs)
	//    {
	//        TInstance target = this._weakInstance.Target as TInstance;

	//        if (target != null)
	//        {
	//            if (this.OnEventAction != null)
	//                this.OnEventAction(target, source, eventArgs);
	//        }
	//        else
	//        {
	//            this.Detach();
	//        }
	//    }

	//    public void Detach()
	//    {
	//        if (this.OnDetachAction != null)
	//        {
	//            this.OnDetachAction(this);
	//            this.OnDetachAction = null;
	//        }
	//    }
	//}

}