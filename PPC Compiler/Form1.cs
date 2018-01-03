﻿using AutocompleteMenuNS;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Collections;

namespace PPC_Compiler
{
    public partial class Form1 : Form
    {
        AutocompleteMenu LabelMenu = new AutocompleteMenu();
        OpenFileDialog OpenDialog = new OpenFileDialog();
        SaveFileDialog SaveDialog = new SaveFileDialog();
        string CompilerOutput = "";

        public static string[] Registers = {
            "r0", "r1", "r2", "r3", "r4", "r5", "r6", "r7", "r8", "r9", "r10", "r11", "r12", "r13", "r14", "r15", "r16", "r17", "r18", "r19", "r20", "r21",
            "r22", "r23", "r24", "r25", "r26", "r27", "r28", "r29", "r30", "r31"
        };

        public static string[] Instructions = {
            "li", "lis", "add", "addi", "mr", "lbz", "lbzx", "lhz", "lhzx", "lwz", "lwzx", "stb", "sth", "stw", "b", "bl", "bctr", "beq", "bne", "ble", "bge", "blt", "bgt",
            "bdnz", "rlwinm", "addis", "cmpwi", "cmplwi", "cmpw", "cmplw", "mtctr", "and", "or", "andi", "ori", "divwu", "mulli", "srawi", "subf", "subis", "mtlr", "blr",
            "bctrl", "bcctr", "add.", "rlwinm.", "rlwimi", "addo", "addo.", "addc", "addc.", "addco", "addco.", "adde", "adde.", "addeo", "addeo.", "addic", "addic.", 
            "addme", "addme.", "addmeo", "addmeo.", "addze", "addze.", "addzeo", "addzeo.", "and.", "andc", "andc.", "andi.", "andis", "andis.", "ba", "bla", "bclr",
            "bclrl", "cmp", "cmpi", "cmpl", "cmpli", "cntlzw", "cntlzw.", "crand", "crandc", "creqv", "crnand", "crnor", "cror", "crorc", "crxor", "stwu", "mflr",
        };

        public Form1()
        {
            InitializeComponent();
            editor.Margins[0].Type = ScintillaNET.MarginType.Number;
            editor.Margins[0].Width = 28;
            LabelMenu.TargetControlWrapper = new ScintillaWrapper(editor);

            // Construct Custom Lexer
            editor.Lexer = ScintillaNET.Lexer.Cpp;
            editor.StyleResetDefault();
            editor.Styles[ScintillaNET.Style.Default].Font = "Consolas";
            editor.Styles[ScintillaNET.Style.Default].Size = 10;
            editor.Styles[ScintillaNET.Style.Default].BackColor = Color.DarkGray;
            editor.Styles[ScintillaNET.Style.Default].BackColor = Color.White;
            editor.StyleClearAll();

            editor.Styles[ScintillaNET.Style.Cpp.Default].BackColor = Color.DarkGray;
            editor.Styles[ScintillaNET.Style.Cpp.Default].BackColor = Color.White;
            editor.Styles[ScintillaNET.Style.Cpp.Word].ForeColor = Color.Blue;
            editor.Styles[ScintillaNET.Style.Cpp.Word2].ForeColor = Color.LightSeaGreen;
            editor.Styles[ScintillaNET.Style.Cpp.Number].ForeColor = Color.Olive;
            editor.Styles[ScintillaNET.Style.Cpp.String].ForeColor = Color.FromArgb(163, 21, 21);
            editor.Styles[ScintillaNET.Style.Cpp.Character].ForeColor = Color.FromArgb(163, 21, 21);
            editor.Styles[ScintillaNET.Style.Cpp.Comment].ForeColor = Color.LightGreen;
            editor.Styles[ScintillaNET.Style.Cpp.CommentLine].ForeColor = Color.Green;

            string Keywords = "";
            foreach (string Instruction in Instructions)
                Keywords += Instruction + " ";

            string RegisterKeywords = "";
            foreach (string Register in Registers)
                RegisterKeywords += Register + " ";

            editor.SetKeywords(0, Keywords);
            editor.SetKeywords(1, RegisterKeywords);

            OpenDialog.Filter = "PowerPC Assembly Source Files|*.pas|Text Files|*.txt|All Files|*.*";
            OpenDialog.DefaultExt = "PowerPC Assembly Source Files|*.pas";
            OpenDialog.FileName = "";

            SaveDialog.Filter = "PowerPC Assembly Source Files|*.pas|Text Files|*.txt|All Files|*.*";
            SaveDialog.DefaultExt = "PowerPC Assembly Source Files|*.pas";
            SaveDialog.FileName = "";

            LabelMenu.SetAutocompleteItems(new DynamicCollection(editor));
            toolStripComboBox1.SelectedIndex = 0;
        }

