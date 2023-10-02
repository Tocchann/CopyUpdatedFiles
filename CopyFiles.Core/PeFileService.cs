using CopyFiles.Core.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Threading.Tasks;

namespace CopyFiles.Core;

public static class PeFileService
{
	/// <summary>
	/// ファイルがPE形式のファイルか？
	/// </summary>
	/// <param name="fileImage">ファイルイメージ(オンメモリチェック)</param>
	/// <returns>PEファイル形式かどうかの判定結果</returns>
	public static bool IsValidPE( byte[] fileImage )
	{
		var pos = GetPeSignaturePos( fileImage );
		return pos != -1;
	}
	/// <summary>
	/// PEファイルのSECTIONデータエリア内のハッシュ対象領域(にあたるもの)を取り出す
	/// 理想的には、隙間部分をきれいに穴埋めしたいんだけどそこまではやらない
	/// </summary>
	/// <param name="fileImage">ファイルイメージ</param>
	/// <param name="count">一致範囲のバイト数</param>
	/// <returns>一致範囲の確認スタート位置</returns>
	public static int CalcHashArea( byte[] fileImage, out int count )
	{
		int offset = 0;
		count = fileImage.Length;
		// PEファイルではない場合は全域がハッシュ領域
		if( !IsValidPE( fileImage ) )
		{
			return offset;
		}
		// 実際のexe/dll ならセクションデータが存在するのでヘッダーサイズより大きなエリアになる
		int minExeSize = Marshal.SizeOf<IMAGE_DOS_HEADER>() +
			sizeof( uint ) +
			Marshal.SizeOf<IMAGE_FILE_HEADER>() +
			Marshal.SizeOf<IMAGE_OPTIONAL_HEADER32>() +
			Marshal.SizeOf<IMAGE_DATA_DIRECTORY>() * Literals.IMAGE_NUMBEROF_DIRECTORY_ENTRIES +
			Marshal.SizeOf<IMAGE_SECTION_HEADER>(); // 少なくとも１つ以上のセクションデータがあるはず
													// ヘッダーだけしかないということはあり得ない(ここは簡易チェックでよい)
		if( minExeSize >= fileImage.Length )
		{
			return offset;
		}
		// PEファイルアクセスはC形式構造体へのアクセスが頻発するので
		// IntPtrなバッファにコピーしていろいろ処理する
		var moduleBuffer = Marshal.AllocHGlobal( count );
		Marshal.Copy( fileImage, 0, moduleBuffer, count );
		var imageDosHeader = Marshal.PtrToStructure<IMAGE_DOS_HEADER>( moduleBuffer );
		// 念のための再チェック(IsValidPEではじいている)
		Debug.Assert( imageDosHeader.e_magic == Literals.IMAGE_DOS_SIGNATURE );
		var signature = Marshal.ReadInt32( moduleBuffer, imageDosHeader.e_lfanew );
		Debug.Assert( signature == Literals.IMAGE_NT_SIGNATURE );
		int headerOffset = imageDosHeader.e_lfanew + sizeof( uint ); //	シグネチャの後ろを指す
		var imageFileHeader = Marshal.PtrToStructure<IMAGE_FILE_HEADER>( moduleBuffer + headerOffset );

		int sectionDataStartPos = headerOffset + Marshal.SizeOf<IMAGE_FILE_HEADER>()+
			imageFileHeader.SizeOfOptionalHeader;
		var sectionHeaderSize = Marshal.SizeOf<IMAGE_SECTION_HEADER>();
		offset = fileImage.Length;
		int maxPos = 0;
		for( int index = 0 ; index < imageFileHeader.NumberOfSections ; index++ )
		{
			var imageSectionHeader = Marshal.PtrToStructure<IMAGE_SECTION_HEADER>( moduleBuffer + sectionDataStartPos + sectionHeaderSize * index );
			int sectionStartPos = (int)imageSectionHeader.PointerToRawData;
			int sectionEndPos = sectionStartPos + (int)imageSectionHeader.SizeOfRawData;
			if( offset > sectionStartPos )
			{
				offset = sectionStartPos;
			}
			if( maxPos < sectionEndPos )
			{
				maxPos = sectionEndPos;
			}
		}
		// count はサイズをセットするので、値を再計算する(署名がなければ、ヘッダーを除外した値になっているはず)
		Debug.Assert( maxPos <= fileImage.Length );
		if( maxPos > fileImage.Length )
		{
			maxPos = fileImage.Length;
		}
		count = maxPos - offset;
		return offset;
	}
	/// <summary>
	/// 署名されているか？
	/// </summary>
	/// <param name="fileImage">ファイルイメージ(オンメモリチェック)</param>
	/// <returns>署名を持っているかの判定結果。PEファイルではない場合も署名は持っていないと判断するので要注意</returns>
	public static bool IsSetSignatgure( byte[] fileImage )
	{
		if( !IsValidPE( fileImage ) )
		{
			return false;
		}
		// 実際のexe/dll ならセクションデータが存在するのでヘッダーサイズより大きなエリアになる
		int minExeSize = Marshal.SizeOf<IMAGE_DOS_HEADER>() +
			sizeof( uint ) +
			Marshal.SizeOf<IMAGE_FILE_HEADER>() +
			Marshal.SizeOf<IMAGE_OPTIONAL_HEADER32>() +
			Marshal.SizeOf<IMAGE_DATA_DIRECTORY>() * Literals.IMAGE_NUMBEROF_DIRECTORY_ENTRIES +
			0;//Marshal.SizeOf<IMAGE_SECTION_HEADER>(); // 少なくとも１つ以上のセクションデータがあるはず
													// ヘッダーだけしかないということはあり得ない(ここは簡易チェックでよい)
		if( minExeSize >= fileImage.Length )
		{
			return false;
		}
		// PEファイルアクセスはC形式構造体へのアクセスが頻発するので
		// IntPtrなバッファにコピーしていろいろ処理する
		var moduleBuffer = Marshal.AllocHGlobal( fileImage.Length );
		Marshal.Copy( fileImage, 0, moduleBuffer, fileImage.Length );
		var imageDosHeader = Marshal.PtrToStructure<IMAGE_DOS_HEADER>( moduleBuffer );
		// 念のための再チェック(IsValidPEではじいている)
		Debug.Assert( imageDosHeader.e_magic == Literals.IMAGE_DOS_SIGNATURE );
		var signature = Marshal.ReadInt32( moduleBuffer, imageDosHeader.e_lfanew );
		Debug.Assert( signature == Literals.IMAGE_NT_SIGNATURE );
		int headerOffset = imageDosHeader.e_lfanew + sizeof( uint ); //	シグネチャの後ろを指す
		var imageFileHeader = Marshal.PtrToStructure<IMAGE_FILE_HEADER>( moduleBuffer + headerOffset );

		headerOffset += Marshal.SizeOf<IMAGE_FILE_HEADER>();
		if( imageFileHeader.Machine == IMAGE_FILE_MACHINE.I386 )
		{
			headerOffset += Marshal.SizeOf<IMAGE_OPTIONAL_HEADER32>();
		}
		else if( imageFileHeader.Machine == IMAGE_FILE_MACHINE.AMD64 )
		{
			headerOffset += Marshal.SizeOf<IMAGE_OPTIONAL_HEADER64>();
		}
		else
		{
			throw new NotImplementedException();	//	x86/x64以外は考慮しない
		}
		var dataDirectorySize = Marshal.SizeOf<IMAGE_DATA_DIRECTORY>();
		var signaturePos = dataDirectorySize * (int)IMAGE_DIRECTORY_ENTRY.SECURITY;
		var dataDirectory = Marshal.PtrToStructure<IMAGE_DATA_DIRECTORY>( moduleBuffer + headerOffset + signaturePos );
		return dataDirectory.VirtualAddress != 0;
	}

