using CopyFiles.Contracts.Views;
using CopyFiles.ViewModels;
using Microsoft.Extensions.Logging;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CopyFiles.Views
{
	/// <summary>
	/// AppendFolderView.xaml の相互作用ロジック
	/// </summary>
	public partial class AppendFolderDialog : IAppendFolderDialog
	{
		private ILogger<AppendFolderDialog> m_logger;

		public AppendFolderDialog( ILogger<AppendFolderDialog> logger, AppendFolderViewModel vm )
		{
			InitializeComponent();
			m_logger = logger;
			DataContext = vm;
			vm.PropertyChanged += ( s, e ) =>
			{
				if( e.PropertyName == nameof( DialogResult ) )
				{
					DialogResult = vm.DialogResult;
				}
			};
		}
		public AppendFolderViewModel ViewModel => (AppendFolderViewModel)DataContext;
		public bool? ShowWindow()
		{
			// ポップアップなのでオーナーを設定しておく必要がある
			Owner = App.Current.MainWindow;
			return base.ShowDialog();
		}
	}
}
