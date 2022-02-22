using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace GeneratorAppMain.Forms
{
    struct _IMAGE_FILE_HEADER
    {
        public ushort Machine;
        public ushort NumberOfSections;
        public uint TimeDateStamp;
        public uint PointerToSymbolTable;
        public uint NumberOfSymbols;
        public ushort SizeOfOptionalHeader;
        public ushort Characteristics;
    };

    public partial class AboutForm : Form
    {
        public AboutForm()
        {
            InitializeComponent();

            var version = Assembly.GetEntryAssembly().GetName().Version;
            labelVersion.Text = $"Version: {version.Major}.{version.Minor}.{version.Build}";
            labelBuild.Text = $"Build: {version.Revision}";
            labelDate.Text = $"Date: {GetLinkerTimestampUtc(Assembly.GetEntryAssembly().Location):MMM dd yyyy HH:mm:ss}";
        }

        public static DateTime GetLinkerTimestampUtc(string filePath)
        {
            const int peHeaderOffset = 60;
            const int linkerTimestampOffset = 8;
            var bytes = new byte[2048];

            using (var file = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                file.Read(bytes, 0, bytes.Length);
            }

            var headerPos = BitConverter.ToInt32(bytes, peHeaderOffset);
            var secondsSince1970 = BitConverter.ToInt32(bytes, headerPos + linkerTimestampOffset);
            var dt = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return dt.AddSeconds(secondsSince1970);
        }
    }
}
