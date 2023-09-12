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
		var ownerWindow = Application.Current.MainWindow;
		foreach( Window window in Application.Current.Windows )
		{
			if( window.IsActive )
			{
				ownerWindow = window;
				break;
			}
		}
		return ownerWindow;
	}
}
