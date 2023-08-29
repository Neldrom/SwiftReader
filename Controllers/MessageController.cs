using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Collections.Specialized;
using System.Text.RegularExpressions;

public class SwiftMessage
{
    public string NetworkDeliveryStatus { get; set; }
    public string YourBIC { get; set; }
    public string SessionNumber { get; set; }
    public string SequenceNumber { get; set; }
    public string IOMode { get; set; }
    public string MessageType { get; set; }
    public string RecipientBIC { get; set; }
    public string MessagePriority { get; set; }
    public string TransactionReferenceNumber { get; set; }
    public string ReletedReference { get; set; }
    public string Narrative { get; set; }
    public string Mac { get; set; }
    public string Chk { get; set; }
}
namespace SwiftReader.Controllers
{


    [Route("api/messages")]
    [ApiController]
    public class MessageController : ControllerBase
    {
        public static SwiftMessage ParseSwiftMessage(string swiftMessage)
        {
            SwiftMessage message = new SwiftMessage();

            // Extract fields using regex
            Regex regex = new Regex(@"{(\d+):|([^{}]+)}|({MAC:[0-9A-F]+})({CHK:[0-9A-F]+})}");
            MatchCollection matches = regex.Matches(swiftMessage);
            for (int i = 0; i < matches.Count - 1; i++)
            {
                if (matches[0].Groups[0].Success)
                {
                    string fieldNumber = matches[i].Groups[1].Value;
                    string fieldValue = matches[i + 1].Groups[2].Value;

                    switch (fieldNumber)
                    {
                        case "1":
                            if (fieldValue[1] == '0')
                                message.NetworkDeliveryStatus = "FIN";
                            else if (fieldValue[1] == '2')
                                message.NetworkDeliveryStatus = "ACK";
                            message.YourBIC = fieldValue.Substring(3, 8) + fieldValue.Substring(12, 3);
                            message.SessionNumber = fieldValue.Substring(15, 4);
                            message.SequenceNumber = fieldValue.Substring(19, 6);
                            break;
                        case "2":
                            if (fieldValue[0] == 'I')
                                message.IOMode = "Input Mode";
                            else if (fieldValue[0] == 'O')
                                message.IOMode = "Output Mode";
                            message.MessageType = fieldValue.Substring(1, 3);
                            message.RecipientBIC = fieldValue.Substring(4, 8) + fieldValue.Substring(13, 3);
                            switch (fieldValue[fieldValue.Length - 1])
                            {
                                case 'U':
                                    message.MessagePriority = "Urgent";
                                    break;
                                case 'N':
                                    message.MessagePriority = "Normal";
                                    break;
                                case 'S':
                                    message.MessagePriority = "System";
                                    break;
                            }
                            break;
                        case "4":
                            string pattern = @":(20|21|79):(.*?)(?=\n:|\z)";
                            MatchCollection field4Matches = Regex.Matches(fieldValue, pattern, RegexOptions.Singleline | RegexOptions.Multiline);
                            foreach (Match match1 in field4Matches)
                            {
                                string identifier = match1.Groups[1].Value;
                                string value = match1.Groups[2].Value.Trim();
                            }
                            message.TransactionReferenceNumber = field4Matches[0].Groups[2].Value;
                            message.ReletedReference = field4Matches[1].Groups[2].Value;
                            message.Narrative = field4Matches[2].Groups[2].Value;
                            break;
                        case "5":
                            message.Mac = matches[i + 1].Groups[3].Value.Trim('{', '}');
                            message.Chk = matches[i + 1].Groups[4].Value.Trim('{', '}');
                            break;
                    }
                }
            }

            return message;
        }

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
                SwiftMessage message = ParseSwiftMessage(content);
                using (var connection = new SqliteConnection(_connectionString))
                {
                    connection.Open();

                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "INSERT INTO Messages (NetworkDeliveryStatus, YourBIC, SessionNumber, SequenceNumber, IOMode," +
                            " MessageType, RecipientBIC, MessagePriority, TransactionReferenceNumber, ReletedReference, Narrative, Mac, Chk) VALUES (@NetworkDeliveryStatus," +
                            " @YourBIC, @SessionNumber, @SequenceNumber, @IOMode, @MessageType, @RecipientBIC, @MessagePriority, " +
                            "@TransactionReferenceNumber, @ReletedReference, @Narrative, @Mac, @Chk)";
                        command.Parameters.AddWithValue("@NetworkDeliveryStatus", message.NetworkDeliveryStatus);
                        command.Parameters.AddWithValue("@YourBIC", message.YourBIC);
                        command.Parameters.AddWithValue("@SessionNumber", message.SessionNumber);
                        command.Parameters.AddWithValue("@SequenceNumber", message.SequenceNumber);
                        command.Parameters.AddWithValue("@IOMode", message.IOMode);
                        command.Parameters.AddWithValue("@MessageType", message.MessageType);
                        command.Parameters.AddWithValue("@RecipientBIC", message.RecipientBIC);
                        command.Parameters.AddWithValue("@MessagePriority", message.MessagePriority);
                        command.Parameters.AddWithValue("@TransactionReferenceNumber", message.TransactionReferenceNumber);
                        command.Parameters.AddWithValue("@ReletedReference", message.ReletedReference);
                        command.Parameters.AddWithValue("@Narrative", message.Narrative);
                        command.Parameters.AddWithValue("@Mac", message.Mac);
                        command.Parameters.AddWithValue("@Chk", message.Chk);
                        command.ExecuteNonQuery();
                    }
                }
                return Ok("File uploaded and processed.");
            }
        }
    }
}
