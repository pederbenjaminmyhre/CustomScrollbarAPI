using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Threading.Tasks;

namespace CustomScrollbar.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CountMembers : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public CountMembers(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet("count-root-nodes")]
        public async Task<IActionResult> GetRootNodeCount()
        {
            int rootNodeCount = 0;
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

            return Ok(new { RootNodeCount = rootNodeCount });
        }
    }
}
