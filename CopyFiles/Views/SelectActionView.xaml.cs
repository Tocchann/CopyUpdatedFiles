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
/// SelectActionView.xaml の相互作用ロジック
/// </summary>
public partial class SelectActionView : ISelectActionView
{
	public SelectActionView( SelectActionViewModel vm )
	{
		InitializeComponent();
		DataContext = vm;
	}
	public bool? ShowWindow()
	{
		Show();
		return true;
	}
}