        private void OutputHandler(object sendingProcess, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                Console.WriteLine(e.Data);
                if (e.Data.Contains("Error"))
                {
                    CompilerOutput += e.Data + "\n";
                }
            }
        }

        private string StripComments(string Text)
        {
            string StrippedSource = "";
            string Line;

            using (var Reader = new StringReader(Text))
            {
                while ((Line = Reader.ReadLine()) != null)
                {
                    if (Line.Contains("//"))
                    {
                        StrippedSource += Line.Substring(0, Line.IndexOf("//")) + "\n";
                    }
                    else
                    {
                        StrippedSource += Line + "\n";
                    }
                }
            }

            return StrippedSource;
        }

        private void RunExecutable(string ProcessPath, string Arguments)
        {
            var ExecutableProcess = new Process();
            ExecutableProcess.StartInfo.FileName = ProcessPath;
            ExecutableProcess.StartInfo.Arguments = Arguments;
            ExecutableProcess.StartInfo.UseShellExecute = false;
            ExecutableProcess.StartInfo.CreateNoWindow = true;
            ExecutableProcess.StartInfo.RedirectStandardOutput = true;
            ExecutableProcess.StartInfo.RedirectStandardError = true;
            ExecutableProcess.OutputDataReceived += OutputHandler;
            ExecutableProcess.ErrorDataReceived += OutputHandler;

            ExecutableProcess.Start();

            ExecutableProcess.BeginOutputReadLine();
            ExecutableProcess.BeginErrorReadLine();

            ExecutableProcess.WaitForExit();

            if (ExecutableProcess.ExitCode != 0)
            {
                Console.WriteLine(Path.GetFileName(ProcessPath) + " exited with: " + ExecutableProcess.ExitCode);
            }
        }

        private string CompileGekkoAssembly(string input)
        {
            var Path = Application.StartupPath;
            var gekkoAs = Path + "\\powerpc-gekko-as.exe";
            var gekkoLd = Path + "\\powerpc-gekko-ld.exe";
            var gekkoObjCopy = Path + "\\powerpc-gekko-objcopy.exe";

            CompilerOutput = "";

            if (File.Exists(gekkoAs) && File.Exists(gekkoLd) && File.Exists(gekkoObjCopy))
            {
                var TextFile = File.CreateText(Path + "\\in.txt");
                TextFile.Write(StripComments(editor.Text));
                TextFile.Flush();
                TextFile.Close();

                RunExecutable(gekkoAs, "-mregnames -mgekko in.txt -o out.o");

                if (File.Exists(Path + "\\in.txt"))
                {
                    File.Delete(Path + "\\in.txt");
                }

                if (File.Exists(Path + "\\out.o"))
                {
                    RunExecutable(gekkoLd, "-Ttext 0x80000000 out.o");
                    RunExecutable(gekkoObjCopy, "-O binary out.o out.bin");

                    File.Delete(Path + "\\out.o");

                    if (File.Exists(Path + "\\out.bin"))
                    {
                        try
                        {
                            string PPCHexAssemblyString = "";
                            var Data = File.ReadAllBytes(Path + "\\out.bin");
                            File.Delete(Path + "\\out.bin");

                            int[] ConvertedData = new int[Data.Length / 4];
                            for (int i = 0; i < ConvertedData.Length; i++)
                            {
                                int idx = i * 4;
                                ConvertedData[i] = (Data[idx] << 24) | (Data[idx + 1] << 16) | (Data[idx + 2] << 8) | Data[idx + 3];
                            }

                            for (int i = 0; i < ConvertedData.Length; i++)
                            {
                                PPCHexAssemblyString += ConvertedData[i].ToString("X8") + "\r\n";
                            }

                            Debug.WriteLine(PPCHexAssemblyString);

                            return PPCHexAssemblyString;
                        }
                        catch
                        {

                        }
                    }
                }
            }

            return CompilerOutput;
        }

