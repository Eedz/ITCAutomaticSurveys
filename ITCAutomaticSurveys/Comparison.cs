using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

namespace ITCAutomaticSurveys
{
    class Comparison
    {

        // general comparison options
        bool selfCompare;
        bool doCompare; // 

        //
        bool hidePrimary;
        bool showDeletedFields;
        bool showDeletedQuestions;
        bool reInsertDeletions;
        bool hideIdenticalWordings;
        bool showOrderChanges;
        bool beforeAfterReport;
        bool ignoreSimilarWords;
        bool convertTrackedChanges;
        bool matchOnRename;

        // ordercomparisons
        bool includeWordings;
        bool bySection;

        // highlight options
        public enum HScheme { Sequential = 1, AcrossCountry = 2 }
        public enum HStyle { Classic = 1, TrackedChanges = 2 }
        bool highlight;
        HStyle highlightStyle;
        HScheme highlightScheme;
        bool highlightNR;
        bool hybridHighlight;

        public Comparison()
        {
            selfCompare = false;
            doCompare = false;

            hidePrimary = false;
            showDeletedFields = false;
            showDeletedQuestions = false;
            reInsertDeletions = false;
            hideIdenticalWordings = false;
            showOrderChanges = false;
            beforeAfterReport = false;
            ignoreSimilarWords = false;
            convertTrackedChanges = false;
            matchOnRename = false;

            includeWordings = false;
            bySection = false;

            highlight = false;
            highlightStyle = HStyle.Classic;
            highlightScheme = HScheme.Sequential;
            highlightNR = true;
            hybridHighlight = false;
        }

        public bool DoCompare { get => doCompare; set => doCompare = value; }
        public bool SelfCompare { get => selfCompare; set => selfCompare = value; }

        [CategoryAttribute("Layout Options"), DescriptionAttribute("Hide the primary survey.")]
        public bool HidePrimary { get => hidePrimary; set => hidePrimary = value; }
        [CategoryAttribute("Highlighting Options"), DescriptionAttribute("Show deleted fields in blue.")]
        public bool ShowDeletedFields { get => showDeletedFields; set => showDeletedFields = value; }
        [CategoryAttribute("Highlighting Options"), DescriptionAttribute("Show deleted questions (depends on Scheme).")]
        public bool ShowDeletedQuestions { get => showDeletedQuestions; set => showDeletedQuestions = value; }
        [CategoryAttribute("Highlighting Options"), DescriptionAttribute("Re-insert deleted questions after the last common question.")]
        public bool ReInsertDeletions { get => reInsertDeletions; set => reInsertDeletions = value; }
        [CategoryAttribute("Layout Options"), DescriptionAttribute("Hide identical wording fields.")]
        public bool HideIdenticalWordings { get => hideIdenticalWordings; set => hideIdenticalWordings = value; }
        [CategoryAttribute("Highlighting Options"), DescriptionAttribute("Highlight changes to the order or questions.")]
        public bool ShowOrderChanges { get => showOrderChanges; set => showOrderChanges = value; }
        [CategoryAttribute("Layout Options"), DescriptionAttribute("Create a 3-column report in old-marked-new order.")]
        public bool BeforeAfterReport { get => beforeAfterReport; set => beforeAfterReport = value; }
        [CategoryAttribute("Highlighting Options"), DescriptionAttribute("Ignore words with alternate spellings (honor/honour).")]
        public bool IgnoreSimilarWords { get => ignoreSimilarWords; set => ignoreSimilarWords = value; }
        [CategoryAttribute("Highlighting Options"), DescriptionAttribute("Convert tracked changes highlighting to actual tracked changes.")]
        public bool ConvertTrackedChanges { get => convertTrackedChanges; set => convertTrackedChanges = value; }
        [CategoryAttribute("Highlighting Options"), DescriptionAttribute("Try to match questions based on previous names.")]
        public bool MatchOnRename { get => matchOnRename; set => matchOnRename = value; }

        

        [CategoryAttribute("Highlighting Options"), DescriptionAttribute("Highlight differences between surveys."), DefaultValueAttribute(true)]
        public bool Highlight { get => highlight; set => highlight = value; }

        [CategoryAttribute("Highlighting Options"), DescriptionAttribute("Classic (Green, Yellow, Blue) or Tracked Changes."), DisplayNameAttribute("Style"), DefaultValueAttribute(1)]
        public HStyle HighlightStyle { get => highlightStyle; set => highlightStyle = value; }

        [CategoryAttribute("Highlighting Options"), DescriptionAttribute("Determines how deleted questions are shown."), DisplayNameAttribute("Scheme"),DefaultValueAttribute(1)]
        public HScheme HighlightScheme { get => highlightScheme; set => highlightScheme = value; }
        [CategoryAttribute("Highlighting Options"), DescriptionAttribute("Highlight changes in the Non-response field."), DefaultValueAttribute(true)]
        public bool HighlightNR { get => highlightNR; set => highlightNR = value; }
        [CategoryAttribute("Highlighting Options"), DescriptionAttribute("For wording differences, use both Green and Tracked Changes."), DefaultValueAttribute(false)]
        public bool HybridHighlight { get => hybridHighlight; set => hybridHighlight = value; }

        [CategoryAttribute("Order Comparisons"), DescriptionAttribute("Include a column for the wordings."), DefaultValueAttribute(false)]
        public bool IncludeWordings { get => includeWordings; set => includeWordings = value; }
        [CategoryAttribute("Order Comparisons"), DescriptionAttribute("Display the report by section."), DefaultValueAttribute(false)]
        public bool BySection { get => bySection; set => bySection = value; }


    }
}
