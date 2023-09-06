using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CopyFiles.Models;

public partial class TargetInformation : ObservableObject
{
	[ObservableProperty]
	string source;

	[ObservableProperty]
	string destination;

	public TargetInformation()
	{
		Source = string.Empty;
		Destination = string.Empty;
	}
}
