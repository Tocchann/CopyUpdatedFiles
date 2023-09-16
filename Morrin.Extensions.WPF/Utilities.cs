using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Morrin.Extensions.WPF;

public static class Utilities
{
	public static Window? GetOwnerWindow()
	{
		var window = default(Window);
		foreach( Window search in Application.Current.Windows )
		{
			if( search.IsActive && search.Parent == null )
			{
				window = search;
				break;
			}
		}
		if( window == null )
		{
			window = Application.Current.MainWindow;
		}
		return window;
	}
}
