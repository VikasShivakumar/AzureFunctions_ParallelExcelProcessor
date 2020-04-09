using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using System;

namespace ParallelExcelProcessor
{
    public class AzureStorageConnectionFactory
    {
        public CloudStorageAccount GetCloudStorageAccount(IConfiguration configuration)
        {
            if (!CloudStorageAccount.TryParse(configuration["ConnectionStrings:StorageConnectionString"], out CloudStorageAccount storageAccount))
            {
                throw new Exception(@"Can't create a storage account handler");
            }
            return storageAccount;
        }
    }
}
