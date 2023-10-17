using CommunityToolkit.Mvvm.ComponentModel;
using CopyFiles.Contracts.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CopyFiles.ViewModels
{
	public partial class ImplementProgressBarBaseViewModel : ObservableObject, IProgressBarService
	{
		[ObservableProperty]
		bool isProgressBarVisible;

		[ObservableProperty]
		bool isIndeterminate;

		[ObservableProperty]
		int progressMin;

		[ObservableProperty]
		int progressMax;

		[ObservableProperty]
		int progressValue;
	}
}