	private static int GetPeSignaturePos( byte[] fileImage )
	{
		// 少なくともIMAGE_DOS_HEADERサイズは必須
		int dosHeaderLength = Marshal.SizeOf<IMAGE_DOS_HEADER>();
		if( fileImage.Length < dosHeaderLength )
		{
			return -1;
		}
		var data = ReadInt16( fileImage, 0 );
		if( data == Literals.IMAGE_DOS_SIGNATURE )
		{
			data = ReadInt32( fileImage, dosHeaderLength-sizeof(uint) );
			if( data + sizeof(uint) <= fileImage.Length )
			{
				var signature = ReadInt32( fileImage, (int)data );
				if( signature == Literals.IMAGE_NT_SIGNATURE )
				{
					return (int)data;
				}
			}
		}
		return -1;
	}
	private static int ReadInt32( byte[] fileImage, int offset )
	{
		int data = 0;
		for( int index = 0 ; index < sizeof( Int32 ) ; index++ )
		{
			int newData = fileImage[offset + index];
			newData <<= 8 * index;
			data |= newData;
		}
		return data;
	}
	private static int ReadInt16( byte[] fileImage, int offset )
	{
		int data = 0;
		for( int index = 0 ; index < sizeof( Int16 ) ; index++ )
		{
			int newData = fileImage[offset + index];
			newData <<= 8 * index;
			data |= newData;
		}
		return data;
	}
	public static void DumpPeHeader( byte[] fileImage )
	{
		var dosHeaderSize = Marshal.SizeOf<IMAGE_DOS_HEADER>();
		// 少なくともヘッダーエリア分がないとダンプは不可能
		if( fileImage.Length <= dosHeaderSize )
		{
			Trace.WriteLine( "PEファイルではありません。" );
			return;
		}
		var moduleBuffer = Marshal.AllocHGlobal( fileImage.Length );
		Marshal.Copy( fileImage, 0, moduleBuffer, fileImage.Length );
		var imageDosHeader = Marshal.PtrToStructure<IMAGE_DOS_HEADER>( moduleBuffer );
		if( imageDosHeader.e_magic != Literals.IMAGE_DOS_SIGNATURE )
		{
			Trace.WriteLine( "PEファイルではありません。" );
			return;
		}
		if( fileImage.Length <= imageDosHeader.e_lfanew + sizeof( uint ) )
		{
			Trace.WriteLine( "PEファイルではありません。" );
			return;
		}
		var signature = Marshal.ReadInt32( moduleBuffer + imageDosHeader.e_lfanew );
		if( signature != Literals.IMAGE_NT_SIGNATURE )
		{
			Trace.WriteLine( "PEファイルではありません。" );
			return;
		}
		DumpStruture( imageDosHeader );

		Trace.WriteLine( $"Signature={signature:X08}(\"{Encoding.ASCII.GetString( fileImage, imageDosHeader.e_lfanew, 2 )}\")" );

		int headerOffset = imageDosHeader.e_lfanew + sizeof( uint );
		var imageFileHeader = Marshal.PtrToStructure<IMAGE_FILE_HEADER>( moduleBuffer + headerOffset );
		DumpStruture( headerOffset, imageFileHeader );
		headerOffset += Marshal.SizeOf<IMAGE_FILE_HEADER>();

		int optionalHeaderSize = 0;
		if( imageFileHeader.Machine == IMAGE_FILE_MACHINE.I386 )
		{
			var optionalHeader32 = Marshal.PtrToStructure<IMAGE_OPTIONAL_HEADER32>( moduleBuffer + headerOffset );
			DumpStruture( headerOffset, optionalHeader32 );
			optionalHeaderSize = Marshal.SizeOf<IMAGE_OPTIONAL_HEADER32>();
		}
		else if( imageFileHeader.Machine == IMAGE_FILE_MACHINE.AMD64 )
		{
			var optionalHeader64 = Marshal.PtrToStructure<IMAGE_OPTIONAL_HEADER64>( moduleBuffer + headerOffset );
			DumpStruture( headerOffset, optionalHeader64 );
			optionalHeaderSize = Marshal.SizeOf<IMAGE_OPTIONAL_HEADER64>();
		}

		Trace.WriteLine( $"Offset:{headerOffset + optionalHeaderSize:X08}" );
		var dataDirectorySize = Marshal.SizeOf<IMAGE_DATA_DIRECTORY>();
		// IMAGE_DATA_DIRECTORYは固定配列
		for( int index = 0 ; index < Literals.IMAGE_NUMBEROF_DIRECTORY_ENTRIES ; index++ )
		{
			IMAGE_DIRECTORY_ENTRY entryName = (IMAGE_DIRECTORY_ENTRY)index;
			var dataDirectory = Marshal.PtrToStructure<IMAGE_DATA_DIRECTORY>( moduleBuffer + headerOffset + optionalHeaderSize + dataDirectorySize * index );
			Trace.WriteLine( $"IMAGE_DATA_DIRECTORY[{entryName}({index})].VirtualAddress={dataDirectory.VirtualAddress:X08}" );
			Trace.WriteLine( $"IMAGE_DATA_DIRECTORY[{entryName}({index})].Size={dataDirectory.Size:X08}" );
		}
		var sectionHeaderSize = Marshal.SizeOf<IMAGE_SECTION_HEADER>();
		int sectionDataStartPos = headerOffset + imageFileHeader.SizeOfOptionalHeader;
		for( int index = 0 ; index < imageFileHeader.NumberOfSections ; index++ )
		{
			var imageSectionHeader = Marshal.PtrToStructure<IMAGE_SECTION_HEADER>( moduleBuffer + sectionDataStartPos + sectionHeaderSize * index );
			DumpStruture( sectionDataStartPos + sectionHeaderSize * index, imageSectionHeader );
		}
		Trace.WriteLine( $"EndOfHeader={sectionDataStartPos + sectionHeaderSize * imageFileHeader.NumberOfSections:X08}" );
	}

