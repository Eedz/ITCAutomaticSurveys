using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Word = Microsoft.Office.Interop.Word;

namespace ITCAutomaticSurveys
{
    class ReportFormatting
    {
        
        //TODO probably do not need appWord argument
        public ReportFormatting()
        {

        }

        public void FormatTags (Word.Application appWord, Word.Document doc, bool highlight)
        {
            FormatStyle ( doc);
            InterpretFontTags(appWord, doc);
            if ( highlight) { InterpretHighlightTags( doc); }
            InterpretFillTags( doc);
        }

        public void FormatStyle(Word.Document doc) {
            Word.Range rng = doc.Content;
            Word.Find f = rng.Find;

            // indents
            f.Replacement.ClearFormatting();
            f.Replacement.Text = "\\1";
            f.Replacement.ParagraphFormat.IndentCharWidth(1);
            FindAndReplace(doc, "\\[indent\\](*)\\[/indent\\]", f);

            f.Replacement.ClearFormatting();
            f.Replacement.Text = "\\1";
            f.Replacement.ParagraphFormat.IndentCharWidth(2);
            FindAndReplace(doc, "\\[indent2\\](*)\\[/indent2\\]", f);

            f.Replacement.ClearFormatting();
            f.Replacement.Text = "\\1";
            f.Replacement.ParagraphFormat.IndentCharWidth(3);
            FindAndReplace(doc, "\\[indent3\\](*)\\[/indent3\\]", f);

            f.Replacement.ClearFormatting();
            f.Replacement.Text = "\\1";
            f.Replacement.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
            FindAndReplace(doc, "\\[center\\](*)\\[/center\\]", f);

            // punctuation
            f.Replacement.ClearFormatting();
            f.Replacement.Text = " ";
            FindAndReplace(doc, "&nbsp;", f);

            f.Replacement.ClearFormatting();
            f.Replacement.Text = ">";
            FindAndReplace(doc, "&gt;", f);

            f.Replacement.ClearFormatting();
            f.Replacement.Text = "<";
            FindAndReplace(doc, "&lt;", f);

            f.Replacement.ClearFormatting();
            f.Replacement.Text = "&";
            FindAndReplace(doc, "&amp;", f);

            f.Replacement.ClearFormatting();
            f.Replacement.Text = "\"";
            FindAndReplace(doc, "&quot;", f);
        }

        public void InterpretFontTags(Word.Application appWord, Word.Document doc) {
            Word.Range rng = doc.Content;
            Word.Find f = rng.Find;
            
            // Font options
            f.Replacement.ClearFormatting();
            f.Replacement.Text = "\\1";
            f.Replacement.Font.Bold = 1;
            FindAndReplace(doc, "\\<strong\\>(*)\\</strong\\>", f);

            f.Replacement.ClearFormatting();
            f.Replacement.Text = "\\1";
            f.Replacement.Font.Italic = 1;
            FindAndReplace(doc, "\\<em\\>(*)\\</em\\>", f);
            
            f.Replacement.ClearFormatting();
            f.Replacement.Text = "\\1";
            f.Replacement.Font.Underline = Word.WdUnderline.wdUnderlineSingle;
            FindAndReplace(doc, "\\<u\\>(*)\\</u\\>", f);

            // Font colors
            f.Replacement.ClearFormatting();
            f.Replacement.Text = "\\1";
            f.Replacement.Font.Color = Word.WdColor.wdColorLightBlue;
            FindAndReplace(doc, "\\<lblue\\>(*)\\</lblue\\>", f);

            f.Replacement.ClearFormatting();
            f.Replacement.Text = "\\1";
            f.Replacement.Font.Color = Word.WdColor.wdColorRed;
            FindAndReplace(doc, "\\<red\\>(*)\\</red\\>", f);

            f.Replacement.ClearFormatting();
            f.Replacement.Text = "\\1";
            f.Replacement.Font.Color = Word.WdColor.wdColorGray35;
            FindAndReplace(doc, "\\<gray\\>(*)\\</gray\\>", f);

            // tracked changes tags

        }
        public void InterpretHighlightTags(Word.Document doc) { }
        public void InterpretFillTags(Word.Document doc) { }
        public void ConvertTC(Word.Document doc) { }
        public void FormatShading(Word.Document doc) { }
        public void FindAndReplace (Word.Document doc, String findText, Word.Find f)
        {
            f.MatchWildcards = true;
            bool done = false;

            while (!done){
                f.Execute(findText, Type.Missing, Type.Missing, Type.Missing,
                    Type.Missing, Type.Missing, Type.Missing,
                    Type.Missing, Type.Missing, Type.Missing,
                    Word.WdReplace.wdReplaceAll);
                if (!f.Found) { done = true; }
                
            }
            
           
        }

        public void FormatHeadings(Word.Document doc, int c, bool subheads)
        {
            String txt;
            for (int i = 1; i <= doc.Tables[1].Rows.Count; i++)
            {
                txt = doc.Tables[1].Cell(i, 0).Range.Text;
                if (txt.Contains("head") || txt.Contains("!"))
                {
                    // set heading style and properties
                    doc.Tables[1].Rows[i].Range.Paragraphs.set_Style(Word.WdBuiltinStyle.wdStyleHeading1);
                    doc.Tables[1].Rows[i].SetHeight(20, Word.WdRowHeightRule.wdRowHeightAuto);
                    doc.Tables[1].Rows[i].Cells.VerticalAlignment = Word.WdCellVerticalAlignment.wdCellAlignVerticalCenter;
                    doc.Tables[1].Rows[i].Borders.OutsideColor = Word.WdColor.wdColorBlack;
                    doc.Tables[1].Rows[i].Borders.InsideColor = Word.WdColor.wdColorBlack;
                    doc.Tables[1].Rows[i].Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
                    doc.Tables[1].Rows[i].Range.Font.Bold = 1;
                    doc.Tables[1].Rows[i].Range.Font.Size = 12;
                    doc.Tables[1].Rows[i].Range.Font.Color = Word.WdColor.wdColorBlack;
                    // clear text in all cells up to c
                    for (int j = 0; j <= c; j++)
                    {
                        doc.Tables[1].Cell(i, j).Range.Text = "";
                    }
                }


                if (txt.Contains("subhead") && subheads)
                {
                    doc.Tables[1].Rows[i].Shading.ForegroundPatternColor = Word.WdColor.wdColorSkyBlue;
                }
                else if (txt.Contains("head") || txt.Contains("!"))
                {
                    doc.Tables[1].Rows[i].Shading.ForegroundPatternColor = Word.WdColor.wdColorRose;
                }
            }
        }
    }
}
