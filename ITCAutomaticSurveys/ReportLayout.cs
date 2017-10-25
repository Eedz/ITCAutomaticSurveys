using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ITCAutomaticSurveys
{
    class ReportLayout
    {
        int paperSize;
        int fileFormat;
        int toc;
        bool coverPage;
        bool blankColumn;

        public ReportLayout()
        {
            paperSize = 1;
            fileFormat = 1;
            toc = 1;
            coverPage = false;
            blankColumn = false;
        }


        public int PaperSize
        {
            get { return paperSize; }
            // only accept values 1-4, otherwise default to 1
            set
            {
                if (value >= 1 && value <= 4)
                {
                    paperSize = value;
                }
                else
                {
                    paperSize = 1;
                }
            }
        }

        public int FileFormat {
            get { return fileFormat; }
            // only accept values 1 or 2, otherwise default to 1
            set
            {
                if (value == 1 || value == 2){
                    fileFormat = value;
                }
                else
                {
                    fileFormat = 1;
                }
            }
        }

        public int ToC {
            get { return toc; }
            set
            {
                // only accept values 1-3, otherwise default to 1
                if (value >= 1 && value <= 3)
                {
                    toc = value;
                }
                else
                {
                    toc = 1;
                }
            }
        }

        public bool CoverPage { get => coverPage; set => coverPage = value; }
        public bool BlankColumn { get => blankColumn; set => blankColumn = value; }
    }
}
