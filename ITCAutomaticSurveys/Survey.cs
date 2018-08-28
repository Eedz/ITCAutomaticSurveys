using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.ComponentModel;

//using System.Runtime.CompilerServices;

namespace ITCAutomaticSurveys
{
    class Survey //: INotifyPropertyChanged
    {
        #region Survey Properties
        
        SqlDataAdapter sql;
        SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["ISISConnectionString"].ConnectionString);

        DataSet SurveyDataSet;
        // data tables for this survey 
        public DataTable rawTable;              // raw survey content, separated into fields
        public DataTable commentTable;          // table holding comments
        public List<DataTable> translationTables;    // tables holding translation data
        public DataTable filterTable;           // table holding filters
        public DataTable finalTable;            // table holding the final output
        
        //Dictionary<int, Variable> questions;  // Variable object not yet implemented

        int id;                                 // unique id
        String surveyCode;
        DateTime backend;                         // file name of backup
        String webName;                         // the long name of this survey

        // filters are currently report-level but that may change
        int qRangeLow;
        int qRangeHigh;
        List<String> prefixes;
        String[] headings;
        List<String> varnames;

        // comment filters
        DateTime? commentDate;
        List<int> commentAuthors; // make a class of names?
        List<int> commentSources;

        // fields
        List<String> repeatedFields;
        List<String> commentFields;
        List<String> transFields;
        List<String> stdFields;
        bool varlabelCol;
        bool filterCol;
        bool commentCol;

        String essentialList; // comma-separated list of essential varnames (and their Qnums) in this survey

        //attributes
        bool primary;   // true if this is the primary survey
        bool qnum;      // true if this is the qnum-defining survey
        bool corrected; // true if this uses corrected wordings
        bool marked;    // true if the survey contains tracked changes (for 3-way report)

        // report level options
        bool includePrevNames;
        bool excludeTempNames;

        // errors and results
        // qnu list

        #endregion

        #region Constructors
        // blank constructor
        // TODO create constructors for quick reports + auto surveys (create an enum?)
        public Survey() {

            surveyCode = "";
            backend = DateTime.Today;
            webName = "";

            SurveyDataSet = new DataSet("Survey" + id);
            sql = new SqlDataAdapter();
            rawTable = new DataTable();

            qRangeLow = 0;
            qRangeHigh = 0;
            prefixes = new List<String>();
            varnames = new List<String>();
            headings = null;

            commentDate = null;
            commentAuthors = new List<int>();
            commentSources = new List<int>();

            repeatedFields = new List<String>();
            commentFields = new List<String>();
            transFields = new List<String>();

            stdFields = new List<String>
            {
                "PreP",
                "PreI",
                "PreA",
                "LitQ",
                "RespOptions",
                "NRCodes",
                "PstI",
                "PstP"
            };
            
            varlabelCol = false;
            filterCol = false;
            commentCol = false;

            essentialList = "";

            primary = false;  
            qnum = false ;     
            corrected = false ;
            marked  =false;

            includePrevNames = false;
            excludeTempNames = true;

        }
        #endregion 

        #region Methods and Functions

        // source tables
        // create rawTable, commentTable, translationTables
        
        public void GenerateSourceTable() {
            bool useBackup = false ;
            if (backend != DateTime.Today) { useBackup = true; }
                // create survey table (from backup or current)

            if (useBackup)
            {
                //GetBackupTable();
                GetSurveyTable();
            }
            else
            {
                GetSurveyTable();
            }

            // get corrected wordings
            if (corrected) { GetCorrectedWordings(); }

            // delete correctedflag column (or leave it in until the end?)
            //rawTable.Columns.Remove("CorrectedFlag");

            // insert comments into raw table
            if (commentCol) {
                MakeCommentTable();
                rawTable.Merge(commentTable, false, MissingSchemaAction.Add);
                // deallocate comment table
                commentTable.Dispose();
            }

            // insert filters into raw table
            if (filterCol) {
                MakeFilterTable();
                
            }

            if (transFields != null && transFields.Count != 0)
            {
                //if !(useSingleField) 
                if (useBackup)
                {
                    MakeTranslationTableBackup();
                }
                else
                {
                    MakeTranslationTable();
                }
                // insert translations now? or later?
            }

            // get essential question list
            GetEssentialQuestions();
            
            


            
            // deallocate filter table
            //filterTable.Dispose();
        }

