using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CopyFiles.Contracts.Views;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CopyFiles.ViewModels;

public partial class SelectActionViewModel : ObservableObject
{
	[RelayCommand]
	void CopyBuildFiles()
	{
		m_logger.LogInformation( System.Reflection.MethodBase.GetCurrentMethod()?.Name );
		var dlg = App.Current.GetService<ICopyFileView>();
		dlg?.ShowWindow();
	}

	[RelayCommand]
	void CopyNonSignedFiles()
	{
		m_logger.LogInformation( System.Reflection.MethodBase.GetCurrentMethod()?.Name );
		var dlg = App.Current.GetService<INonSignedFileCopyView>();
		dlg?.ShowWindow();
	}

	public SelectActionViewModel( ILogger<SelectActionViewModel> logger )
	{
		m_logger = logger;
	}
	[DesignOnly( true )]
#pragma warning disable CS8618 // null 非許容のフィールドには、コンストラクターの終了時に null 以外の値が入っていなければなりません。Null 許容として宣言することをご検討ください。
	public SelectActionViewModel()
#pragma warning restore CS8618 // null 非許容のフィールドには、コンストラクターの終了時に null 以外の値が入っていなければなりません。Null 許容として宣言することをご検討ください。
	{
	}
	private ILogger<SelectActionViewModel> m_logger;
}