	private static void DumpStruture( IMAGE_DOS_HEADER structure )
	{
		Trace.WriteLine( $"sizeof(IMAGE_DOS_HEADER)={Marshal.SizeOf<IMAGE_DOS_HEADER>()}" );
		Trace.WriteLine( $"IMAGE_DOS_HEADER.e_magic={structure.e_magic:X04}" );
		Trace.WriteLine( $"IMAGE_DOS_HEADER.e_cblp={structure.e_cblp:X04}" );
		Trace.WriteLine( $"IMAGE_DOS_HEADER.e_cp={structure.e_cp:X04}" );
		Trace.WriteLine( $"IMAGE_DOS_HEADER.e_crlc={structure.e_crlc:X04}" );
		Trace.WriteLine( $"IMAGE_DOS_HEADER.e_cparhdr={structure.e_cparhdr:X04}" );
		Trace.WriteLine( $"IMAGE_DOS_HEADER.e_minalloc={structure.e_minalloc:X04}" );
		Trace.WriteLine( $"IMAGE_DOS_HEADER.e_maxalloc={structure.e_maxalloc:X04}" );
		Trace.WriteLine( $"IMAGE_DOS_HEADER.e_ss={structure.e_ss:X04}" );
		Trace.WriteLine( $"IMAGE_DOS_HEADER.e_sp={structure.e_sp:X04}" );
		Trace.WriteLine( $"IMAGE_DOS_HEADER.e_csum={structure.e_csum:X04}" );
		Trace.WriteLine( $"IMAGE_DOS_HEADER.e_ip={structure.e_ip:X04}" );
		Trace.WriteLine( $"IMAGE_DOS_HEADER.e_cs={structure.e_cs:X04}" );
		Trace.WriteLine( $"IMAGE_DOS_HEADER.e_lfarlc={structure.e_lfarlc:X04}" );
		Trace.WriteLine( $"IMAGE_DOS_HEADER.e_ovno={structure.e_ovno:X04}" );
		Trace.WriteLine( $"IMAGE_DOS_HEADER.e_res[0]={structure.e_res_0:X04}" );
		Trace.WriteLine( $"IMAGE_DOS_HEADER.e_res[1]={structure.e_res_1:X04}" );
		Trace.WriteLine( $"IMAGE_DOS_HEADER.e_res[2]={structure.e_res_2:X04}" );
		Trace.WriteLine( $"IMAGE_DOS_HEADER.e_res[3]={structure.e_res_3:X04}" );
		Trace.WriteLine( $"IMAGE_DOS_HEADER.e_oemid={structure.e_oemid:X04}" );
		Trace.WriteLine( $"IMAGE_DOS_HEADER.e_oeminfo={structure.e_oeminfo:X04}" );
		Trace.WriteLine( $"IMAGE_DOS_HEADER.e_res2[0]={structure.e_res2_0:X04}" );
		Trace.WriteLine( $"IMAGE_DOS_HEADER.e_res2[1]={structure.e_res2_1:X04}" );
		Trace.WriteLine( $"IMAGE_DOS_HEADER.e_res2[2]={structure.e_res2_2:X04}" );
		Trace.WriteLine( $"IMAGE_DOS_HEADER.e_res2[3]={structure.e_res2_3:X04}" );
		Trace.WriteLine( $"IMAGE_DOS_HEADER.e_res2[4]={structure.e_res2_4:X04}" );
		Trace.WriteLine( $"IMAGE_DOS_HEADER.e_res2[5]={structure.e_res2_5:X04}" );
		Trace.WriteLine( $"IMAGE_DOS_HEADER.e_res2[6]={structure.e_res2_6:X04}" );
		Trace.WriteLine( $"IMAGE_DOS_HEADER.e_res2[7]={structure.e_res2_7:X04}" );
		Trace.WriteLine( $"IMAGE_DOS_HEADER.e_res2[8]={structure.e_res2_8:X04}" );
		Trace.WriteLine( $"IMAGE_DOS_HEADER.e_res2[9]={structure.e_res2_9:X04}" );
		Trace.WriteLine( $"IMAGE_DOS_HEADER.e_lfanew={structure.e_lfanew:X08}" );
	}

