using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using CustomScrollbar.Models;
using System.Data.SqlTypes; // Ensure this using directive is present

namespace CustomScrollbar.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GetAnalyticalData : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<GetAnalyticalData> _logger;

        public GetAnalyticalData(IConfiguration configuration, ILogger<GetAnalyticalData> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet("scroll-grid")]
        public async Task<IActionResult> ScrollGrid(int firstTreeRow, int rowCount, int firstTreeColumn, int columnCount)
        {
            // Get TreeSegment table
            TreeSegments treeSegments = HttpContext.Session.GetObject<TreeSegments>("TreeSegments");

            // Save TreeSegment table
            this.SaveTreeSegmentTable(treeSegments);

            // Create queryListTable.
            DataTable queryListTable = CreateQueryListTable();

            // Make the list of query requests.
            int lastTreeRow = firstTreeRow + rowCount - 1;
            int lastTreeColumn = firstTreeColumn + columnCount - 1;
            foreach (DataRow segment in treeSegments.SegmentTable.Rows)
            {
                if ((int)segment["LastTreeRow"] >= firstTreeRow && (int)segment["FirstTreeRow"] <= lastTreeRow)
                {
                    DataRow row = queryListTable.NewRow();
                    row["parentId"] = (int)segment["ParentID"];

                    // offset for first query request
                    if ((int)segment["FirstTreeRow"] <= firstTreeRow && firstTreeRow <= (int)segment["LastTreeRow"])
                    {
                        int segment_FirstCustomSortID = (int)segment["FirstCustomSortID"];
                        int segment_FirstTreeRow = (int)segment["FirstTreeRow"];
                        int offset = segment_FirstCustomSortID - segment_FirstTreeRow;
                        row["firstRecordNumber"] = firstTreeRow - offset;
                    }
                    else
                    {
                        row["firstRecordNumber"] = (int)segment["FirstCustomSortID"];
                    }

                    // offset for last query request
                    if ((int)segment["FirstTreeRow"] <= lastTreeRow && lastTreeRow <= (int)segment["LastTreeRow"])
                    {
                        int segment_FirstCustomSortID = (int)segment["FirstCustomSortID"];
                        int segment_FirstTreeRow = (int)segment["FirstTreeRow"];
                        int offset = segment_FirstCustomSortID - segment_FirstTreeRow;
                        row["lastRecordNumber"] = lastTreeRow - ((-1) * offset);
                    }
                    else
                    {
                        row["lastRecordNumber"] = (int)segment["LastCustomSortID"];
                    }

                    row["treeLevel"] = (int)segment["TreeLevel"];
                    queryListTable.Rows.Add(row);
                }
            }


            // Get the data for the first segment
            DataTable result = await GetDataByTreeSegment(queryListTable, firstTreeColumn, lastTreeColumn);

            // Convert DataTable to List<Dictionary<string, object>>
            var jsonFriendlyResult = this.DataTableToDictionaryList(result);

            return Ok(jsonFriendlyResult);
        }

        /*

        [HttpGet("contract-node")]
        public async Task<IActionResult> ContractNode(int rootParentID, int rowCount, int columnCount)
        {
            // Get TreeSegment table
            TreeSegments treeSegments = HttpContext.Session.GetObject<TreeSegments>("TreeSegments");

            // Insert TreeSegment


            // Return both the record count and data
            var response = new InitializeGridResponse
            {
                RecordCount = recordCountForTopLevel,
                Data = jsonFriendlyResult
            };

            return Ok(response);
        }

        */





        //[HttpGet("expand-node")]
        //public async Task<IActionResult> ExpandNode(
        //    // the first 4 variables are used to determine what data is returned to the grid
        //    int firstTreeRow, // first visible
        //    int rowCount,
        //    int firstTreeColumn, // first visible
        //    int columnCount,
        //    // the last 5 variables are used to modify the TreeSegments
        //    int clickedNode_parentID, // The parentID of the clicked node
        //    int clickedNode_ID, // The ID of the clicked node
        //    int clickedNode_treeLevel, // The treeLevel of the clicked node
        //    int clickedNode_childRecordCount, // The recordCount of the clicked node
        //    int clickedNode_customSortID // The customSortID of the clicked node
        //    )
        //{

        [HttpPost("expand-node")]
        public async Task<IActionResult> ExpandNode([FromBody] ExpandNodeRequest request)
        {
            if (request == null)
            {
                return BadRequest("Invalid request payload.");
            }

            // Extract parameters from request object
            int firstTreeRow = request.FirstTreeRow;
            int rowCount = request.RowCount;
            int firstTreeColumn = request.FirstTreeColumn;
            int columnCount = request.ColumnCount;
            int clickedNode_parentID = request.ClickedNode_ParentID;
            int clickedNode_ID = request.ClickedNode_ID;
            int clickedNode_treeLevel = request.ClickedNode_TreeLevel;
            int clickedNode_childRecordCount = request.ClickedNode_ChildRecordCount;
            int clickedNode_customSortID = request.ClickedNode_CustomSortID;
            string Log1 = request.log1.ToString();


            // Get TreeSegment table
            TreeSegments treeSegments = HttpContext.Session.GetObject<TreeSegments>("TreeSegments");

            // Get "Parent TreeSegment" and store values in variables
            int p1_segmentID = 0, p1_parentSegmentID = 0, p1_segmentPosition = 0;
            int p1_parentID = 0, p1_treeLevel = 0, p1_recordCount = 0;
            int p1_firstTreeRow = 0, p1_lastTreeRow = 0;
            int p1_firstCustomSortID = 0, p1_lastCustomSortID = 0;
            DataRow? parentRecord = treeSegments.SegmentTable.AsEnumerable()
                .FirstOrDefault(r => r.Field<int>("ParentID") == clickedNode_parentID &&
                clickedNode_customSortID >= r.Field<int>("FirstCustomSortID") &&
                clickedNode_customSortID <= r.Field<int>("LastCustomSortID"));
            if (parentRecord != null)
            {
                p1_segmentID = parentRecord.Field<int>("SegmentID");
                p1_parentSegmentID = parentRecord.Field<int>("ParentSegmentID");
                p1_segmentPosition = parentRecord.Field<int>("SegmentPosition");
                p1_parentID = parentRecord.Field<int>("ParentID");
                p1_treeLevel = parentRecord.Field<int>("TreeLevel");
                p1_recordCount = parentRecord.Field<int>("RecordCount");
                p1_firstTreeRow = parentRecord.Field<int>("FirstTreeRow");
                p1_lastTreeRow = parentRecord.Field<int>("LastTreeRow");
                p1_firstCustomSortID = parentRecord.Field<int>("FirstCustomSortID");
                p1_lastCustomSortID = parentRecord.Field<int>("LastCustomSortID");
            }

            // If the clickedNode_customSortID < "Parent TreeSegment".LastCustomSortID, then SplitIsNeeded flag is set to true
            bool SplitIsNeeded = clickedNode_customSortID < p1_lastCustomSortID ? true : false;

            // If SplitIsNeeded, calculate changes to "Parent TreeSegment"
            int p2_segmentID = 0, p2_parentSegmentID = 0, p2_segmentPosition = 0, p2_parentID = 0, p2_treeLevel = 0, p2_recordCount = 0;
            int p2_firstTreeRow = 0, p2_lastTreeRow = 0, p2_firstCustomSortID = 0, p2_lastCustomSortID = 0;
            if(SplitIsNeeded) { 
                p2_segmentID = p1_segmentID; // no change
                p2_parentSegmentID = p1_parentSegmentID; // no change
                p2_segmentPosition = p1_segmentPosition; // no change
                p2_parentID = p1_parentID; // no change
                p2_treeLevel = p1_treeLevel; // no change
                p2_recordCount = clickedNode_customSortID - p1_firstCustomSortID + 1;
                p2_firstTreeRow = p1_firstTreeRow; // no change
                p2_lastTreeRow = p1_firstTreeRow + p2_recordCount - 1;
                p2_firstCustomSortID = p1_firstCustomSortID; // no change
                p2_lastCustomSortID = p1_firstCustomSortID + p2_recordCount - 1;
            }

            // Make record for "Inserted TreeSegment"
            int i_segmentID = 0, i_parentSegmentID = 0, i_segmentPosition = 0, i_parentID = 0, i_treeLevel = 0, i_recordCount = 0;
            int i_firstTreeRow = 0, i_lastTreeRow = 0, i_firstCustomSortID = 0, i_lastCustomSortID = 0;
            //i_segmentID = 0; //Auto-generated
            i_parentSegmentID = p1_segmentID;
            i_segmentPosition = p1_segmentPosition + 1;
            i_parentID = clickedNode_ID;
            i_treeLevel = clickedNode_treeLevel + 1;
            i_recordCount = clickedNode_childRecordCount;
            i_firstTreeRow = p2_lastTreeRow + 1;
            i_lastTreeRow = i_firstTreeRow + clickedNode_childRecordCount - 1;
            i_firstCustomSortID = 1;
            i_lastCustomSortID = clickedNode_childRecordCount;

            // If SplitIsNeeded, make record for "Split TreeSegment" (when the parent TreeSegment gets split)
            int s_segmentID = 0, s_parentSegmentID = 0, s_segmentPosition = 0, s_parentID = 0, s_treeLevel = 0, s_recordCount = 0;
            int s_firstTreeRow = 0, s_lastTreeRow = 0, s_firstCustomSortID = 0, s_lastCustomSortID = 0;
            
            if (SplitIsNeeded)
            {
                //s_segmentID = 0; Auto-generated
                s_parentSegmentID = p1_parentSegmentID;
                s_segmentPosition = i_segmentPosition + 1;
                s_parentID = p1_parentID;
                s_treeLevel = p1_treeLevel;
                s_recordCount = p1_recordCount - p2_recordCount;
                s_firstTreeRow = i_lastTreeRow + 1;
                s_lastTreeRow = s_firstTreeRow + s_recordCount - 1;
                s_firstCustomSortID = p2_lastCustomSortID + 1;
                s_lastCustomSortID = s_firstCustomSortID + s_recordCount - 1;
            }

            // Do any checks

            // If SplitIsNeeded, update parent TreeSegment
            if (SplitIsNeeded && parentRecord != null)
            {
                //parentRecord.SetField("SegmentID", p2_segmentID); // AutoIncrement
                parentRecord.SetField("ParentSegmentID", p2_parentSegmentID);
                parentRecord.SetField("SegmentPosition", p2_segmentPosition);
                parentRecord.SetField("ParentID", p2_parentID);
                parentRecord.SetField("TreeLevel", p2_treeLevel);
                parentRecord.SetField("RecordCount", p2_recordCount);
                parentRecord.SetField("FirstTreeRow", p2_firstTreeRow);
                parentRecord.SetField("LastTreeRow", p2_lastTreeRow);
                parentRecord.SetField("FirstCustomSortID", p2_firstCustomSortID);
                parentRecord.SetField("LastCustomSortID", p2_lastCustomSortID);
            }

            // Insert "Inserted TreeSegment" immediately after "Parent TreeSegment"
            int parentRecordIndex = treeSegments.SegmentTable.Rows.IndexOf(parentRecord);
            int insertedRecordIndex = 0;
            if (parentRecordIndex >= 0) // Ensure previousRecord is found
            {
                DataRow insertedRecord = treeSegments.SegmentTable.NewRow();
                //insertedRecord["SegmentID"] = i_segmentID; // AutoIncrement
                insertedRecord["ParentSegmentID"] = i_parentSegmentID;
                insertedRecord["SegmentPosition"] = i_segmentPosition;
                insertedRecord["ParentID"] = i_parentID;
                insertedRecord["TreeLevel"] = i_treeLevel;
                insertedRecord["RecordCount"] = i_recordCount;
                insertedRecord["FirstTreeRow"] = i_firstTreeRow;
                insertedRecord["LastTreeRow"] = i_lastTreeRow;
                insertedRecord["FirstCustomSortID"] = i_firstCustomSortID;
                insertedRecord["LastCustomSortID"] = i_lastCustomSortID;
                treeSegments.SegmentTable.Rows.InsertAt(insertedRecord, parentRecordIndex + 1);
                insertedRecordIndex = treeSegments.SegmentTable.Rows.IndexOf(insertedRecord);
            }

            // If SplitIsNeeded, insert "Split TreeSegment" immediately after "Inserted TreeSegment"
            int splitRecordIndex = 0;
            if (SplitIsNeeded && insertedRecordIndex >= 0) 
            {
                DataRow splitRecord = treeSegments.SegmentTable.NewRow();
                //splitRecord["SegmentID"] = s_segmentID; // AutoIncrement
                splitRecord["ParentSegmentID"] = s_parentSegmentID;
                splitRecord["SegmentPosition"] = s_segmentPosition;
                splitRecord["ParentID"] = s_parentID;
                splitRecord["TreeLevel"] = s_treeLevel;
                splitRecord["RecordCount"] = s_recordCount;
                splitRecord["FirstTreeRow"] = s_firstTreeRow;
                splitRecord["LastTreeRow"] = s_lastTreeRow;
                splitRecord["FirstCustomSortID"] = s_firstCustomSortID;
                splitRecord["LastCustomSortID"] = s_lastCustomSortID;
                treeSegments.SegmentTable.Rows.InsertAt(splitRecord, insertedRecordIndex + 1);
                splitRecordIndex = treeSegments.SegmentTable.Rows.IndexOf(splitRecord);
            }

            // Make cascading changes to subsequent TreeSegments
            int indexofLastInsertedRecord = splitRecordIndex > 0 ? splitRecordIndex : insertedRecordIndex;
            int numberOfSegmentsInserted = splitRecordIndex > 0 ? 2 : 1;
            int startRecordIndex = indexofLastInsertedRecord + 1;
            if ((treeSegments.SegmentTable.Rows.Count-1) > indexofLastInsertedRecord)
            {
                // Loop through all rows starting from index 4 (after index 3)
                for (int i = startRecordIndex; i < treeSegments.SegmentTable.Rows.Count; i++)
                {
                    DataRow row = treeSegments.SegmentTable.Rows[i];

                    // Increment the values in the specified columns
                    row["SegmentPosition"] = i + 1; // row.Field<int>("SegmentPosition") + numberOfSegmentsInserted;
                    row["FirstTreeRow"] = row.Field<int>("FirstTreeRow") + i_recordCount;
                    row["LastTreeRow"] = row.Field<int>("LastTreeRow") + i_recordCount;
                }
            }

            // Save TreeSegment table
            this.SaveTreeSegmentTable(treeSegments);

            int logId = 0;
            if (!string.IsNullOrEmpty(Log1))
            {
                logId = SaveTreeSegmentTableToDatabase("ExpandNode", Log1, treeSegments.SegmentTable);
            }

            // Create queryListTable.
            DataTable queryListTable = CreateQueryListTable();

            // Make the list of query requests.
            int lastTreeRow = firstTreeRow + rowCount - 1;
            int lastTreeColumn = firstTreeColumn + columnCount - 1;
            foreach (DataRow segment in treeSegments.SegmentTable.Rows)
            {
                if ((int)segment["LastTreeRow"] >= firstTreeRow && (int)segment["FirstTreeRow"] <= lastTreeRow)
                {
                    DataRow row = queryListTable.NewRow();
                    row["parentId"] = (int)segment["ParentID"];

                    // offset for first query request
                    if ((int)segment["FirstTreeRow"] <= firstTreeRow && firstTreeRow <= (int)segment["LastTreeRow"])
                    {
                        int segment_FirstCustomSortID = (int)segment["FirstCustomSortID"];
                        int segment_FirstTreeRow = (int)segment["FirstTreeRow"];
                        int offset = segment_FirstCustomSortID - segment_FirstTreeRow;
                        row["firstRecordNumber"] = firstTreeRow - offset;
                    }
                    else
                    {
                        row["firstRecordNumber"] = (int)segment["FirstCustomSortID"];
                    }

                    // offset for last query request
                    if ((int)segment["FirstTreeRow"] <= lastTreeRow && lastTreeRow <= (int)segment["LastTreeRow"])
                    {
                        int segment_FirstCustomSortID = (int)segment["FirstCustomSortID"];
                        int segment_FirstTreeRow = (int)segment["FirstTreeRow"];
                        int offset = segment_FirstCustomSortID - segment_FirstTreeRow;
                        row["lastRecordNumber"] = lastTreeRow - ((-1)*offset);
                    }
                    else
                    {
                        row["lastRecordNumber"] = (int)segment["LastCustomSortID"];
                    }

                    row["treeLevel"] = (int)segment["TreeLevel"];
                    queryListTable.Rows.Add(row);
                }
            }

            // Call GetDataByTreeSegment
            DataTable result = await GetDataByTreeSegment(queryListTable, firstTreeColumn, lastTreeColumn, logId);

            // Convert DataTable to List<Dictionary<string, object>>
            var jsonFriendlyResult = this.DataTableToDictionaryList(result);

            // return Ok(jsonFriendlyResult);
            return Ok(jsonFriendlyResult);

        }


        [HttpPost("initialize-grid")]
        public async Task<IActionResult> InitializeGrid([FromBody] InitializeGridRequest request)
        {
            _logger.LogInformation("InitializeGrid called with rootParentID: {rootParentID}, rowCount: {rowCount}, columnCount: {columnCount}",
                                    request.RootParentID, request.RowCount, request.ColumnCount);

            try
            {
                // Get the record count for the top level
                int recordCountForTopLevel = (int)await GetRootNodeCount();

                // Create TreeSegment table
                TreeSegments treeSegments = CreateTreeSegmentTable();

                // Insert the first segment
                DataRow row1 = treeSegments.SegmentTable.NewRow();
                row1["SegmentPosition"] = 1;
                row1["ParentSegmentID"] = 0; // No parent for the root segment
                row1["ParentID"] = request.RootParentID;
                row1["TreeLevel"] = 1;
                row1["RecordCount"] = recordCountForTopLevel;
                row1["FirstTreeRow"] = 1;
                row1["LastTreeRow"] = recordCountForTopLevel;
                row1["FirstCustomSortID"] = 1;
                row1["LastCustomSortID"] = recordCountForTopLevel;
                treeSegments.SegmentTable.Rows.Add(row1);

                int logId = 0;
                if (!string.IsNullOrEmpty(request.Log1))
                {
                    logId = SaveTreeSegmentTableToDatabase("InitializeGrid", request.Log1, treeSegments.SegmentTable);
                }

                // Make the list of query requests
                DataTable queryListTable = CreateQueryListTable();

                // Make the first query request
                DataRow row = queryListTable.NewRow();
                row["parentId"] = request.RootParentID;
                row["firstRecordNumber"] = 1;
                row["lastRecordNumber"] = request.RowCount;
                row["treeLevel"] = 1;
                queryListTable.Rows.Add(row);

                // Get the data for the first segment
                DataTable result = await GetDataByTreeSegment(queryListTable, 1, request.ColumnCount, logId);

                // Convert DataTable to List<Dictionary<string, object>>
                var jsonFriendlyResult = this.DataTableToDictionaryList(result);

                // Return both the record count and data
                var response = new InitializeGridResponse
                {
                    RecordCount = recordCountForTopLevel,
                    Data = jsonFriendlyResult
                };

                treeSegments.topLevelRecordCount = recordCountForTopLevel;
                this.SaveTreeSegmentTable(treeSegments);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while initializing the grid.");
                return StatusCode(StatusCodes.Status500InternalServerError, "Internal server error");
            }
        }



        private TreeSegments CreateTreeSegmentTable()
        {
            TreeSegments treeSegmentsInstance = HttpContext.Session.GetObject<TreeSegments>("TreeSegments");
            if (treeSegmentsInstance == null)
            {
                treeSegmentsInstance = new TreeSegments();
                HttpContext.Session.SetObject("TreeSegments", treeSegmentsInstance);
            }
            else
            {
                treeSegmentsInstance.SegmentTable.Clear();
            }

            return treeSegmentsInstance;
        }

        private void SaveTreeSegmentTable(TreeSegments treeSegmentsInstance)
        {
            // Save to session variable
            HttpContext.Session.SetObject("TreeSegments", treeSegmentsInstance);
        }

        private int SaveTreeSegmentTableToDatabase(string EventName, string Log1, DataTable treeSegments)
        {
            // Save to database
            int logId = 0;
            string connectionString = _configuration.GetConnectionString("DefaultConnection");
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                using (SqlCommand cmd = new SqlCommand("SaveSegmentTable", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Define a parameter for the table-valued type
                    SqlParameter tableParam = cmd.Parameters.AddWithValue("@SegmentTableSegment", treeSegments);
                    tableParam.SqlDbType = SqlDbType.Structured;
                    tableParam.TypeName = "SegmentTableType";

                    SqlParameter log1Param = cmd.Parameters.AddWithValue("@log1", Log1);
                    log1Param.SqlDbType = SqlDbType.VarChar;
                    log1Param.Size = -1;

                    SqlParameter eventName = cmd.Parameters.AddWithValue("@eventName", EventName);
                    eventName.SqlDbType = SqlDbType.VarChar;
                    eventName.Size =100;

                    SqlParameter logIdParam = new SqlParameter("@logId", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.Output
                    };
                    cmd.Parameters.Add(logIdParam);


                    cmd.ExecuteNonQuery();

                    logId = (int)logIdParam.Value;
                }
            }
            return logId;
        }

        private async Task<DataTable> GetDataByTreeSegment(DataTable queryListTable, int firstColumnNumber, int lastColumnNumber, int logId = 0)
        {

            // Call stored procedure
            DataTable returnDataTable = new DataTable();
            string storedProcedureName = "dbo.GetDataByTreeSegment";
            string connectionString = _configuration.GetConnectionString("DefaultConnection");
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                using (SqlCommand command = new SqlCommand(storedProcedureName, connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    SqlParameter treeSegmentParam = new SqlParameter("@TreeSegment", SqlDbType.Structured)
                    {
                        TypeName = "dbo.TreeSegmentType",
                        Value = queryListTable
                    };
                    command.Parameters.Add(treeSegmentParam);
                    command.Parameters.Add(new SqlParameter("@firstColumnNumber", SqlDbType.Int) { Value = firstColumnNumber });
                    command.Parameters.Add(new SqlParameter("@lastColumnNumber", SqlDbType.Int) { Value = lastColumnNumber });
                    command.Parameters.Add(new SqlParameter("@logID", SqlDbType.Int) { Value = logId });
                    using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                    {
                        await connection.OpenAsync();
                        adapter.Fill(returnDataTable);
                    }
                }
            }
            return returnDataTable;
        }

        private async Task<SqlInt32> GetRootNodeCount()
        {
            SqlInt32 rootNodeCount = 0;
            string connectionString = _configuration.GetConnectionString("DefaultConnection");
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                using (SqlCommand command = new SqlCommand("dbo.CountRootNodes", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    // Define the output parameter
                    SqlParameter outputParam = new SqlParameter("@NumberOfRootNodes", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.Output
                    };
                    command.Parameters.Add(outputParam);
                    await connection.OpenAsync();
                    await command.ExecuteNonQueryAsync();
                    // Retrieve the output value
                    rootNodeCount = command.Parameters["@NumberOfRootNodes"].Value != DBNull.Value ? (int)command.Parameters["@NumberOfRootNodes"].Value : 0;
                }
            }
            return rootNodeCount;
        }

        private static DataTable CreateQueryListTable()
        {
            // Transfer List<TreeSegment> to DataTable
            DataTable queryListTable = new DataTable();
            queryListTable.Columns.Add("parentId", typeof(int));
            queryListTable.Columns.Add("firstRecordNumber", typeof(int));
            queryListTable.Columns.Add("lastRecordNumber", typeof(int));
            queryListTable.Columns.Add("treeLevel", typeof(int));
            return queryListTable;
        }

        private List<Dictionary<string, object>> DataTableToDictionaryList(DataTable table)
        {
            var list = new List<Dictionary<string, object>>();

            foreach (DataRow row in table.Rows)
            {
                var dict = new Dictionary<string, object>();
                foreach (DataColumn col in table.Columns)
                {
                    dict[col.ColumnName] = row[col];
                }
                list.Add(dict);
            }

            return list;
        }
    }

    public class TreeSegment
    {
        public int ParentId { get; set; }
        public int FirstRecordNumber { get; set; }
        public int LastRecordNumber { get; set; }
        public int TreeLevel { get; set; }
    }

    public class InitializeGridResponse
    {
        public int RecordCount { get; set; }
        public List<Dictionary<string, object>> Data { get; set; }
    }

    public class InitializeGridRequest
    {
        public int RootParentID { get; set; }
        public int RowCount { get; set; }
        public int ColumnCount { get; set; }
        public string Log1 { get; set; }
    }

    public class ExpandNodeRequest
    {
        public int FirstTreeRow { get; set; }
        public int RowCount { get; set; }
        public int FirstTreeColumn { get; set; }
        public int ColumnCount { get; set; }
        public int ClickedNode_ParentID { get; set; }
        public int ClickedNode_ID { get; set; }
        public int ClickedNode_TreeLevel { get; set; }
        public int ClickedNode_ChildRecordCount { get; set; }
        public int ClickedNode_CustomSortID { get; set; }
        public string log1 { get; set; }
    }

}
