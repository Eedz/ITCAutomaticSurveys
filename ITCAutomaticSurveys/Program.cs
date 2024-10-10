using ITCLib;
using ITCReportLib;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;

namespace ITCAutomaticSurveys
{
    class Program
    {
        static bool allSurveys;
        static string singleCode;
        static DateTime? singleDate = null;

        static List<ReportSurvey> SurveyList;


        static StreamWriter log;
        static string logFile = "log.txt";

#if DEBUG
        static string filePath = Properties.Settings.Default["AutoSurveysFolderTest"].ToString();
        static string filePathFilters = Properties.Settings.Default["AutoSurveysFolderWithFiltersTest"].ToString();
#else
        static string filePath = Properties.Settings.Default["AutoSurveysFolder"].ToString();
        static string filePathFilters = Properties.Settings.Default["AutoSurveysFolderWithFilters"].ToString();
#endif

        static void Main(string[] args)
        {
            try
            {
                string strExeFilePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                string strWorkPath = System.IO.Path.GetDirectoryName(strExeFilePath);

                log = File.AppendText(Path.Combine(strWorkPath, logFile));
            }catch (UnauthorizedAccessException e)
            {
                Console.WriteLine(e.Message);
            }
            LogMessage("Generating automatic surveys...");

            // process arguments
            if (args.Length != 0)
                ProcessArgs(args);

            SurveyList = (from Survey s in DBAction.GetAllSurveysInfo()
                          select new ReportSurvey(s)).ToList();

            LogMessage("Checking for deleted surveys...");
            // delete documents for surveys that no longer exist
            RemoveDeletedSurveys(filePath);
            RemoveDeletedSurveys(filePathFilters);

            List<ReportSurvey> changed = GetSurveys();

            // now run the report for each survey in the list
            foreach (ReportSurvey survey in changed) 
            {
                survey.AddQuestions(DBAction.GetSurveyQuestions(survey));

                var images = DBAction.GetSurveyImages(survey);
                foreach (SurveyImage img in images)
                {
                    var q = survey.QuestionByRefVar(img.VarName);
                    if (q != null) q.Images.Add(img);
                }

                survey.MakeFilterList();
                DBAction.FillPreviousNames(survey, true);

                GenerateSurvey(survey, false, filePath);        // no filters               
                GenerateSurvey(survey, true, filePathFilters);  // with filters
            }

            LogMessage("Done!");
            log.WriteLine();
            log.Close();
        }

        /// <summary>
        /// Generate a survey report for the provided survey. Existing reports are deleted first.
        /// </summary>
        /// <param name="survey"></param>
        /// <param name="withFilters"></param>
        /// <param name="filePath"></param>
        private static void GenerateSurvey(ReportSurvey survey, bool withFilters, string filePath)
        {
            // delete existing document
            try
            {
                foreach (string f in Directory.EnumerateFiles(filePath, survey.SurveyCode + ",*.doc?"))
                {
                    LogMessage("Removing existing file: " + f);
                    File.Delete(f);
                }
            }
            catch
            {
                LogMessage("File in use, skipping " + survey.SurveyCode + "...");
                return;
            }

            try
            {
                if (withFilters)
                    LogMessage("Generating " + survey.SurveyCode + " (with filters)...");
                else
                    LogMessage("Generating " + survey.SurveyCode + "...");
                
                RefreshSurvey(survey, withFilters);
            }
            catch (Exception e)
            {
                LogMessage("Error generating " + survey.SurveyCode + "\r\n" + e.Message);
            }
        }

        /// <summary>
        /// Create a standard report for the provided survey.
        /// </summary>
        /// <param name="survey">Report survey content.</param>
        /// <param name="withFilters">True if the report should include a filter column.</param>
        private static void RefreshSurvey(ReportSurvey survey, bool withFilters)
        {
            StandardSurveyReport report = new StandardSurveyReport(survey);
            report.Options.ToC = true;
            report.Options.FormattingOptions.VarChangesCol = withFilters;
            report.Options.FormattingOptions.ColorSubs = true;

            if (withFilters)
                report.SurveyContent.ContentOptions.ContentColumns.Add("Filters");
            
            report.CreateReport();

            string dateString = singleDate == null ? DateTime.Today.ToString("ddMMMyyyy") : singleDate.Value.ToString("ddMMMyyyy");
            string filepath = withFilters? filePathFilters : filePath;
            string filename = withFilters? report.SurveyContent.SurveyCode + ", with filters, " + dateString : report.SurveyContent.SurveyCode + ", " + dateString;
            
            AutoSurveyPrinter printer = new AutoSurveyPrinter(report, filename);
            printer.FolderPath = filepath;

            if (withFilters)
            {
                printer.OutputOptions.PaperSize = PaperSizes.Legal;
            }

            printer.PrintReport();

            GC.Collect();
        }

        /// <summary>
        /// Write a message to the log and the console.
        /// </summary>
        /// <param name="message"></param>
        private static void LogMessage(string message)
        {
            try
            {
                log.WriteLine(DateTime.Now + ": " + message);
                Console.WriteLine(message);
            }catch (UnauthorizedAccessException e)
            {
                Console.WriteLine("Unable to write to log file. Access denied.");
            }
        }

        /// <summary>
        /// Remove any files in the folder that have a survey code that no longer exists.
        /// </summary>
        /// <param name="folder"></param>
        private static void RemoveDeletedSurveys(string folder)
        {
            // get list of all surveys
            List<string> surveyCodes = SurveyList.Select(x=>x.SurveyCode).ToList();

            // loop through files
            DirectoryInfo dir = new DirectoryInfo(folder);
     
            var files = dir.GetFiles();
           
            foreach (FileInfo file in files)
            {
                if (!file.Name.EndsWith(".docx"))
                    continue;

                int comma = file.Name.IndexOf(",");
                
                if (comma == -1)
                    continue;

                // get survey code
                string surveyCode = file.Name.Substring(0, comma);

                // check against survey list
                if (!surveyCodes.Contains(surveyCode))
                {
                    file.Delete();
                }
            }
        }

        /// <summary>
        /// Set application options by examining the command line arguments
        /// </summary>
        /// <param name="args"></param>
        private static void ProcessArgs(string [] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "a":
                        allSurveys = true;
                        break;
                    case "d":
                        singleDate = DateTime.Parse(args[i + 1]);
                        break;
                    case "s":
                        singleCode = args[i + 1];
                        break;
                    case "y":
                        singleDate = DateTime.Today.PreviousWorkDay();
                        break;
                }
            }
        }

        /// <summary>
        /// Return a list of surveys to be generated.
        /// </summary>
        /// <returns></returns>
        private static List<ReportSurvey> GetSurveys()
        {
            List<ReportSurvey> changed = null;

            try
            {
                if (allSurveys)
                {
                    changed = SurveyList;
                }
                else if (singleCode != null)
                {
                    changed = SurveyList.Where(x => x.SurveyCode.Equals(singleCode)).ToList();
                }
                else if (singleDate != null)
                {
                    changed = (from Survey s in DBAction.GetChangedSurveys(singleDate.Value)
                               select new ReportSurvey(s)).ToList();
                }
                else
                {
                    changed = (from Survey s in DBAction.GetChangedSurveys(DateTime.Today)
                               select new ReportSurvey(s)).ToList();
                }
            }
            catch
            {
                LogMessage("Error: Could not retrieve survey list.");
            }

            return (changed ?? new List<ReportSurvey>());
        }
    }
}
