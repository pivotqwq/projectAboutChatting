using MongoDB.Bson;
using MongoDB.Driver;

namespace ChatService.Services
{
    public class MessageRepository
    {
        private readonly IMongoCollection<BsonDocument> _messages;

        public MessageRepository(IMongoDatabase database)
        {
            _messages = database.GetCollection<BsonDocument>("messages");
            CreateIndexesAsync(database).GetAwaiter().GetResult();
        }

        private static async Task CreateIndexesAsync(IMongoDatabase db)
        {
            var coll = db.GetCollection<BsonDocument>("messages");
            var indexModels = new List<CreateIndexModel<BsonDocument>>
            {
                new CreateIndexModel<BsonDocument>(Builders<BsonDocument>.IndexKeys.Ascending("Type").Ascending("FromUserId").Ascending("ToUserId").Ascending("CreatedAt")),
                new CreateIndexModel<BsonDocument>(Builders<BsonDocument>.IndexKeys.Ascending("Type").Ascending("GroupId").Ascending("CreatedAt"))
            };
            await coll.Indexes.CreateManyAsync(indexModels);
        }

        public Task InsertAsync(BsonDocument doc)
        {
            return _messages.InsertOneAsync(doc);
        }

        public async Task<BsonDocument?> GetByIdAsync(string id)
        {
            return await _messages.Find(Builders<BsonDocument>.Filter.Eq("Id", id)).FirstOrDefaultAsync();
        }

        public async Task<bool> SoftDeleteAsync(string id, string deletedBy)
        {
            var filter = Builders<BsonDocument>.Filter.Eq("Id", id);
            var update = Builders<BsonDocument>.Update
                .Set("Deleted", true)
                .Set("DeletedAt", DateTime.UtcNow)
                .Set("DeletedBy", deletedBy)
                .Set("Content", "");

            var result = await _messages.UpdateOneAsync(filter, update);
            return result.ModifiedCount > 0;
        }

        public async Task<IReadOnlyList<BsonDocument>> GetPrivateHistoryAsync(string userA, string userB, DateTime? beforeUtc, int pageSize)
        {
            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq("Type", "private") &
                         ((builder.Eq("FromUserId", userA) & builder.Eq("ToUserId", userB)) |
                          (builder.Eq("FromUserId", userB) & builder.Eq("ToUserId", userA)));
            if (beforeUtc.HasValue)
            {
                filter &= builder.Lt("CreatedAt", beforeUtc.Value);
            }
            var sort = Builders<BsonDocument>.Sort.Descending("CreatedAt");
            var docs = await _messages.Find(filter).Sort(sort).Limit(pageSize).ToListAsync();
            docs.Reverse(); // return ascending
            return docs;
        }

        public async Task<IReadOnlyList<BsonDocument>> GetGroupHistoryAsync(string groupId, DateTime? beforeUtc, int pageSize)
        {
            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq("Type", "group") & builder.Eq("GroupId", groupId);
            if (beforeUtc.HasValue)
            {
                filter &= builder.Lt("CreatedAt", beforeUtc.Value);
            }
            var sort = Builders<BsonDocument>.Sort.Descending("CreatedAt");
            var docs = await _messages.Find(filter).Sort(sort).Limit(pageSize).ToListAsync();
            docs.Reverse();
            return docs;
        }

        public async Task<IReadOnlyList<BsonDocument>> GetChannelHistoryAsync(string channelId, DateTime? beforeUtc, int pageSize)
        {
            var builder = Builders<BsonDocument>.Filter;
            // 频道消息使用 GroupId 字段存储 channelId（与群聊复用字段）
            var filter = builder.Eq("Type", "channel") & builder.Eq("GroupId", channelId);
            if (beforeUtc.HasValue)
            {
                filter &= builder.Lt("CreatedAt", beforeUtc.Value);
            }
            var sort = Builders<BsonDocument>.Sort.Descending("CreatedAt");
            var docs = await _messages.Find(filter).Sort(sort).Limit(pageSize).ToListAsync();
            docs.Reverse();
            return docs;
        }
    }
}


