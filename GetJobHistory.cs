using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Linq;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage;
using System.Text;
using System.Net.Http;
using System.Net;
using System.Collections.Generic;
using System.ComponentModel;

namespace re_platform_fapp_adls_getjobcardhistory
{
    public class MyPoco : TableEntity
    {
        public DateTime ActualDeliveryDateandTime { get; set; }

        public double InvoiceAmount { get; set; }

        public string InvoiceNumber { get; set; }

        public string RegistrationNumber { get; set; }

        public string StoreCode { get; set; }

        public string UserId { get; set; }

        public string UserMobileNumber { get; set; }


    }
    public class HistoryJobLogs : TableEntity
    {
        public HistoryJobLogs() { }
        public string Status { get; set; }

        public string Message { get; set; }
    }

    public static class GetJobHistory
    {
        [FunctionName("getjobcardhistory")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            var authentication = req.Headers["Authentication"].ToString();
            if (authentication == "jgkbih4VXqJO1nCN8l4X4m1UqIOWcQj15zbOx3fxYhk=")
            {

                string chassisNumber = req.Query["vehiclechassis"];
                if (chassisNumber != string.Empty && chassisNumber != null)
                {
                    var jsonResult = string.Empty;
                    List<MyPoco> listOfObjects = new List<MyPoco>();
                    CloudStorageAccount storageAccount = CloudStorageAccount.Parse("DefaultEndpointsProtocol=https;AccountName=redatalake;AccountKey=AnWrOm+WVdGEP3POg76d5eIoNaW676NCSiBsDdcmYw3R7Bz5+WkGNL63VQx5Zg4acw2qf4aEkzOfysYdkaFtxg==;EndpointSuffix=core.windows.net");
                    CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
                    CloudTable table = tableClient.GetTableReference("HistoricalJobCard");
                    CloudTable logTable = tableClient.GetTableReference("JobCardLogs");
                    var pooo = string.Empty;
                    try
                    {
                        string val1 = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, chassisNumber);
                        string val2 = TableQuery.GenerateFilterCondition("InvoiceAmount", QueryComparisons.NotEqual, string.Empty);

                         TableQuery<MyPoco> rangeQuery = new TableQuery<MyPoco>().Where(TableQuery.CombineFilters(
                              val1,
                              TableOperators.And,
                              val2));
                         
                        foreach (MyPoco entity in
                            await table.ExecuteQuerySegmentedAsync(rangeQuery, null))
                        {
                            if (entity.InvoiceAmount.ToString() != null || entity.InvoiceAmount.ToString() != string.Empty)
                            {
                                listOfObjects.Add(entity);

                            }
                        }
                        jsonResult = JsonConvert.SerializeObject(listOfObjects);
                        //jsonResult = jsonResult.Replace("ActualDeliveryDateandTime", "InvoiceDate").Replace("InvoiceAmount", "BillAmount").Replace("InvoiceNumber", "ServiceInvoiceNum").Replace("StoreCode", "BrnchId").Replace("RegistrationNumber", "RegNo").Replace("PartitionKey", "ChassisNo");
                    }
                    catch (Exception e)
                    {
                        InsertJobLog(chassisNumber, e.Message, logTable);
                        return new HttpResponseMessage(HttpStatusCode.BadGateway)
                        {
                            Content = new StringContent(e.Message)
                        };
                    }
                    if (jsonResult != string.Empty)
                    {
                        InsertJobLog(chassisNumber, "Success", logTable);
                    }
                    else
                    {
                        InsertJobLog(chassisNumber, "No Data", logTable);
                    }
                    
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(jsonResult, Encoding.UTF8, "application/json")
                    };
                }
                else
                {
                    
                     return new HttpResponseMessage(HttpStatusCode.BadRequest)
                    {
                        Content = new StringContent("Invalid Chassis Number")
                    };
                }
            }
            else
            {
                return new HttpResponseMessage(HttpStatusCode.ProxyAuthenticationRequired)
                {
                    Content = new StringContent("Provide Authentication Key in Header")
                };
            }
        }

        private static void InsertJobLog(string chassisno, string message, CloudTable table)
        {
            var newEntity = new HistoryJobLogs
            {
                PartitionKey = chassisno,
                RowKey = Guid.NewGuid().ToString(),
                Status = message
            };

            TableOperation insert = TableOperation.Insert(newEntity);
            table.ExecuteAsync(insert);

        }
    }




}
