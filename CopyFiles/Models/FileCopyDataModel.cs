using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CopyFiles.Models;

public class FileCopyDataModel
{
	public bool IsHideIgnoreFiles { get; set; }
	public bool IsDispCopyFilesOnly { get; set; }
	public TargetInformation[] TargetInformations { get; set; } = Array.Empty<TargetInformation>();
}
