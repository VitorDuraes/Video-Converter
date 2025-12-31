using MongoDB.Driver;
using MongoDB.Driver.GridFS;

namespace ConverterService.Services
{
    public class StorageService
    {
        private readonly IGridFSBucket _gridFs;
        private readonly IMongoDatabase _database;


        public StorageService(IConfiguration configuration)
        {
            var client = new MongoClient(configuration.GetConnectionString("MongoDbConnection"));
            _database = client.GetDatabase("video_converter_db");
            _gridFs = new GridFSBucket(_database, new GridFSBucketOptions
            {
                BucketName = "files"
            });
        }

        public async Task<Stream> DownloadVideoAsync(string fileId)
        {
            try
            {
                var objectId = new MongoDB.Bson.ObjectId(fileId);
                return await _gridFs.OpenDownloadStreamAsync(objectId);

            }
            catch (Exception ex)
            {
                return null;
            }
        }
        public async Task<string> UploadMp3Async(string originalFileId, Stream stream, string userEmail)
        {
            var options = new GridFSUploadOptions
            {
                Metadata = new MongoDB.Bson.BsonDocument
                {
                    { "userEmail", userEmail },
                    { "originalFileId", originalFileId },
                    { "contentType", "audio/mpeg" }

                }
            };
            var newFileId = await _gridFs.UploadFromStreamAsync(
                $"{originalFileId}.mp3",
                stream,
                options
            );
            return newFileId.ToString();
        }
    }
}