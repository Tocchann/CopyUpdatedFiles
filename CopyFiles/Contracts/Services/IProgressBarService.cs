using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CopyFiles.Contracts.Services;

public interface IProgressBarService
{
	bool IsProgressBarVisible { get; set; }
	bool IsIndeterminate { get; set; }
	int ProgressMin { get; set; }
	int ProgressMax { get; set; }
	int ProgressValue { get; set; }
}