        // Create the raw survey table containing words, corrected and table flags, and varlabel (if needed) from a backup
        // This could be achieved by changing the FROM clause in GetSurveyTable but often there are columns that don't exist in the backups, due to their age
        // and all the changes that have happened to the database over the years. 
        public void GetBackupTable() { }

        // Create the raw survey table containing wordings, corrected and table flags, and varlabel (if needed)
        public void GetSurveyTable() {
            String query = "";
            String where = "";
            String strQFilter;
            
            // form the query
            // standard fields
            query = "SELECT ID, Qnum AS SortBy, Survey, VarName, refVarName, Qnum, AltQnum, CorrectedFlag, TableFormat ";

            // wording fields, replace &gt; &lt; and &nbsp; right away
            for (int i = 0; i < stdFields.Count; i++)
            {
                query = query + ", Replace(Replace(Replace(" + stdFields[i] + ", '&gt;','>'), '&lt;', '<'), '&nbsp;', ' ') AS " + stdFields[i];
            }
            
            query = query.TrimEnd(',', ' ');
            // other fields
            if (varlabelCol) { query = query + ", VarLabel"; }

            // FROM and WHERE
            query = query + " FROM qrySurveyQuestions WHERE Survey ='" + surveyCode + "'";

            // question range WHERE
            strQFilter = GetQuestionFilter();
            if (strQFilter != "") { where = " AND " + strQFilter; }

            query = query + where + " ORDER BY Qnum ASC";

            // run the query and fill the data table
            conn.Open();
            sql.SelectCommand = new SqlCommand(query, conn);
            sql.Fill(rawTable);
            
            conn.Close();
            rawTable.PrimaryKey = new DataColumn[] { rawTable.Columns["ID"] };
            // clear varlabel from heading rows
            if (varlabelCol)
            {
                String refVar;
                foreach (DataRow row in rawTable.Rows)
                {
                    refVar = row["refVarName"].ToString();
                    if (refVar.StartsWith ("Z")) { row["VarLabel"] = ""; }
                }
            }

        }

        // Look up and apply corrected wordings to the raw table
        public void GetCorrectedWordings() {
            DataTable corrTable;
            
            corrTable = new DataTable();
            sql.SelectCommand = new SqlCommand("SELECT C.QID AS ID, SN.VarName, C.PreP, C.PreI, C.PreA, C.LitQ, C.PstI, C.PstP, C.RespOptions," +
                "C.NRCodes FROM qryCorrectedSurveyNumbers AS C INNER JOIN qrySurveyQuestions AS SN ON C.QID = SN.ID " +
                "WHERE SN.Survey ='" + surveyCode + "'", conn);

            conn.Open();
            sql.Fill(corrTable);
            conn.Close();
            corrTable.PrimaryKey = new DataColumn[] { corrTable.Columns["ID"] };
            rawTable.Merge(corrTable);

            corrTable.Dispose();
        }

        // Create a table for each translation language (OR combine them right here?) (TODO)
        public void MakeTranslationTable() {
            String query = "";
            String where = "";
            String whereLang = "";
            String strQFilter;

            // instantiate the data tables
            translationTables = new List<DataTable>();

            // create the filter for the query
            where = "WHERE Survey = '" + surveyCode + "'";
            strQFilter = GetQuestionFilter();
            if (strQFilter != "") { where += " AND " + strQFilter; }

            // create a data table for each language, set its primary key, add it to the list of translation tables
            for (int i = 0; i < transFields.Count; i++)
            {
                DataTable t;
                t = new DataTable();
                whereLang = " AND Lang ='" + transFields[i] + "'";

                query = "SELECT QID AS ID, Survey, VarName, refVarName, Replace(Replace(Replace(Translation, '&gt;', '>'), '&lt;', '<'), '&nbsp;', ' ') AS [" + transFields[i] + "] FROM qrySurveyQuestionsTranslation " + where + whereLang;
                
                // run the query and fill the data table
                conn.Open();
                sql.SelectCommand = new SqlCommand(query, conn);
                sql.Fill(t);

                conn.Close();

                t.PrimaryKey = new DataColumn[] { t.Columns["ID"] };

                // TODO get corrected wordings (see GetCorrectedWordings)
                // get headings? maybe not

                translationTables.Add(t);
            }
        }

