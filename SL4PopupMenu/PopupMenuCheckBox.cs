// Copyright (c) 2009 Ziad Jeeroburkhan. All Rights Reserved.
// GNU Library General Public License (LGPL) 
// (http://sl4popupmenu.codeplex.com/license)

using System.Windows;
using System.Windows.Media;
using System;
using System.Threading;

namespace SL4PopupMenu
{
	/// <summary>
	/// The Popupmenu CheckBox class.
	/// </summary>
	public class PopupMenuCheckBox : PopupMenuItem
	{
		public event RoutedEventHandler CheckedValueChanged;

		public bool IsThreeState { get; set; }

		public ImageSource ImageCheckedSource { get; set; }

		public ImageSource ImageUncheckedSource { get; set; }

		public ImageSource ImageIntermediateSource { get; set; }

		/// <summary>
		/// Use the the image on the right margin instead to display the checkbox state.
		/// </summary>
		public bool IsFlipped { get; set; }

		/// <summary>
		/// The checked state of the checkbox.
		/// A three state value(Checked, Intermediate or Unchecked) is used when IsThreeState is set to true.
		/// The respective images for each state can be set via the properties ImageCheckedPath, ImageIntermediatePath
		/// and ImageUncheckedPath.
		/// </summary>
		public bool? IsChecked
		{
			get { return (bool?)GetValue(IsCheckedProperty); }
			set { SetValue(IsCheckedProperty, value); }
		}

		public static readonly DependencyProperty IsCheckedProperty = RegisterDependency<bool?>("IsChecked", false, (d, e) =>
		{
			var pmcb = (PopupMenuCheckBox)d;
			// Store the EventHandler into a local temp variable for thread-safety
			//EventHandler temp;
			if (!pmcb.IsChecked.HasValue)
			{
				pmcb.SetImageSource(pmcb.ImageIntermediateSource, pmcb.IsFlipped);
				//temp = Interlocked.CompareExchange(ref pmcb.Indeterminate, null, null);
			}
			else if (pmcb.IsChecked.Value)
			{
				pmcb.SetImageSource(pmcb.ImageCheckedSource, pmcb.IsFlipped);
				//temp = Interlocked.CompareExchange(ref pmcb.Checked, null, null);
			}
			else
			{
				pmcb.SetImageSource(pmcb.ImageUncheckedSource, pmcb.IsFlipped);
				//temp = Interlocked.CompareExchange(ref pmcb.Unchecked, null, null);
			}

			//if (temp != null)
			//	temp(pmcb, EventArgs.Empty);
		});

		private void SetImageSource(ImageSource imageSource, bool targetImageOnRight)
		{
			if (targetImageOnRight)
				ImageRightSource = imageSource;
			else
				ImageSource = imageSource;
		}


		public PopupMenuCheckBox()
			: base(null, null, true)
		{ }

		public PopupMenuCheckBox(object header)
			: base(null, header, true)
		{ }

		public override bool OnClick(bool closeMenu, RoutedEventArgs e)
		{
			if (IsEnabled)
				ToggleCheckedValue(closeMenu, e);
			return base.OnClick(closeMenu, e);
		}

		public void ToggleCheckedValue(bool closeMenu, RoutedEventArgs e)
		{
			if (!IsThreeState)
			{
				IsChecked = !IsChecked ?? true; // null -> true
			}
			else
			{
				if (IsChecked.HasValue)
					IsChecked = IsChecked.Value ? (bool?)null : true; // false -> true || true -> null
				else
					IsChecked = false; // null -> false
			}

			if (CheckedValueChanged != null)
				CheckedValueChanged(this, e);

			if (Command != null && Command.CanExecute(CommandParameter))
				Command.Execute(CommandParameter);
		}
	}
}