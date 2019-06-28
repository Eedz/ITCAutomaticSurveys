using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Configuration;
using System.IO;
using ITCLib;

namespace ITCAutomaticSurveys
{
    class Program
    {
        static List<ReportSurvey> changed;
        static SurveyReport SR;
        static bool allSurveys;
        static string singleCode;
        static string singleDate;

        #if DEBUG
        static string filePath = Properties.Settings.Default["AutoSurveysFolderTest"].ToString();
        static string filePathFilters = Properties.Settings.Default["AutoSurveysFolderWithFiltersTest"].ToString();
#else
        static string filePath = Properties.Settings.Default["AutoSurveysFolder"].ToString();
        static string filePathFilters = Properties.Settings.Default["AutoSurveysFolderWithFilters"].ToString();
#endif

        static void Main(string[] args)
        {
            // process arguments
            if (args.Length != 0)
                ProcessArgs(args);

            changed = GetSurveyList(allSurveys);

            // now run the report for each survey in the list
            for (int i = 0; i < changed.Count; i++) {

                // delete existing document
                foreach (string f in Directory.EnumerateFiles(filePath, changed[i].SurveyCode + ",*.doc?"))
                {
                    File.Delete(f);
                }

                // populate the survey
                DBAction.FillQuestions(changed[i]);

                RefreshSurvey(changed[i], false);

                // delete existing document
                foreach (string f in Directory.EnumerateFiles(filePathFilters, changed[i].SurveyCode + ",*.doc?"))
                {
                    File.Delete(f);
                }

                RefreshSurvey(changed[i], true);

            }

        }

        private static void RefreshSurvey(ReportSurvey s, bool withFilters)
        {
           // set report options
           SR = new SurveyReport
           {
               Batch = true,
               VarChangesCol = true,
               ExcludeTempChanges = true,
               Details = "",
               ReportType = ReportTypes.Standard,
               ColorSubs = true
           };

            s.FilterCol = withFilters;
            if (withFilters)
            {
                s.MakeFilterList();
                SR.FileName = filePathFilters;
            }
            else
            {
                SR.FileName = filePath;
            }

            // add the current survey to the report
            SR.AddSurvey(s);

            // run the report
            SR.GenerateReport();
            SR.OutputReportTableXML();

            GC.Collect();
        }

        // Set application options by examining the command line arguments
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
                        singleDate = args[i + 1];
                        break;
                    case "s":
                        singleCode = args[i + 1];
                        break;
                }
            }
        }

        /// <summary>
        ///  Return a list of surveys to be generated.
        /// </summary>
        /// <param name="allSurveys"></param>
        private static List<ReportSurvey> GetSurveyList(bool allSurveys)
        {
            List<ReportSurvey> changed;
            ReportSurvey s;

            changed = new List<ReportSurvey>();

            using (SqlDataAdapter sql = new SqlDataAdapter())
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["ISISConnectionStringTest"].ConnectionString))
            {
                conn.Open();

                string query="";
            
                sql.SelectCommand = new SqlCommand();

                if (allSurveys)
                {
                    query = "SELECT ID, Survey, SurveyTitle FROM tblStudyAttributes";
                    
                }
                else if (singleCode != null)
                {
                    query = "SELECT ID, Survey, SurveyTitle FROM tblStudyAttributes WHERE Survey = @survey";
                    sql.SelectCommand.Parameters.AddWithValue("@survey", singleCode);
                }
                else if (singleDate != null)
                {
                    query = "SELECT A.ID, A.Survey, B.SurveyTitle " +
                        "FROM FN_getChangedSurveys(@date) AS A INNER JOIN tblStudyAttributes AS B ON A.Survey = B.Survey " +
                        "GROUP BY A.ID, A.Survey, B.SurveyTitle";

                    sql.SelectCommand.Parameters.AddWithValue("@date", singleDate);
                }
                else
                {
                    query = "SELECT A.ID, A.Survey, B.SurveyTitle " +
                        "FROM FN_getChangedSurveys(@date) AS A INNER JOIN tblStudyAttributes AS B ON A.Survey = B.Survey " +
                        "GROUP BY A.ID, A.Survey, B.SurveyTitle";

                    sql.SelectCommand.Parameters.AddWithValue("@date", DateTime.Today);

                }

                sql.SelectCommand.Connection = conn;
                sql.SelectCommand.CommandText = query;

                try
                {
                    using (SqlDataReader rdr = sql.SelectCommand.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            s = new ReportSurvey
                            {
                                ID = 1,
                                SID = (int)rdr["ID"],
                                SurveyCode = rdr["Survey"].ToString(),
                                Title = rdr["SurveyTitle"].ToString(),
                                Backend = DateTime.Today,
                                Primary = true,
                                Qnum = true,
                                WebName = rdr["SurveyTitle"].ToString()

                            };
                            changed.Add(s);
                        }
                    }
                }
                catch (Exception)
                {
                    int i = 0;
                }

                return changed;              
            }
        }
    }
}