        public void MakeTranslationTableBackup() { }

        public void MakeTranslationTableFromFields() { }

        // Fills the commentTable DataTable with comments for this survey
        // TODO: make comment table local 
        public void MakeCommentTable() {
            commentTable = new DataTable();
            String cmdText = "SELECT ID, VarName, Comments FROM tvf_surveyVarComments(@survey";


            SqlCommand cmd = new SqlCommand();

            cmd.Parameters.Add("@survey", SqlDbType.VarChar, 50);
            cmd.Parameters["@survey"].Value = surveyCode;
            // TODO: look at condensing this
            if (commentFields != null && commentFields.Count != 0)
            {
                cmd.Parameters.Add("@commentTypes", SqlDbType.VarChar);
                cmd.Parameters["@commentTypes"].Value = String.Join(",", commentFields);
                cmdText += ",@commentTypes";
            }
            else { cmdText += ",null"; }

            if (commentDate != null)
            {
                cmd.Parameters.Add("@commentDate", SqlDbType.DateTime);
                cmd.Parameters["@commentDate"].Value = commentDate;
                cmdText += ",@commentDate";
            }else { cmdText += ",null"; }

            if (commentAuthors != null && commentAuthors.Count != 0)
            {
                cmd.Parameters.Add("@commentAuthors", SqlDbType.VarChar);
                cmd.Parameters["@commentAuthors"].Value = String.Join(",",commentAuthors);
                cmdText += ",@commentAuthors";
            } else { cmdText += ",null"; }

            if (commentSources != null && commentSources.Count != 0)
            {
                cmd.Parameters.Add("@commentSources", SqlDbType.VarChar);
                cmd.Parameters["@commentSources"].Value = String.Join(",", commentSources);
                cmdText += ",@commentSources";
            }
            else { cmdText += ",null"; }
            cmdText += ")";
            // set the command text and connection
            cmd.CommandText = cmdText;
            cmd.Connection = conn;
            // set the sql adapter's command to the cmd object
            sql.SelectCommand = cmd;
            // open connection and fill the table with results
            conn.Open();
            sql.Fill(commentTable);
            conn.Close();
            commentTable.PrimaryKey = new DataColumn[] { commentTable.Columns["ID"] };

            
        }

        public void MakeFilterTable() { }