	private static void DumpStruture( int headerOffset, IMAGE_FILE_HEADER structure )
	{
		ushort decValue = (ushort)structure.Machine;
		Trace.WriteLine( $"IMAGE_FILE_HEADER.Machine={structure.Machine}({decValue:X04})" );
		Trace.WriteLine( $"IMAGE_FILE_HEADER.NumberOfSections={structure.NumberOfSections:X04}" );
		Trace.WriteLine( $"IMAGE_FILE_HEADER.TimeDateStamp={structure.TimeDateStamp:X08}" );
		Trace.WriteLine( $"IMAGE_FILE_HEADER.PointerToSymbolTable={structure.PointerToSymbolTable:X08}" );
		Trace.WriteLine( $"IMAGE_FILE_HEADER.NumberOfSymbols={structure.NumberOfSymbols:X08}" );
		Trace.WriteLine( $"IMAGE_FILE_HEADER.SizeOfOptionalHeader={structure.SizeOfOptionalHeader:X04}" );
		decValue = (ushort)structure.Characteristics;
		Trace.WriteLine( $"IMAGE_FILE_HEADER.Characteristics={structure.Characteristics}({decValue:X04})" );
	}

	private static void DumpStruture( int headerOffset, IMAGE_OPTIONAL_HEADER32 structure )
	{
		Trace.WriteLine( $"Offset:{headerOffset:X08}(sizeof(IMAGE_OPTIONAL_HEADER32)={Marshal.SizeOf<IMAGE_OPTIONAL_HEADER32>()}" );
		ushort decValue = (ushort)structure.Magic;
		Trace.WriteLine( $"IMAGE_OPTIONAL_HEADER32.Magic={structure.Magic}({decValue:X04})" );
		Trace.WriteLine( $"IMAGE_OPTIONAL_HEADER32.MajorLinkerVersion={structure.MajorLinkerVersion:X02}" );
		Trace.WriteLine( $"IMAGE_OPTIONAL_HEADER32.MinorLinkerVersion={structure.MinorLinkerVersion:X02}" );
		Trace.WriteLine( $"IMAGE_OPTIONAL_HEADER32.SizeOfCode={structure.SizeOfCode:X08}" );
		Trace.WriteLine( $"IMAGE_OPTIONAL_HEADER32.SizeOfInitializedData={structure.SizeOfInitializedData:X08}" );
		Trace.WriteLine( $"IMAGE_OPTIONAL_HEADER32.SizeOfUninitializedData={structure.SizeOfUninitializedData:X08}" );
		Trace.WriteLine( $"IMAGE_OPTIONAL_HEADER32.AddressOfEntryPoint={structure.AddressOfEntryPoint:X08}" );
		Trace.WriteLine( $"IMAGE_OPTIONAL_HEADER32.BaseOfCode={structure.BaseOfCode:X08}" );
		Trace.WriteLine( $"IMAGE_OPTIONAL_HEADER32.BaseOfData={structure.BaseOfData:X08}" );
		Trace.WriteLine( $"IMAGE_OPTIONAL_HEADER32.ImageBase={structure.ImageBase:X08}" );
		Trace.WriteLine( $"IMAGE_OPTIONAL_HEADER32.SectionAlignment={structure.SectionAlignment:X08}" );
		Trace.WriteLine( $"IMAGE_OPTIONAL_HEADER32.FileAlignment={structure.FileAlignment:X08}" );
		Trace.WriteLine( $"IMAGE_OPTIONAL_HEADER32.MajorOperatingSystemVersion={structure.MajorOperatingSystemVersion:X04}" );
		Trace.WriteLine( $"IMAGE_OPTIONAL_HEADER32.MinorOperatingSystemVersion={structure.MinorOperatingSystemVersion:X04}" );
		Trace.WriteLine( $"IMAGE_OPTIONAL_HEADER32.MajorImageVersion={structure.MajorImageVersion:X04}" );
		Trace.WriteLine( $"IMAGE_OPTIONAL_HEADER32.MinorImageVersion={structure.MinorImageVersion:X04}" );
		Trace.WriteLine( $"IMAGE_OPTIONAL_HEADER32.MajorSubsystemVersion={structure.MajorSubsystemVersion:X04}" );
		Trace.WriteLine( $"IMAGE_OPTIONAL_HEADER32.MinorSubsystemVersion={structure.MinorSubsystemVersion:X04}" );
		Trace.WriteLine( $"IMAGE_OPTIONAL_HEADER32.Win32VersionValue={structure.Win32VersionValue:X08}" );
		Trace.WriteLine( $"IMAGE_OPTIONAL_HEADER32.SizeOfImage={structure.SizeOfImage:X08}" );
		Trace.WriteLine( $"IMAGE_OPTIONAL_HEADER32.SizeOfHeaders={structure.SizeOfHeaders:X08}" );
		Trace.WriteLine( $"IMAGE_OPTIONAL_HEADER32.CheckSum={structure.CheckSum:X08}" );
		Trace.WriteLine( $"IMAGE_OPTIONAL_HEADER32.Subsystem={structure.Subsystem:X04}" );
		Trace.WriteLine( $"IMAGE_OPTIONAL_HEADER32.DllCharacteristics={structure.DllCharacteristics:X04}" );
		Trace.WriteLine( $"IMAGE_OPTIONAL_HEADER32.SizeOfStackReserve={structure.SizeOfStackReserve:X08}" );
		Trace.WriteLine( $"IMAGE_OPTIONAL_HEADER32.SizeOfStackCommit={structure.SizeOfStackCommit:X08}" );
		Trace.WriteLine( $"IMAGE_OPTIONAL_HEADER32.SizeOfHeapReserve={structure.SizeOfHeapReserve:X08}" );
		Trace.WriteLine( $"IMAGE_OPTIONAL_HEADER32.SizeOfHeapCommit={structure.SizeOfHeapCommit:X08}" );
		Trace.WriteLine( $"IMAGE_OPTIONAL_HEADER32.LoaderFlags={structure.LoaderFlags:X08}" );
		Trace.WriteLine( $"IMAGE_OPTIONAL_HEADER32.NumberOfRvaAndSizes={structure.NumberOfRvaAndSizes:X08}" );
	}

