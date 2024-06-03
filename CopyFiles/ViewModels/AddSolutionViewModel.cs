using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Morrin.Extensions.Abstractions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace CopyFiles.ViewModels;

public partial class AddSolutionViewModel : ObservableObject
{
	public string[] TargetSolutions { get; set; }

	[ObservableProperty]
	string solutionName;

	[ObservableProperty]
	bool? dialogResult;

	[RelayCommand]
	public void Ok()
	{
		if( string.IsNullOrWhiteSpace( SolutionName ) )
		{
			m_alert.Show( "ソリューション名が空白です", IDispAlert.Buttons.OK, IDispAlert.Icon.Asterisk );
		}
		else if( TargetSolutions.Contains( SolutionName ) )
		{
			m_alert.Show( "ソリューション名が既に存在します", IDispAlert.Buttons.OK, IDispAlert.Icon.Asterisk );
		}
		else
		{
			DialogResult = true;
		}
	}
	[RelayCommand]
	public void Cancel()
	{
		DialogResult = false;
	}

	public AddSolutionViewModel( ILogger<AddSolutionViewModel> logger, IDispAlert alert )
	{
		m_logger = logger;
		m_alert = alert;
		SolutionName = "";
		TargetSolutions = Array.Empty<string>();
	}
	[DesignOnly( true )]
#pragma warning disable CS8618 // null 非許容のフィールドには、コンストラクターの終了時に null 以外の値が入っていなければなりません。Null 許容として宣言することをご検討ください。
	public AddSolutionViewModel()
#pragma warning restore CS8618 // null 非許容のフィールドには、コンストラクターの終了時に null 以外の値が入っていなければなりません。Null 許容として宣言することをご検討ください。
	{
	}
	private ILogger<AddSolutionViewModel> m_logger;
	private IDispAlert m_alert;
}
