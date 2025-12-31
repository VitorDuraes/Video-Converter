// src/GatewayService/Services/StorageService.cs
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using MongoDB.Bson;

namespace GatewayService.Services
{
    public class StorageService
    {
        private readonly IGridFSBucket _bucket; // Usar apenas um bucket

        public StorageService(IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("MongoDbConnection");
            var client = new MongoClient(connectionString);

            // USAR UM ÚNICO BANCO DE DADOS
            var database = client.GetDatabase("video_converter_db");

            _bucket = new GridFSBucket(database, new GridFSBucketOptions
            {
                BucketName = "files" // Usar um único nome de bucket
            });
        }

        public async Task<string> UploadVideoAsync(Stream stream, string fileName)
        {
            // Adicionar metadata para identificar o tipo de arquivo
            var options = new GridFSUploadOptions
            {
                Metadata = new BsonDocument("contentType", "video/mp4")
            };
            var id = await _bucket.UploadFromStreamAsync(fileName, stream, options);
            return id.ToString();
        }

        // Este método será usado para verificar se o MP3 está pronto
        public async Task<(Stream Stream, string FileName, string ContentType)> DownloadFileAsync(string id)
        {
            try
            {
                var objectId = ObjectId.Parse(id);
                var fileInfo = await _bucket.Find(Builders<GridFSFileInfo>.Filter.Eq("_id", objectId)).FirstOrDefaultAsync();

                if (fileInfo == null)
                {
                    return (null, null, null);
                }

                var stream = await _bucket.OpenDownloadStreamAsync(objectId);
                var contentType = fileInfo.Metadata.GetValue("contentType", "application/octet-stream").AsString;

                return (stream, fileInfo.Filename, contentType);
            }
            catch (Exception)
            {
                return (null, null, null);
            }
        }
    }
}
