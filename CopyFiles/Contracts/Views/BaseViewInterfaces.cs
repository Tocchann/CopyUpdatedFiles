using CopyFiles.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CopyFiles.Contracts.Views;

public interface IView
{
	bool? ShowWindow();
}
public interface ISelectActionView : IView { }
public interface ICopyFileView : IView { }
public interface INonSignedFileCopyView : IView { }
public interface IAddSolutionDialog : IView
{
	AddSolutionViewModel ViewModel { get; }
}
