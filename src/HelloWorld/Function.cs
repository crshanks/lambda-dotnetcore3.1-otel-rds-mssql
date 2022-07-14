using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text.Json;

using OpenTelemetry;
using OpenTelemetry.Contrib.Instrumentation.AWSLambda.Implementation;
using OpenTelemetry.Trace;

using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace HelloWorld
{

    public class Function
    {
        public static TracerProvider tracerProvider;

        // private static readonly ActivitySource MyActivitySource = new ActivitySource("MyCompany.MyProduct.MyLibrary");
        
        static Function()
        {
            // This switch must be set before creating the GrpcChannel/HttpClient.
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

            tracerProvider = Sdk.CreateTracerProviderBuilder()
                .AddAWSInstrumentation()
                .AddSqlClientInstrumentation(
                    options => options.SetDbStatementForText = true)
                .AddOtlpExporter()
                .AddAWSLambdaConfigurations()
                .Build();
        }

        // use AwsSdkSample::AwsSdkSample.Function::TracingFunctionHandler as input Lambda handler instead
        public APIGatewayProxyResponse TracingFunctionHandler(APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
        {
            return AWSLambdaWrapper.Trace(tracerProvider, FunctionHandler, apigProxyEvent, context);
        }

        public APIGatewayProxyResponse FunctionHandler(APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
        {
            string brands = GetBrands();

            var body = new Dictionary<string, string>
            {
                { "message", "hello world" },
                { "brands", brands },
            };

            return new APIGatewayProxyResponse
            {
                Body = JsonSerializer.Serialize(body),
                StatusCode = 200,
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
            };
        }

        public string GetBrands()
        {
            string brands = "Brands: ";
            try {
                // DB Connection Variables
                string server = Environment.GetEnvironmentVariable("MSSQL_DB_ENDPOINT");
                string database = Environment.GetEnvironmentVariable("MSSQL_DATABASE");
                string username = Environment.GetEnvironmentVariable("MSSQL_USER");
                string pwd = Environment.GetEnvironmentVariable("MSSQL_PASSWORD");
                string ConnectionString = String.Format("Data Source={0};Initial Catalog={1};User ID={2};Password={3}",server,database,username,pwd);

                List<string> myDbItems = new List<string>();
                Console.WriteLine("pre connection");
                using (var Conn = new SqlConnection(ConnectionString))
                {
                    Console.WriteLine("post connection");
                    using (var Cmd = new SqlCommand($"select brand_name from BikeStores.production.brands", Conn))
                    {
                        Console.WriteLine("post SqlCommand()");
                        // Open SQL Connection
                        Conn.Open();
                        Console.WriteLine("post Conn.Open()");
                        // Execute SQL Command
                        SqlDataReader rdr = Cmd.ExecuteReader();
                        Console.WriteLine("post Cmd.ExecuteReader()");
                        // Loop through the results and add to list
                        while (rdr.Read())
                        {
                            myDbItems.Add(rdr[0].ToString());
                        }
                        Console.WriteLine("post Read()s");
                        // Generate Output
                        for (int i = 0; i < myDbItems.Count; i++) {
                            brands += $" {myDbItems[i]}";
                            // Add a comma if it's not the last item
                            if (i != myDbItems.Count - 1) {
                                brands += ",";
                            }
                        }
                    }
                }
                Console.WriteLine(brands);
            } catch (Exception e) {
                Console.WriteLine(e.Message);
            } finally {
                Console.WriteLine("Executed");
            }
            // return brands;
            return brands;
        }

    }
}
