using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CopyFiles.Models;


public class TargetSolutionCopyInformation
{
	public FileCopyDataModel CopyFileDataModel { get; set; } = new();
	public FileCopyDataModel NonSignedFileCopyDataModel { get; set; } = new();
	public string[] TargetIsmFiles { get; set; } = Array.Empty<string>();
}

public class Properties
{
	public Dictionary<string, TargetSolutionCopyInformation> TargetSolutions { get; set; } = new();
	public string TargetSolution { get; set; } = string.Empty;
}
