// Copyright (c) 2009 Ziad Jeeroburkhan. All Rights Reserved.
// GNU Library General Public License (LGPL) 
// (http://sl4popupmenu.codeplex.com/license)

using System.Windows;
using System.Windows.Media;

namespace SL4PopupMenu
{
	/// <summary>
	/// The PopupMenu Seperator implementation.
	/// </summary>
	public class PopupMenuSeparator : PopupMenuItem
	{
		/// <summary>
		/// The PopupMenu separator class.
		/// </summary>
		public PopupMenuSeparator()
			: base(null, null, true)
		{
			HorizontalSeparatorVisibility = Visibility.Visible;
			IsEnabled = false;
			CloseOnClick = false;

			Color endColor = SeparatorEndColor;
			if (endColor.A + 10 <= 255)
				endColor.A += 10;

			HorizontalSeparatorBrush = PopupMenuUtils.MakeColorGradient(SeparatorStartColor, endColor, 90);
		}
	}
}