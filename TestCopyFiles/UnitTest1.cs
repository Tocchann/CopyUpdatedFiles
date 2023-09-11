using CopyFiles.Core;
using NUnit.Framework.Internal;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace TestCopyFiles;

public class Tests
{
	[SetUp]
	public void Setup()
	{
	}
	[Test]
	public void CheckFilePath()
	{
		var filePath = Path.Combine( AppDomain.CurrentDomain.BaseDirectory, "Signed.exe" );
		Trace.WriteLine( filePath );
		Assert.IsTrue( File.Exists(filePath), $"File.Exists({filePath})" );
	}
	[Test] public void CheckStructSize()
	{
		Assert.That( Marshal.SizeOf<CopyFiles.Core.Interop.IMAGE_DOS_HEADER>() - sizeof( uint ), Is.EqualTo( 0x3C ) );
	}
	[Test]
	public void DumpSignedExe()
	{
		var filePath = Path.Combine( AppDomain.CurrentDomain.BaseDirectory, "Signed.exe" );
		DumpExe( filePath );
	}
	[Test]
	public void DumpNonSignedExe()
	{
		var filePath = Path.Combine( AppDomain.CurrentDomain.BaseDirectory, "NonSigned.exe" );
		DumpExe( filePath );
	}
	[Test]
	public void DumpSetupFmExe()
	{
		var filePath = Path.Combine( AppDomain.CurrentDomain.BaseDirectory, "setup_fm.exe" );
		DumpExe( filePath );
	}
	[Test]
	public void DumpBinaryFile()
	{
		var filePath = Path.Combine( AppDomain.CurrentDomain.BaseDirectory, "TestCopyFiles.pdb" );
		DumpNonExe( filePath );
	}
	[Test]
	public void DumpTextFile()
	{
		var filePath = Path.Combine( AppDomain.CurrentDomain.BaseDirectory, "TestCopyFiles.runtimeconfig.json" );
		DumpNonExe( filePath );
	}
	private void DumpExe( string filePath )
	{
		Trace.WriteLine( filePath );
		var fileImage = File.ReadAllBytes( filePath );
		Assert.IsNotNull( fileImage, "fileImage is not null" );
		Assert.IsTrue( fileImage.Length > 0 );
		Assert.IsTrue( PeFileService.IsValidPE( fileImage ), "PeFileService.IsValidPE( fileImage )" );
		PeFileService.DumpPeHeader( fileImage );
		var offset = PeFileService.CalcHashArea( fileImage, out var count );
		Assert.IsTrue( offset > 0 && offset + count <= fileImage.Length, $"{offset} > 0 && {offset} + {count} <= {fileImage.Length}" );
	}
	private void DumpNonExe( string filePath )
	{
		Trace.WriteLine( filePath );
		var fileImage = File.ReadAllBytes( filePath );
		Assert.IsNotNull( fileImage, "fileImage is not null" );
		Assert.IsTrue( fileImage.Length > 0 );
		var offset = PeFileService.CalcHashArea( fileImage, out var count );
		Assert.IsTrue( offset == 0 && count == fileImage.Length, $"{offset} == 0 && {count} == {fileImage.Length}" );
	}
}