# Parallel Excel Processor

Durable Functions is an extension that helps developers build reliable, serverless parallel processors using Fan out pattern 

This project downloads the excel file from azure storage and reads it using ExcelDataReader and writes to MSSQL in parallel tasks for each sheet

# NuGet Packages

Package Name | NuGet
---|---
ExcelDataReader | (https://www.nuget.org/packages/ExcelDataReader/)
Microsoft.Azure.WebJobs.Extensions.DurableTask | (https://www.nuget.org/packages/Microsoft.Azure.WebJobs.Extensions.DurableTask)
ExcelDataReader.DataSet | (https://www.nuget.org/packages/ExcelDataReader.DataSet/)