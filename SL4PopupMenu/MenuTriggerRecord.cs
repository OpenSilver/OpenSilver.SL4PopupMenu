// Copyright (c) 2009 Ziad Jeeroburkhan. All Rights Reserved.
// GNU Library General Public License (LGPL) 
// (http://sl4popupmenu.codeplex.com/license)

using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Linq;

namespace SL4PopupMenu
{
	/// <summary>
	/// This class keeps a record of the trigger element associated with a menu.
	/// Each record is stored in the global list MenuTriggers within PopupMenuManager.
	/// A weak reference is used for the trigger element to eliminate potential memory leak issues.
	/// </summary>
	public class MenuTriggerRecord
	{
		/// <summary>
		/// The trigger element used to open the menu.
		/// </summary>
		public PopupMenuBase PopupMenuBase { get; set; }

		// Using a weak reference to avoid memory leak issues.
		private WeakReference _wrTriggerElement;

		//public static readonly DependencyProperty InternalDataContextProperty = DependencyProperty.Register("InternalDataContext",
		//typeof(Object), typeof(FrameworkElement), new PropertyMetadata((sender, e) =>
		//{
		//    PopupMenuManager.MenuTriggers.Where(mt => mt.TriggerElement == sender && mt.PopupMenuBase.InheritDataContext).ToList()
		//                                 .ForEach(mt => mt.PopupMenuBase.UpdateTriggers());
		//}));

		/// <summary>
		/// The trigger element used to open the menu.
		/// </summary>
		public FrameworkElement TriggerElement
		{
			get
			{
				object triggerElement = _wrTriggerElement.Target;
				// Remove our object from MenuTriggers if it no longer exists.
				if (triggerElement == null)
				{
					PopupMenuManager.MenuTriggers.Remove(this);
					return null;
				}
				return triggerElement as FrameworkElement;
			}

			set
			{
				_wrTriggerElement = new WeakReference(value);
				//if (this.PopupMenuBase.InheritDataContext)
				//    value.SetBinding(InternalDataContextProperty, new System.Windows.Data.Binding());
			}
		}

		/// <summary>
		/// The trigger type used to open the menu.
		/// </summary>
		public TriggerTypes TriggerType { get; set; }

	}
}
