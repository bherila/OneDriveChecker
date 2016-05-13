using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace OneDriveChecker
{
    public class Program
    {
        public static void Main(string[] args)
        {
            string startFolder = args[0];
            if (Directory.Exists(startFolder))
            {
                Result result = new Result();
                result.Add(Checkfolder(startFolder));
                if (result.ErrorsFound > 0)
                {
                    StringBuilder sb = new StringBuilder();
                    var reasons = result.Problems.GroupBy(c => c.ErrorReason);
                    foreach (var errormsg in reasons.OrderBy(c => c.Key))
                    {
                        sb.Append(errormsg.Key + System.Environment.NewLine);
                        foreach (var f in errormsg)
                        {
                            if (f.Reference.Length>0){
                                sb.Append("\t" + f.Reference);            
                            }
                            sb.Append("\t" + f.FileName + System.Environment.NewLine);                        }
                    }
                    File.WriteAllText("results.txt",sb.ToString());
                    Console.WriteLine("-------------------------------------------------");
                    Console.WriteLine(result.FilesChecked + " files checked");
                    Console.WriteLine(result.DirsChecked + " directories checked");
                    Console.WriteLine(result.ErrorsFound + " problems encountered");
                    Console.WriteLine("They can be reviewed in results.txt");
                }
            }
            else
            {
                Console.WriteLine("Folder does not exist: " + startFolder);
            }
        }

        private static Result Checkfolder(string folder)
        {
            Console.WriteLine("Checking " + folder);
            DirectoryInfo diFolder = new DirectoryInfo(folder);
            Result problems = new Result();
            string[]
            invalidNames = { "CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9" };
            Regex reAllPeriods = new Regex("^(\\.)+$");
            Regex reStartWithSpaces = new Regex("^( )+");
            Regex reEndsWithSpaces = new Regex("( )+$");
            Regex reEndsWithPeriods = new Regex("(\\.)+$");
            try
            {
                problems.DirsChecked += 1;
                foreach (FileInfo f in diFolder.GetFiles())
                {
                    problems.FilesChecked += 1;
                    int index = 0;
                    foreach (char c in f.Name)
                    {
                        if ((byte)Convert.ToChar(c) < 31)
                        {
                            //Characters whose integer representations are in the range from 1 through 31
                            problems.AddProblem(new ProblemFile
                            {
                                FileName = f.FullName,
                                ErrorReason = "Invalid unprintable in filename (char code <31)",
                                Reference = "char(" + (byte)Convert.ToChar(c) +  ") at index " + index + " " + f.Name.Substring(0,index+1) + "<--" 
                            });
                        }
                        else if (("<>:\\|/?*\"").Contains(c))
                        {
                            // < (less than)
                            // > (greater than)
                            // : (colon)
                            // " (double quote)
                            // / (forward slash)
                            // \ (backslash)
                            // | (vertical bar or pipe)
                            // ? (question mark)
                            // * (asterisk)
                            problems.AddProblem(new ProblemFile
                            {
                                FileName = f.FullName,
                                ErrorReason = "Invalid character in filename (" + c + ")",
                                Reference = c + " at index " + index + " " + f.Name.Substring(0,index+1) + "<--"
                            });
                        }
                        index ++;
                    }
                    if (f.FullName.Length > 180)
                    {
                        problems.AddProblem(new ProblemFile
                        {
                            FileName = f.FullName,
                            ErrorReason = "Possibly long path name",
                            Reference = f.FullName.Length + " characters"
                        });
                    }
                    if (invalidNames.Contains(f.Name))
                    {
                        //CON, PRN, AUX, NUL, COM1, COM2, COM3, COM4, COM5, COM6, COM7, COM8, COM9, LPT1, LPT2, LPT3, LPT4, LPT5, LPT6, LPT7, LPT8, and LPT9
                        problems.AddProblem(new ProblemFile
                        {
                            FileName = f.FullName,
                            ErrorReason = "Windows files can't be named " + f.Name
                        });
                    }
                    if (reAllPeriods.Match(f.Name).Success)
                    {
                        problems.AddProblem(new ProblemFile
                        {
                            FileName = f.FullName,
                            ErrorReason = "File names can't be all periods"
                        });
                    }
                    if (reStartWithSpaces.Match(f.Name).Success)
                    {
                        problems.AddProblem(new ProblemFile
                        {
                            FileName = f.FullName,
                            ErrorReason = "File names can't begin with space(s)"
                        });
                    }
                    if (reEndsWithSpaces.Match(f.Name).Success)
                    {
                        problems.AddProblem(new ProblemFile
                        {
                            FileName = f.FullName,
                            ErrorReason = "File names can't end with space(s)"
                        });
                    }
                    if (reEndsWithPeriods.Match(f.Name).Success)
                    {
                        problems.AddProblem(new ProblemFile
                        {
                            FileName = f.FullName,
                            ErrorReason = "File names can't end with period(s)"
                        });
                    }
                }
                foreach (DirectoryInfo d in diFolder.GetDirectories())
                {
                    if (d.FullName.Length > 180)
                    {
                        problems.AddProblem(new ProblemFile
                        {
                            FileName = d.FullName,
                            ErrorReason = "Possibly long path name",
                            Reference = d.FullName.Length + " characters"
                            
                        });
                    }
                    problems.Add(Checkfolder(d.FullName));
                }
            }
            catch (Exception ex)
            {
                problems.AddProblem(new ProblemFile
                {
                    FileName = folder,
                    ErrorReason = ex.Message
                });
            }

            return problems;
        }

        public class Result
        {
            public int FilesChecked { get; set; }
            public int DirsChecked { get; set; }
            public int ErrorsFound { get; set; }
            public List<ProblemFile> Problems { get; set; }

            public Result()
            {
                Problems = new List<ProblemFile>();
            }

            public void Add(Result r)
            {
                FilesChecked += r.FilesChecked;
                DirsChecked += r.DirsChecked;
                ErrorsFound += r.ErrorsFound;
                Problems.AddRange(r.Problems);
            }

            public void AddProblem(ProblemFile p)
            {
                Problems.Add(p);
                ErrorsFound += 1;
            }
        }

        public class ProblemFile
        {
            public string FileName { get; set; }
            public string ErrorReason { get; set; }
            public string Reference {get; set;}
            public ProblemFile(){
                FileName="";
                ErrorReason="";
                Reference="";
            }
            
        }
    }

}