	private static void DumpStruture( int headerOffset, IMAGE_OPTIONAL_HEADER64 structure )
	{
		Trace.WriteLine( $"Offset:{headerOffset:X08}(sizeof(IMAGE_OPTIONAL_HEADER64)={Marshal.SizeOf<IMAGE_OPTIONAL_HEADER64>()}" );
		ushort decValue = (ushort)structure.Magic;
		Trace.WriteLine( $"IMAGE_OPTIONAL_HEADER64.Magic={structure.Magic}({decValue:X04})" );
		Trace.WriteLine( $"IMAGE_OPTIONAL_HEADER64.MajorLinkerVersion={structure.MajorLinkerVersion:X02}" );
		Trace.WriteLine( $"IMAGE_OPTIONAL_HEADER64.MinorLinkerVersion={structure.MinorLinkerVersion:X02}" );
		Trace.WriteLine( $"IMAGE_OPTIONAL_HEADER64.SizeOfCode={structure.SizeOfCode:X08}" );
		Trace.WriteLine( $"IMAGE_OPTIONAL_HEADER64.SizeOfInitializedData={structure.SizeOfInitializedData:X08}" );
		Trace.WriteLine( $"IMAGE_OPTIONAL_HEADER64.SizeOfUninitializedData={structure.SizeOfUninitializedData:X08}" );
		Trace.WriteLine( $"IMAGE_OPTIONAL_HEADER64.AddressOfEntryPoint={structure.AddressOfEntryPoint:X08}" );
		Trace.WriteLine( $"IMAGE_OPTIONAL_HEADER64.BaseOfCode={structure.BaseOfCode:X08}" );
		Trace.WriteLine( $"IMAGE_OPTIONAL_HEADER64.ImageBase={structure.ImageBase:X016}" );
		Trace.WriteLine( $"IMAGE_OPTIONAL_HEADER64.SectionAlignment={structure.SectionAlignment:X08}" );
		Trace.WriteLine( $"IMAGE_OPTIONAL_HEADER64.FileAlignment={structure.FileAlignment:X08}" );
		Trace.WriteLine( $"IMAGE_OPTIONAL_HEADER64.MajorOperatingSystemVersion={structure.MajorOperatingSystemVersion:X04}" );
		Trace.WriteLine( $"IMAGE_OPTIONAL_HEADER64.MinorOperatingSystemVersion={structure.MinorOperatingSystemVersion:X04}" );
		Trace.WriteLine( $"IMAGE_OPTIONAL_HEADER64.MajorImageVersion={structure.MajorImageVersion:X04}" );
		Trace.WriteLine( $"IMAGE_OPTIONAL_HEADER64.MinorImageVersion={structure.MinorImageVersion:X04}" );
		Trace.WriteLine( $"IMAGE_OPTIONAL_HEADER64.MajorSubsystemVersion={structure.MajorSubsystemVersion:X04}" );
		Trace.WriteLine( $"IMAGE_OPTIONAL_HEADER64.MinorSubsystemVersion={structure.MinorSubsystemVersion:X04}" );
		Trace.WriteLine( $"IMAGE_OPTIONAL_HEADER64.Win32VersionValue={structure.Win32VersionValue:X08}" );
		Trace.WriteLine( $"IMAGE_OPTIONAL_HEADER64.SizeOfImage={structure.SizeOfImage:X08}" );
		Trace.WriteLine( $"IMAGE_OPTIONAL_HEADER64.SizeOfHeaders={structure.SizeOfHeaders:X08}" );
		Trace.WriteLine( $"IMAGE_OPTIONAL_HEADER64.CheckSum={structure.CheckSum:X08}" );
		Trace.WriteLine( $"IMAGE_OPTIONAL_HEADER64.Subsystem={structure.Subsystem:X08}" );
		Trace.WriteLine( $"IMAGE_OPTIONAL_HEADER64.DllCharacteristics={structure.DllCharacteristics:X04}" );
		Trace.WriteLine( $"IMAGE_OPTIONAL_HEADER64.SizeOfStackReserve={structure.SizeOfStackReserve:X016}" );
		Trace.WriteLine( $"IMAGE_OPTIONAL_HEADER64.SizeOfStackCommit={structure.SizeOfStackCommit:X016}" );
		Trace.WriteLine( $"IMAGE_OPTIONAL_HEADER64.SizeOfHeapReserve={structure.SizeOfHeapReserve:X016}" );
		Trace.WriteLine( $"IMAGE_OPTIONAL_HEADER64.SizeOfHeapCommit={structure.SizeOfHeapCommit:X016}" );
		Trace.WriteLine( $"IMAGE_OPTIONAL_HEADER64.LoaderFlags={structure.LoaderFlags:X08}" );
		Trace.WriteLine( $"IMAGE_OPTIONAL_HEADER64.NumberOfRvaAndSizes={structure.NumberOfRvaAndSizes:X08}" );
	}
	private static void DumpStruture( int headerOffset, IMAGE_SECTION_HEADER structure )
	{
		Trace.WriteLine( $"Offset:{headerOffset:X08}(sizeof(IMAGE_SECTION_HEADER)={Marshal.SizeOf<IMAGE_SECTION_HEADER>()}" );
		Trace.WriteLine( $"IMAGE_SECTION_HEADER.Name={structure.Name.ToString()}" );
		Trace.WriteLine( $"IMAGE_SECTION_HEADER.VirtualSize={structure.VirtualSize:X08}" );
		Trace.WriteLine( $"IMAGE_SECTION_HEADER.VirtualAddress={structure.VirtualAddress:X08}" );
		Trace.WriteLine( $"IMAGE_SECTION_HEADER.SizeOfRawData={structure.SizeOfRawData:X08}" );
		Trace.WriteLine( $"IMAGE_SECTION_HEADER.PointerToRawData={structure.PointerToRawData:X08}" );
		Trace.WriteLine( $"IMAGE_SECTION_HEADER.PointerToRelocations={structure.PointerToRelocations}" );
		Trace.WriteLine( $"IMAGE_SECTION_HEADER.PointerToLinenumbers={structure.PointerToLinenumbers:X08}" );
		Trace.WriteLine( $"IMAGE_SECTION_HEADER.NumberOfRelocations={structure.NumberOfRelocations:X04}" );
		Trace.WriteLine( $"IMAGE_SECTION_HEADER.NumberOfLinenumbers={structure.NumberOfLinenumbers:X04}" );
		uint uintValue = (uint)structure.Characteristics;
		Trace.WriteLine( $"IMAGE_SECTION_HEADER.Characteristics={structure.Characteristics}({uintValue:X08})" );
	}
}
