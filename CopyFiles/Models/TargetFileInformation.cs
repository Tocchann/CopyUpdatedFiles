using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CopyFiles.Models;

public enum TargetStatus
{
	Unknown,
	NotExist,
	Different,
	Same,
}
public partial class TargetFileInformation : TargetInformation
{
	[ObservableProperty]
	TargetStatus status;

	[ObservableProperty]
	bool ignore;
}
