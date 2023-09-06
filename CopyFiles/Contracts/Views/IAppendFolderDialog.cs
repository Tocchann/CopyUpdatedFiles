using CopyFiles.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CopyFiles.Contracts.Views
{
	public interface IAppendFolderDialog : IView
	{
		AppendFolderViewModel ViewModel { get; }
	}
}
