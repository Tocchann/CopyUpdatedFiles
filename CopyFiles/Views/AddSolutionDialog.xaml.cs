using CopyFiles.Contracts.Views;
using CopyFiles.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace CopyFiles.Views;

/// <summary>
/// AddSolutionDialog.xaml の相互作用ロジック
/// </summary>
public partial class AddSolutionDialog : IAddSolutionDialog
{
	public AddSolutionDialog( AddSolutionViewModel vm )
	{
		InitializeComponent();
		DataContext = vm;
		vm.PropertyChanged += ( s, e ) =>
		{
			if( e.PropertyName == nameof( ViewModel.DialogResult ) )
			{
				DialogResult = ViewModel.DialogResult;
			}
		};
	}

	public AddSolutionViewModel ViewModel => (AddSolutionViewModel)DataContext;

	public bool? ShowWindow()
	{
		Owner = Application.Current.MainWindow;
		return ShowDialog();
	}
}
