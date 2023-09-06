using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CopyFiles.Models;

public enum TargetStatus
{
	Unknown, // 不明
	NotExist, // コピー先がない
	Different, // コピー先と異なる
	SameFullMatch, // コピー先と一致(日付も一致)
	SameWithoutDate, // コピー先と一致(ただし日付違い。ビルドされたけど内容が変わっていない)
	SameWithoutSize, // サイズは違うが内容は一致(実行ファイルで署名の有無が該当)
}
public partial class TargetFileInformation : TargetInformation
{
	[ObservableProperty]
	TargetStatus status;

	[ObservableProperty]
	bool ignore;
}
