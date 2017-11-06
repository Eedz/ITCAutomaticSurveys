using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Data;
using System.Configuration;
using System.IO;

namespace ITCAutomaticSurveys
{
    class Program
    {

        static List<Survey> changed;
        static SurveyReport SR;

        #if DEBUG 
        static String filePath = Properties.Settings.Default["AutoSurveysFolderTest"].ToString();
        #else   
        static String filePath = Properties.Settings.Default["AutoSurveysFolder"].ToString();
        #endif

        static void Main(string[] args)
        {
            
            changed =  new List<Survey>();
            // fill the 'changed' list
            GetSurveyList();

            // set report options
            SR = new SurveyReport
            {
                Batch = true,
                VarChangesCol = true,
                ExcludeTempChanges = true,
                Details = "",
                ReportType = 1,
                ColorSubs = true,
                Automatic = true
            };
            
            List<Survey> single = new List<Survey>();
            // now run the report for each survey in the list
            for (int i = 0; i < changed.Count; i++) {

                // delete existing document
                foreach (string f in Directory.EnumerateFiles(filePath, changed[i].SurveyCode + "*.docx"))
                {
                    File.Delete(f);
                }

                // add the current survey to the report
                single.Add(changed[i]);
                SR.Surveys = single;
                SR.ColOrder = changed[i].SurveyCode;
                SR.FileName = filePath;
                // run the report
                SR.GenerateSurveyReport();
                single = new List<Survey>();
            }

        }

        public static void GetSurveyList()
        {
            SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["ISISConnectionString"].ConnectionString);
            DataTable surveyListTable = new DataTable("ChangedSurveys");
            String query = "SELECT A.Survey, B.SurveyTitle " +
                "FROM FN_getChangedSurveys(@date) AS A INNER JOIN tblStudyAttributes AS B ON A.Survey = B.Survey " + 
                "GROUP BY A.Survey, B.SurveyTitle";
            SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.Add("@date", SqlDbType.DateTime);
            cmd.Parameters["@date"].Value = DateTime.Today; // Utilities.PreviousWorkDay(DateTime.Today);
            SqlDataAdapter adapter = new SqlDataAdapter(cmd);
            adapter.Fill(surveyListTable);

            
            Survey s;
            // get list of surveys that need to be generated (via server query)
            foreach (DataRow r in surveyListTable.Rows)
            {
                s = new Survey
                {
                    ID = 1,
                    SurveyCode = r["Survey"].ToString(),
                    Backend = DateTime.Today,
                    Primary = true,
                    Qnum = true,
                    IncludePrevNames = true,
                    ExcludeTempNames = true,
                    WebName = r["SurveyTitle"].ToString()

                };
                changed.Add(s);
                
            }
            conn.Close();
        }
    }
}