        // final table (TODO)
        public void MakeReportTable() {

            RemoveRepeats();

            
            List<String> columnNames = new List<String>();
            List<String> columnTypes = new List<String>();
            String questionColumnName = "";
            String colName = "";
            // construct finalTable
            // determine the fields that will appear in finalTable
            for (int i = 0; i < rawTable.Columns.Count; i++)
            {
                switch (rawTable.Columns[i].Caption)
                {
                    case "ID":
                    case "SortBy":
                    case "Survey":
                    case "PreP":
                    case "PreI":
                    case "PreA":
                    case "LitQ":
                    case "PstI":
                    case "PstP":
                    case "RespOptions":
                    case "NRCodes":
                    
                        break;
                    default:
                        columnNames.Add(rawTable.Columns[i].Caption);
                        columnTypes.Add("string");
                        break;

                }
                
            }
            // add a column for the question text
            questionColumnName = GetQuestionColumnName();
            columnNames.Add(questionColumnName);
            columnTypes.Add("string");
            finalTable = Utilities.CreateDataTable(surveyCode + ID + "_Final", columnNames.ToArray(), columnTypes.ToArray());
            DataRow newrow;
            // use DataRow[] dr = rawTable.Select to get all records for each operation
           
            // insert fields into finalTable from rawTable
            foreach (DataRow row in rawTable.Rows)
            {
                

                
                // NRformat
                // inline routing
                // semitel
                // table format tags
                // headings

                newrow = finalTable.NewRow();
                foreach (DataColumn col in row.Table.Columns)
                {
                    colName = col.Caption;
                    col.AllowDBNull = true;
                    var currentValue = row[colName]; 
                    switch (colName)
                    {
                        case "ID":
                        case "SortBy":
                        case "Survey":
                        case "PreP":
                        case "PreI":
                            // semi tel
                            
                        case "PreA":
                        case "LitQ":
                            // semi tel
                            // table format
                        case "PstI":
                        case "PstP":
                        case "RespOptions":
                            // long lists
                            if (!row.IsNull(colName))
                            {
                                int lines = Utilities.CountLines((String)row[colName].ToString());
                                if (lines >= 25)
                                {
                                    row[colName] = "[center](Response options omitted)[/center]";
                                    row.AcceptChanges();
                                }
                            }
                            // inline routing
                            // semi tel
                            // table format
                            break;
                        case "NRCodes":
                            // NRFormat
                            // inline routing
                            // table format
                            break;
                        case "Qnum":
                            
                            newrow[colName] = row[colName];
                            break;
                        case "VarName":
                            // headings
                            if (currentValue.ToString().StartsWith("Z") && !currentValue.ToString().EndsWith("s"))
                            {
                                row["Qnum"] = "reghead";
                                row.AcceptChanges();
                            }

                            if (currentValue.ToString().StartsWith("Z") && currentValue.ToString().EndsWith("s"))
                            {
                                row["Qnum"] = "subhead";
                                row.AcceptChanges();
                            }



                            // varname changes
                            if (includePrevNames && !row.IsNull(colName) && !currentValue.ToString().StartsWith("Z") ) {
                                row[colName] = currentValue + " " + GetPreviousNames((String)currentValue);
                                row.AcceptChanges();
                            }
                            // corrected 
                            if ((bool)row["CorrectedFlag"])
                            {
                                if (corrected) {row[colName] = row[colName] + "\r\n" + "[C]"; }
                                else { row[col] = row[colName] + "\r\n" + "[A]"; }

                            }
                            newrow[colName] = row[colName];
                            break;
                        default:
                            newrow[colName] = row[colName];
                            break;
                    }


                }
                // if this is varname BI104, set essential questions list TODO
                if (row["refVarName"].Equals( "BI104"))
                {
                    newrow[questionColumnName] = GetQuestionText(row) + "\r\n<strong>" + essentialList + "</strong>";
                }
                else
                {
                    newrow[questionColumnName] = GetQuestionText(row);
                }
                finalTable.Rows.Add(newrow);
            }

            // check enumeration and delete or keep Qnum/AltQnum
            finalTable.Columns.Remove("AltQnum");


            // delete unneeded fields
            finalTable.Columns.Remove("CorrectedFlag");
            finalTable.Columns.Remove("TableFormat");
            finalTable.Columns.Remove("refVarName");





        }

        private String GetPreviousNames(String varname)
        {
            
            String varlist = "";
            SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["ISISConnectionString"].ConnectionString);
            DataTable surveyListTable = new DataTable("ChangedSurveys");
            String query = "SELECT dbo.FN_VarNamePreviousNames(@varname, @survey, @excludeTemp)";
            SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.Add("@varname", SqlDbType.VarChar);
            cmd.Parameters["@varname"].Value = varname;
            cmd.Parameters.Add("@survey", SqlDbType.VarChar);
            cmd.Parameters["@survey"].Value = SurveyCode;
            cmd.Parameters.Add("@excludeTemp", SqlDbType.Bit);
            cmd.Parameters["@excludeTemp"].Value = excludeTempNames;