        private string GetConvertedAssembly()
        {
            var Path = Application.StartupPath;
            if (File.Exists(Path + "\\powerpc-eabi-elf-as.exe") && File.Exists(Path + "\\powerpc-eabi-elf-objcopy.exe"))
            {
                CompilerOutput = "";

                var TextFile = File.CreateText(Path + "\\in.txt");
                TextFile.Write(StripComments(editor.Text));
                TextFile.Flush();
                TextFile.Close();

                RunExecutable(Path + "\\powerpc-eabi-elf-as.exe", "-mregnames -o out.o in.txt");
                RunExecutable(Path + "\\powerpc-eabi-elf-objcopy.exe", "-O binary out.o out.bin");

                if (File.Exists(Path + "\\in.txt"))
                {
                    File.Delete(Path + "\\in.txt");
                }

                if (File.Exists(Path + "\\out.o"))
                {
                    File.Delete(Path + "\\out.o");
                }

                if (File.Exists(Path + "\\out.bin"))
                {
                    try
                    {
                        string PPCHexAssemblyString = "";
                        var Data = File.ReadAllBytes(Path + "\\out.bin");
                        File.Delete(Path + "\\out.bin");

                        int[] ConvertedData = new int[Data.Length / 4];
                        for (int i = 0; i < ConvertedData.Length; i++)
                        {
                            int idx = i * 4;
                            ConvertedData[i] = (Data[idx] << 24) | (Data[idx + 1] << 16) | (Data[idx + 2] << 8) | Data[idx + 3];
                        }

                        for(int i = 0; i < ConvertedData.Length; i++)
                        {
                            PPCHexAssemblyString += ConvertedData[i].ToString("X8") + "\r\n";
                        }

                        Debug.WriteLine(PPCHexAssemblyString);

                        return PPCHexAssemblyString;
                    }
                    catch
                    {

                    }
                }
            }
            return CompilerOutput;
        }

        private void buildToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var PPCString = "";
            if (toolStripComboBox1.SelectedIndex == 0)
                PPCString = GetConvertedAssembly();
            else if (toolStripComboBox1.SelectedIndex == 1)
                PPCString = CompileGekkoAssembly(StripComments(editor.Text));

            outputTextBox.Text = PPCString;
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (OpenDialog.ShowDialog() == DialogResult.OK && File.Exists(OpenDialog.FileName))
            {
                editor.Text = File.ReadAllText(OpenDialog.FileName);
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (SaveDialog.ShowDialog() == DialogResult.OK)
            {
                if (File.Exists(SaveDialog.FileName))
                {
                    File.Delete(SaveDialog.FileName);
                }

                using (var Writer = new BinaryWriter(new FileStream(SaveDialog.FileName, FileMode.OpenOrCreate)))
                {
                    Writer.Write(Encoding.ASCII.GetBytes(editor.Text));
                    Writer.Flush();
                    Writer.Close();
                }
            }
        }

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            editor.Text = ".text\r\n.org 0\r\n.globl _start\r\n\r\n// Main Entry Point\r\n_start:\r\nblr";
            editor.CurrentPosition = editor.Text.Length;
            editor.SelectionStart = editor.CurrentPosition;
        }

        internal class DynamicCollection : IEnumerable<AutocompleteItem>
        {
            ScintillaNET.Scintilla Editor;

            public DynamicCollection(ScintillaNET.Scintilla editor)
            {
                Editor = editor;
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }

            public IEnumerator<AutocompleteItem> GetEnumerator()
            {
                return BuildList().GetEnumerator();
            }

            private IEnumerable<AutocompleteItem> BuildList()
            {
                var Labels = new Dictionary<string, string>();

                foreach (string s in Instructions)
                    Labels.Add(s, s);

                foreach (string s in Registers)
                    Labels.Add(s, s);

                foreach (Match m in Regex.Matches(Editor.Text, @"^[^:\r\n]+:\r?\n", RegexOptions.Multiline))
                {
                    var Value = m.Value.Replace(":", "");
                    Labels[Value] = Value;
                }

                foreach (var Label in Labels.Keys)
                    yield return new AutocompleteItem(Label);
            }
        }
    }
}
