using System;

public interface IDataContextChangedHandler<T> where T : FrameworkElement
{
	void DataContextChanged(T sender, DependencyPropertyChangedEventArgs e);
}


public static class DataContextChangedHelper<T> where T : FrameworkElement, IDataContextChangedHandler<T>
{
	private const string INTERNAL_CONTEXT = "InternalDataContext";

	public static readonly DependencyProperty InternalDataContextProperty =
		DependencyProperty.Register(INTERNAL_CONTEXT,
									typeof(Object),
									typeof(T),
									new PropertyMetadata(_DataContextChanged));

	private static void _DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
	{
		T control = (T)sender;
		control.DataContextChanged(control, e);
	}

	public static void Bind(T control)
	{
		control.SetBinding(InternalDataContextProperty, new Binding());
	}
}