            conn.Open();
            try {
                varlist = (String)cmd.ExecuteScalar();
            }
            catch (System.Data.SqlClient.SqlException ex)
            {
                return "error";
            }
            conn.Close();
            if (!varlist.Equals(varname)) { varlist = "(Prev. " + varlist.Substring(varname.Length +1) + ")"; } else { varlist = ""; }
            return varlist;
        }

        private String GetQuestionText(DataRow row, String newline = "\r\n")
        {
            String questionText = "";
            if (row.Table.Columns.Contains("PreP") && !row.IsNull("PreP") && !row["PreP"].Equals("")) { questionText += "<strong>" + row["PreP"] + "</strong>" + newline; }
            if (row.Table.Columns.Contains("PreI") && !row.IsNull("PreI") && !row["PreI"].Equals("")) { questionText += "<em>" + row["PreI"] + "</em>" + newline; }
            if (row.Table.Columns.Contains("PreA") && !row.IsNull("PreA") && !row["PreA"].Equals("")) { questionText += row["PreA"] + newline; }
            if (row.Table.Columns.Contains("LitQ") && !row.IsNull("LitQ") && !row["LitQ"].Equals("")) { questionText += "[indent]" + row["LitQ"] + "[/indent]" + newline; }
            if (row.Table.Columns.Contains("RespOptions") && !row.IsNull("RespOptions") && !row["RespOptions"].Equals("")) { questionText += "[indent3]" + row["RespOptions"] + "[/indent3]" + newline; }
            if (row.Table.Columns.Contains("NRCodes") && !row.IsNull("NRCodes") && !row["NRCodes"].Equals("")) { questionText += "[indent3]" +  row["NRCodes"] + "[/indent3]" + newline; }
            if (row.Table.Columns.Contains("PstI") && !row.IsNull("PstI") && !row["PstI"].Equals("")) { questionText += "<em>" + row["PstI"] + "</em>" + newline; }
            if (row.Table.Columns.Contains("PstP") && !row.IsNull("PstP") && !row["PstP"].Equals("")) { questionText += "<strong>" + row["PstP"] + "</strong>"; }

            // replace all "<br>" tags with newline characters
            questionText = questionText.Replace("<br>", newline);
            questionText = Utilities.TrimString(questionText, newline);

            return questionText;
        }

        private String GetQuestionColumnName()
        {
            String column = "";
            column = surveyCode.Replace(".", "");
            if (!backend.Equals(DateTime.Today)) { column += "_" + backend.ToString("d"); }
            if (corrected) { column += "_Corrected"; }
            if (marked) { column += "_Marked"; }
            return column;
        }

        // functions
        public String GetQRangeFilter() {
            String filter = "";
            if (qRangeLow == 0 && qRangeHigh == 0) { return ""; }
            if (qRangeLow <= qRangeHigh)
            {
                filter = "Qnum BETWEEN '" + qRangeLow.ToString().PadLeft(3, '0') + "' AND '" + qRangeHigh.ToString().PadLeft(3, '0') + "'";
            }
            return filter;
        }

        // Returns a WHERE clause using the properties qRange, prefixes, and varnames (and headings if it is decided to use them again)
        public String GetQuestionFilter()
        {
            String filter = "";

            filter = GetQRangeFilter();
            
            if (prefixes != null && prefixes.Count != 0) { filter += " AND Left(VarName,2) IN ('" + String.Join("','", prefixes) + "')"; }
            if (varnames != null && varnames.Count != 0) { filter += " AND VarName IN ('" + String.Join("','", varnames) + "')"; }
            //if (headings != null && headings.Count != 0) { filter += " AND (" + GetHeadingFilter() + ")"; }
            // TODO trim AND from the edges 
            //filter.Trim();
            return filter;
        }

        public String GetTranslation(int index)
        {
            return "";
            //return TransFields(index);
        }

               

        // heading filters not supported at this time
        public String GetHeadingFilter() { return "1=1"; }

        public override String ToString() { return "ID: " + ID + "\r\n" + "Survey: " + SurveyCode + "\r\n" + "Backend: " + Backend; }

        // sets the essentialList property
        public void GetEssentialQuestions() {
            String varlist = "";
            System.Text.RegularExpressions.Regex rx = new System.Text.RegularExpressions.Regex("go to [A-Z][A-Z][0-9][0-9][0-9], then BI9");

            var query = from r in rawTable.AsEnumerable()
                        where r.Field<string>("PstP") != null && rx.IsMatch(r.Field<string>("PstP"))
                        select r;

            // if there are any variables with the special PstP instruction, create a list of them
            if (query.Any()) { 
                foreach (var item in query)
                {
                    varlist += item["VarName"] + " (" + item["Qnum"] + "), ";
                }

                varlist = varlist.Substring(0, varlist.Length - 2);
            }
            essentialList = varlist;
        }

        // Remove repeated values from the wording fields (PreP, PreI, PreA, LitQ, PstI, Pstp, RespOptions, NRCodes) unless they are requested.
        public void RemoveRepeats() {
            int mainQnum = 0;
            String currQnum = "";
            String currField = "";
            int currQnumInt = 0;
            bool firstRow = true;
            Object[] refRow = null; // this array will hold the 'a' question's fields
            // only try to remove repeats if there are more than 0 rows
            if (rawTable.Rows.Count == 0) return;
            // sort table by SortBy
            rawTable.DefaultView.Sort = "SortBy ASC";
            //
            foreach (DataRow r in rawTable.Rows)
            {
                currQnum = (String)r["Qnum"];
                if (currQnum.Length != 4 && currQnum.Length != 5) { continue; }

                // get the integer value of the current qnum
                int.TryParse(currQnum.Substring(0,3), out currQnumInt);

                // if this row is in table format, we need to remove all repeats, regardless of repeated designations
                if ((bool)r["TableFormat"]) {
                    // TODO set repeated fields to none (or all?)

                }
                
                // if this is a non-series row, the first member of a series, the first row in the report, or a new Qnum, make this row the reference row
                if (currQnum.Length == 3 || (currQnum.Length == 4 && currQnum.EndsWith("a")) || firstRow || currQnumInt != mainQnum)
                {
                    mainQnum = currQnumInt;
                    // copy the row's contents into an array
                    refRow = (Object[]) r.ItemArray.Clone();
                }
                else
                {
                    // if we are inside a series, compare the wording fields to the reference row
                    for (int i = 0; i < r.Table.Columns.Count;i++)
                    {
                        currField = r.Table.Columns[i].Caption; // field name
                        // if the current column is a standard wording column and has not been designated as a repeated field, compare wordings
                        if (stdFields.Contains(currField) && !repeatedFields.Contains(currField))
                        {
                            // if the current row's wording field matches the reference row, clear it. 
                            // otherwise, set the reference row's field to the current row's field
                            // this will cause a new reference point for that particular field, but not the fields that were identical to the original reference row
                            if (r[i].Equals(refRow[i])) // field is identical to reference row's field, clear it
                            {
                                r[i] = "";
                                r.AcceptChanges();
                            }
                            else // field is different from reference row's field, use this value as the new reference for this field
                            {
                                refRow[i] = r[i];
                            }
                        }
                    }
                }

                firstRow = false; // after once through the loop, we are no longer on the first row
            }
        }

        public int TranslationCount() { return TransFields.Count; }

        public void ReplaceQN() { }

        public String InsertRoutingQnum() { return ""; }

        public void ReplaceQN2() { }

        public void InsertCC() { }

        // possible unneeded once comments are retrieved with server function
        public void RemoveRepeatedComments() { }
        #endregion

        #region Gets and Sets

        public int ID { get => id; set => id = value; }
        public String SurveyCode { get => surveyCode; set => surveyCode = value; }
        public DateTime Backend { get => backend; set => backend = value; }
        public List<String> Prefixes { get => prefixes; set => prefixes = value; }
        public String[] Headings { get => headings; set => headings = value; }
        public List<String> Varnames { get => varnames; set => varnames = value; }
        public DateTime? CommentDate { get => commentDate; set => commentDate = value; }
        public List<int> CommentAuthors { get => commentAuthors; set => commentAuthors = value; }
        public List<int> CommentSources { get => commentSources; set => commentSources = value; }
        public List<String> CommentFields
        {
            get => commentFields;
            set
            {
                commentFields = value;
                if (commentFields != null && commentFields.Count!= 0) { commentCol = true; }
            }
        }
        public List<String> TransFields { get => transFields; set => transFields = value; }
        public List<String> StdFields { get => stdFields; set => stdFields = value; }
        public bool VarLabelCol { get => varlabelCol; set => varlabelCol = value; }
        public bool FilterCol { get => filterCol; set => filterCol = value; }
        public bool CommentCol { get => commentCol; set => commentCol = value; }
        public String EssentialList { get => essentialList; set => essentialList = value; }
        public bool Primary { get => primary; set => primary = value; }
        public bool Qnum { get => qnum; set => qnum = value; }
        public bool Corrected { get => corrected; set => corrected = value; }
        public bool Marked { get => marked; set => marked = value; }
        public int QRangeLow { get => qRangeLow; set => qRangeLow = value; }
        public int QRangeHigh { get => qRangeHigh; set => qRangeHigh = value; }
        public string WebName { get => webName; set => webName = value; }
        public bool IncludePrevNames { get => includePrevNames; set => includePrevNames = value; }
        public bool ExcludeTempNames { get => excludeTempNames; set => excludeTempNames = value; }

        #endregion



    }
}
