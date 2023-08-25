using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using System.IO;

namespace SwiftReader.Controllers
{
    [Route("api/messages")]
    [ApiController]
    public class MessageController : ControllerBase
    {
        private const string _connectionString = "Data Source=Messages.db;";

        [HttpPost("upload")]
        public IActionResult UploadSwiftFile([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

            using (var reader = new StreamReader(file.OpenReadStream()))
            {
                string content = reader.ReadToEnd();

                using (var connection = new SqliteConnection(_connectionString))
                {
                    connection.Open();

                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "INSERT INTO Messages (Message) VALUES (@message)";
                        command.Parameters.AddWithValue("@message", content);
                        command.ExecuteNonQuery();
                    }
                }

                return Ok("File uploaded and processed.");
            }
        }
    }
}
