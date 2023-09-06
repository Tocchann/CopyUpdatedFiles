namespace Morrin.Extensions.Abstractions;
/// <summary>
/// 汎用型アラートメッセージ表示のインターフェース実装は各実装クラスに依存
/// 実装はアプリケーションに依存するため分離して定義。各enumはSystem.Windows.MessageBox何某 と同じ値
/// </summary>
public interface IDispAlert
{
	public enum Result
	{
		None = 0,
		OK = 1,
		Cancel = 2,
		Yes = 6,
		No = 7
	}
	public enum Buttons
	{
		OK = 0,
		OKCancel = 1,
		YesNoCancel = 3,
		YesNo = 4
	}
	public enum Icon
	{
		None = 0,
		Error = 16,
		Hand = 16,
		Stop = 16,
		Question = 32,
		Exclamation = 48,
		Warning = 48,
		Asterisk = 64,
		Information = 64
	}
	[Flags]
	public enum Options
	{
		None = 0,
		DefaultDesktopOnly = 0x20000,
		RightAlign = 0x80000,
		RtlReading = 0x100000,
		ServiceNotification = 0x200000
	}
	public Result Show( string message, Buttons button = Buttons.OK, Icon icon = Icon.Exclamation, Result defaultResult = Result.None, Options options = Options.None );
	public Result Show( string message, string title, Buttons button = Buttons.OK, Icon icon = Icon.Exclamation, Result defaultResult = Result.None, Options options = Options.None );
}
