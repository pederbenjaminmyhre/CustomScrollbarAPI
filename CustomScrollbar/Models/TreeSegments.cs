using System.Data;

namespace CustomScrollbar.Models
{
    [Serializable]
    public class TreeSegments
    {
        public DataTable SegmentTable { get; private set; }

        public TreeSegments()
        {
            SegmentTable = new DataTable();
            DataColumn segmentID = SegmentTable.Columns.Add("SegmentID", typeof(int)); // identity column
            segmentID.AutoIncrement = true;
            segmentID.AutoIncrementSeed = 1;
            segmentID.AutoIncrementStep = 1;
            SegmentTable.Columns.Add("ParentSegmentID", typeof(int)); // recursive child key
            SegmentTable.Columns.Add("SegmentPosition", typeof(int)); // the position of the segment
            SegmentTable.Columns.Add("ParentID", typeof(int)); // the parent of the segment
            SegmentTable.Columns.Add("TreeLevel", typeof(int)); // the level of the segment
            SegmentTable.Columns.Add("RecordCount", typeof(int)); // the number of siblings in the segment
            SegmentTable.Columns.Add("FirstTreeRow", typeof(int)); // the first tree row in the segment
            SegmentTable.Columns.Add("LastTreeRow", typeof(int)); // the last tree row in the segment
            SegmentTable.Columns.Add("FirstCustomSortID", typeof(int)); // the first sibling in the segment
            SegmentTable.Columns.Add("LastCustomSortID", typeof(int)); // the last sibling in the segment
        }

        public void insertSegment(int parentSegmentID, int segmentPosition, int parentId, int treeLevel, int recordCount, int firstTreeRow, int lastTreeRow, int firstCustomSortID, int lastCustomSortID)
        {
            SegmentTable.Rows.Add(null, parentSegmentID, segmentPosition, parentId, treeLevel, recordCount, firstTreeRow, lastTreeRow, firstCustomSortID, lastCustomSortID);
        }

        private int _topLevelRecordCount;
        public int topLevelRecordCount
        {
            get => _topLevelRecordCount;
            set
            {
                _topLevelRecordCount = value;
                _totalRecordCount = value;
            }
        }

        private int _totalRecordCount;
        public int totalRecordCount
        {
            get => _totalRecordCount;
            set => _totalRecordCount = value;
        }
    }
}
