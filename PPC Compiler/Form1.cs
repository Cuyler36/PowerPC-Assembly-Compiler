using AutocompleteMenuNS;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;

namespace PPC_Compiler
{
    public partial class Form1 : Form
    {
        AutocompleteMenu InstructionMenu = new AutocompleteMenu();
        AutocompleteMenu RegisterMenu = new AutocompleteMenu();
        OpenFileDialog OpenDialog = new OpenFileDialog();
        SaveFileDialog SaveDialog = new SaveFileDialog();
        string CompilerOutput = "";

        string[] Registers = {
            "r0", "r1", "r2", "r3", "r4", "r5", "r6", "r7", "r8", "r9", "r10", "r11", "r12", "r13", "r14", "r15", "r16", "r17", "r18", "r19", "r20", "r21",
            "r22", "r23", "r24", "r25", "r26", "r27", "r28", "r29", "r30", "r31"
        };

        string[] Instructions = {
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
            BuildInstructionAutocompleteMenu();
            InstructionMenu.TargetControlWrapper = new ScintillaWrapper(editor);
            RegisterMenu.TargetControlWrapper = new ScintillaWrapper(editor);

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
        }

        private void BuildInstructionAutocompleteMenu()
        {
            var InstructionItems = new List<AutocompleteItem>();
            
            foreach (var Item in Instructions)
            {
                InstructionItems.Add(new SnippetAutocompleteItem(Item) { ImageIndex = 1 });
            }

            InstructionMenu.SetAutocompleteItems(InstructionItems);
        }

        private void BuildRegisterAutocompleteMenu()
        {
            var RegisterItems = new List<AutocompleteItem>();

            foreach (var Item in Registers)
            {
                RegisterItems.Add(new SnippetAutocompleteItem(Item) { ImageIndex = 1 });
            }

            RegisterMenu.SetAutocompleteItems(RegisterItems);
        }

        private void OutputHandler(object sendingProcess, DataReceivedEventArgs e)
        {
            Console.WriteLine(e.Data);
            if (!string.IsNullOrEmpty(e.Data) && e.Data.Contains("Error"))
            {
                CompilerOutput += e.Data + "\n";
            }
        }

        private string GetConvertedAssembly()
        {
            var Path = Application.StartupPath;
            if (File.Exists(Path + "\\powerpc-eabi-elf-as.exe") && File.Exists(Path + "\\powerpc-eabi-elf-objcopy.exe"))
            {
                CompilerOutput = "";

                var PPC_Compiler_Process = new Process();
                PPC_Compiler_Process.StartInfo.FileName = Path + "\\powerpc-eabi-elf-as.exe";

                var TextFile = File.CreateText(Path + "\\in.txt");
                TextFile.Write(editor.Text);
                TextFile.Flush();
                TextFile.Close();

                PPC_Compiler_Process.StartInfo.Arguments = "-mregnames -o out.o in.txt";
                PPC_Compiler_Process.StartInfo.UseShellExecute = false;
                PPC_Compiler_Process.StartInfo.CreateNoWindow = true;
                PPC_Compiler_Process.StartInfo.RedirectStandardOutput = true;
                PPC_Compiler_Process.StartInfo.RedirectStandardError = true;
                PPC_Compiler_Process.OutputDataReceived += OutputHandler;
                PPC_Compiler_Process.ErrorDataReceived += OutputHandler;

                PPC_Compiler_Process.Start();

                PPC_Compiler_Process.BeginOutputReadLine();
                PPC_Compiler_Process.BeginErrorReadLine();

                PPC_Compiler_Process.WaitForExit();

                if (PPC_Compiler_Process.ExitCode != 0)
                {
                    Console.WriteLine("powerpc-eabi-elf-as.exe exited with: " + PPC_Compiler_Process.ExitCode);
                }

                var PPC_Binary_Process = new Process();
                PPC_Binary_Process.StartInfo.FileName = Path + "\\powerpc-eabi-elf-objcopy.exe";
                PPC_Binary_Process.StartInfo.Arguments = "-O binary out.o out.bin";
                PPC_Binary_Process.StartInfo.UseShellExecute = false;
                PPC_Binary_Process.StartInfo.CreateNoWindow = true;
                PPC_Binary_Process.Start();

                PPC_Binary_Process.WaitForExit();

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
            var PPCString = GetConvertedAssembly();
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
            editor.Text = ".text\n.org 0\n.globl _start\n\n_start:\nblr";
        }
    }
}
